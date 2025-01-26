using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace AssetInventory
{
    [Serializable]
    // used to contain results of join calls
    public sealed class AssetInfo : AssetFile
    {
        public enum ImportStateOptions
        {
            Unknown = 0,
            Queued = 1,
            Missing = 2,
            Importing = 3,
            Imported = 4,
            Failed = 5,
            Cancelled = 6
        }

        public enum DependencyStateOptions
        {
            Unknown = 0,
            Calculating = 1,
            Done = 2,
            NotPossible = 3,
            Failed = 4
        }

        public int ParentId { get; set; }
        public Asset.Source AssetSource { get; set; }
        public string Location { get; set; }
        public string OriginalLocation { get; set; }
        public string OriginalLocationKey { get; set; }
        public string Registry { get; set; }
        public string Repository { get; set; }
        public PackageSource PackageSource { get; set; }
        public int ForeignId { get; set; }
        public long PackageSize { get; set; }
        public string SafeName { get; set; }
        public string DisplayName { get; set; }
        public string SafePublisher { get; set; }
        public string DisplayPublisher { get; set; }
        public string SafeCategory { get; set; }
        public string DisplayCategory { get; set; }
        public int PublisherId { get; set; }
        public Asset.State CurrentState { get; set; }
        public Asset.SubState CurrentSubState { get; set; }
        public string Slug { get; set; }
        public int Revision { get; set; }
        public string Description { get; set; }
        public string KeyFeatures { get; set; }
        public string CompatibilityInfo { get; set; }
        public string SupportedUnityVersions { get; set; }
        public string Keywords { get; set; }
        public string Version { get; set; }
        public string LatestVersion { get; set; }
        public Asset.Strategy UpdateStrategy { get; set; }
        public string License { get; set; }
        public string LicenseLocation { get; set; }
        public DateTime LastRelease { get; set; }
        public DateTime PurchaseDate { get; set; }
        public DateTime FirstRelease { get; set; }
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
        public int FileCount { get; set; }
        public long UncompressedSize { get; set; }

        // runtime only
        [field: NonSerialized] public AssetInfo ParentInfo { get; set; }
        [field: NonSerialized] public List<AssetInfo> ChildInfos { get; set; } = new List<AssetInfo>();
        public AssetDownloader PackageDownloader;
        public Texture2D PreviewTexture { get; set; }
        public bool IsIndexed => FileCount > 0 && (CurrentState == Asset.State.Done || CurrentState == Asset.State.New); // new is set when deleting local package file
        public bool IsDeprecated => OfficialState == "deprecated";
        public bool IsAbandoned => OfficialState == "disabled";
        public bool IsMaterialized { get; set; }
        public ImportStateOptions ImportState { get; set; }
        public DependencyStateOptions DependencyState { get; set; } = DependencyStateOptions.Unknown;
        public List<AssetFile> Dependencies { get; set; }
        public List<AssetFile> MediaDependencies { get; set; }
        public List<AssetFile> ScriptDependencies { get; set; }
        public long DependencySize { get; set; }
        public bool WasOutdated { get; set; }
        public List<AssetMedia> AllMedia { get; set; }
        public List<AssetMedia> Media { get; set; }

        private bool _tagsDone;
        private List<TagInfo> _packageTags;
        private List<TagInfo> _assetTags;
        private int _tagHash;
        private string _packageSamplesLoaded;
        private IEnumerable<UnityEditor.PackageManager.UI.Sample> _packageSamples;

        public List<TagInfo> PackageTags
        {
            get
            {
                EnsureTagsLoaded();
                return _packageTags;
            }
        }

        public void SetTagsDirty() => _tagsDone = false;

        public List<TagInfo> AssetTags
        {
            get
            {
                EnsureTagsLoaded();
                return _assetTags;
            }
        }

        public bool Downloaded
        {
            get
            {
                if (ParentInfo != null) return ParentInfo.Downloaded;
                if (_downloaded != null) return _downloaded.Value;

                // special asset types
                if (AssetSource == Asset.Source.RegistryPackage
                    || (AssetSource == Asset.Source.Archive && File.Exists(GetLocation(true)))
                    || (AssetSource == Asset.Source.Directory && Directory.Exists(GetLocation(true))))
                {
                    _downloaded = true;
                    return _downloaded.Value;
                }

                // "none" is a special case, it's a placeholder for assets that are not attached
                if (SafeName == Asset.NONE)
                {
                    _downloaded = true;
                    return _downloaded.Value;
                }

                // check for missing location
                string location = GetLocation(true);
                if (string.IsNullOrEmpty(location))
                {
                    _downloaded = false;
                    return _downloaded.Value;
                }

                // check for missing file
                if (!File.Exists(location))
                {
                    _downloaded = false;
                    return _downloaded.Value;
                }

                // due to Unity bug verify downloaded asset is indeed asset in question, could be multi-versioned asset
                if (AssetSource == Asset.Source.AssetStorePackage || AssetInventory.Config.showCustomPackageUpdates)
                {
                    AssetHeader header = UnityPackageImporter.ReadHeader(location, true);
                    if (header != null && int.TryParse(header.id, out int id))
                    {
                        if (id != ForeignId)
                        {
                            _downloaded = false;
                            _downloadedActual = header.version;
                        }
                    }
                }
                _downloaded = _downloaded == null;
                return _downloaded.Value;
            }
        }
        private bool? _downloaded;
        private bool? _updateAvailable;
        private bool? _updateAvailableForced;
        private bool? _updateAvailableList;
        private bool? _updateAvailableListForced;

        public string DownloadedActual
        {
            get
            {
                if (ParentInfo != null) return ParentInfo.DownloadedActual;
                if (_downloaded == null)
                {
                    // ensure cache is filled
                    bool _ = Downloaded;
                }
                return _downloadedActual;
            }
        }
        private string _downloadedActual;
        private string _forcedTargetVersion;

        public AssetInfo()
        {
        }

        public AssetInfo(Asset asset)
        {
            AssetId = asset.Id;
            AssetSource = asset.AssetSource;
            Location = asset.Location;
            OriginalLocation = asset.OriginalLocation;
            OriginalLocationKey = asset.OriginalLocationKey;
            Registry = asset.Registry;
            Repository = asset.Repository;
            PackageSource = asset.PackageSource;
            ForeignId = asset.ForeignId;
            PackageSize = asset.PackageSize;
            SafeName = asset.SafeName;
            DisplayName = asset.DisplayName;
            SafePublisher = asset.SafePublisher;
            DisplayPublisher = asset.DisplayPublisher;
            SafeCategory = asset.SafeCategory;
            DisplayCategory = asset.DisplayCategory;
            PublisherId = asset.PublisherId;
            CurrentState = asset.CurrentState;
            CurrentSubState = asset.CurrentSubState;
            Slug = asset.Slug;
            Revision = asset.Revision;
            Description = asset.Description;
            KeyFeatures = asset.KeyFeatures;
            CompatibilityInfo = asset.CompatibilityInfo;
            SupportedUnityVersions = asset.SupportedUnityVersions;
            Keywords = asset.Keywords;
            Version = asset.Version;
            LatestVersion = asset.LatestVersion;
            UpdateStrategy = asset.UpdateStrategy;
            License = asset.License;
            LicenseLocation = asset.LicenseLocation;
            LastRelease = asset.LastRelease;
            PurchaseDate = asset.PurchaseDate;
            FirstRelease = asset.FirstRelease;
            AssetRating = asset.AssetRating;
            RatingCount = asset.RatingCount;
            Hotness = asset.Hotness;
            PriceEur = asset.PriceEur;
            PriceUsd = asset.PriceUsd;
            PriceCny = asset.PriceCny;
            Requirements = asset.Requirements;
            ReleaseNotes = asset.ReleaseNotes;
            OfficialState = asset.OfficialState;
            IsHidden = asset.IsHidden;
            Exclude = asset.Exclude;
            Backup = asset.Backup;
            KeepExtracted = asset.KeepExtracted;
            UploadId = asset.UploadId;
            ETag = asset.ETag;
            LastOnlineRefresh = asset.LastOnlineRefresh;
        }

        private void EnsureTagsLoaded()
        {
            if (!_tagsDone || AssetInventory.TagHash != _tagHash)
            {
                _assetTags = AssetInventory.GetAssetTags(Id);
                _packageTags = AssetInventory.GetPackageTags(AssetId);
                _tagsDone = true;
                _tagHash = AssetInventory.TagHash;
            }
        }

        public bool IsIndirectPackageDependency()
        {
            if (AssetSource != Asset.Source.RegistryPackage) return false;

            PackageInfo pInfo = AssetStore.GetInstalledPackage(this);
            return pInfo != null && !pInfo.isDirectDependency;
        }

        public bool HasSamples()
        {
            IEnumerable<UnityEditor.PackageManager.UI.Sample> packageSamples = GetSamples();
            return packageSamples != null && packageSamples.Any();
        }

        public IEnumerable<UnityEditor.PackageManager.UI.Sample> GetSamples()
        {
            if (AssetSource != Asset.Source.RegistryPackage) return null;

            string installedPackageVersion = InstalledPackageVersion();
            if (installedPackageVersion == null) return null;

            if (_packageSamplesLoaded != installedPackageVersion)
            {
                _packageSamplesLoaded = installedPackageVersion;
                _packageSamples = UnityEditor.PackageManager.UI.Sample.FindByPackage(SafeName, installedPackageVersion);
            }
            return _packageSamples;
        }

        public string InstalledPackageVersion()
        {
            if (AssetSource != Asset.Source.RegistryPackage) return null;

            PackageInfo pInfo = AssetStore.GetInstalledPackage(this);
            return pInfo?.version;
        }

        public string TargetPackageVersion()
        {
            if (AssetSource != Asset.Source.RegistryPackage) return null;
            if (!string.IsNullOrEmpty(_forcedTargetVersion)) return _forcedTargetVersion;

            PackageInfo pInfo = AssetStore.GetPackageInfo(this);
            if (pInfo == null) return null;

            switch (UpdateStrategy)
            {
                case Asset.Strategy.LatestStableCompatible:
                    return pInfo.versions.compatible.LastOrDefault(p => !p.ToLowerInvariant().Contains("pre") && !p.ToLowerInvariant().Contains("exp"));

                case Asset.Strategy.LatestCompatible:
                    return pInfo.versions.compatible.LastOrDefault();

                case Asset.Strategy.Recommended:
                    return string.IsNullOrWhiteSpace(GetVerifiedVersion(pInfo)) ? null : GetVerifiedVersion(pInfo);

                case Asset.Strategy.RecommendedOrLatestStableCompatible:
                    if (string.IsNullOrWhiteSpace(GetVerifiedVersion(pInfo)))
                    {
                        return pInfo.versions.compatible.LastOrDefault(p => !p.ToLowerInvariant().Contains("pre") && !p.ToLowerInvariant().Contains("exp"));
                    }
                    return GetVerifiedVersion(pInfo);

                case Asset.Strategy.Manually:
                    return null;
            }

            return null;
        }

        #if UNITY_2022_2_OR_NEWER
        private string GetVerifiedVersion(PackageInfo pInfo) => pInfo.versions.recommended;
        #else
        private string GetVerifiedVersion(PackageInfo pInfo) => pInfo.versions.verified;
        #endif

        public string GetDisplayName(bool extended = false)
        {
            string result = string.IsNullOrEmpty(DisplayName) ? SafeName : DisplayName;
            if (extended && AssetSource == Asset.Source.RegistryPackage && !string.IsNullOrWhiteSpace(InstalledPackageVersion())) result += " - " + InstalledPackageVersion();
            return result;
        }

        public string GetDisplayPublisher() => string.IsNullOrEmpty(DisplayPublisher) ? SafePublisher : DisplayPublisher;
        public string GetDisplayCategory() => string.IsNullOrEmpty(DisplayCategory) ? SafeCategory : DisplayCategory;

        public string GetChangeLog(string versionOverride = null)
        {
            if (string.IsNullOrWhiteSpace(ReleaseNotes) && Registry == Asset.UNITY_REGISTRY)
            {
                SemVer version = new SemVer(string.IsNullOrEmpty(versionOverride) ? Version : versionOverride);
                return $"https://docs.unity3d.com/Packages/{SafeName}@{version.Major}.{version.Minor}/changelog/CHANGELOG.html";
            }
            return ReleaseNotes;
        }

        public string GetChangeLogURL(string versionOverride = null)
        {
            string changeLog = GetChangeLog(versionOverride);

            return AssetUtils.IsUrl(changeLog) ? changeLog : null;
        }

        public string GetLocation(bool expanded)
        {
            return expanded ? AssetInventory.DeRel(Location) : Location;
        }

        public string GetVersion()
        {
            if (AssetSource == Asset.Source.RegistryPackage) return InstalledPackageVersion();

            if (ParentId > 0 && ParentInfo != null && (ForeignId == 0 || ForeignId == ParentInfo.ForeignId)) return ParentInfo.GetVersion();
            return Version;
        }

        // keep in sync with copy in Asset
        public async Task<string> GetLocation(bool expanded, bool resolveParent)
        {
            string archivePath = Location;
            if (resolveParent && ParentId > 0)
            {
                Asset parentAsset = ParentInfo?.ToAsset();
                if (parentAsset == null) parentAsset = DBAdapter.DB.Find<Asset>(ParentId);
                if (parentAsset == null)
                {
                    Debug.LogError($"Could not resolve parent asset of '{GetDisplayName()}'.");
                }
                else
                {
                    if (!AssetInventory.IsMaterialized(parentAsset)) await AssetInventory.ExtractAsset(parentAsset);

                    string[] arr = Location.Split(Asset.SUB_PATH);
                    AssetFile parentAssetFile = DBAdapter.DB.Query<AssetFile>("select * from AssetFile where AssetId=? and Path=?", ParentId, arr.Last()).FirstOrDefault();
                    if (parentAssetFile == null)
                    {
                        Debug.LogError($"Could not resolve package in parent index {arr.Last()}.");
                        return null;
                    }
                    archivePath = await AssetInventory.EnsureMaterializedAsset(parentAsset, parentAssetFile);
                }
            }
            return expanded ? AssetInventory.DeRel(archivePath) : archivePath;
        }

        public void SetLocation(string location)
        {
            if (location == null)
            {
                Location = null;
                return;
            }
            Location = AssetInventory.MakeRelative(location);
        }

        public bool IsLocationUnmappedRelative()
        {
            return AssetInventory.IsRel(Location) && AssetInventory.DeRel(Location, true) == null;
        }

        public bool IsUpdateAvailable(bool force = true)
        {
            // quick checks can remain uncached
            if (ParentId > 0) return false;
            if (WasOutdated) return false;

            if (force && _updateAvailableForced != null) return _updateAvailableForced.Value;
            if (!force && _updateAvailable != null) return _updateAvailable.Value;

            if (IsAbandoned || IsDeprecated)
            {
                _updateAvailable = false;
                _updateAvailableForced = false;
                return false;
            }

            // registry packages should only flag update if inside current project and compatible
            if (AssetSource == Asset.Source.RegistryPackage)
            {
                if (!AssetInventory.Config.showIndirectPackageUpdates)
                {
                    PackageInfo pInfo = AssetStore.GetInstalledPackage(this);
                    if (pInfo != null && !pInfo.isDirectDependency)
                    {
                        _updateAvailable = false;
                        _updateAvailableForced = false;
                        return false;
                    }
                }
                bool packageUpdateAvailable = InstalledPackageVersion() != null && TargetPackageVersion() != null && InstalledPackageVersion() != TargetPackageVersion();

                _updateAvailable = packageUpdateAvailable;
                _updateAvailableForced = packageUpdateAvailable;

                return packageUpdateAvailable;
            }

            // custom packages are typically treated as not updateable
            if (!force && AssetSource == Asset.Source.CustomPackage && !AssetInventory.Config.showCustomPackageUpdates)
            {
                _updateAvailable = false;
                return false;
            }

            // check for missing version information
            if (string.IsNullOrWhiteSpace(Version) || string.IsNullOrWhiteSpace(LatestVersion))
            {
                _updateAvailable = false;
                _updateAvailableForced = false;
                return false;
            }

            // compare versions
            bool updateAvailable = new SemVer(Version) < new SemVer(LatestVersion);
            if (force)
            {
                _updateAvailableForced = updateAvailable;
            }
            else
            {
                _updateAvailable = updateAvailable;
            }
            return updateAvailable;
        }

        public bool IsUpdateAvailable(List<AssetInfo> assets, bool force = true)
        {
            if (ParentId > 0) return false;
            if (force && _updateAvailableListForced != null) return _updateAvailableListForced.Value;
            if (!force && _updateAvailableList != null) return _updateAvailableList.Value;

            bool isOlderVersion = IsUpdateAvailable(force);
            if (isOlderVersion && assets != null && AssetSource != Asset.Source.RegistryPackage)
            {
                // if asset in that version is already loaded don't flag as update available
                if (assets.Any(a => a.AssetSource == Asset.Source.AssetStorePackage && a.ForeignId == ForeignId && a.Version == LatestVersion && !string.IsNullOrEmpty(a.GetLocation(true))))
                {
                    if (force)
                    {
                        _updateAvailableListForced = false;
                    }
                    else
                    {
                        _updateAvailableList = false;
                    }
                    return false;
                }
            }
            if (force)
            {
                _updateAvailableListForced = isOlderVersion;
            }
            else
            {
                _updateAvailableList = isOlderVersion;
            }
            return isOlderVersion;
        }

        public bool IsDownloading()
        {
            if (ParentInfo != null) return ParentInfo.IsDownloading();
            return PackageDownloader != null && PackageDownloader.GetState().state == AssetDownloader.State.Downloading;
        }

        // duplicated from Asset to avoid thousands of unnecessary casts
        public string GetCalculatedLocation()
        {
            if (string.IsNullOrEmpty(SafePublisher) || string.IsNullOrEmpty(SafeCategory) || string.IsNullOrEmpty(SafeName)) return null;

            return System.IO.Path.Combine(AssetInventory.GetAssetCacheFolder(), SafePublisher, SafeCategory, SafeName + ".unitypackage").Replace("\\", "/");
        }

        public AssetInfo WithTreeData(string name, int id = 0, int depth = 0)
        {
            m_Name = name;
            m_ID = id;
            m_Depth = depth;

            return this;
        }

        public AssetInfo WithTreeId(int id)
        {
            m_ID = id;

            return this;
        }

        public AssetInfo WithProjectPath(string path)
        {
            ProjectPath = path;

            return this;
        }

        public Texture GetFallbackIcon()
        {
            Texture result = null;
            if (AssetSource == Asset.Source.RegistryPackage)
            {
                #if UNITY_2020_1_OR_NEWER
                result = EditorGUIUtility.IconContent("d_Package Manager@2x").image; 
                #else
                result = EditorGUIUtility.IconContent("d_PreMatCube@2x").image;
                #endif
            }
            else if (AssetSource == Asset.Source.Archive)
            {
                result = EditorGUIUtility.IconContent("d_FilterByType@2x").image;
            }
            else if (AssetSource == Asset.Source.Directory)
            {
                result = EditorGUIUtility.IconContent("d_Folder Icon").image;
            }
            else if (AssetSource == Asset.Source.CustomPackage)
            {
                result = EditorGUIUtility.IconContent("d_ModelImporter Icon").image;
            }

            return result;
        }

        public Asset ToAsset()
        {
            Asset result = new Asset
            {
                AssetSource = AssetSource,
                DisplayCategory = DisplayCategory,
                SafeCategory = SafeCategory,
                CurrentState = CurrentState,
                CurrentSubState = CurrentSubState,
                Id = AssetId,
                ParentId = ParentId,
                Slug = Slug,
                Revision = Revision,
                Description = Description,
                KeyFeatures = KeyFeatures,
                CompatibilityInfo = CompatibilityInfo,
                SupportedUnityVersions = SupportedUnityVersions,
                Keywords = Keywords,
                Version = Version,
                LatestVersion = LatestVersion,
                UpdateStrategy = UpdateStrategy,
                License = License,
                LicenseLocation = LicenseLocation,
                PurchaseDate = PurchaseDate,
                LastRelease = LastRelease,
                FirstRelease = FirstRelease,
                AssetRating = AssetRating,
                RatingCount = RatingCount,
                Hotness = Hotness,
                PriceEur = PriceEur,
                PriceUsd = PriceUsd,
                PriceCny = PriceCny,
                Requirements = Requirements,
                ReleaseNotes = ReleaseNotes,
                OfficialState = OfficialState,
                IsHidden = IsHidden,
                Exclude = Exclude,
                UploadId = UploadId,
                ETag = ETag,
                OriginalLocation = OriginalLocation,
                OriginalLocationKey = OriginalLocationKey,
                ForeignId = ForeignId,
                SafeName = SafeName,
                DisplayName = DisplayName,
                PackageSize = PackageSize,
                SafePublisher = SafePublisher,
                DisplayPublisher = DisplayPublisher,
                PublisherId = PublisherId
            };
            result.SetLocation(Location);
            if (ParentInfo != null) result.ParentAsset = ParentInfo.ToAsset();

            return result;
        }

        public string GetItemLink()
        {
            return $"https://assetstore.unity.com/packages/slug/{ForeignId}";
        }

        public string GetPublisherLink()
        {
            return $"https://assetstore.unity.com/publishers/{PublisherId}";
        }

        public int GetChildDepth()
        {
            if (ParentId == 0) return 0;
            return ParentInfo.GetChildDepth() + 1;
        }

        public float GetPrice()
        {
            switch (AssetInventory.Config.currency)
            {
                case 0: return PriceEur;
                case 1: return PriceUsd;
                case 2: return PriceCny;
            }
            return 0;
        }

        public string GetPriceText()
        {
            return GetPriceText(GetPrice());
        }

        public string GetPriceText(float priceVal)
        {
            string price = priceVal.ToString("N2");
            switch (AssetInventory.Config.currency)
            {
                case 0: return $"€{price}";
                case 1: return $"${price}";
                case 2: return $"¥{price}";
            }

            return price;
        }

        public void Refresh(bool downloadStateOnly = false)
        {
            ParentInfo?.Refresh(downloadStateOnly);

            _downloaded = null;
            _downloadedActual = null;
            _updateAvailable = null;
            _updateAvailableForced = null;
            _updateAvailableList = null;
            _updateAvailableListForced = null;
            if (downloadStateOnly) return;

            WasOutdated = false;
        }

        public void ForceTargetVersion(string newVersion)
        {
            _forcedTargetVersion = newVersion;
        }

        public int GetFolderSpecType()
        {
            if (IsArchive()) return 2;
            if (IsPackage()) return 0;

            return 1;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(FileName))
            {
                return $"Asset Package '{GetDisplayName()}' ({AssetId}, {FileCount} files)";
            }
            return $"Asset Info '{FileName}' ({GetDisplayName()})'";
        }
    }
}