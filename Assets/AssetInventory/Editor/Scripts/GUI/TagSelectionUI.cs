using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AssetInventory
{
    public sealed class TagSelectionUI : PopupWindowContent
    {
        private List<AssetInfo> _assetInfo;
        private List<Tag> _tags;
        private string _newTag;
        private Vector2 _tagsScrollPos;
        private bool _firstRunDone;
        private SearchField SearchField => _searchField = _searchField ?? new SearchField();
        private SearchField _searchField;
        private TagAssignment.Target _target;
        private Action _onChange;

        public void Init(TagAssignment.Target target, Action onChange = null)
        {
            _target = target;
            _onChange = onChange;
            _tags = DBAdapter.DB.Table<Tag>().OrderBy(t => t.Name).ToList();
        }

        public void SetAssets(List<AssetInfo> infos)
        {
            _assetInfo = infos;
        }

        public override void OnGUI(Rect rect)
        {
            if (_assetInfo == null) return;
            if (Event.current.isKey && Event.current.keyCode == KeyCode.Return)
            {
                _assetInfo.ForEach(info => AssetInventory.AddTagAssignment(info, _newTag, _target));
                _newTag = "";
            }
            GUILayout.BeginHorizontal();
            _newTag = SearchField.OnGUI(_newTag, GUILayout.ExpandWidth(true));
            if (GUILayout.Button(EditorGUIUtility.IconContent("Settings", "|Manage Tags").image, EditorStyles.label))
            {
                TagsUI tagsUI = TagsUI.ShowWindow();
                tagsUI.Init();
            }
            GUILayout.EndHorizontal();
            if (_tags != null)
            {
                if (_tags.Count == 0)
                {
                    EditorGUILayout.HelpBox("No tags created yet. Use the textfield above to create the first tag.", MessageType.Info);
                }
                else
                {
                    _tagsScrollPos = GUILayout.BeginScrollView(_tagsScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandWidth(true));
                    int shownTags = 0;
                    foreach (Tag tag in _tags)
                    {
                        // don't show already added tags (for case of only one item selected, otherwise assigning it to all)
                        switch (_target)
                        {
                            case TagAssignment.Target.Package:
                                if (_assetInfo.Count == 1 && _assetInfo[0].PackageTags.Any(t => t.TagId == tag.Id)) continue;
                                break;

                            case TagAssignment.Target.Asset:
                                if (_assetInfo.Count == 1 && _assetInfo[0].AssetTags.Any(t => t.TagId == tag.Id)) continue;
                                break;
                        }
                        if (!string.IsNullOrWhiteSpace(_newTag) && !tag.Name.ToLowerInvariant().Contains(_newTag.ToLowerInvariant())) continue;
                        shownTags++;

                        GUILayout.BeginHorizontal();
                        GUILayout.Space(8);
                        UIStyles.DrawTag(tag.Name, tag.GetColor(), () =>
                        {
                            _assetInfo.ForEach(info => AssetInventory.AddTagAssignment(info, tag.Name, _target));
                            _onChange?.Invoke();
                        }, UIStyles.TagStyle.Add);
                        GUILayout.EndHorizontal();
                    }
                    if (shownTags == 0)
                    {
                        if (string.IsNullOrWhiteSpace(_newTag))
                        {
                            EditorGUILayout.HelpBox("All existing tags were assigned already. Use the textfield above to create additional tags.", MessageType.Info);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("Press RETURN to create a new tag", MessageType.Info);
                        }
                    }
                    GUILayout.EndScrollView();
                }
            }
            if (!_firstRunDone)
            {
                SearchField.SetFocus();
                _firstRunDone = true;
            }
        }
    }
}