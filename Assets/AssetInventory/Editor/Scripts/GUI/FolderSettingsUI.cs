using UnityEditor;
using UnityEngine;

namespace AssetInventory
{
    public sealed class FolderSettingsUI : PopupWindowContent
    {
        private FolderSpec _spec;

        public void Init(FolderSpec spec)
        {
            _spec = spec;
        }

        public override void OnGUI(Rect rect)
        {
            editorWindow.maxSize = new Vector2(340, 190);
            editorWindow.minSize = editorWindow.maxSize;
            int width = 140;

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(UIStyles.Content("Content", "Type of content to scan for"), EditorStyles.boldLabel, GUILayout.Width(width));
            _spec.folderType = EditorGUILayout.Popup(_spec.folderType, UIStyles.FolderTypes);
            GUILayout.EndHorizontal();

            switch (_spec.folderType)
            {
                case 0: // packages
                    EditorGUI.BeginDisabledGroup(true);
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Assign Package", "Will connect indexed files to a new package with the name of the package file."), EditorStyles.boldLabel, GUILayout.Width(width));
                    EditorGUILayout.Toggle(true);
                    GUILayout.EndHorizontal();
                    EditorGUI.EndDisabledGroup();

                    RenderAssignTag(width);
                    break;

                case 1: // media
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Find", "File types to search for"), EditorStyles.boldLabel, GUILayout.Width(width));
                    _spec.scanFor = EditorGUILayout.Popup(_spec.scanFor, UIStyles.MediaTypes);
                    GUILayout.EndHorizontal();

                    if (_spec.scanFor == 7)
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("Pattern", "e.g. *.jpg;*.wav"), EditorStyles.boldLabel, GUILayout.Width(width));
                        _spec.pattern = EditorGUILayout.TextField(_spec.pattern);
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Create Previews", "Recommended. Will generate previews and additional metadata but requires more time during indexing."), EditorStyles.boldLabel, GUILayout.Width(width));
                    _spec.createPreviews = EditorGUILayout.Toggle(_spec.createPreviews);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Remove Orphans", "Recommended. Will check for deleted files and remove them from the index."), EditorStyles.boldLabel, GUILayout.Width(width));
                    _spec.removeOrphans = EditorGUILayout.Toggle(_spec.removeOrphans);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Assign Package", $"Will connect indexed files to a new package with the name of the folder. Otherwise list them under '{Asset.NONE}'."), EditorStyles.boldLabel, GUILayout.Width(width));
                    _spec.attachToPackage = EditorGUILayout.Toggle(_spec.attachToPackage);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Detect Unity Projects", "Will check if the folder is the root of a Unity project and will only index the Assets/ folder while keeping the name of the Unity project as the package name."), EditorStyles.boldLabel, GUILayout.Width(width));
                    _spec.detectUnityProjects = EditorGUILayout.Toggle(_spec.detectUnityProjects);
                    GUILayout.EndHorizontal();

                    break;

                case 2: // archive
                    EditorGUI.BeginDisabledGroup(true);
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Assign Package", "Will connect indexed files to a new package with the name of the archive."), EditorStyles.boldLabel, GUILayout.Width(width));
                    EditorGUILayout.Toggle(true);
                    GUILayout.EndHorizontal();
                    EditorGUI.EndDisabledGroup();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Create Previews", "Recommended. Will generate previews and additional metadata but requires more time during indexing."), EditorStyles.boldLabel, GUILayout.Width(width));
                    _spec.createPreviews = EditorGUILayout.Toggle(_spec.createPreviews);
                    GUILayout.EndHorizontal();

                    if (AssetInventory.DEBUG_MODE)
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("Prefer Packages", "If the archive contains unity package files the package will be indexed instead and all other files ignored."), EditorStyles.boldLabel, GUILayout.Width(width));
                        _spec.preferPackages = EditorGUILayout.Toggle(_spec.preferPackages);
                        GUILayout.EndHorizontal();
                    }
                    RenderAssignTag(width);
                    break;
            }

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(UIStyles.Content("Check File Sizes", "Will update files upon changes in file size. Can significantly slow down the process if files are stored on slow drives or network shares."), EditorStyles.boldLabel, GUILayout.Width(width));
            _spec.checkSize = EditorGUILayout.Toggle(_spec.checkSize);
            GUILayout.EndHorizontal();

            if (AssetInventory.ShowAdvanced())
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(UIStyles.Content("Store Relative", "Persists file paths relative without the concrete base folder location to allow reusing the same database from different systems."), EditorStyles.boldLabel, GUILayout.Width(width));
                if (_spec.storeRelative)
                {
                    if (GUILayout.Button(UIStyles.Content("Disable..."), GUILayout.ExpandWidth(false)))
                    {
                        RelativeUI relativeUI = RelativeUI.ShowWindow();
                        relativeUI.Init(_spec);
                        editorWindow.Close();
                    }
                }
                else
                {
                    if (GUILayout.Button(UIStyles.Content("Enable..."), GUILayout.ExpandWidth(false)))
                    {
                        RelativeUI relativeUI = RelativeUI.ShowWindow();
                        relativeUI.Init(_spec);
                        editorWindow.Close();
                    }
                }
                GUILayout.EndHorizontal();
            }

            if (EditorGUI.EndChangeCheck()) AssetInventory.SaveConfig();
        }

        private void RenderAssignTag(int width)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(UIStyles.Content("Assign Tag", "Will assign a tag to all found packages. This makes it easy to filter for them later on."), EditorStyles.boldLabel, GUILayout.Width(width));
            _spec.assignTag = EditorGUILayout.Toggle(_spec.assignTag);
            GUILayout.EndHorizontal();

            if (_spec.assignTag)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(UIStyles.Content("Tag", "Tag to assign to each package."), EditorStyles.boldLabel, GUILayout.Width(width));
                _spec.tag = EditorGUILayout.TextField(_spec.tag);
                GUILayout.EndHorizontal();
            }
        }
    }
}