using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JD.EditorAudioUtils;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
#if !UNITY_2021_2_OR_NEWER
using Unity.SharpZipLib.Zip;
#endif
using UnityEditor;
using UnityEditor.Callbacks;
#if USE_URP_CONVERTER
using UnityEditor.Rendering.Universal;
#endif
using UnityEngine;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using Random = UnityEngine.Random;

namespace AssetInventory
{
    public static class AssetInventory
    {
        public const string TOOL_VERSION = "2.3.0";
        public const string ASSET_STORE_LINK = "https://u3d.as/3e4D";
        public const string ASSET_STORE_FOLDER_NAME = "Asset Store-5.x";
        public const string DEFINE_SYMBOL = "ASSET_INVENTORY";
        public const string TEMP_FOLDER = "_AssetInventoryTemp";
        public const int ASSET_STORE_ID = 890400;
        public const string TAG_START = "[";
        public const string TAG_END = "]";
        public static readonly bool DEBUG_MODE = false;

        private const string PARTIAL_INDICATOR = "ai-partial.info";
        private static readonly string[] ConversionExtensions = {"mat", "fbx"};
        private static readonly string[] ScanDependencies =
        {
            "prefab", "mat", "controller", "anim", "asset", "physicmaterial", "physicsmaterial", "sbs", "sbsar", "cubemap", "shader", "cginc", "hlsl", "shadergraph", "shadersubgraph", "terrainlayer", "inputactions"
        };

        private static readonly string[] ScanMetaDependencies =
        {
            "shader", "ttf", "otf", "js", "obj", "fbx", "uxml", "uss", "inputactions", "tss", "nn", "cs"
        };

        public static string CurrentMain { get; set; }
        public static string CurrentMainItem { get; set; }
        public static int MainCount { get; set; }
        public static int MainProgress { get; set; }
        public static string UsedConfigLocation { get; private set; }

        public static event Action OnPackagesUpdated;
        public static event Action OnTagsChanged;
        public static event Action OnIndexingDone;
        public static event Action<Asset> OnPackageImageLoaded;

        private const int MAX_DROPDOWN_ITEMS = 25;
        private const int FOLDER_CACHE_TIME = 60;
        private const string CONFIG_NAME = "AssetInventoryConfig.json";
        private const string DIAG_PURCHASES = "Purchases.json";
        private static readonly Regex FileGuid = new Regex("guid: (?:([a-z0-9]*))");

        private static bool InitDone { get; set; }
        private static UpdateObserver _observer;
        private static readonly TimedCache<string> _assetCacheFolder = new TimedCache<string>();
        private static readonly TimedCache<string> _materializeFolder = new TimedCache<string>();
        private static readonly TimedCache<string> _previewFolder = new TimedCache<string>();

        private static IEnumerable<TagInfo> Tags
        {
            get
            {
                if (_tags == null) LoadTagAssignments();
                return _tags;
            }
        }
        private static List<TagInfo> _tags;

        public static List<RelativeLocation> RelativeLocations
        {
            get
            {
                if (_relativeLocations == null) LoadRelativeLocations();
                return _relativeLocations;
            }
        }
        private static List<RelativeLocation> _relativeLocations;

        public static List<RelativeLocation> UserRelativeLocations
        {
            get
            {
                if (_userRelativeLocations == null) LoadRelativeLocations();
                return _userRelativeLocations;
            }
        }
        private static List<RelativeLocation> _userRelativeLocations;

        public static AssetInventorySettings Config
        {
            get
            {
                if (_config == null) LoadConfig();
                return _config;
            }
        }

        private static AssetInventorySettings _config;
        public static readonly List<string> ConfigErrors = new List<string>();

        public static bool IndexingInProgress { get; set; }
        public static bool ClearCacheInProgress { get; private set; }

        public static Dictionary<string, string[]> TypeGroups { get; } = new Dictionary<string, string[]>
        {
            {"Audio", new[] {"wav", "mp3", "ogg", "aiff", "aif", "mod", "it", "s3m", "xm", "flac"}},
            {
                "Images",
                new[]
                {
                    "png", "jpg", "jpeg", "bmp", "tga", "tif", "tiff", "psd", "svg", "webp", "ico", "exr", "gif", "hdr",
                    "iff", "pict"
                }
            },
            {"Video", new[] {"avi", "asf", "dv", "m4v", "mov", "mp4", "mpg", "mpeg", "ogv", "vp8", "webm", "wmv"}},
            {"Prefabs", new[] {"prefab"}},
            {"Materials", new[] {"mat", "physicmaterial", "physicsmaterial", "sbs", "sbsar", "cubemap"}},
            {"Shaders", new[] {"shader", "shadergraph", "shadersubgraph", "compute"}},
            {"Models", new[] {"fbx", "obj", "blend", "dae", "3ds", "dxf", "max", "c4d", "mb", "ma"}},
            {"Animations", new[] {"anim"}},
            {"Scripts", new[] {"cs", "php"}},
            {"Libraries", new[] {"zip", "rar", "7z", "unitypackage", "so", "bundle", "dll", "jar"}},
            {"Documents", new[] {"md", "doc", "docx", "txt", "json", "rtf", "pdf", "htm", "html", "readme", "xml", "chm", "csv"}}
        };

        public static int TagHash { get; private set; }

        [DidReloadScripts(1)]
        public static void AutoInit()
        {
            // this will be run after a recompile so keep to a minimum, e.g. ensure third party tools can work
            EditorApplication.delayCall += () => Init();
        }

        public static void ReInit()
        {
            InitDone = false;
            LoadConfig();
            Init();
        }

        public static void Init(bool secondTry = false)
        {
            if (InitDone) return;

            SetupDefines();

            _materializeFolder.Clear();
            _assetCacheFolder.Clear();
            _previewFolder.Clear();

            string folder = GetStorageFolder();
            if (!Directory.Exists(folder))
            {
                try
                {
                    Directory.CreateDirectory(folder);
                }
                catch (Exception e)
                {
                    if (secondTry)
                    {
                        Debug.LogError($"Could not create storage folder for database in default location '{folder}' as well. Giving up: {e.Message}");
                    }
                    else
                    {
                        Debug.LogError($"Could not create storage folder '{folder}' for database. Reverting to default location: {e.Message}");
                        Config.customStorageLocation = null;
                        SaveConfig();
                        Init(true);
                        return;
                    }
                }
            }
            DBAdapter.InitDB();
            PreviewGenerator.Clear();

            UpgradeUtil.PerformUpgrades();
            LoadTagAssignments();
            LoadRelativeLocations();
            UpdateSystemData();

            AssetStore.FillBufferOnDemand(true);

            InitDone = true;
        }

        public static void StartCacheObserver()
        {
            GetObserver().Start();
        }

        public static void StopCacheObserver()
        {
            GetObserver().Stop();
        }

        public static UpdateObserver GetObserver()
        {
            if (_observer == null) _observer = new UpdateObserver(GetAssetCacheFolder(), new[] {"unitypackage", "tmp"});
            return _observer;
        }

        private static void SetupDefines()
        {
            if (!AssetUtils.HasDefine(DEFINE_SYMBOL)) AssetUtils.AddDefine(DEFINE_SYMBOL);
        }

        private static void UpdateSystemData()
        {
            SystemData data = new SystemData();
            data.Key = SystemInfo.deviceUniqueIdentifier;
            data.Name = SystemInfo.deviceName;
            data.Type = SystemInfo.deviceType.ToString();
            data.Model = SystemInfo.deviceModel;
            data.OS = SystemInfo.operatingSystem;
            data.LastUsed = DateTime.Now;

            try
            {
                DBAdapter.DB.InsertOrReplace(data);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not update system data: {e.Message}");
            }
        }

        public static bool IsFileType(string path, string type)
        {
            if (path == null) return false;
            return TypeGroups[type].Contains(IOUtils.GetExtensionWithoutDot(path).ToLowerInvariant());
        }

        public static string GetStorageFolder()
        {
            if (!string.IsNullOrEmpty(Config.customStorageLocation)) return Path.GetFullPath(Config.customStorageLocation);

            return IOUtils.PathCombine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AssetInventory");
        }

        private static string GetConfigLocation()
        {
            // search for local project-specific override first
            string guid = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(CONFIG_NAME)).FirstOrDefault();
            if (guid != null) return AssetDatabase.GUIDToAssetPath(guid);

            // second fallback is environment variable
            string configPath = Environment.GetEnvironmentVariable("ASSETINVENTORY_CONFIG_PATH");
            if (!string.IsNullOrWhiteSpace(configPath)) return IOUtils.PathCombine(configPath, CONFIG_NAME);

            // finally use from central well-known folder
            return IOUtils.PathCombine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), CONFIG_NAME);
        }

        public static string GetPreviewFolder(string customFolder = null, bool noCache = false)
        {
            if (!noCache && _previewFolder.TryGetValue(out string path)) return path;

            string previewPath = IOUtils.PathCombine(customFolder ?? GetStorageFolder(), "Previews");
            if (!Directory.Exists(previewPath)) Directory.CreateDirectory(previewPath);

            if (!noCache) _previewFolder.SetValue(previewPath, TimeSpan.FromSeconds(FOLDER_CACHE_TIME));

            return previewPath;
        }

        public static string GetBackupFolder(bool createOnDemand = true)
        {
            string backupPath = string.IsNullOrWhiteSpace(Config.backupFolder)
                ? IOUtils.PathCombine(GetStorageFolder(), "Backups")
                : Config.backupFolder;
            if (createOnDemand && !Directory.Exists(backupPath)) Directory.CreateDirectory(backupPath);
            return backupPath;
        }

        public static string GetMaterializeFolder()
        {
            if (_materializeFolder.TryGetValue(out string path)) return path;

            string cachePath = string.IsNullOrWhiteSpace(Config.cacheFolder)
                ? IOUtils.PathCombine(GetStorageFolder(), "Extracted")
                : Config.cacheFolder;

            _materializeFolder.SetValue(cachePath, TimeSpan.FromSeconds(FOLDER_CACHE_TIME));

            return cachePath;
        }

        public static string GetMaterializedAssetPath(Asset asset)
        {
            // append the Id to support identically named packages in different locations
            return IOUtils.PathCombine(GetMaterializeFolder(), asset.SafeName + " - " + asset.Id);
        }

        public static async Task<string> ExtractAsset(Asset asset, AssetFile assetFile = null, bool fileOnly = false)
        {
            if (string.IsNullOrEmpty(asset.GetLocation(true))) return null;

            // make sure parents are extracted first
            string archivePath = await asset.GetLocation(true, true);
            if (!File.Exists(archivePath))
            {
                Debug.LogError($"Asset has vanished since last refresh and cannot be indexed: {archivePath}");

                if (asset.ParentId <= 0)
                {
                    // reflect new state
                    // TODO: consider rel systems 
                    asset.SetLocation(null);
                    DBAdapter.DB.Execute("update Asset set Location=null where Id=?", asset.Id);
                }
                return null;
            }

            string tempPath = GetMaterializedAssetPath(asset);

            // delete existing cache if interested in whole bundle to make sure everything is there
            if (assetFile == null || !fileOnly || asset.KeepExtracted)
            {
                int retries = 0;
                while (retries < 5 && Directory.Exists(tempPath))
                {
                    try
                    {
                        await Task.Run(() => Directory.Delete(tempPath, true));
                        break;
                    }
                    catch (Exception)
                    {
                        retries++;
                        await Task.Delay(500);
                    }
                }
                if (Directory.Exists(tempPath)) Debug.LogWarning($"Could not remove temporary directory: {tempPath}");

                try
                {
                    if (asset.AssetSource == Asset.Source.Archive)
                    {
#if UNITY_2021_2_OR_NEWER
                        await Task.Run(() => IOUtils.ExtractArchive(archivePath, tempPath));
#else
                        if (asset.Location.ToLowerInvariant().EndsWith(".zip"))
                        {
                            FastZip fastZip = new FastZip();
                            await Task.Run(() => fastZip.ExtractZip(archivePath, tempPath, null));
                        }
#endif
                    }
                    else
                    {
                        // special handling for Tar as that will throw null errors with SharpCompress
                        await Task.Run(() => TarUtil.ExtractGz(archivePath, tempPath));
                    }

                    // safety delay in case this is a network drive which needs some time to unlock all files
                    await Task.Delay(100);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Could not extract archive '{archivePath}' due to errors. Index results will be partial: {e.Message}");
                    return null;
                }

                return Directory.Exists(tempPath) ? tempPath : null;
            }

            // single file only
            string targetPath = Path.Combine(GetMaterializedAssetPath(asset), assetFile.GetSourcePath(true));
            if (File.Exists(targetPath)) return targetPath;

            try
            {
                if (asset.AssetSource == Asset.Source.Archive)
                {
                    // TODO: switch to single file
#if UNITY_2021_2_OR_NEWER
                    await Task.Run(() => IOUtils.ExtractArchive(archivePath, tempPath));
#else
                    if (asset.Location.ToLowerInvariant().EndsWith(".zip"))
                    {
                        FastZip fastZip = new FastZip();
                        await Task.Run(() => fastZip.ExtractZip(archivePath, tempPath, null));
                    }
#endif
                }
                else
                {
                    // special handling for Tar as that will throw null errors with SharpCompress
                    await Task.Run(() => TarUtil.ExtractGzFile(archivePath, assetFile.GetSourcePath(true), tempPath));
                    string indicator = Path.Combine(tempPath, PARTIAL_INDICATOR);
                    if (!File.Exists(indicator)) File.WriteAllText(indicator, DateTime.Now.ToString(CultureInfo.InvariantCulture));
                }

                // safety delay in case this is a network drive which needs some time to unlock all files
                await Task.Delay(100);
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not extract archive '{archivePath}' due to errors: {e.Message}");
                return null;
            }

            return File.Exists(targetPath) ? targetPath : null;
        }

        public static bool IsMaterialized(Asset asset, AssetFile assetFile = null)
        {
            if (asset.AssetSource == Asset.Source.Directory || asset.AssetSource == Asset.Source.RegistryPackage)
            {
                if (assetFile != null) return File.Exists(assetFile.GetSourcePath(true));
                return Directory.Exists(asset.GetLocation(true));
            }

            string assetPath = GetMaterializedAssetPath(asset);
            return assetFile != null
                ? File.Exists(Path.Combine(assetPath, assetFile.GetSourcePath(true)))
                : Directory.Exists(assetPath);
        }

        public static async Task<string> EnsureMaterializedAsset(AssetInfo info, bool fileOnly = false)
        {
            string targetPath = await EnsureMaterializedAsset(info.ToAsset(), info, fileOnly);
            info.IsMaterialized = IsMaterialized(info.ToAsset(), info);
            return targetPath;
        }

        public static async Task<string> EnsureMaterializedAsset(Asset asset, AssetFile assetFile = null, bool fileOnly = false)
        {
            if (asset.AssetSource == Asset.Source.Directory || asset.AssetSource == Asset.Source.RegistryPackage)
            {
                return File.Exists(assetFile.GetSourcePath(true)) ? assetFile.GetSourcePath(true) : null;
            }

            // ensure parent hierarchy is extracted first
            string archivePath = await asset.GetLocation(true, true);
            if (string.IsNullOrEmpty(archivePath) || !File.Exists(archivePath)) return null;

            string targetPath;
            if (assetFile == null)
            {
                targetPath = GetMaterializedAssetPath(asset);
                if (!Directory.Exists(targetPath) || File.Exists(Path.Combine(targetPath, PARTIAL_INDICATOR))) await ExtractAsset(asset);
                if (!Directory.Exists(targetPath)) return null;
            }
            else
            {
                string sourcePath = Path.Combine(GetMaterializedAssetPath(asset), assetFile.GetSourcePath(true));
                if (!File.Exists(sourcePath))
                {
                    if (await ExtractAsset(asset, assetFile, fileOnly) == null)
                    {
                        Debug.LogError($"Archive could not be extracted: {asset}");
                        return null;
                    }
                }
                if (!File.Exists(sourcePath))
                {
                    // file is most likely not contained in package anymore, remove from index
                    Debug.LogError($"File is not contained in this version of the package anymore. Removing from index: {assetFile.FileName}");

                    DBAdapter.DB.Execute("delete from AssetFile where Id=?", assetFile.Id);
                    assetFile.Id = 0;
                    return null;
                }

                targetPath = Path.Combine(Path.GetDirectoryName(sourcePath), "Content", Path.GetFileName(assetFile.GetPath(true)));
                try
                {
                    if (!File.Exists(targetPath))
                    {
                        string directoryName = Path.GetDirectoryName(targetPath);
                        if (!Directory.Exists(directoryName)) Directory.CreateDirectory(directoryName);
                        File.Copy(sourcePath, targetPath, true);
                    }

                    string sourceMetaPath = sourcePath + ".meta";
                    string targetMetaPath = targetPath + ".meta";
                    if (File.Exists(sourceMetaPath) && !File.Exists(targetMetaPath)) File.Copy(sourceMetaPath, targetMetaPath, true);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Could not extract file. Most likely the target device ran out of space: {e.Message}");
                    return null;
                }
            }

            return targetPath;
        }

        public static bool NeedsDependencyScan(string type)
        {
            return ScanDependencies.Contains(type) || ScanMetaDependencies.Contains(type);
        }

        public static async Task CalculateDependencies(AssetInfo info)
        {
            info.DependencyState = AssetInfo.DependencyStateOptions.Calculating;
            info.Dependencies = new List<AssetFile>();

            string targetPath = await EnsureMaterializedAsset(info.ToAsset(), info);
            if (targetPath == null)
            {
                info.DependencyState = AssetInfo.DependencyStateOptions.Failed;
                return;
            }

            info.Dependencies = (await DoCalculateDependencies(info, targetPath)).OrderBy(af => af.Path).ToList();
            info.DependencySize = info.Dependencies.Sum(af => af.Size);
            info.MediaDependencies = info.Dependencies.Where(af => af.Type != "cs" && af.Type != "dll").ToList();
            info.ScriptDependencies = info.Dependencies.Where(af => af.Type == "cs" || af.Type == "dll").ToList();

            // clean-up again on-demand
            string tempDir = Path.Combine(Application.dataPath, TEMP_FOLDER);
            if (Directory.Exists(tempDir))
            {
                await IOUtils.DeleteFileOrDirectory(tempDir);
                await IOUtils.DeleteFileOrDirectory(tempDir + ".meta");
                AssetDatabase.Refresh();
            }
            if (info.DependencyState == AssetInfo.DependencyStateOptions.Calculating) info.DependencyState = AssetInfo.DependencyStateOptions.Done; // otherwise error along the way
        }

        private static async Task<List<AssetFile>> DoCalculateDependencies(AssetInfo info, string path, List<AssetFile> result = null)
        {
            if (result == null) result = new List<AssetFile>();

            // only scan file types that contain guid references
            string extension = IOUtils.GetExtensionWithoutDot(path).ToLowerInvariant();

            // meta files can also contain dependencies
            if (ScanMetaDependencies.Contains(extension))
            {
                string metaPath = path + ".meta";
                if (File.Exists(metaPath)) await DoCalculateDependencies(info, metaPath, result);
            }

            if (extension != "meta" && !ScanDependencies.Contains(extension)) return result;

            if (string.IsNullOrEmpty(info.Guid))
            {
                info.DependencyState = AssetInfo.DependencyStateOptions.Failed;
                return result;
            }

#if UNITY_2021_2_OR_NEWER
            string content = await File.ReadAllTextAsync(path);
#else
            string content = File.ReadAllText(path);
#endif

            if (extension == "shader" || extension == "cginc" || extension == "hlsl")
            {
                // include files
                List<string> includedFiles = AssetUtils.ExtractIncludedFiles(content);
                foreach (string include in includedFiles)
                {
                    string metaPath = path + ".meta";
                    if (!File.Exists(metaPath)) continue;

                    string curGuid = AssetUtils.ExtractGuidFromFile(metaPath);
                    if (curGuid == null) continue;

                    AssetFile curAf = DBAdapter.DB.Find<AssetFile>(a => a.Guid == curGuid && a.AssetId == info.AssetId);
                    if (curAf == null) continue;

                    string includePath = Path.Combine(Path.GetDirectoryName(curAf.Path), include).Replace("\\", "/");
                    AssetFile af = DBAdapter.DB.Find<AssetFile>(a => a.Path == includePath && a.AssetId == info.AssetId);
                    if (af == null) continue;
                    if (result.Any(r => r.Guid == af.Guid)) continue;

                    result.Add(af);
                    await ScanDependencyResult(info, result, af);
                }

                if (extension == "shader")
                {
                    // custom editors
                    List<string> editorFiles = AssetUtils.ExtractCustomEditors(content);
                    foreach (string include in editorFiles)
                    {
                        string includePath = include + ".cs"; // file could also be named differently than class name, would require code analysis
                        AssetFile af = DBAdapter.DB.Find<AssetFile>(a => a.FileName == includePath && a.AssetId == info.AssetId);
                        if (af == null) continue;
                        if (result.Any(r => r.Guid == af.Guid)) continue;

                        result.Add(af);
                        await ScanDependencyResult(info, result, af);
                    }
                }
            }
            else if (extension != "meta" && !content.StartsWith("%YAML"))
            {
                // reserialize prefabs on-the-fly by copying them over which will cause Unity to change the encoding upon refresh
                // this will not work but throw missing script errors instead if there are any attached
                string targetDir = Path.Combine(Application.dataPath, TEMP_FOLDER);
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                string targetFile = Path.Combine("Assets", TEMP_FOLDER, Path.GetFileName(path));
                File.Copy(path, targetFile, true);
                AssetDatabase.Refresh();

#if UNITY_2021_2_OR_NEWER
                content = await File.ReadAllTextAsync(targetFile);
#else
                content = File.ReadAllText(targetFile);
#endif

                // if it still does not work, might be because of missing scripts inside prefabs
                if (!content.StartsWith("%YAML"))
                {
                    if (targetFile.ToLowerInvariant().EndsWith(".prefab"))
                    {
                        try
                        {
                            GameObject go = PrefabUtility.LoadPrefabContents(targetFile);
                            int removed = go.transform.RemoveMissingScripts();
                            if (removed > 0)
                            {
                                PrefabUtility.SaveAsPrefabAsset(go, targetFile);
                                PrefabUtility.UnloadPrefabContents(go);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Invalid prefab '{info}' encountered: {e.Message}");
                            info.DependencyState = AssetInfo.DependencyStateOptions.Failed;
                            return result;
                        }

#if UNITY_2021_2_OR_NEWER
                        content = await File.ReadAllTextAsync(targetFile);
#else
                        content = File.ReadAllText(targetFile);
#endif

                        // final check
                        if (!content.StartsWith("%YAML"))
                        {
                            info.DependencyState = AssetInfo.DependencyStateOptions.NotPossible;
                            return result;
                        }
                    }
                    else
                    {
                        info.DependencyState = AssetInfo.DependencyStateOptions.NotPossible;
                        return result;
                    }
                }
            }

            MatchCollection matches = FileGuid.Matches(content);

            foreach (Match match in matches)
            {
                string guid = match.Groups[1].Value;
                if (result.Any(r => r.Guid == guid)) continue; // break recursion
                if (guid == info.Guid) continue; // ignore self

                // find file with guid inside the respective package only, don't look into others that might repackage it
                // since that can throw errors if the asset was not downloaded yet or has an identical Id although being different
                AssetFile af = DBAdapter.DB.Find<AssetFile>(a => a.Guid == guid && a.AssetId == info.AssetId);

                // ignore missing guids as they are not in the package, so we can't do anything about them
                if (af == null) continue;

                result.Add(af);
                await ScanDependencyResult(info, result, af);
            }

            return result.Distinct().ToList();
        }

        private static async Task ScanDependencyResult(AssetInfo info, List<AssetFile> result, AssetFile af)
        {
            string targetPath = await EnsureMaterializedAsset(info.ToAsset(), af);
            if (targetPath == null)
            {
                Debug.LogWarning($"Could not materialize dependency: {af.Path}");
                return;
            }

            await DoCalculateDependencies(info, targetPath, result);
        }

        public static int MarkAudioWithMissingLengthForIndexing()
        {
            return DBAdapter.DB.Execute("update AssetFile set Size = 0 where Length = 0 and Type in ('" + string.Join("','", TypeGroups["Audio"]) + "')");
        }

        public static List<AssetInfo> LoadAssets()
        {
            string indexedQuery = "SELECT *, Count(*) as FileCount, Sum(af.Size) as UncompressedSize from AssetFile af left join Asset on Asset.Id = af.AssetId group by af.AssetId order by Lower(Asset.SafeName)";
            Dictionary<int, AssetInfo> indexedResult = DBAdapter.DB.Query<AssetInfo>(indexedQuery).ToDictionary(a => a.AssetId);

            // always filter out built-in packages for now, just confusing
            string allQuery = "SELECT *, Id as AssetId from Asset where PackageSource != 2 order by Lower(SafeName)";
            List<AssetInfo> result = DBAdapter.DB.Query<AssetInfo>(allQuery);

            // sqlite does not support "right join", therefore merge two queries manually 
            result.ForEach(asset =>
            {
                if (indexedResult.TryGetValue(asset.Id, out AssetInfo match))
                {
                    asset.FileCount = match.FileCount;
                    asset.UncompressedSize = match.UncompressedSize;
                }
            });

            ResolveParents(result, result);
            GetObserver().SetAll(result);

            return result;
        }

        public static void ResolveParents(List<AssetInfo> assets, List<AssetInfo> allAssets)
        {
            Dictionary<int, AssetInfo> assetDict = allAssets.ToDictionary(a => a.AssetId);

            foreach (AssetInfo asset in assets)
            {
                if (asset.ParentId > 0 && asset.ParentInfo == null)
                {
                    if (assetDict.TryGetValue(asset.ParentId, out AssetInfo parentInfo))
                    {
                        asset.ParentInfo = parentInfo;
                        parentInfo.ChildInfos.Add(asset);
                    }
                }
            }
        }

        public static string[] ExtractAssetNames(IEnumerable<AssetInfo> assets, bool includeIdForDuplicates)
        {
            bool intoSubmenu = Config.groupLists && assets.Count(a => a.FileCount > 0) > MAX_DROPDOWN_ITEMS;
            List<string> result = new List<string> {"-all-"};
            List<string> assetNames = new List<string>();

            foreach (AssetInfo asset in assets)
            {
                if (asset.FileCount > 0 && !asset.Exclude)
                {
                    string safeName = intoSubmenu && !asset.SafeName.StartsWith("-")
                        ? asset.SafeName.Substring(0, 1).ToUpperInvariant() + "/" + asset.SafeName
                        : asset.SafeName;

                    if (includeIdForDuplicates && asset.SafeName != Asset.NONE)
                    {
                        safeName = $"{safeName} [{asset.AssetId}]";
                    }

                    assetNames.Add(safeName);
                }
            }
            assetNames.Sort();

            if (assetNames.Count > 0)
            {
                result.Add(string.Empty);
                result.AddRange(assetNames);

                int noneIdx = result.IndexOf(Asset.NONE);
                if (noneIdx >= 0)
                {
                    string tmp = result[noneIdx];
                    result.RemoveAt(noneIdx);
                    result.Insert(1, tmp);
                }
            }

            return result.ToArray();
        }

        public static string[] ExtractTagNames(List<Tag> tags)
        {
            bool intoSubmenu = Config.groupLists && tags.Count > MAX_DROPDOWN_ITEMS;
            List<string> result = new List<string> {"-all-", "-none-", string.Empty};
            result.AddRange(tags
                .Select(a =>
                    intoSubmenu && !a.Name.StartsWith("-")
                        ? a.Name.Substring(0, 1).ToUpperInvariant() + "/" + a.Name
                        : a.Name)
                .OrderBy(s => s));

            if (result.Count == 2) result.RemoveAt(1);

            return result.ToArray();
        }

        public static string[] ExtractPublisherNames(IEnumerable<AssetInfo> assets)
        {
            bool intoSubmenu =
                Config.groupLists &&
                assets.Count(a => a.FileCount > 0) >
                MAX_DROPDOWN_ITEMS; // approximation, publishers != assets but roughly the same
            List<string> result = new List<string> {"-all-", string.Empty};
            result.AddRange(assets
                .Where(a => a.FileCount > 0)
                .Where(a => !a.Exclude)
                .Where(a => !string.IsNullOrEmpty(a.SafePublisher))
                .Select(a =>
                    intoSubmenu
                        ? a.SafePublisher.Substring(0, 1).ToUpperInvariant() + "/" + a.SafePublisher
                        : a.SafePublisher)
                .Distinct()
                .OrderBy(s => s));

            if (result.Count == 2) result.RemoveAt(1);

            return result.ToArray();
        }

        public static string[] ExtractCategoryNames(IEnumerable<AssetInfo> assets)
        {
            bool intoSubmenu = Config.groupLists;
            List<string> result = new List<string> {"-all-", string.Empty};
            result.AddRange(assets
                .Where(a => a.FileCount > 0)
                .Where(a => !a.Exclude)
                .Where(a => !string.IsNullOrEmpty(a.SafeCategory))
                .Select(a =>
                {
                    if (intoSubmenu)
                    {
                        string[] arr = a.GetDisplayCategory().Split('/');
                        return arr[0] + "/" + a.SafeCategory;
                    }

                    return a.SafeCategory;
                })
                .Distinct()
                .OrderBy(s => s));

            if (result.Count == 2) result.RemoveAt(1);

            return result.ToArray();
        }

        public static string[] LoadTypes()
        {
            List<string> result = new List<string> {"-all-", string.Empty};

            string query = "SELECT Distinct(Type) from AssetFile where Type not null and Type != \"\" order by Type";
            List<string> raw = DBAdapter.DB.QueryScalars<string>($"{query}");

            List<string> groupTypes = new List<string>();
            foreach (KeyValuePair<string, string[]> group in TypeGroups)
            {
                groupTypes.AddRange(group.Value);
                foreach (string type in group.Value)
                {
                    if (raw.Contains(type))
                    {
                        result.Add($"{group.Key}");
                        break;
                    }
                }
            }

            if (result.Last() != "") result.Add(string.Empty);

            // others
            result.AddRange(raw.Where(r => !groupTypes.Contains(r)).Select(type => $"Others/{type}"));

            // all
            result.AddRange(raw.Select(type => $"All/{type}"));

            if (result.Count == 2) result.RemoveAt(1);

            return result.ToArray();
        }

        public static async Task<long> GetCacheFolderSize()
        {
            return await IOUtils.GetFolderSize(GetMaterializeFolder());
        }

        public static async Task<long> GetPersistedCacheSize()
        {
            if (!Directory.Exists(GetMaterializeFolder())) return 0;

            long result = 0;

            List<Asset> keepAssets = DBAdapter.DB.Table<Asset>().Where(a => a.KeepExtracted).ToList();
            List<string> keepPaths = keepAssets.Select(a => GetMaterializedAssetPath(a).ToLowerInvariant()).ToList();
            string[] packages = Directory.GetDirectories(GetMaterializeFolder());
            foreach (string package in packages)
            {
                if (!keepPaths.Contains(package.ToLowerInvariant())) continue;
                result += await IOUtils.GetFolderSize(package);
            }

            return result;
        }

        public static async Task<long> GetBackupFolderSize()
        {
            return await IOUtils.GetFolderSize(GetBackupFolder());
        }

        public static async Task<long> GetPreviewFolderSize()
        {
            return await IOUtils.GetFolderSize(GetPreviewFolder());
        }

        public static async void RefreshIndex(bool force = false)
        {
            IndexingInProgress = true;
            AssetProgress.CancellationRequested = false;

            Init();

            // pass 1: metadata
            // special handling for normal asset store assets since directory structure yields additional information
            if (Config.indexAssetCache)
            {
                string assetDownloadCache = GetAssetCacheFolder();
                if (Directory.Exists(assetDownloadCache))
                {
                    // check if forced local update is requested after upgrading
                    AppProperty forceLocalUpdate = DBAdapter.DB.Find<AppProperty>("ForceLocalUpdate");
                    if (forceLocalUpdate != null && forceLocalUpdate.Value.ToLowerInvariant() == "true")
                    {
                        force = true;
                        DBAdapter.DB.Delete<AppProperty>("ForceLocalUpdate");
                    }

                    await new UnityPackageImporter().IndexRoughLocal(new FolderSpec(assetDownloadCache), true, force);
                }
                else
                {
                    Debug.LogWarning($"Could not find the asset download folder: {assetDownloadCache}");
                    EditorUtility.DisplayDialog("Error",
                        $"Could not find the asset download folder: {assetDownloadCache}.\n\nEither nothing was downloaded yet through the Package Manager or you changed the Asset cache location. In the latter case, please configure the new location under Settings.",
                        "OK");
                }
            }

            if (Config.indexPackageCache)
            {
                string packageDownloadCache = GetPackageCacheFolder();
                if (Directory.Exists(packageDownloadCache))
                {
                    await new PackageImporter().IndexRough(packageDownloadCache, true);
                }
                else
                {
                    Debug.LogWarning($"Could not find the package download folder: {packageDownloadCache}");
                    EditorUtility.DisplayDialog("Error",
                        $"Could not find the package download folder: {packageDownloadCache}.\n\nEither nothing was downloaded yet through the Package Manager or you changed the Package cache location. In the latter case, please configure the new location under Settings.",
                        "OK");
                }
            }

            // pass 2: details
            if (Config.indexAssetCache && Config.indexAssetPackageContents) await new UnityPackageImporter().IndexDetails();
            if (Config.indexPackageCache) await new PackageImporter().IndexDetails();

            // scan custom folders
            if (Config.indexAdditionalFolders)
            {
                for (int i = 0; i < Config.folders.Count; i++)
                {
                    if (AssetProgress.CancellationRequested) break;

                    FolderSpec spec = Config.folders[i];
                    if (!spec.enabled) continue;
                    if (!Directory.Exists(spec.GetLocation(true)))
                    {
                        Debug.LogWarning($"Specified folder to scan for assets does not exist anymore: {spec.location}");
                        continue;
                    }

                    switch (spec.folderType)
                    {
                        case 0:
                            bool hasAssetStoreLayout = Path.GetFileName(spec.GetLocation(true)) == ASSET_STORE_FOLDER_NAME;
                            await new UnityPackageImporter().IndexRoughLocal(spec, hasAssetStoreLayout, force);

                            if (Config.indexAssetPackageContents) await new UnityPackageImporter().IndexDetails();
                            break;

                        case 1:
                            await new MediaImporter().Index(spec);
                            break;

                        case 2:
                            await new ArchiveImporter().Index(spec);
                            break;

                        default:
                            Debug.LogError($"Unsupported folder scan type: {spec.folderType}");
                            break;
                    }
                }
            }

            // pass 3: online index
            if (Config.indexAssetCache && Config.downloadAssets)
            {
                List<AssetInfo> assets = LoadAssets()
                    .Where(info =>
                        info.AssetSource == Asset.Source.AssetStorePackage &&
                        !info.Exclude &&
                        !info.IsAbandoned && !info.IsIndexed && !string.IsNullOrEmpty(info.OfficialState)
                        && !info.Downloaded)
                    .ToList();

                // needs to be started as coroutine due to download triggering which cannot happen outside main thread 
                bool done = false;
                EditorCoroutineUtility.StartCoroutineOwnerless(new UnityPackageImporter().IndexRoughOnline(assets, () => done = true));
                do
                {
                    await Task.Delay(100);
                } while (!done);
            }

            // pass 4: index colors
            if (Config.extractColors)
            {
                await new ColorImporter().Index();
            }

            // pass 5: AI captions
            if (Config.createAICaptions)
            {
                await new CaptionCreator().Index();
            }

            // pass 6: backup
            if (Config.createBackups)
            {
                AssetBackup backup = new AssetBackup();
                await backup.Sync();
            }

            // final pass: start over once if that was the very first time indexing since after all updates are pulled the indexing might crunch additional data
            AppProperty initialIndexingDone = DBAdapter.DB.Find<AppProperty>("InitialIndexingDone");
            if (!AssetProgress.CancellationRequested && (initialIndexingDone == null || initialIndexingDone.Value.ToLowerInvariant() != "true"))
            {
                DBAdapter.DB.InsertOrReplace(new AppProperty("InitialIndexingDone", "true"));
                RefreshIndex(true);
                return;
            }

            IndexingInProgress = false;
            OnIndexingDone?.Invoke();
        }

        public static async void RefreshIndex(AssetInfo info)
        {
            IndexingInProgress = true;
            AssetProgress.CancellationRequested = false;

            switch (info.AssetSource)
            {
                case Asset.Source.AssetStorePackage:
                case Asset.Source.CustomPackage:
                    await new UnityPackageImporter().IndexDetails(info.Id);
                    break;

                case Asset.Source.RegistryPackage:
                    await new PackageImporter().IndexDetails(info.Id);
                    break;

                case Asset.Source.Archive:
                    await new ArchiveImporter().IndexDetails(info.ToAsset());
                    break;

                case Asset.Source.Directory:
                    FolderSpec spec = Config.folders.FirstOrDefault(f => f.location == info.Location && f.folderType == info.GetFolderSpecType());
                    if (spec != null) await new MediaImporter().Index(spec);
                    break;

                default:
                    Debug.LogError($"Unsupported asset source of '{info.GetDisplayName()}' for index refresh: {info.AssetSource}");
                    break;
            }

            IndexingInProgress = false;
            OnIndexingDone?.Invoke();
        }

        public static async Task ProcessSubPackages(Asset asset, List<AssetFile> subPackages)
        {
            List<AssetFile> unityPackages = subPackages.Where(p => p.IsPackage()).ToList();
            List<AssetFile> archives = subPackages.Where(p => p.IsArchive()).ToList();

            if (unityPackages.Count > 0)
            {
                await UnityPackageImporter.ProcessSubPackages(asset, unityPackages);
            }

            if (archives.Count > 0)
            {
                await ArchiveImporter.ProcessSubArchives(asset, archives);
            }
        }

        public static string GetAssetCacheFolder()
        {
            if (_assetCacheFolder.TryGetValue(out string path)) return path;

            string result;

            // explicit custom configuration always wins
            if (Config.assetCacheLocationType == 1 && !string.IsNullOrWhiteSpace(Config.assetCacheLocation))
            {
                result = Config.assetCacheLocation;
            }
            // then try what Unity is reporting itself
            else if (!string.IsNullOrWhiteSpace(AssetStore.GetAssetCacheFolder()))
            {
                result = AssetStore.GetAssetCacheFolder();
            }
            else
            {
                // environment variable overrides default location
                string envPath = IOUtils.GetEnvVar("ASSETSTORE_CACHE_PATH");
                if (!string.IsNullOrWhiteSpace(envPath))
                {
                    result = envPath;
                }
                else
                {
                    // custom special location (Unity 2022+) overrides default as well, kept in for legacy compatibility
                    string customLocation = Config.folders.FirstOrDefault(f => f.GetLocation(true).EndsWith(ASSET_STORE_FOLDER_NAME))?.GetLocation(true);
                    if (!string.IsNullOrWhiteSpace(customLocation))
                    {
                        result = customLocation;
                    }
                    else
                    {
#if UNITY_EDITOR_WIN
                        result = IOUtils.PathCombine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity", ASSET_STORE_FOLDER_NAME);
#endif
#if UNITY_EDITOR_OSX
                        result = IOUtils.PathCombine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Unity", ASSET_STORE_FOLDER_NAME);
#endif
#if UNITY_EDITOR_LINUX
                        result = IOUtils.PathCombine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".local/share/unity3d", ASSET_STORE_FOLDER_NAME);
#endif
                    }
                }
            }
            if (result != null) result = result.Replace("\\", "/");

            _assetCacheFolder.SetValue(result, TimeSpan.FromSeconds(FOLDER_CACHE_TIME));
            return result;
        }

        public static string GetPackageCacheFolder()
        {
            string result;
            if (Config.packageCacheLocationType == 1 && !string.IsNullOrWhiteSpace(Config.packageCacheLocation))
            {
                result = Config.packageCacheLocation;
            }
            else
            {
#if UNITY_EDITOR_WIN
                result = IOUtils.PathCombine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity", "cache", "packages");
#endif
#if UNITY_EDITOR_OSX
                result = IOUtils.PathCombine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Unity", "cache", "packages");
#endif
#if UNITY_EDITOR_LINUX
                result = IOUtils.PathCombine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".config/unity3d/cache/packages");
#endif
            }
            if (result != null) result = result.Replace("\\", "/");

            return result;
        }

        public static async void ClearCache(Action callback = null)
        {
            ClearCacheInProgress = true;
            try
            {
                string cachePath = GetMaterializeFolder();
                if (Directory.Exists(cachePath))
                {
                    List<Asset> keepAssets = DBAdapter.DB.Table<Asset>().Where(a => a.KeepExtracted).ToList();
                    List<string> keepPaths = keepAssets.Select(a => GetMaterializedAssetPath(a).ToLowerInvariant()).ToList();

                    // go through 1 by 1 to keep persisted packages in the cache
                    string[] packages = Directory.GetDirectories(cachePath);
                    foreach (string package in packages)
                    {
                        if (keepPaths.Contains(package.ToLowerInvariant())) continue;
                        await IOUtils.DeleteFileOrDirectory(package);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not delete full cache directory: {e.Message}");
            }

            ClearCacheInProgress = false;
            callback?.Invoke();
        }

        private static void LoadConfig()
        {
            string configLocation = GetConfigLocation();
            UsedConfigLocation = configLocation;

            if (configLocation == null || !File.Exists(configLocation))
            {
                _config = new AssetInventorySettings();
                return;
            }

            ConfigErrors.Clear();
            _config = JsonConvert.DeserializeObject<AssetInventorySettings>(File.ReadAllText(configLocation), new JsonSerializerSettings
            {
                Error = delegate(object _, ErrorEventArgs args)
                {
                    ConfigErrors.Add(args.ErrorContext.Error.Message);

                    Debug.LogError("Invalid config file format: " + args.ErrorContext.Error.Message);
                    args.ErrorContext.Handled = true;
                }
            });
            if (_config == null) _config = new AssetInventorySettings();
            if (_config.folders == null) _config.folders = new List<FolderSpec>();

            // ensure all paths are in the correct format
            _config.folders.ForEach(f => f.location = f.location?.Replace("\\", "/"));
        }

        public static void SaveConfig()
        {
            string configFile = GetConfigLocation();
            if (configFile == null) return;

            try
            {
                File.WriteAllText(configFile, JsonConvert.SerializeObject(_config, Formatting.Indented));
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not persist configuration. It might be locked by another application: {e.Message}");
            }
        }

        public static void ResetConfig()
        {
            DBAdapter.Close(); // in case DB path changes

            _config = new AssetInventorySettings();
            SaveConfig();
            AssetDatabase.Refresh();
        }

        public static async Task<AssetPurchases> FetchOnlineAssets()
        {
            AssetStore.CancellationRequested = false;
            AssetPurchases assets = await AssetStore.RetrievePurchases();
            if (assets == null) return null; // happens if token was invalid 

            CurrentMain = "Phase 2/3: Updating purchases";
            MainCount = assets.results.Count;
            MainProgress = 1;
            int progressId = MetaProgress.Start("Updating purchases");

            // store for later troubleshooting
            File.WriteAllText(Path.Combine(GetStorageFolder(), DIAG_PURCHASES), JsonConvert.SerializeObject(assets, Formatting.Indented));

            bool tagsChanged = false;
            try
            {
                for (int i = 0; i < MainCount; i++)
                {
                    MainProgress = i + 1;
                    MetaProgress.Report(progressId, i + 1, MainCount, string.Empty);
                    if (i % 50 == 0) await Task.Yield(); // let editor breath
                    if (AssetStore.CancellationRequested) break;

                    AssetPurchase purchase = assets.results[i];

                    // update all known assets with that foreignId to support updating duplicate assets as well 
                    List<Asset> existingAssets = DBAdapter.DB.Table<Asset>().Where(a => a.ForeignId == purchase.packageId).ToList();
                    if (existingAssets.Count == 0 || existingAssets.Count(a => a.AssetSource == Asset.Source.AssetStorePackage || (a.AssetSource == Asset.Source.RegistryPackage && a.ForeignId > 0)) == 0)
                    {
                        // create new asset on-demand or if only available as custom asset so far
                        Asset asset = purchase.ToAsset();
                        asset.SafeName = purchase.CalculatedSafeName;
                        if (Config.excludeByDefault) asset.Exclude = true;
                        if (Config.extractByDefault) asset.KeepExtracted = true;
                        if (Config.backupByDefault) asset.Backup = true;
                        DBAdapter.DB.Insert(asset);
                        existingAssets.Add(asset);
                    }

                    for (int i2 = 0; i2 < existingAssets.Count; i2++)
                    {
                        Asset asset = existingAssets[i2];

                        // temporarily store guessed safe name to ensure locally indexed files are mapped correctly
                        // will be overridden in detail run
                        asset.DisplayName = purchase.displayName.Trim();
                        asset.ForeignId = purchase.packageId;
                        if (!string.IsNullOrEmpty(purchase.grantTime))
                        {
                            if (DateTime.TryParse(purchase.grantTime, out DateTime result))
                            {
                                asset.PurchaseDate = result;
                            }
                        }

                        if (string.IsNullOrEmpty(asset.SafeName)) asset.SafeName = purchase.CalculatedSafeName;

                        // override data with local truth in case header information exists
                        if (File.Exists(asset.GetLocation(true)))
                        {
                            AssetHeader header = UnityPackageImporter.ReadHeader(asset.GetLocation(true), true);
                            UnityPackageImporter.ApplyHeader(header, asset);
                        }

                        DBAdapter.DB.Update(asset);

                        // handle tags
                        if (purchase.tagging != null)
                        {
                            foreach (string tag in purchase.tagging)
                            {
                                if (AddTagAssignment(asset.Id, tag, TagAssignment.Target.Package, true)) tagsChanged = true;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not update purchases: {e.Message}");
            }

            if (tagsChanged)
            {
                LoadTags();
                LoadTagAssignments();
            }

            CurrentMain = null;
            MetaProgress.Remove(progressId);

            return assets;
        }

        public static async Task FetchAssetsDetails(bool forceUpdate = false, int assetId = 0, bool skipProgress = false)
        {
            if (forceUpdate)
            {
                DBAdapter.DB.Execute("update Asset set ETag=null, LastOnlineRefresh=0" + (assetId > 0 ? " where Id=" + assetId : string.Empty));
            }

            IEnumerable<Asset> dbAssets = DBAdapter.DB.Table<Asset>()
                .Where(a => a.ForeignId > 0)
                .ToList();

            if (assetId > 0)
            {
                dbAssets = dbAssets.Where(a => a.Id == assetId);
            }
            else
            {
                dbAssets = dbAssets.Where(a => (DateTime.Now - a.LastOnlineRefresh).TotalDays >= Config.assetStoreRefreshCycle);
            }
            List<Asset> assets = dbAssets.OrderBy(a => a.LastOnlineRefresh).ToList();

            if (!skipProgress)
            {
                CurrentMain = "Phase 3/3: Updating package details";
                MainCount = assets.Count;
                MainProgress = 1;
            }
            int progressId = MetaProgress.Start("Updating package details");
            string previewFolder = GetPreviewFolder();

            for (int i = 0; i < assets.Count; i++)
            {
                Asset asset = assets[i];
                int id = asset.ForeignId;

                if (!skipProgress) MainProgress = i + 1;
                MetaProgress.Report(progressId, i + 1, MainCount, string.Empty);
                if (i % 5 == 0) await Task.Yield(); // let editor breath
                if (!skipProgress && AssetStore.CancellationRequested) break;

                AssetDetails details = await AssetStore.RetrieveAssetDetails(id, asset.ETag);
                if (details == null) // happens if unchanged through etag
                {
                    asset.LastOnlineRefresh = DateTime.Now;
                    DBAdapter.DB.Update(asset);
                    continue;
                }
                if (!string.IsNullOrEmpty(details.packageName) && asset.AssetSource != Asset.Source.RegistryPackage)
                {
                    // special case of registry packages listed on asset store
                    // registry package could already exist so make sure to only have one entry
                    Asset existing = DBAdapter.DB.Find<Asset>(a => a.SafeName == details.packageName && a.AssetSource == Asset.Source.RegistryPackage);
                    if (existing != null)
                    {
                        DBAdapter.DB.Delete(asset);
                        assets[i] = existing;
                        asset = existing;
                    }
                    asset.AssetSource = Asset.Source.RegistryPackage;
                    asset.SafeName = details.packageName;
                    asset.ForeignId = id;
                }

                // check if disabled, then download links are not available anymore, deprecated would still work
                DownloadInfo downloadDetails = null;
                if (asset.AssetSource == Asset.Source.AssetStorePackage && details.state != "disabled")
                {
                    downloadDetails = await AssetStore.RetrieveAssetDownloadInfo(id, code =>
                    {
                        // if unauthorized then seat was removed again for that user, mark asset as custom
                        if (code == 403)
                        {
                            asset.AssetSource = Asset.Source.CustomPackage;
                            DBAdapter.DB.Execute("UPDATE Asset set AssetSource=? where Id=?", Asset.Source.CustomPackage, asset.Id);

                            Debug.Log($"No more access to {asset}. Seat was probably removed. Switching asset source to custom and disabling download possibility.");
                        }
                    });
                    if (asset.AssetSource == Asset.Source.AssetStorePackage && (downloadDetails == null || string.IsNullOrEmpty(downloadDetails.filename_safe_package_name)))
                    {
                        Debug.Log($"Could not fetch download detail information for '{asset.SafeName}'");
                        continue;
                    }
                }

                // reload asset to ensure working on latest copy, otherwise might lose package size information
                if (downloadDetails != null)
                {
                    asset.UploadId = downloadDetails.upload_id;
                    asset.SafeName = downloadDetails.filename_safe_package_name;
                    asset.SafeCategory = downloadDetails.filename_safe_category_name;
                    asset.SafePublisher = downloadDetails.filename_safe_publisher_name;
                    asset.OriginalLocation = downloadDetails.url;
                    asset.OriginalLocationKey = downloadDetails.key;
                    if (asset.AssetSource == Asset.Source.AssetStorePackage && !string.IsNullOrEmpty(asset.GetLocation(true)) && asset.GetCalculatedLocation().ToLower() != asset.GetLocation(true).ToLower())
                    {
                        asset.CurrentSubState = Asset.SubState.Outdated;
                    }
                    else
                    {
                        asset.CurrentSubState = Asset.SubState.None;
                    }
                }

                asset.LastOnlineRefresh = DateTime.Now;
                asset.OfficialState = details.state;
                asset.ETag = details.ETag;
                asset.DisplayName = details.name;
                asset.DisplayPublisher = details.productPublisher?.name;
                asset.DisplayCategory = details.category?.name;
                if (details.properties != null && details.properties.ContainsKey("firstPublishedDate") && DateTime.TryParse(details.properties["firstPublishedDate"], out DateTime firstPublishedDate))
                {
                    asset.FirstRelease = firstPublishedDate;
                }
                if (int.TryParse(details.publisherId, out int publisherId)) asset.PublisherId = publisherId;

                // prices
                if (details.productRatings != null)
                {
                    NumberStyles style = NumberStyles.Number;
                    CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");

                    AssetPrice eurPrice = details.productRatings.FirstOrDefault(r => r.currency.ToLowerInvariant() == "eur");
                    if (eurPrice != null && float.TryParse(eurPrice.finalPrice, style, culture, out float eur)) asset.PriceEur = eur;
                    AssetPrice usdPrice = details.productRatings.FirstOrDefault(r => r.currency.ToLowerInvariant() == "usd");
                    if (usdPrice != null && float.TryParse(usdPrice.finalPrice, style, culture, out float usd)) asset.PriceUsd = usd;
                    AssetPrice yenPrice = details.productRatings.FirstOrDefault(r => r.currency.ToLowerInvariant() == "cny");
                    if (yenPrice != null && float.TryParse(yenPrice.finalPrice, style, culture, out float yen)) asset.PriceCny = yen;
                }

                if (string.IsNullOrEmpty(asset.SafeName)) asset.SafeName = AssetUtils.GuessSafeName(details.name);
                asset.Description = details.description;
                asset.Requirements = string.Join(", ", details.requirements);
                asset.Keywords = string.Join(", ", details.keyWords);
                asset.SupportedUnityVersions = string.Join(", ", details.supportedUnityVersions);
                asset.Revision = details.revision;
                asset.Slug = details.slug;
                asset.LatestVersion = details.version.name;
                asset.LastRelease = details.version.publishedDate;
                if (details.productReview != null)
                {
                    asset.AssetRating = details.productReview.ratingAverage;
                    asset.RatingCount = int.Parse(details.productReview.ratingCount);
                    if (float.TryParse(details.productReview.hotness, NumberStyles.Float, CultureInfo.InvariantCulture, out float hotness)) asset.Hotness = hotness;
                }

                asset.CompatibilityInfo = details.compatibilityInfo;
                asset.ReleaseNotes = details.publishNotes;
                asset.KeyFeatures = details.keyFeatures;
                if (asset.PackageSize == 0 && details.uploads != null)
                {
                    // use size of download for latest Unity version
                    KeyValuePair<string, UploadInfo> upload = details.uploads
                        .OrderBy(pair => new SemVer(pair.Key))
                        .LastOrDefault();
                    if (upload.Value != null && int.TryParse(upload.Value.downloadSize, out int size))
                    {
                        asset.PackageSize = size;
                    }
                }

                // linked but not-purchased packages should not contain null for safe_names for search filters to work
                if (downloadDetails == null && asset.AssetSource == Asset.Source.CustomPackage)
                {
                    if (string.IsNullOrWhiteSpace(asset.SafePublisher)) asset.SafePublisher = asset.DisplayPublisher;
                    if (string.IsNullOrWhiteSpace(asset.SafeCategory)) asset.SafeCategory = asset.DisplayCategory;
                }

                // override data with local truth in case header information exists
                if (File.Exists(asset.GetLocation(true)))
                {
                    AssetHeader header = UnityPackageImporter.ReadHeader(asset.GetLocation(true), true);
                    UnityPackageImporter.ApplyHeader(header, asset);
                }

                DBAdapter.DB.Update(asset);
                PersistMedia(asset, details);

                // load package icon on demand
                string icon = details.mainImage?.icon;
                if (!string.IsNullOrWhiteSpace(icon) && string.IsNullOrWhiteSpace(asset.GetPreviewFile(previewFolder)))
                {
#pragma warning disable CS4014
                    AssetUtils.LoadImageAsync(icon, asset.GetPreviewFile(previewFolder, false)).ContinueWith(task =>
#pragma warning restore CS4014
                    {
                        if (task.Exception != null)
                        {
                            Debug.LogError($"Failed to download image from {icon}: {task.Exception.Message}");
                        }
                        else
                        {
                            OnPackageImageLoaded?.Invoke(asset);
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
            }

            if (!skipProgress) CurrentMain = null;
            MetaProgress.Remove(progressId);
            OnPackagesUpdated?.Invoke();
        }

        private static void PersistMedia(Asset asset, AssetDetails details)
        {
            List<AssetMedia> existing = DBAdapter.DB.Query<AssetMedia>("select * from AssetMedia where AssetId=?", asset.Id).ToList();

            // handle main image
            if (!string.IsNullOrWhiteSpace(details.mainImage?.url)) StoreMedia(existing, new AssetMedia {AssetId = asset.Id, Type = "main", Url = details.mainImage.url});
            if (!string.IsNullOrWhiteSpace(details.mainImage?.icon)) StoreMedia(existing, new AssetMedia {AssetId = asset.Id, Type = "icon", Url = details.mainImage.icon});
            if (!string.IsNullOrWhiteSpace(details.mainImage?.icon25)) StoreMedia(existing, new AssetMedia {AssetId = asset.Id, Type = "icon25", Url = details.mainImage.icon25});
            if (!string.IsNullOrWhiteSpace(details.mainImage?.icon75)) StoreMedia(existing, new AssetMedia {AssetId = asset.Id, Type = "icon75", Url = details.mainImage.icon75});
            if (!string.IsNullOrWhiteSpace(details.mainImage?.small)) StoreMedia(existing, new AssetMedia {AssetId = asset.Id, Type = "small", Url = details.mainImage.small});
            if (!string.IsNullOrWhiteSpace(details.mainImage?.small_v2)) StoreMedia(existing, new AssetMedia {AssetId = asset.Id, Type = "small_v2", Url = details.mainImage.small_v2});
            if (!string.IsNullOrWhiteSpace(details.mainImage?.big)) StoreMedia(existing, new AssetMedia {AssetId = asset.Id, Type = "big", Url = details.mainImage.big});
            if (!string.IsNullOrWhiteSpace(details.mainImage?.big_v2)) StoreMedia(existing, new AssetMedia {AssetId = asset.Id, Type = "big_v2", Url = details.mainImage.big_v2});
            if (!string.IsNullOrWhiteSpace(details.mainImage?.facebook)) StoreMedia(existing, new AssetMedia {AssetId = asset.Id, Type = "facebook", Url = details.mainImage.facebook});

            // handle screenshots & videos
            for (int i = 0; i < details.images.Length; i++)
            {
                AssetImage img = details.images[i];
                StoreMedia(existing, new AssetMedia {Order = i, AssetId = asset.Id, Type = img.type, Url = img.imageUrl, ThumbnailUrl = img.thumbnailUrl, Width = img.width, Height = img.height, WebpUrl = img.webpUrl});
            }

            // TODO: remove outdated
        }

        private static void StoreMedia(List<AssetMedia> existing, AssetMedia media)
        {
            AssetMedia match = existing.FirstOrDefault(m => m.Type == media.Type && m.Url == media.Url);
            if (match == null)
            {
                DBAdapter.DB.Insert(media);
                existing.Add(media);
            }
            else
            {
                media.Id = match.Id;
                DBAdapter.DB.Update(media);
            }
        }

        public static void LoadMedia(AssetInfo info)
        {
            if (info.ParentInfo != null)
            {
                LoadMedia(info.ParentInfo);
                info.AllMedia = info.ParentInfo.AllMedia;
                info.Media = info.ParentInfo.Media;
                return;
            }

            // when already downloading don't trigger again
            if (info.AllMedia != null && info.AllMedia.Any(m => m.IsDownloading)) return;

            info.AllMedia = DBAdapter.DB.Query<AssetMedia>("select * from AssetMedia where AssetId=? order by [Order]", info.AssetId).ToList();
            info.Media = info.AllMedia.Where(m => m.Type == "main" || m.Type == "screenshot" || m.Type == "youtube").ToList();
            DownloadMedia(info);
        }

        private static async void DownloadMedia(AssetInfo info)
        {
            List<AssetMedia> files = info.Media.Where(m => !m.IsDownloading).OrderBy(m => m.Order).ToList();
            for (int i = 0; i < files.Count; i++)
            {
                if (info.Media == null) return; // happens when cancelled

                // thumbnail
                if (!string.IsNullOrWhiteSpace(files[i].ThumbnailUrl))
                {
                    string thumbnailFile = info.ToAsset().GetMediaThumbnailFile(files[i], GetPreviewFolder(), false);
                    if (!File.Exists(thumbnailFile))
                    {
                        if (info.Media == null) return; // happens when cancelled
                        info.Media[i].IsDownloading = true;
                        await AssetUtils.LoadImageAsync(files[i].ThumbnailUrl, thumbnailFile);
                        if (info.Media == null) return; // happens when cancelled
                        info.Media[i].IsDownloading = false;
                    }
                    if (File.Exists(thumbnailFile)) files[i].ThumbnailTexture = await AssetUtils.LoadLocalTexture(thumbnailFile, false);
                }

                // full
                if (files[i].Type != "youtube" && !string.IsNullOrWhiteSpace(files[i].Url))
                {
                    string targetFile = info.ToAsset().GetMediaFile(files[i], GetPreviewFolder(), false);
                    if (!File.Exists(targetFile))
                    {
                        if (info.Media == null) return; // happens when cancelled
                        info.Media[i].IsDownloading = true;
                        await AssetUtils.LoadImageAsync(files[i].Url, targetFile);
                        if (info.Media == null) return; // happens when cancelled
                        info.Media[i].IsDownloading = false;
                    }
                    if (File.Exists(targetFile)) files[i].Texture = await AssetUtils.LoadLocalTexture(targetFile, false);
                }
            }
        }

        public static int CountPurchasedAssets(IEnumerable<AssetInfo> assets)
        {
            return assets.Count(a => a.ParentId == 0 && (a.AssetSource == Asset.Source.AssetStorePackage || (a.AssetSource == Asset.Source.RegistryPackage && a.ForeignId > 0)));
        }

        public static void MoveDatabase(string targetFolder)
        {
            string targetDBFile = Path.Combine(targetFolder, Path.GetFileName(DBAdapter.GetDBPath()));
            if (File.Exists(targetDBFile)) File.Delete(targetDBFile);
            string oldStorageFolder = GetStorageFolder();
            DBAdapter.Close();

            bool success = false;
            try
            {
                // for safety copy first, then delete old state after everything is done
                EditorUtility.DisplayProgressBar("Moving Database", "Copying database to new location...", 0.2f);
                File.Copy(DBAdapter.GetDBPath(), targetDBFile);
                EditorUtility.ClearProgressBar();

                EditorUtility.DisplayProgressBar("Moving Preview Images", "Copying preview images to new location...", 0.4f);
                IOUtils.CopyDirectory(GetPreviewFolder(), GetPreviewFolder(targetFolder, true));
                EditorUtility.ClearProgressBar();

                // set new location
                SwitchDatabase(targetFolder);
                success = true;
            }
            catch
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Error Moving Data",
                    "There were errors moving the existing database to a new location. Check the error log for details. Current database location remains unchanged.",
                    "OK");
            }

            if (success)
            {
                EditorUtility.DisplayProgressBar("Freeing Up Space", "Removing backup files from old location...", 0.8f);
                Directory.Delete(oldStorageFolder, true);
                EditorUtility.ClearProgressBar();
            }
        }

        public static void SwitchDatabase(string targetFolder)
        {
            DBAdapter.Close();
            AssetUtils.ClearCache();
            Config.customStorageLocation = targetFolder;
            SaveConfig();

            InitDone = false;
            Init();
        }

        public static void ForgetAssetFile(AssetInfo info)
        {
            DBAdapter.DB.Execute("DELETE from AssetFile where Id=?", info.Id);
        }

        public static Asset ForgetPackage(AssetInfo info, bool removeExclusion = false)
        {
            DBAdapter.DB.Execute("DELETE from AssetFile where AssetId=?", info.AssetId);

            Asset existing = DBAdapter.DB.Find<Asset>(info.AssetId);
            if (existing == null) return null;

            existing.CurrentState = Asset.State.New;
            info.CurrentState = Asset.State.New;
            existing.LastOnlineRefresh = DateTime.MinValue;
            info.LastOnlineRefresh = DateTime.MinValue;
            existing.ETag = null;
            info.ETag = null;
            if (removeExclusion)
            {
                existing.Exclude = false;
                info.Exclude = false;
            }

            DBAdapter.DB.Update(existing);

            return existing;
        }

        public static void RemovePackage(AssetInfo info, bool deleteFiles)
        {
            // delete child packages first
            foreach (AssetInfo childInfo in info.ChildInfos)
            {
                RemovePackage(childInfo, deleteFiles);
            }

            if (deleteFiles && info.ParentId == 0)
            {
                if (File.Exists(info.GetLocation(true))) File.Delete(info.GetLocation(true));
                if (Directory.Exists(info.GetLocation(true))) Directory.Delete(info.GetLocation(true), true);
            }
            string previewFolder = Path.Combine(GetPreviewFolder(), info.AssetId.ToString());
            if (Directory.Exists(previewFolder)) Directory.Delete(previewFolder, true);

            Asset existing = ForgetPackage(info);
            if (existing == null) return;

            DBAdapter.DB.Execute("DELETE from AssetMedia where AssetId=?", info.AssetId);
            DBAdapter.DB.Execute("DELETE from TagAssignment where TagTarget=? and TargetId=?", TagAssignment.Target.Package, info.AssetId);
            DBAdapter.DB.Execute("DELETE from Asset where Id=?", info.AssetId);
        }

        public static async Task<string> CopyTo(AssetInfo info, string selectedPath, bool withDependencies = false, bool withScripts = false, bool fromDragDrop = false)
        {
            string result = null;
            string sourcePath = await EnsureMaterializedAsset(info);
            bool conversionNeeded = false;
            if (sourcePath != null)
            {
                string finalPath = selectedPath;

                // complex import structure only supported for Unity Packages
                int finalImportStructure = info.AssetSource == Asset.Source.CustomPackage ||
                    info.AssetSource == Asset.Source.Archive ||
                    info.AssetSource == Asset.Source.AssetStorePackage
                        ? Config.importStructure
                        : 0;

                // calculate dependencies on demand
                while (info.DependencyState == AssetInfo.DependencyStateOptions.Calculating) await Task.Yield();
                if (withDependencies && info.DependencyState == AssetInfo.DependencyStateOptions.Unknown)
                {
                    await CalculateDependencies(info);
                }

                // override again for single files without dependencies in drag & drop scenario as that feels more natural
                if (fromDragDrop && (info.Dependencies == null || info.Dependencies.Count == 0)) finalImportStructure = 0;

                switch (finalImportStructure)
                {
                    case 0:
                        // put into subfolder if multiple files are affected
                        if (withDependencies && info.Dependencies != null && info.Dependencies.Count > 0)
                        {
                            finalPath = Path.Combine(finalPath.RemoveTrailing("."), Path.GetFileNameWithoutExtension(info.FileName)).Trim().RemoveTrailing(".");
                            if (!Directory.Exists(finalPath)) Directory.CreateDirectory(finalPath);
                        }

                        break;

                    case 1:
                        string path = info.Path;
                        if (path.ToLowerInvariant().StartsWith("assets/")) path = path.Substring(7);
                        finalPath = Path.Combine(selectedPath, Path.GetDirectoryName(path));
                        break;
                }

                string targetPath = Path.Combine(finalPath, Path.GetFileName(sourcePath));
                DoCopyTo(sourcePath, targetPath);
                result = targetPath;
                if (ConversionExtensions.Contains(IOUtils.GetExtensionWithoutDot(targetPath).ToLowerInvariant())) conversionNeeded = true;

                if (withDependencies)
                {
                    List<AssetFile> deps = withScripts ? info.Dependencies : info.MediaDependencies;
                    if (deps != null)
                    {
                        for (int i = 0; i < deps.Count; i++)
                        {
                            if (ConversionExtensions.Contains(IOUtils.GetExtensionWithoutDot(deps[i].FileName).ToLowerInvariant())) conversionNeeded = true;

                            // check if already in target
                            if (!string.IsNullOrEmpty(deps[i].Guid))
                            {
                                string assetPath = AssetDatabase.GUIDToAssetPath(deps[i].Guid);
                                if (!string.IsNullOrWhiteSpace(assetPath) && File.Exists(assetPath)) continue;
                            }

                            sourcePath = await EnsureMaterializedAsset(info.ToAsset(), deps[i]);
                            if (sourcePath != null)
                            {
                                switch (finalImportStructure)
                                {
                                    case 0:
                                        targetPath = Path.Combine(finalPath, Path.GetFileName(deps[i].Path));
                                        break;

                                    case 1:
                                        string path = deps[i].Path;
                                        if (path.ToLowerInvariant().StartsWith("assets/")) path = path.Substring(7);
                                        targetPath = Path.Combine(selectedPath, path);
                                        break;
                                }

                                DoCopyTo(sourcePath, targetPath);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError($"Dependency calculation failed for {info}");
                    }
                }

                AssetDatabase.Refresh();

                if (string.IsNullOrEmpty(info.Guid))
                {
                    // special case of original index without GUID, fall back to file check only
                    if (File.Exists(targetPath)) info.ProjectPath = targetPath;
                }
                else
                {
                    info.ProjectPath = AssetDatabase.GUIDToAssetPath(info.Guid);
                }

                if (Config.convertToPipeline && conversionNeeded)
                {
#if USE_URP_CONVERTER
                    if (AssetUtils.IsOnURP())
                    {
                        Converters.RunInBatchMode(
                            ConverterContainerId.BuiltInToURP
                            , new List<ConverterId>
                            {
                                ConverterId.Material,
                                ConverterId.ReadonlyMaterial
                            }
                            , ConverterFilter.Inclusive
                        );
                    }
#endif
                }

                Config.statsImports++;
                SaveConfig();
            }

            return result;
        }

        private static void DoCopyTo(string sourcePath, string targetPath)
        {
            string targetFolder = Path.GetDirectoryName(targetPath);
            if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);
            File.Copy(sourcePath, targetPath, true);

            string sourceMetaPath = sourcePath + ".meta";
            string targetMetaPath = targetPath + ".meta";
            if (File.Exists(sourceMetaPath)) File.Copy(sourceMetaPath, targetMetaPath, true);
        }

        public static async Task PlayAudio(AssetInfo info)
        {
            string targetPath;

            // check if in project already, then skip extraction
            if (info.InProject)
            {
                targetPath = IOUtils.PathCombine(Path.GetDirectoryName(Application.dataPath), info.ProjectPath);
            }
            else
            {
                targetPath = await EnsureMaterializedAsset(info, Config.extractSingleFiles);
            }

            EditorAudioUtility.StopAllPreviewClips();
            if (targetPath != null)
            {
                AudioClip clip = await AssetUtils.LoadAudioFromFile(targetPath);
                if (clip != null) EditorAudioUtility.PlayPreviewClip(clip, 0, Config.loopAudio);
            }
        }

        public static void SetAssetExclusion(AssetInfo info, bool exclude)
        {
            Asset asset = DBAdapter.DB.Find<Asset>(info.AssetId);
            if (asset == null) return;

            asset.Exclude = exclude;
            info.Exclude = exclude;

            DBAdapter.DB.Update(asset);
        }

        public static void SetAssetBackup(AssetInfo info, bool backup)
        {
            Asset asset = DBAdapter.DB.Find<Asset>(info.AssetId);
            if (asset == null) return;

            asset.Backup = backup;
            info.Backup = backup;

            DBAdapter.DB.Update(asset);
        }

        public static bool ShowAdvanced()
        {
            return !Config.hideAdvanced || Event.current.control;
        }

        public static void SetVersion(AssetInfo info, string version)
        {
            Asset asset = DBAdapter.DB.Find<Asset>(info.AssetId);
            if (asset == null) return;

            asset.Version = version;
            info.Version = version;

            DBAdapter.DB.Update(asset);
        }

        public static void SetPackageVersion(AssetInfo info, PackageInfo package)
        {
            Asset asset = DBAdapter.DB.Find<Asset>(info.AssetId);
            if (asset == null) return;

            asset.LatestVersion = package.versions.latestCompatible;
            info.LatestVersion = package.versions.latestCompatible;

            DBAdapter.DB.Update(asset);
        }

        public static void SetAssetExtraction(AssetInfo info, bool extract)
        {
            Asset asset = DBAdapter.DB.Find<Asset>(info.AssetId);
            if (asset == null) return;

            asset.KeepExtracted = extract;
            info.KeepExtracted = extract;

            if (extract)
            {
#pragma warning disable CS4014
                EnsureMaterializedAsset(info.ToAsset());
#pragma warning restore CS4014
            }

            DBAdapter.DB.Update(asset);
        }

        public static void SetAssetUpdateStrategy(AssetInfo info, Asset.Strategy strategy)
        {
            Asset asset = DBAdapter.DB.Find<Asset>(info.AssetId);
            if (asset == null) return;

            asset.UpdateStrategy = strategy;
            info.UpdateStrategy = strategy;

            DBAdapter.DB.Update(asset);
        }

        public static bool AddTagAssignment(int targetId, string tag, TagAssignment.Target target, bool fromAssetStore = false)
        {
            Tag existingT = AddTag(tag, fromAssetStore);
            if (existingT == null) return false;

            TagAssignment existingA = DBAdapter.DB.Find<TagAssignment>(t => t.TagId == existingT.Id && t.TargetId == targetId && t.TagTarget == target);
            if (existingA != null) return false; // already added

            TagAssignment newAssignment = new TagAssignment(existingT.Id, target, targetId);
            DBAdapter.DB.Insert(newAssignment);

            return true;
        }

        public static bool AddTagAssignment(AssetInfo info, string tag, TagAssignment.Target target)
        {
            if (!AddTagAssignment(target == TagAssignment.Target.Asset ? info.Id : info.AssetId, tag, target)) return false;

            LoadTagAssignments(info);

            return true;
        }

        public static void RemoveTagAssignment(AssetInfo info, TagInfo tagInfo, bool autoReload = true)
        {
            DBAdapter.DB.Delete<TagAssignment>(tagInfo.Id);

            if (autoReload) LoadTagAssignments(info);
        }

        public static void RemoveAssetTagAssignment(List<AssetInfo> infos, string name)
        {
            infos.ForEach(info =>
            {
                TagInfo tagInfo = info.AssetTags?.Find(t => t.Name == name);
                if (tagInfo == null) return;
                RemoveTagAssignment(info, tagInfo, false);
                info.AssetTags.RemoveAll(t => t.Name == name);
                info.SetTagsDirty();
            });
            LoadTagAssignments();
        }

        public static void RemovePackageTagAssignment(List<AssetInfo> infos, string name)
        {
            infos.ForEach(info =>
            {
                TagInfo tagInfo = info.PackageTags?.Find(t => t.Name == name);
                if (tagInfo == null) return;
                RemoveTagAssignment(info, tagInfo, false);
                info.PackageTags.RemoveAll(t => t.Name == name);
                info.SetTagsDirty();
            });
            LoadTagAssignments();
        }

        public static void LoadTagAssignments(AssetInfo info = null)
        {
            string dataQuery = "SELECT *, TagAssignment.Id as Id from TagAssignment inner join Tag on Tag.Id = TagAssignment.TagId order by TagTarget, TargetId";
            _tags = DBAdapter.DB.Query<TagInfo>($"{dataQuery}").ToList();
            TagHash = Random.Range(0, int.MaxValue);

            info?.SetTagsDirty();
            OnTagsChanged?.Invoke();
        }

        public static List<TagInfo> GetAssetTags(int assetFileId)
        {
            return Tags?.Where(t => t.TagTarget == TagAssignment.Target.Asset && t.TargetId == assetFileId)
                .OrderBy(t => t.Name).ToList();
        }

        public static List<TagInfo> GetPackageTags(int assetId)
        {
            return Tags?.Where(t => t.TagTarget == TagAssignment.Target.Package && t.TargetId == assetId)
                .OrderBy(t => t.Name).ToList();
        }

        public static void SaveTag(Tag tag)
        {
            DBAdapter.DB.Update(tag);
            LoadTagAssignments();
        }

        public static Tag AddTag(string name, bool fromAssetStore = false)
        {
            name = name.Trim();
            if (string.IsNullOrWhiteSpace(name)) return null;

            Tag tag = DBAdapter.DB.Find<Tag>(t => t.Name.ToLower() == name.ToLower());
            if (tag == null)
            {
                tag = new Tag(name);
                tag.FromAssetStore = fromAssetStore;
                DBAdapter.DB.Insert(tag);

                OnTagsChanged?.Invoke();
            }
            else if (!tag.FromAssetStore && fromAssetStore)
            {
                tag.FromAssetStore = true;
                DBAdapter.DB.Update(tag); // don't trigger changed event in such cases, this is just for bookkeeping
            }

            return tag;
        }

        public static void RenameTag(Tag tag, string newName)
        {
            newName = newName.Trim();
            if (string.IsNullOrWhiteSpace(newName)) return;

            tag.Name = newName;
            DBAdapter.DB.Update(tag);
            LoadTagAssignments();
        }

        public static void DeleteTag(Tag tag)
        {
            DBAdapter.DB.Execute("DELETE from TagAssignment where TagId=?", tag.Id);
            DBAdapter.DB.Delete<Tag>(tag.Id);
            LoadTagAssignments();
        }

        public static List<Tag> LoadTags()
        {
            return DBAdapter.DB.Table<Tag>().ToList().OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static void LoadRelativeLocations()
        {
            string dataQuery = "SELECT * from RelativeLocation order by Key, Location";
            List<RelativeLocation> locations = DBAdapter.DB.Query<RelativeLocation>($"{dataQuery}").ToList();
            locations.ForEach(l => l.SetLocation(l.Location)); // ensure all paths use forward slashes

            string curSystem = GetSystemId();
            _relativeLocations = locations.Where(l => l.System == curSystem).ToList();

            // add predefined locations
            _relativeLocations.Insert(0, new RelativeLocation("ac", curSystem, GetAssetCacheFolder()));
            _relativeLocations.Insert(1, new RelativeLocation("pc", curSystem, GetPackageCacheFolder()));

            foreach (RelativeLocation location in locations.Where(l => l.System != curSystem))
            {
                // add key as undefined if not there
                if (!_relativeLocations.Any(rl => rl.Key == location.Key))
                {
                    _relativeLocations.Add(new RelativeLocation(location.Key, curSystem, null));
                }

                // add location inside other systems for reference
                RelativeLocation loc = _relativeLocations.First(rl => rl.Key == location.Key);
                if (loc.otherLocations == null) loc.otherLocations = new List<string>();
                loc.otherLocations.Add(location.Location);
            }

            // ensure never null
            _relativeLocations.ForEach(rl =>
            {
                if (rl.otherLocations == null) rl.otherLocations = new List<string>();
            });

            _userRelativeLocations = _relativeLocations.Where(rl => rl.Key != "ac" && rl.Key != "pc").ToList();
        }

        public static void ConnectToAssetStore(AssetInfo info, AssetDetails details)
        {
            Asset existing = DBAdapter.DB.Find<Asset>(info.AssetId);
            if (existing == null) return;

            existing.ETag = null;
            info.ETag = null;
            existing.ForeignId = int.Parse(details.packageId);
            info.ForeignId = int.Parse(details.packageId);
            existing.LastOnlineRefresh = DateTime.MinValue;
            info.LastOnlineRefresh = DateTime.MinValue;

            DBAdapter.DB.Update(existing);
        }

        public static void DisconnectFromAssetStore(AssetInfo info, bool removeMetadata)
        {
            Asset existing = DBAdapter.DB.Find<Asset>(info.AssetId);
            if (existing == null) return;

            existing.ForeignId = 0;
            info.ForeignId = 0;

            if (removeMetadata)
            {
                existing.AssetRating = null;
                info.AssetRating = null;
                existing.SafePublisher = null;
                info.SafePublisher = null;
                existing.DisplayPublisher = null;
                info.DisplayPublisher = null;
                existing.SafeCategory = null;
                info.SafeCategory = null;
                existing.DisplayCategory = null;
                info.DisplayCategory = null;
                existing.DisplayName = null;
                info.DisplayName = null;
                existing.OfficialState = null;
                info.OfficialState = null;
            }

            DBAdapter.DB.Update(existing);
        }

        public static string CreateDebugReport()
        {
            string result = "Asset Inventory Support Diagnostics\n";
            result += $"\nDate: {DateTime.Now}";
            result += $"\nVersion: {TOOL_VERSION}";
            result += $"\nUnity: {Application.unityVersion}";
            result += $"\nPlatform: {Application.platform}";
            result += $"\nOS: {Environment.OSVersion}";
            result += $"\nLanguage: {Application.systemLanguage}";

            List<AssetInfo> assets = LoadAssets();
            result += $"\n\n{assets.Count} Packages";
            foreach (AssetInfo asset in assets)
            {
                result += $"\n{asset} ({asset.SafeName}) - {asset.AssetSource} - {asset.GetVersion()}";
            }

            List<Tag> tags = LoadTags();
            result += $"\n\n{tags.Count} Tags";
            foreach (Tag tag in tags)
            {
                result += $"\n{tag} ({tag.Id})";
            }

            LoadTagAssignments();
            result += $"\n\n{_tags.Count} Tag Assignments";
            foreach (TagInfo tag in _tags)
            {
                result += $"\n{tag})";
            }

            return result;
        }

        public static string GetSystemId()
        {
            return SystemInfo.deviceUniqueIdentifier; // + "test";
        }

        public static bool IsRel(string path)
        {
            return path != null && path.StartsWith(TAG_START);
        }

        public static string GetRelKey(string path)
        {
            return path.Replace(TAG_START, "").Replace(TAG_END, "");
        }

        public static string DeRel(string path, bool emptyIfMissing = false)
        {
            if (path == null) return null;
            if (!IsRel(path)) return path;

            foreach (RelativeLocation location in RelativeLocations)
            {
                if (string.IsNullOrWhiteSpace(location.Location))
                {
                    if (emptyIfMissing) return null;
                    continue;
                }

                path = path.Replace($"{TAG_START}{location.Key}{TAG_END}", location.Location);
            }

            // check if some rule caught it
            if (IsRel(path) && emptyIfMissing) return null;

            return path;
        }

        public static string MakeRelative(string path)
        {
            path = path.Replace("\\", "/");
            for (int i = 0; i < RelativeLocations.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(RelativeLocations[i].Location)) continue;
                path = path.Replace(RelativeLocations[i].Location, $"{TAG_START}{RelativeLocations[i].Key}{TAG_END}");
            }

            return path;
        }

        public static AssetInfo GetAssetByPath(string path, Asset asset)
        {
            string query = "SELECT *, AssetFile.Id as Id from AssetFile left join Asset on Asset.Id = AssetFile.AssetId where Lower(AssetFile.Path) = ? and Asset.Id = ?";
            List<AssetInfo> result = DBAdapter.DB.Query<AssetInfo>(query, path.ToLowerInvariant(), asset.Id);

            return result.FirstOrDefault();
        }

        public static void RegisterSelection(List<AssetInfo> assets)
        {
            GetObserver().SetPrioritized(assets);
        }

        public static void TriggerPackageRefresh()
        {
            OnPackagesUpdated?.Invoke();
        }

#if USE_URP_CONVERTER
        public static void SetPipelineConversion(bool active)
        {
            Config.convertToPipeline = active;
            SaveConfig();
            
            SetupDefines();
        }
#endif

        // for debugging purposes
        private static void ScanMetaFiles()
        {
            string[] packages = Directory.GetFiles(GetMaterializeFolder(), "*.meta", SearchOption.AllDirectories);
            MainCount = packages.Length;
            for (int i = 0; i < packages.Length; i++)
            {
                string content = File.ReadAllText(packages[i]);
                MatchCollection matches = FileGuid.Matches(content);
                if (matches.Count <= 1) continue;
                string pathFile = Path.Combine(Path.GetDirectoryName(packages[i]), "pathname");
                if (!File.Exists(pathFile)) continue;

                string pathName = File.ReadAllText(pathFile);
                if (pathName.ToLowerInvariant().Contains("fbx")
                    || pathName.ToLowerInvariant().Contains("shadergraph")
                    || pathName.ToLowerInvariant().Contains("ttf")
                    || pathName.ToLowerInvariant().Contains("otf")
                    || pathName.ToLowerInvariant().Contains("cs")
                    || pathName.ToLowerInvariant().Contains("png")
                    || pathName.ToLowerInvariant().Contains("obj")
                    || pathName.ToLowerInvariant().Contains("uxml")
                    || pathName.ToLowerInvariant().Contains("js")
                    || pathName.ToLowerInvariant().Contains("uss")
                    || pathName.ToLowerInvariant().Contains("nn")
                    || pathName.ToLowerInvariant().Contains("tss")
                    || pathName.ToLowerInvariant().Contains("inputactions")
                    || pathName.ToLowerInvariant().Contains("shader")) continue;

                Debug.Log($"Found meta file with multiple guids: {packages[i]}");
                break;
            }
        }
    }
}