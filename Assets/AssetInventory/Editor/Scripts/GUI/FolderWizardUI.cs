using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetInventory
{
    public sealed class FolderWizardUI : EditorWindow
    {
        private string _folder;
        private bool _calculating;
        private bool _activateUnityPackages = true;
        private bool _activateMediaFolders = true;
        private bool _activateArchives = true;
        private bool _unityPackagesAlreadyActive;
        private bool _mediaFoldersAlreadyActive;
        private bool _archivesAlreadyActive;
        private int _packageCount;
        private int _mediaCount;
        private int _assetCount;
        private int _archiveCount;
        private bool _isUnityFolder;

        public static FolderWizardUI ShowWindow()
        {
            FolderWizardUI window = GetWindow<FolderWizardUI>("Folder Wizard");
            window.minSize = new Vector2(750, 370);
            window.maxSize = window.minSize;

            return window;
        }

        public void Init(string folder)
        {
            _folder = folder;
            ParseFolder();
        }

        public void OnGUI()
        {
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Folder", EditorStyles.boldLabel, GUILayout.Width(50));
            EditorGUILayout.LabelField(_folder);
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Folders can be scanned for different file types. Each type uses a different importer that can be activated below and configured subsequently with additional settings.", EditorStyles.wordWrappedLabel);

            EditorGUI.BeginDisabledGroup(_calculating);

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();

            int spacing = 6;
            GUILayout.Space(spacing);
            GUILayout.BeginVertical("Unity Packages", "window");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("- Scans for *.unitypackage files", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("- Creates a new package with the name of the file", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("- Automatically links package to Asset Store entries", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("- Extracts previews from package", EditorStyles.wordWrappedLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"Detected packages: {_packageCount:N0}", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (_unityPackagesAlreadyActive)
            {
                EditorGUILayout.LabelField("Already Active", UIStyles.centerLabel);
            }
            else
            {
                _activateUnityPackages = EditorGUILayout.ToggleLeft("Activate", _activateUnityPackages, GUILayout.Width(80));
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(spacing);
            GUILayout.BeginVertical("Media Files", "window");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("- Scans for image, audio, model or any files", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("- Creates a new package with the name of the folder", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("- Creates previews while indexing", EditorStyles.wordWrappedLabel);
            GUILayout.FlexibleSpace();
            if (_isUnityFolder)
            {
                EditorGUILayout.HelpBox("Unity project detected. Will use special indexing logic.", MessageType.Info);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"Detected files: {_assetCount:N0}", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                EditorGUILayout.LabelField($"Detected media files: {_mediaCount:N0}", EditorStyles.centeredGreyMiniLabel);
            }
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (_mediaFoldersAlreadyActive)
            {
                EditorGUILayout.LabelField("Already Active", UIStyles.centerLabel);
            }
            else
            {
                _activateMediaFolders = EditorGUILayout.ToggleLeft("Activate", _activateMediaFolders, GUILayout.Width(80));
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(spacing);
            GUILayout.BeginVertical("Archives", "window");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("- Scans for zip/7z/rar archives", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("- Creates a new package with the name of the file", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("- Creates previews while indexing", EditorStyles.wordWrappedLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"Detected archives: {_archiveCount:N0}", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (_archivesAlreadyActive)
            {
                EditorGUILayout.LabelField("Already Active", UIStyles.centerLabel);
            }
            else
            {
                _activateArchives = EditorGUILayout.ToggleLeft("Activate", _activateArchives, GUILayout.Width(80));
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(spacing);
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("OK", GUILayout.Height(40))) SaveSettings();
            // if (GUILayout.Button("Refresh", GUILayout.Height(40), GUILayout.Width(80))) ParseFolder();
            GUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();
        }

        private void SaveSettings()
        {
            if (_activateUnityPackages && !_unityPackagesAlreadyActive) AssetInventory.Config.folders.Add(GetSpec(_folder, 0));
            if (_activateMediaFolders && !_mediaFoldersAlreadyActive) AssetInventory.Config.folders.Add(GetSpec(_folder, 1));
            if (_activateArchives && !_archivesAlreadyActive) AssetInventory.Config.folders.Add(GetSpec(_folder, 2));

            AssetInventory.SaveConfig();
            Close();
        }

        private FolderSpec GetSpec(string folder, int type)
        {
            FolderSpec spec = new FolderSpec();
            spec.folderType = type;
            spec.location = folder;
            if (AssetInventory.IsRel(folder))
            {
                spec.storeRelative = true;
                spec.relativeKey = AssetInventory.GetRelKey(folder);
            }

            // scan for all files if that is a Unity project
            if (type == 1 && _isUnityFolder) spec.scanFor = 1;

            return spec;
        }

        private void ParseFolder()
        {
            _calculating = true;

            // determine media extensions
            List<string> mediaExt = new List<string>();
            mediaExt.AddRange(new[] {"Audio", "Images", "Models"});

            List<string> mediaTypes = new List<string>();
            mediaExt.ForEach(t => mediaTypes.AddRange(AssetInventory.TypeGroups[t]));

            string deRel = AssetInventory.DeRel(_folder);
            _isUnityFolder = AssetUtils.IsUnityProject(deRel);

            // scan
            string[] files = Directory.GetFiles(deRel, "*.*", SearchOption.AllDirectories);
            _packageCount = files.Count(f => GetExtension(f) == "unitypackage");
            _mediaCount = files.Count(f => mediaTypes.Contains(GetExtension(f)));
            _archiveCount = files.Count(f => GetExtension(f) == "zip" || GetExtension(f) == "rar" || GetExtension(f) == "7z");

            if (_isUnityFolder)
            {
                string assetFolder = Path.Combine(deRel, "Assets");
                _assetCount = files.Count(f => f.StartsWith(assetFolder));
            }
            else
            {
                _assetCount = 0;
            }

            _activateUnityPackages = _packageCount > 0 && !_isUnityFolder;
            _activateMediaFolders = _mediaCount > 0 || _isUnityFolder;
            _activateArchives = _archiveCount > 0 && !_isUnityFolder;

            _unityPackagesAlreadyActive = AssetInventory.Config.folders.Count(spec => spec.location == _folder && spec.folderType == 0) > 0;
            _mediaFoldersAlreadyActive = AssetInventory.Config.folders.Count(spec => spec.location == _folder && spec.folderType == 1) > 0;
            _archivesAlreadyActive = AssetInventory.Config.folders.Count(spec => spec.location == _folder && spec.folderType == 2) > 0;

            _calculating = false;
        }

        private string GetExtension(string fileName)
        {
            return IOUtils.GetExtensionWithoutDot(fileName).ToLowerInvariant();
        }
    }
}
