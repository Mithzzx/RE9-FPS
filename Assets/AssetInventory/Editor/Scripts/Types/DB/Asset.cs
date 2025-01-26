using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SQLite;
using UnityEditor.PackageManager;
using UnityEngine;

namespace AssetInventory
{
    [Serializable]
    public sealed class Asset
    {
        public const string NONE = "-no attached package-";
        public const string UNITY_REGISTRY = "Unity";
        public const char SUB_PATH = '|';

        public enum State
        {
            New = 0,
            InProcess = 1,
            Done = 2,
            Unknown = 3,
            SubInProcess = 4
        }

        public enum SubState
        {
            None = 0,
            Outdated = 1
        }

        public enum Source
        {
            AssetStorePackage = 0,
            CustomPackage = 1,
            Directory = 2,
            RegistryPackage = 3,
            Archive = 4
        }

        public enum Strategy
        {
            Recommended = 3,
            RecommendedOrLatestStableCompatible = 4,
            LatestStableCompatible = 0,
            LatestCompatible = 1,
            Manually = 99
        }

        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [Indexed] public int ParentId { get; set; }
        public Source AssetSource { get; set; }
        public string Location { get; set; }
        public string OriginalLocation { get; set; }
        public string OriginalLocationKey { get; set; }
        public string Registry { get; set; }
        public string Repository { get; set; }
        public PackageSource PackageSource { get; set; }
        [Indexed] public int ForeignId { get; set; }
        public long PackageSize { get; set; }
        [Indexed] public string SafeName { get; set; }
        public string DisplayName { get; set; }
        [Indexed] public string SafePublisher { get; set; }
        public string DisplayPublisher { get; set; }
        [Indexed] public string SafeCategory { get; set; }
        public string DisplayCategory { get; set; }
        public int PublisherId { get; set; }
        public string Slug { get; set; }
        public int Revision { get; set; }
        public string Description { get; set; }
        public string KeyFeatures { get; set; }
        public string CompatibilityInfo { get; set; }
        public string SupportedUnityVersions { get; set; }
        public string Keywords { get; set; }
        public string Version { get; set; }
        public string LatestVersion { get; set; }
        public Strategy UpdateStrategy { get; set; }
        public string License { get; set; } // SPDX identifier format: https://spdx.org/licenses/
        public string LicenseLocation { get; set; }
        public DateTime PurchaseDate { get; set; }
        public DateTime FirstRelease { get; set; }
        public DateTime LastRelease { get; set; }
        public string AssetRating { get; set; }
        public int RatingCount { get; set; }
        public float Hotness { get; set; }
        public float PriceEur { get; set; }
        public float PriceUsd { get; set; }
        public float PriceCny { get; set; }
        public string Requirements { get; set; }
        public string ReleaseNotes { get; set; }
        public string OfficialState { get; set; }
        public bool IsHidden { get; set; }
        public bool Exclude { get; set; }
        public bool Backup { get; set; }
        public bool KeepExtracted { get; set; }
        public string UploadId { get; set; }
        public string ETag { get; set; }
        public DateTime LastOnlineRefresh { get; set; }
        public State CurrentState { get; set; }
        public SubState CurrentSubState { get; set; }

        // runtime
        [Ignore] public Asset ParentAsset { get; set; }
        [Ignore] public Texture2D PreviewTexture { get; set; }

        public Asset()
        {
        }

        public Asset(Package package)
        {
            CopyFrom(package);
        }

        public Asset(PackageInfo package)
        {
            CopyFrom(package);
        }

        // keep in sync with copy in AssetInfo
        public string GetCalculatedLocation()
        {
            if (string.IsNullOrEmpty(SafePublisher) || string.IsNullOrEmpty(SafeCategory) || string.IsNullOrEmpty(SafeName)) return null;

            return Path.Combine(AssetInventory.GetAssetCacheFolder(), SafePublisher, SafeCategory, SafeName + ".unitypackage").Replace("\\", "/");
        }

        // keep in sync with copy in AssetInfo
        public string GetLocation(bool expanded)
        {
            return expanded ? AssetInventory.DeRel(Location) : Location;
        }

        // keep in sync with copy in AssetInfo
        public async Task<string> GetLocation(bool expanded, bool resolveParent)
        {
            string archivePath = Location;
            if (resolveParent && ParentId > 0)
            {
                if (ParentAsset == null) ParentAsset = DBAdapter.DB.Find<Asset>(ParentId);
                if (ParentAsset == null)
                {
                    Debug.LogError($"Could not resolve parent asset of '{DisplayName}'.");
                }
                else
                {
                    if (!AssetInventory.IsMaterialized(ParentAsset)) await AssetInventory.ExtractAsset(ParentAsset);

                    string[] arr = Location.Split(SUB_PATH);
                    AssetFile parentAssetFile = DBAdapter.DB.Query<AssetFile>("select * from AssetFile where AssetId=? and Path=?", ParentId, arr.Last()).FirstOrDefault();
                    if (parentAssetFile == null)
                    {
                        Debug.LogError($"Could not resolve package in parent index {arr.Last()}.");
                        return null;
                    }
                    archivePath = await AssetInventory.EnsureMaterializedAsset(ParentAsset, parentAssetFile);
                }
            }
            return expanded ? AssetInventory.DeRel(archivePath) : archivePath;
        }

        // keep in sync with AssetInfo
        public void SetLocation(string location)
        {
            if (location == null)
            {
                Location = null;
                return;
            }
            Location = AssetInventory.MakeRelative(location);
        }

        public Asset CopyFrom(PackageInfo package)
        {
            AssetSource = Source.RegistryPackage;
            DisplayName = package.displayName;
            Description = package.description;
            SafeCategory = package.type;
            if (!string.IsNullOrEmpty(package.type))
            {
                TextInfo ti = new CultureInfo("en-US", false).TextInfo;
                DisplayCategory = ti.ToTitleCase(package.type);
            }
            SafeName = package.name;
            SafePublisher = package.author?.name;
#if UNITY_2020_1_OR_NEWER
            ReleaseNotes = package.changelogUrl;
            LicenseLocation = package.licensesUrl;
#endif
            if (!string.IsNullOrEmpty(package.version))
            {
                // only set to higher versions, as otherwise there might be import ping-pong situations
                if (new SemVer(package.version) > new SemVer(Version))
                {
                    Version = package.version;
                }
            }
            if (package.keywords != null) Keywords = string.Join(", ", package.keywords);
            PackageSource = package.source;
            LastRelease = package.datePublished ?? DateTime.MinValue;

            if (string.IsNullOrWhiteSpace(SafePublisher) && package.registry != null && package.registry.isDefault) SafePublisher = "Unity";

            // registry
            if (package.registry != null)
            {
                if (package.registry.isDefault)
                {
                    Registry = UNITY_REGISTRY;
                }
                else
                {
                    ScopedRegistry scopedReg = new ScopedRegistry(package.registry);
                    Registry = JsonConvert.SerializeObject(scopedReg);
                }
            }

#if UNITY_2020_1_OR_NEWER
            // repository
            if (package.repository != null)
            {
                Repository repo = new Repository(package.repository);
                if (string.IsNullOrEmpty(repo.revision) && !string.IsNullOrWhiteSpace(package.git?.revision)) repo.revision = package.git.revision;
                Repository = JsonConvert.SerializeObject(repo);
            }
#endif
            // additional source specific settings
            switch (package.source)
            {
                case PackageSource.Git:
                case PackageSource.Embedded:
                case PackageSource.LocalTarball:
                    Location = AssetInventory.MakeRelative(package.resolvedPath);
                    break;
            }

            return this;
        }

        public Asset CopyFrom(Package package)
        {
            AssetSource = Source.RegistryPackage;
            DisplayName = package.displayName;
            Description = package.description;
            SafeCategory = package.type;
            if (!string.IsNullOrEmpty(package.type))
            {
                TextInfo ti = new CultureInfo("en-US", false).TextInfo;
                DisplayCategory = ti.ToTitleCase(package.type);
            }
            SafeName = package.name;
            SafePublisher = package.author?.name;
            ReleaseNotes = package.changelogUrl;
            License = package.license;
            LicenseLocation = package.licensesUrl;
            SupportedUnityVersions = package.unity;
            if (!string.IsNullOrEmpty(package.version))
            {
                // only set to higher versions, as otherwise there might be import ping-pong situations
                if (new SemVer(package.version) > new SemVer(Version))
                {
                    Version = package.version;
                }
            }
            if (package.keywords != null) Keywords = string.Join(", ", package.keywords);
            IsHidden = package.hideInEditor;

            return this;
        }

        public void CopyFrom(Asset package)
        {
            AssetSource = package.AssetSource;
            Location = package.Location;
            OriginalLocation = package.OriginalLocation;
            OriginalLocationKey = package.OriginalLocationKey;
            Registry = package.Registry;
            Repository = package.Repository;
            ForeignId = package.ForeignId;
            PackageSize = package.PackageSize;
            PackageSource = package.PackageSource;
            SafeName = package.SafeName;
            DisplayName = package.DisplayName;
            SafePublisher = package.SafePublisher;
            DisplayPublisher = package.DisplayPublisher;
            SafeCategory = package.SafeCategory;
            DisplayCategory = package.DisplayCategory;
            PublisherId = package.PublisherId;
            Slug = package.Slug;
            Revision = package.Revision;
            Description = package.Description;
            KeyFeatures = package.KeyFeatures;
            CompatibilityInfo = package.CompatibilityInfo;
            SupportedUnityVersions = package.SupportedUnityVersions;
            Keywords = package.Keywords;
            Version = package.Version;
            LatestVersion = package.LatestVersion;
            UpdateStrategy = package.UpdateStrategy;
            License = package.License;
            LicenseLocation = package.LicenseLocation;
            PurchaseDate = package.PurchaseDate;
            FirstRelease = package.FirstRelease;
            LastRelease = package.LastRelease;
            AssetRating = package.AssetRating;
            RatingCount = package.RatingCount;
            Hotness = package.Hotness;
            PriceEur = package.PriceEur;
            PriceUsd = package.PriceUsd;
            PriceCny = package.PriceCny;
            Requirements = package.Requirements;
            ReleaseNotes = package.ReleaseNotes;
            OfficialState = package.OfficialState;
            IsHidden = package.IsHidden;
            Exclude = package.Exclude;
            Backup = package.Backup;
            KeepExtracted = package.KeepExtracted;
            UploadId = package.UploadId;
            ETag = package.ETag;
            LastOnlineRefresh = package.LastOnlineRefresh;
        }

        public string GetPreviewFile(string previewFolder, bool validate = true)
        {
            string file = Path.Combine(previewFolder, Id.ToString(), $"a-{Id}.png");
            if (validate && !File.Exists(file)) file = null;

            return file;
        }

        public string GetMediaFile(AssetMedia media, string previewFolder, bool validate = true)
        {
            string file = Path.Combine(previewFolder, Id.ToString(), $"m-{media.Id}{Path.GetExtension(media.Url)}");
            if (validate && !File.Exists(file)) file = null;

            return file;
        }

        public string GetMediaThumbnailFile(AssetMedia media, string previewFolder, bool validate = true)
        {
            string file = Path.Combine(previewFolder, Id.ToString(), $"mt-{media.Id}{Path.GetExtension(media.ThumbnailUrl)}");
            if (validate && !File.Exists(file)) file = null;

            return file;
        }

        public override string ToString()
        {
            string name = string.IsNullOrEmpty(DisplayName) ? SafeName : DisplayName;
            return $"Package '{name}' ({Location})";
        }

        public static Asset GetNoAsset()
        {
            Asset noAsset = new Asset();
            noAsset.SafeName = NONE;
            noAsset.DisplayName = NONE;
            noAsset.AssetSource = Source.Directory;

            return noAsset;
        }

        public string GetSafeVersion()
        {
            return new SemVer(Version).CleanedVersion.Replace("/", "").Replace("\\", "");
        }
    }
}