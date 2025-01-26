using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace AssetInventory
{
    public sealed class VersionSelectionUI : PopupWindowContent
    {
        private AssetInfo _info;
        private PackageInfo _packageInfo;
        private Vector2 _scrollPos;
        private Action<string> _callback;
        private Repository _repository;

        public void Init(AssetInfo info, Action<string> callback)
        {
            _info = info;
            _callback = callback;
            if (info?.Repository != null) _repository = JsonConvert.DeserializeObject<Repository>(info.Repository);
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(400, 300);
        }

        public override void OnGUI(Rect rect)
        {
            if (_info == null) return;
            if (!AssetStore.IsMetadataAvailable())
            {
                EditorGUILayout.HelpBox("Loading package metadata...", MessageType.Info);
                return;
            }
            if (_packageInfo == null) _packageInfo = AssetStore.GetPackageInfo(_info.SafeName);
            if (_packageInfo == null)
            {
                if (_info.PackageSource == PackageSource.Git)
                {
                    EditorGUILayout.HelpBox($"This is a Git reference to {_repository.url}", MessageType.Info);
                    EditorGUILayout.Space();
                    if (GUILayout.Button(UIStyles.Content($"Install indexed {_info.GetVersion()}"), GUILayout.Width(140)))
                    {
                        _callback?.Invoke(_info.LatestVersion);
                        editorWindow.Close();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Could not find matching package metadata.", MessageType.Warning);
                    EditorGUILayout.Space();
                    if (!string.IsNullOrWhiteSpace(_info.LatestVersion))
                    {
                        if (GUILayout.Button(UIStyles.Content($"Install indexed {_info.LatestVersion}"), GUILayout.Width(140)))
                        {
                            _callback?.Invoke(_info.LatestVersion);
                            editorWindow.Close();
                        }
                    }
                }
                return;
            }
            if (_packageInfo.versions.all.Length == 0)
            {
                if (_packageInfo.source == PackageSource.Embedded)
                {
                    EditorGUILayout.HelpBox("This is an embedded package with no other versions available.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Could not find any other versions.", MessageType.Info);
                }
                return;
            }

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, false);
            List<string> attributes = new List<string>();
            Color oldCol = GUI.backgroundColor;
            foreach (string version in _packageInfo.versions.all.Reverse())
            {
                bool compatible = false;
                bool isCurrent = false;

                attributes.Clear();
                if (_packageInfo.versions.compatible.Contains(version))
                {
                    attributes.Add("compatible");
                    compatible = true;
                }
                else
                {
                    GUI.backgroundColor = Color.yellow;
                }
#if UNITY_2022_2_OR_NEWER
                if (version == _packageInfo.versions.recommended)
#else
                if (version == _packageInfo.versions.verified)
#endif
                {
                    GUI.backgroundColor = Color.green;
                    attributes.Add("recommended");
                }
                if (AssetStore.IsInstalled(_packageInfo.name, version))
                {
                    attributes.Add("installed");
                    isCurrent = true;
                }

                GUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup((!compatible && !AssetInventory.ShowAdvanced()) || isCurrent);
                if (GUILayout.Button(UIStyles.Content(version, "Install this version"), GUILayout.Width(140)))
                {
                    _callback?.Invoke(version);
                    editorWindow.Close();
                }
                EditorGUI.EndDisabledGroup();
                EditorGUI.BeginDisabledGroup(_info.GetChangeLogURL(version) == null);
                if (GUILayout.Button(UIStyles.Content("?", "Changelog"), GUILayout.Width(20)))
                {
                    Application.OpenURL(_info.GetChangeLogURL(version));
                }
                EditorGUI.EndDisabledGroup();
                if (attributes.Count > 0) GUILayout.Label(string.Join(", ", attributes));
                GUILayout.EndHorizontal();

                GUI.backgroundColor = oldCol;
            }
            GUILayout.EndScrollView();
        }
    }
}