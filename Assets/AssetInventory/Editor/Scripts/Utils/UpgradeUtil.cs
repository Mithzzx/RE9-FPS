using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AssetInventory
{
    public static class UpgradeUtil
    {
        public const int CURRENT_CONFIG_VERSION = 2;
        private const int CURRENT_DB_VERSION = 14;

        public static bool LongUpgradeRequired { get; private set; }
        private static List<string> PendingUpgrades { get; set; } = new List<string>();

        public static void PerformUpgrades()
        {
            // filename was introduced in version 2
            AppProperty dbVersion = DBAdapter.DB.Find<AppProperty>("Version");
            int oldVersion;

            AppProperty requireUpgrade = DBAdapter.DB.Find<AppProperty>("UpgradeRequired");
            LongUpgradeRequired = requireUpgrade != null && requireUpgrade.Value.ToLowerInvariant() == "true";

            if (dbVersion == null)
            {
                // Upgrade from Initial to v2
                // add filenames to DB
                List<AssetFile> assetFiles = DBAdapter.DB.Table<AssetFile>().ToList();
                foreach (AssetFile assetFile in assetFiles)
                {
                    assetFile.FileName = Path.GetFileName(assetFile.Path);
                }
                DBAdapter.DB.UpdateAll(assetFiles);
                oldVersion = CURRENT_DB_VERSION;
            }
            else
            {
                oldVersion = int.Parse(dbVersion.Value);
            }
            if (oldVersion < 5)
            {
                // force re-fetching of asset details to get state
                DBAdapter.DB.Execute("update Asset set ETag=null, LastOnlineRefresh=0");
                LongUpgradeRequired = true;

                // change how colors are indexed
                if (DBAdapter.ColumnExists("AssetFile", "DominantColor")) DBAdapter.DB.Execute("alter table AssetFile drop column DominantColor");
                if (DBAdapter.ColumnExists("AssetFile", "DominantColorGroup")) DBAdapter.DB.Execute("alter table AssetFile drop column DominantColorGroup");

                requireUpgrade = new AppProperty("UpgradeRequired", "true");
                DBAdapter.DB.InsertOrReplace(requireUpgrade);

                AppProperty upgradeType = new AppProperty("UpgradeType-PreviewConversion", "true");
                DBAdapter.DB.InsertOrReplace(upgradeType);
            }
            if (oldVersion < 6)
            {
                DBAdapter.DB.Execute("update AssetFile set Hue=-1");
            }
            if (oldVersion < 7)
            {
                if (DBAdapter.ColumnExists("AssetFile", "PreviewFile"))
                {
                    DBAdapter.DB.Execute("alter table AssetFile drop column PreviewFile");
                    DBAdapter.DB.Execute("update AssetFile set PreviewState=99 where PreviewState=0");
                    DBAdapter.DB.Execute("update AssetFile set PreviewState=0 where PreviewState=1");
                    DBAdapter.DB.Execute("update AssetFile set PreviewState=1 where PreviewState=99");
                }
            }
            if (oldVersion < 8)
            {
                if (DBAdapter.ColumnExists("AssetFile", "PreviewImage"))
                {
                    DBAdapter.DB.Execute("alter table AssetFile drop column PreviewImage");
                }
            }
            if (oldVersion < 9)
            {
                if (DBAdapter.ColumnExists("Asset", "PreferredVersion"))
                {
                    DBAdapter.DB.Execute("alter table Asset drop column PreferredVersion");
                }
            }
            if (oldVersion < 10)
            {
                // force re-fetching of asset details to get new state
                DBAdapter.DB.Execute("update Asset set ETag=null, LastOnlineRefresh=0");
            }
            if (oldVersion < 11)
            {
                // force rescanning local assets once to get all correct metadata
                DBAdapter.DB.InsertOrReplace(new AppProperty("ForceLocalUpdate", "true"));
            }
            if (oldVersion < 12)
            {
                if (DBAdapter.ColumnExists("Asset", "PreviewImage"))
                {
                    DBAdapter.DB.Execute("alter table Asset drop column PreviewImage");
                }
                if (DBAdapter.ColumnExists("Asset", "MainImage"))
                {
                    DBAdapter.DB.Execute("alter table Asset drop column MainImage");
                }
                if (DBAdapter.ColumnExists("Asset", "MainImageIcon"))
                {
                    DBAdapter.DB.Execute("alter table Asset drop column MainImageIcon");
                }
                if (DBAdapter.ColumnExists("Asset", "MainImageSmall"))
                {
                    DBAdapter.DB.Execute("alter table Asset drop column MainImageSmall");
                }
                if (DBAdapter.ColumnExists("AssetFile", "ProjectPath"))
                {
                    DBAdapter.DB.Execute("alter table AssetFile drop column ProjectPath");
                }
            }
            if (oldVersion < 13)
            {
                // convert asset cache index to relative structure
                requireUpgrade = new AppProperty("UpgradeRequired", "true");
                DBAdapter.DB.InsertOrReplace(requireUpgrade);
                LongUpgradeRequired = true;

                AppProperty upgradeType = new AppProperty("UpgradeType-AssetCacheConversion", "true");
                DBAdapter.DB.InsertOrReplace(upgradeType);
            }
            if (oldVersion < 14)
            {
                // rename extracted folders
                RenameExtractedFolders();
            }
            if (dbVersion == null || (oldVersion < CURRENT_DB_VERSION && !LongUpgradeRequired))
            {
                DBAdapter.DB.InsertOrReplace(new AppProperty("Version", CURRENT_DB_VERSION.ToString()));
                Debug.Log($"Asset Inventory database upgraded to version {CURRENT_DB_VERSION}");
            }

            // check for config upgrades
            int oldConfigVersion = AssetInventory.Config.version;
            if (oldConfigVersion < 2)
            {
                // change media folders type after introducing new "all" type
                AssetInventory.Config.folders.ForEach(f =>
                {
                    if (f.scanFor > 0) f.scanFor++;
                });
            }
            if (oldConfigVersion < CURRENT_CONFIG_VERSION)
            {
                AssetInventory.Config.version = CURRENT_CONFIG_VERSION;
                AssetInventory.SaveConfig();
                Debug.Log($"Asset Inventory configuration upgraded to version {CURRENT_CONFIG_VERSION}");
            }

            PendingUpgrades = DBAdapter.DB.Table<AppProperty>()
                .Where(a => a.Name.StartsWith("UpgradeType-"))
                .Select(a => a.Name.Substring(12))
                .ToList();
        }

        private static void RenameExtractedFolders()
        {
            List<Asset> assets = DBAdapter.DB.Table<Asset>().ToList();
            foreach (Asset asset in assets)
            {
                if (asset.AssetSource == Asset.Source.Directory || asset.AssetSource == Asset.Source.RegistryPackage) continue;

                string expectedPath = AssetInventory.GetMaterializedAssetPath(asset);
                string oldPath = GetOldMaterializedAssetPath(asset);
                if (Directory.Exists(oldPath) && !Directory.Exists(expectedPath))
                {
                    Directory.Move(oldPath, expectedPath);
                }
            }
        }

        private static string GetOldMaterializedAssetPath(Asset asset)
        {
            return IOUtils.PathCombine(AssetInventory.GetMaterializeFolder(), asset.SafeName);
        }

        private static void StartLongRunningUpgrades()
        {
            foreach (string upgrade in PendingUpgrades)
            {
                switch (upgrade.ToLowerInvariant())
                {
                    case "previewconversion":
                        UpgradePreviewImageStructure();
                        break;

                    case "assetcacheconversion":
                        UpgradeAssetCashLocation();
                        break;
                }
            }

            DBAdapter.DB.Execute("delete from AppProperty where Name like ?", "UpgradeType-%");
            DBAdapter.DB.Delete<AppProperty>("UpgradeRequired");
            AppProperty newVersion = new AppProperty("Version", CURRENT_DB_VERSION.ToString());
            DBAdapter.DB.InsertOrReplace(newVersion);

            LongUpgradeRequired = false;
        }

        private static async void UpgradePreviewImageStructure()
        {
            AssetInventory.CurrentMain = "Upgrading preview images structure...";

            string previewFolder = AssetInventory.GetPreviewFolder();
            IEnumerable<string> files = IOUtils.GetFiles(previewFolder, new[] {"*.png"});
            AssetInventory.MainCount = files.Count();
            AssetInventory.MainProgress = 0;

            int cleanedFiles = 0;
            foreach (string file in files)
            {
                AssetInventory.MainProgress++;
                AssetInventory.CurrentMainItem = file;
                if (AssetInventory.MainProgress % 1000 == 0) await Task.Yield();

                string[] arr = Path.GetFileNameWithoutExtension(file).Split('-');

                string assetId;
                switch (arr[0])
                {
                    case "a":
                        assetId = arr[1];
                        break;

                    case "af":
                        int fileId = int.Parse(arr[1]);
                        AssetFile af = DBAdapter.DB.Find<AssetFile>(fileId);
                        if (af == null)
                        {
                            // legacy, can be removed
                            cleanedFiles++;
                            File.Delete(file);
                            continue;
                        }
                        assetId = af.AssetId.ToString();
                        break;

                    default:
                        Debug.LogError($"Unknown preview type: {file}");
                        continue;
                }

                // move file from root into new sub-structure
                string targetDir = Path.Combine(previewFolder, assetId);
                string targetFile = Path.Combine(targetDir, Path.GetFileName(file));
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                if (File.Exists(targetFile)) File.Delete(targetFile);
                File.Move(file, targetFile);
            }
            Debug.Log($"Cleaned up orphaned preview files: {cleanedFiles}");

            AssetInventory.CurrentMain = null;
        }

        private static void UpgradeAssetCashLocation()
        {
            AssetInventory.CurrentMain = "Aligning all paths to use forward slashes...";
            int affected = DBAdapter.DB.Execute("UPDATE Asset SET Location = REPLACE(Location, ?, ?)", "\\", "/");
            Debug.Log($"Changed asset paths: {affected}");

            affected = DBAdapter.DB.Execute("UPDATE Asset SET SafeName = REPLACE(SafeName, ?, ?)", "\\", "/");
            Debug.Log($"Changed asset safe names: {affected}");

            affected = DBAdapter.DB.Execute("UPDATE AssetFile SET Path = REPLACE(Path, ?, ?)", "\\", "/");
            Debug.Log($"Changed asset file paths: {affected}");

            affected = DBAdapter.DB.Execute("UPDATE AssetFile SET SourcePath = REPLACE(SourcePath, ?, ?)", "\\", "/");
            Debug.Log($"Changed asset file source paths: {affected}");

            AssetInventory.CurrentMain = "Upgrading asset cache location persistence...";
            string oldPrefix = AssetInventory.GetAssetCacheFolder();
            string newPrefix = "[ac]";
            affected = DBAdapter.DB.Execute("UPDATE Asset SET Location = REPLACE(Location, ?, ?) WHERE Location LIKE ?", oldPrefix, newPrefix, oldPrefix + "%");
            Debug.Log($"Converted asset cache entries: {affected}");

            AssetInventory.CurrentMain = "Upgrading package cache location persistence...";
            oldPrefix = AssetInventory.GetPackageCacheFolder();
            newPrefix = "[pc]";
            affected = DBAdapter.DB.Execute("UPDATE Asset SET Location = REPLACE(Location, ?, ?) WHERE Location LIKE ?", oldPrefix, newPrefix, oldPrefix + "%");
            Debug.Log($"Converted package cache entries: {affected}");

            AssetInventory.CurrentMain = null;
        }

        public static void DrawUpgradeRequired()
        {
            EditorGUILayout.Space(30);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Box(BasicEditorUI.Logo, EditorStyles.centeredGreyMiniLabel, GUILayout.MaxWidth(300), GUILayout.MaxHeight(300));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("An incompatible database upgrade is required for this version.", UIStyles.whiteCenter);
            EditorGUILayout.LabelField("Ensure you have a backup of your database.", EditorStyles.centeredGreyMiniLabel);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            EditorGUILayout.Space(30);
            EditorGUILayout.LabelField("Pending Upgrades", EditorStyles.boldLabel);
            for (int i = 0; i < PendingUpgrades.Count; i++)
            {
                string upgrade = PendingUpgrades[i];

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField((i + 1) + ".", GUILayout.Width(15));
                switch (upgrade.ToLowerInvariant())
                {
                    case "previewconversion":
                        EditorGUILayout.LabelField("Upgrade preview image structure");
                        break;

                    case "assetcacheconversion":
                        EditorGUILayout.LabelField("Store asset cache paths in a relative fashion in the database, making it easier reusable across devices and align all paths to use forward slashes", EditorStyles.wordWrappedLabel, GUILayout.MaxWidth(300));
                        break;

                    default:
                        Debug.LogError("Unknown upgrade type: " + upgrade);
                        break;

                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(30);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(!string.IsNullOrEmpty(AssetInventory.CurrentMain));
            if (GUILayout.Button("Start Upgrade Process", GUILayout.Height(50))) StartLongRunningUpgrades();
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(AssetInventory.CurrentMain))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(AssetInventory.CurrentMain, UIStyles.whiteCenter);
                EditorGUILayout.Space();
                UIStyles.DrawProgressBar(AssetInventory.MainProgress / (float)AssetInventory.MainCount, AssetInventory.CurrentMainItem);
            }
        }
    }
}