using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AssetInventory
{
    public sealed class TagsUI : EditorWindow
    {
        private List<Tag> _tags;
        private string _newTag;
        private Vector2 _tagsScrollPos;
        private SearchField SearchField => _searchField = _searchField ?? new SearchField();
        private SearchField _searchField;

        public static TagsUI ShowWindow()
        {
            return GetWindow<TagsUI>("Tag Management");
        }

        public void Init()
        {
            _tags = AssetInventory.LoadTags();
        }

        public void OnEnable()
        {
            AssetInventory.OnTagsChanged += Init;
        }

        public void OnDisable()
        {
            AssetInventory.OnTagsChanged -= Init;
        }

        public void OnGUI()
        {
            if (Event.current.isKey && Event.current.keyCode == KeyCode.Return)
            {
                AssetInventory.AddTag(_newTag);
                _newTag = "";
            }
            _newTag = SearchField.OnGUI(_newTag, GUILayout.ExpandWidth(true));
            if (_tags != null)
            {
                EditorGUILayout.Space();
                if (_tags.Count == 0)
                {
                    EditorGUILayout.HelpBox("No tags created yet. Use the textfield above to create the first tag.", MessageType.Info);
                }
                else
                {
                    _tagsScrollPos = GUILayout.BeginScrollView(_tagsScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandWidth(true));
                    foreach (Tag tag in _tags)
                    {
                        // don't show already added tags
                        if (!string.IsNullOrWhiteSpace(_newTag) && !tag.Name.ToLowerInvariant().Contains(_newTag.ToLowerInvariant())) continue;

                        GUILayout.BeginHorizontal();
                        EditorGUI.BeginChangeCheck();
                        tag.Color = "#" + ColorUtility.ToHtmlStringRGB(EditorGUILayout.ColorField(GUIContent.none, tag.GetColor(), false, false, false, GUILayout.Width(20)));
                        if (EditorGUI.EndChangeCheck()) AssetInventory.SaveTag(tag);
                        EditorGUILayout.LabelField(new GUIContent(tag.Name, tag.FromAssetStore ? "From Asset Store" : "Local Tag"));
                        if (GUILayout.Button(EditorGUIUtility.IconContent("editicon.sml", "|Rename tag"), GUILayout.Width(30)))
                        {
                            NameUI nameUI = new NameUI();
                            nameUI.Init(tag.Name, newName => RenameTag(tag, newName));
                            PopupWindow.Show(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0, 0), nameUI);
                        }
                        if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash", "|Remove tag completely"), GUILayout.Width(30)))
                        {
                            AssetInventory.DeleteTag(tag);
                        }
                        GUILayout.EndHorizontal();
                    }
                    if (!string.IsNullOrWhiteSpace(_newTag))
                    {
                        EditorGUILayout.HelpBox("Press RETURN to create a new tag", MessageType.Info);
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("Temporary limitation: Actual tag colors will appear darker than selected here.", MessageType.Info);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Delete All")) _tags.ForEach(AssetInventory.DeleteTag);
                    GUILayout.EndScrollView();
                }
            }
        }

        private void RenameTag(Tag tag, string newName)
        {
            if (string.IsNullOrEmpty(newName) || tag.Name == newName) return;

            Tag existingTag = DBAdapter.DB.Find<Tag>(t => t.Name.ToLower() == newName.ToLower());
            if (existingTag != null)
            {
                EditorUtility.DisplayDialog("Error", "A tag with that name already exists (and merging tags is not yet supported).", "OK");
                return;
            }

            AssetInventory.RenameTag(tag, newName);
        }
    }
}