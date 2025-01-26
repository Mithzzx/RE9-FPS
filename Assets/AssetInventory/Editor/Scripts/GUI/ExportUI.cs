using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace AssetInventory
{
    public sealed class ExportUI : EditorWindow
    {
        private const string REMAINING_EXTENSIONS = "All the Rest";

        private string _separator = ";";
        private Vector2 _scrollPos;
        private List<AssetInfo> _assets;
        private List<ED> _exportFields;
        private List<ED> _overrideFields;
        private List<ED> _exportTypes;
        private string[] _exportOptions;
        private int _selectedExportOption;
        private bool _addHeader = true;
        private bool _showFields;
        private bool _clearTarget;
        private bool _overrideExisting;
        private List<AssetInfo> _packages;
        private int _packageCount;
        private bool _exportInProgress;
        private List<string> _exportableExtensions;
        private int _curProgress;
        private int _maxProgress;
        private bool _autoDownload;

        public static ExportUI ShowWindow()
        {
            ExportUI window = GetWindow<ExportUI>("Export Asset Data");
            window.minSize = new Vector2(400, 300);

            return window;
        }

        public void Init(List<AssetInfo> assets, int exportType)
        {
            _assets = assets;
            _packages = assets.GroupBy(a => a.AssetId).Select(a => a.First()).ToList(); // cast to list to make it serializable during script reloads
            _packageCount = _packages.Count;

            _exportableExtensions = AssetInventory.TypeGroups.SelectMany(tg => tg.Value).ToList();

            _selectedExportOption = exportType;
            _exportOptions = new[] {"Package info to file (CSV)", "Assets to external folder", "Package Override File", "Catalog (HTML)"};
            _exportFields = new List<ED>
            {
                new ED("Asset/Id"),
                new ED("Asset/ParentId"),
                new ED("Asset/ForeignId"),
                new ED("Asset/AssetRating"),
                new ED("Asset/AssetSource"),
                new ED("Asset/CompatibilityInfo", false),
                new ED("Asset/CurrentState", false),
                new ED("Asset/CurrentSubState", false),
                new ED("Asset/Description", false),
                new ED("Asset/DisplayCategory"),
                new ED("Asset/DisplayName"),
                new ED("Asset/DisplayPublisher"),
                new ED("Asset/ETag", false),
                new ED("Asset/Exclude", false),
                new ED("Asset/FirstRelease", false),
                new ED("Asset/Hotness", false),
                new ED("Asset/IsHidden", false),
                new ED("Asset/KeyFeatures", false),
                new ED("Asset/Keywords"),
                new ED("Asset/LastOnlineRefresh", false),
                new ED("Asset/LastRelease"),
                new ED("Asset/LatestVersion"),
                new ED("Asset/License"),
                new ED("Asset/LicenseLocation", false),
                new ED("Asset/Location"),
                new ED("Asset/OriginalLocation", false),
                new ED("Asset/OriginalLocationKey", false),
                new ED("Asset/PackageSize", false),
                new ED("Asset/PackageSource"),
                new ED("Asset/PriceEur", false),
                new ED("Asset/PriceUsd", false),
                new ED("Asset/PriceCny", false),
                new ED("Asset/PurchaseDate"),
                new ED("Asset/RatingCount"),
                new ED("Asset/Registry", false),
                new ED("Asset/ReleaseNotes", false),
                new ED("Asset/Repository", false),
                new ED("Asset/Revision"),
                new ED("Asset/SafeCategory"),
                new ED("Asset/SafeName"),
                new ED("Asset/SafePublisher"),
                new ED("Asset/Slug", false),
                new ED("Asset/SupportedUnityVersions"),
                new ED("Asset/UpdateStrategy", false),
                new ED("Asset/Version"),
                new ED("Asset/PackageTags")
            };
            _overrideFields = new List<ED>
            {
                new ED("Asset/AssetRating", false),
                new ED("Asset/CompatibilityInfo", false),
                new ED("Asset/Description", false),
                new ED("Asset/DisplayCategory", false),
                new ED("Asset/DisplayName", false),
                new ED("Asset/DisplayPublisher", false),
                new ED("Asset/FirstRelease", false),
                new ED("Asset/Hotness", false),
                new ED("Asset/KeyFeatures", false),
                new ED("Asset/Keywords", false),
                new ED("Asset/LastRelease", false),
                new ED("Asset/LatestVersion", false),
                new ED("Asset/License", false),
                new ED("Asset/LicenseLocation", false),
                new ED("Asset/PriceEur", false),
                new ED("Asset/PriceUsd", false),
                new ED("Asset/PriceCny", false),
                new ED("Asset/PurchaseDate", false),
                new ED("Asset/RatingCount", false),
                new ED("Asset/Registry", false),
                new ED("Asset/ReleaseNotes", false),
                new ED("Asset/Repository", false),
                new ED("Asset/Revision", false),
                new ED("Asset/SafeCategory", false),
                new ED("Asset/SafePublisher", false),
                new ED("Asset/Slug", false),
                new ED("Asset/SupportedUnityVersions", false),
                new ED("Asset/Version", false),
                new ED("Asset/PackageTags", false)
            };
            _exportTypes = new List<ED>
            {
                new ED("Audio"),
                new ED("Images"),
                new ED("Video"),
                new ED("Models"),
                new ED("Documents", false),
                new ED("Scripts", false),
                new ED("Shaders", false),
                new ED("Animations", false),
                new ED(REMAINING_EXTENSIONS, false)
            };
        }

        public void OnGUI()
        {
            if (_assets == null || _assets.Count == 0)
            {
                Close();
                return;
            }

            int labelWidth = 110;
            EditorGUI.BeginDisabledGroup(_exportInProgress);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel, GUILayout.MaxWidth(labelWidth));
            if (_packageCount == 1)
            {
                EditorGUILayout.LabelField($"Current Selection ({_assets.First().GetDisplayName()})");
            }
            else
            {
                EditorGUILayout.LabelField($"Current Selection ({_packageCount} packages)");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Action", EditorStyles.boldLabel, GUILayout.Width(labelWidth));
            _selectedExportOption = EditorGUILayout.Popup(_selectedExportOption, _exportOptions);
            GUILayout.EndHorizontal();

            switch (_selectedExportOption)
            {
                case 0:
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Header Line", EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                    _addHeader = EditorGUILayout.Toggle(_addHeader);
                    GUILayout.EndHorizontal();

                    EditorGUILayout.Space();
                    _showFields = EditorGUILayout.Foldout(_showFields, "Fields");
                    if (_showFields)
                    {
                        EditorGUILayout.Space();
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Select All")) _exportFields.ForEach(f => f.isSelected = true);
                        if (GUILayout.Button("Select None")) _exportFields.ForEach(f => f.isSelected = false);
                        if (GUILayout.Button("Select Default")) _exportFields.ForEach(f => f.isSelected = f.isDefault);
                        GUILayout.EndHorizontal();
                        EditorGUILayout.Space();

                        _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandWidth(true));
                        foreach (ED ed in _exportFields)
                        {
                            GUILayout.BeginHorizontal();
                            ed.isSelected = EditorGUILayout.Toggle(ed.isSelected, GUILayout.Width(20));
                            EditorGUILayout.LabelField(ed.field);
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndScrollView();
                    }
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Export...", GUILayout.Height(50))) ExportMetaData();
                    break;

                case 1:
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Clear Target", "Deletes any previously existing export for the specific package, otherwise only copies new files"), EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                    _clearTarget = EditorGUILayout.Toggle(_clearTarget);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Download", "Triggers download of package automatically in case it is not available yet in the cache"), EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                    _autoDownload = EditorGUILayout.Toggle(_autoDownload);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("File Types", EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                    if (GUILayout.Button("Typical", GUILayout.ExpandWidth(false))) _exportTypes.ForEach(et => et.isSelected = et.isDefault);
                    if (GUILayout.Button("All", GUILayout.ExpandWidth(false))) _exportTypes.ForEach(et => et.isSelected = true);
                    if (GUILayout.Button("None", GUILayout.ExpandWidth(false))) _exportTypes.ForEach(et => et.isSelected = false);
                    GUILayout.EndHorizontal();

                    int typeWidth = 70;
                    for (int i = 0; i < _exportTypes.Count; i++)
                    {
                        // show always three items per row
                        if (i % 3 == 0)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(107);
                        }
                        _exportTypes[i].isSelected = EditorGUILayout.Toggle(_exportTypes[i].isSelected, GUILayout.Width(20));
                        EditorGUILayout.LabelField(_exportTypes[i].pointer, GUILayout.Width(typeWidth));
                        if (i % 3 == 2 || i == _exportTypes.Count - 1) GUILayout.EndHorizontal();
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.HelpBox("Make sure you own the appropriate rights in case you intend to use assets in other contexts than Unity!", MessageType.Warning);
                    if (_exportInProgress) UIStyles.DrawProgressBar((float)_curProgress / _maxProgress, $"{_curProgress}/{_maxProgress}");
                    if (GUILayout.Button(_exportInProgress ? "Export in progress" : "Export...", GUILayout.Height(50))) ExportAssets();
                    break;

                case 2:
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Override Existing", ""), EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                    _overrideExisting = EditorGUILayout.Toggle(_overrideExisting);
                    GUILayout.EndHorizontal();

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Fields to override", EditorStyles.boldLabel);
                    EditorGUILayout.Space();
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Select All")) _overrideFields.ForEach(f => f.isSelected = true);
                    if (GUILayout.Button("Select None")) _overrideFields.ForEach(f => f.isSelected = false);
                    GUILayout.EndHorizontal();
                    EditorGUILayout.Space();

                    _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandWidth(true));
                    foreach (ED ed in _overrideFields)
                    {
                        GUILayout.BeginHorizontal();
                        ed.isSelected = EditorGUILayout.Toggle(ed.isSelected, GUILayout.Width(20));
                        EditorGUILayout.LabelField(ed.field);
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndScrollView();

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(_exportInProgress ? "Export in progress" : "Export", GUILayout.Height(50))) ExportOverrides();
                    break;

                case 3:
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("Coming Soon", MessageType.Info);
                    break;
            }
            EditorGUI.EndDisabledGroup();
        }

        private async void ExportAssets()
        {
            string folder = EditorUtility.OpenFolderPanel("Select storage folder for exports", AssetInventory.Config.exportFolder2, "");
            if (string.IsNullOrEmpty(folder)) return;

            AssetInventory.Config.exportFolder2 = Path.GetFullPath(folder);
            AssetInventory.SaveConfig();

            _exportInProgress = true;
            _curProgress = 0;
            _maxProgress = _packages.Count;

            foreach (AssetInfo info in _packages)
            {
                _curProgress++;
                await Task.Yield();

                if (!info.IsIndexed)
                {
                    Debug.LogError($"Skipping package '{info}' since it is not yet indexed.");
                    continue;
                }

                if (!info.Downloaded)
                {
                    if (info.IsAbandoned)
                    {
                        Debug.LogWarning($"Package '{info}' is not locally available and also abandoned and cannot be downloaded anymore. Continuing with next package.");
                        continue;
                    }
                    if (!_autoDownload)
                    {
                        Debug.LogWarning($"Package '{info}' is not downloaded and cannot be exported. Continuing with next package.");
                        continue;
                    }
                    AssetInventory.GetObserver().Attach(info);
                    if (!info.PackageDownloader.IsDownloadSupported()) continue;

                    info.PackageDownloader.Download();
                    do
                    {
                        await Task.Yield();
                    } while (info.IsDownloading());
                    await Task.Delay(3000); // ensure all file operations have finished, can otherwise lead to issues
                    info.Refresh();
                    if (!info.Downloaded)
                    {
                        Debug.LogError($"Downloading '{info}' failed. Continuing with next package.");
                        continue;
                    }
                }

                string targetFolder = Path.Combine(folder, info.SafeName);
                if (_clearTarget && Directory.Exists(targetFolder)) await IOUtils.DeleteFileOrDirectory(targetFolder);
                if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

                // extract package
                string cachePath = AssetInventory.GetMaterializedAssetPath(info.ToAsset());
                bool existing = Directory.Exists(cachePath);

                // gather all indexed files
                List<AssetFile> files = DBAdapter.DB.Query<AssetFile>("SELECT * FROM AssetFile WHERE AssetId = ?", info.AssetId).ToList();
                foreach (AssetFile af in files)
                {
                    bool include = false;
                    foreach (ED type in _exportTypes)
                    {
                        if (!type.isSelected) continue;
                        if (type.pointer != REMAINING_EXTENSIONS)
                        {
                            if (AssetInventory.TypeGroups[type.pointer].Contains(af.Type)) include = true;
                        }
                        else
                        {
                            if (!_exportableExtensions.Contains(af.Type)) include = true;
                        }
                    }
                    if (!include) continue;

                    string targetFile = Path.Combine(targetFolder, af.GetPath(true));
                    if (File.Exists(targetFile)) continue;

                    string sourceFile = await AssetInventory.EnsureMaterializedAsset(info.ToAsset(), af);
                    if (string.IsNullOrEmpty(sourceFile)) continue;

                    string targetDir = Directory.GetParent(targetFile)?.ToString();
                    if (targetDir == null) continue;

                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                    File.Copy(sourceFile, targetFile);
                }
                if (!existing) await IOUtils.DeleteFileOrDirectory(cachePath);
            }
            _exportInProgress = false;
            EditorUtility.RevealInFinder(folder);
        }

        private async void ExportOverrides()
        {
            _exportInProgress = true;
            _curProgress = 0;
            _maxProgress = _packages.Count;

            foreach (AssetInfo info in _packages)
            {
                _curProgress++;
                if (info.AssetSource != Asset.Source.CustomPackage && info.AssetSource != Asset.Source.Archive)
                {
                    Debug.LogWarning($"Skipping package '{info}' since it is not a custom package or archive.");
                    continue;
                }
                await Task.Yield();

                string targetFile = info.GetLocation(true) + ".overrides.json";
                if (!_overrideExisting && File.Exists(targetFile)) continue;

                PackageOverrides po = new PackageOverrides();
                foreach (ED field in _overrideFields.Where(f => f.isSelected))
                {
                    switch (field.field)
                    {
                        case "PackageTags":
                            po.tags = info.PackageTags.Select(pt => pt.Name).ToArray();
                            break;

                        default:
                            if (field.FieldInfo != null)
                            {
                                FieldInfo fi = typeof (PackageOverrides).GetField(field.field.ToLowercaseFirstLetter());
                                if (fi != null)
                                {
                                    fi.SetValue(po, field.FieldInfo.GetValue(info));
                                }
                                else
                                {
                                    Debug.LogError($"Override field '{field.field}' not found.");
                                }
                            }
                            else
                            {
                                Debug.LogError($"Override source field '{field.field}' not found.");
                            }
                            break;
                    }
                }

                File.WriteAllText(targetFile, JsonConvert.SerializeObject(po, new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                }));
            }
            _exportInProgress = false;
        }

        private void ExportMetaData()
        {
            string file = EditorUtility.SaveFilePanel("Target file", AssetInventory.Config.exportFolder, "assets", "csv");
            if (string.IsNullOrEmpty(file)) return;

            _exportInProgress = true;

            AssetInventory.Config.exportFolder = Directory.GetParent(Path.GetFullPath(file))?.ToString();
            AssetInventory.SaveConfig();

            List<string> result = new List<string>();

            if (_addHeader)
            {
                List<object> line = new List<object>();
                foreach (ED field in _exportFields.Where(f => f.isSelected))
                {
                    line.Add(field.field);
                }
                result.Add(string.Join(_separator, line));
            }

            foreach (AssetInfo info in _assets.Where(a => a.SafeName != Asset.NONE))
            {
                List<object> line = new List<object>();
                foreach (ED field in _exportFields.Where(f => f.isSelected))
                {
                    switch (field.field)
                    {
                        case "PackageTags":
                            line.Add(string.Join(",", info.PackageTags.Select(pt => pt.Name)));
                            break;

                        default:
                            if (field.FieldInfo != null)
                            {
                                line.Add(field.FieldInfo.GetValue(info));
                            }
                            else
                            {
                                Debug.LogError($"Export field '{field.field}' not found.");
                            }
                            break;
                    }

                    // make sure delimiter and line breaks are not used 
                    if (line.Last() is string s)
                    {
                        line[line.Count - 1] = s.Replace(_separator, ",").Replace("\n", string.Empty).Replace("\r", string.Empty);
                    }
                }
                result.Add(string.Join(_separator, line));
            }
            File.WriteAllLines(file, result);
            _exportInProgress = false;

            EditorUtility.RevealInFinder(file);
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }

    [Serializable]
    public sealed class ED
    {
        public string pointer;
        public bool isDefault;
        public bool isSelected;

        public string table;
        public string field;

        public PropertyInfo FieldInfo
        {
            get
            {
                if (field == null) return null;
                if (_fieldInfo == null) _fieldInfo = typeof (AssetInfo).GetProperty(field);
                return _fieldInfo;
            }
        }

        private PropertyInfo _fieldInfo;

        public ED(string pointer, bool isDefault = true)
        {
            this.isDefault = isDefault;
            this.pointer = pointer;

            isSelected = isDefault;

            if (pointer.IndexOf('/') >= 0)
            {
                table = pointer.Split('/')[0];
                field = pointer.Split('/')[1];
            }
        }
    }
}