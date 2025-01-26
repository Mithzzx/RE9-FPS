using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetInventory
{
    public sealed class SampleSelectionUI : PopupWindowContent
    {
        private AssetInfo _info;
        private Vector2 _scrollPos;
        private IEnumerable<UnityEditor.PackageManager.UI.Sample> _samples;
        private Texture _importedIcon;

        public void Init(AssetInfo info)
        {
            _info = info;
            _samples = info.GetSamples();
            _importedIcon = UIStyles.IconContent("Valid", "Installed", "|Imported").image;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(400, 200);
        }

        public override void OnGUI(Rect rect)
        {
            if (_info == null) return;
            if (!AssetStore.IsMetadataAvailable())
            {
                EditorGUILayout.HelpBox("Loading package metadata...", MessageType.Info);
                return;
            }
            if (_samples == null || _samples.Count() == 0)
            {
                EditorGUILayout.HelpBox("Package contains no samples.", MessageType.Info);
                return;
            }

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, false);
            foreach (UnityEditor.PackageManager.UI.Sample sample in _samples)
            {
                GUILayout.BeginHorizontal();
                if (sample.isImported)
                {
                    GUILayout.Box(_importedIcon, EditorStyles.centeredGreyMiniLabel, GUILayout.Width(24), GUILayout.Height(24));
                }
                else
                {
                    EditorGUILayout.Space(24);
                }
                GUILayout.BeginVertical();
                EditorGUILayout.LabelField(sample.displayName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(sample.description, EditorStyles.wordWrappedMiniLabel);
                GUILayout.EndVertical();
                if (sample.isImported)
                {
                    if (GUILayout.Button("Remove"))
                    {
                        Directory.Delete(sample.importPath, true);
                        File.Delete(sample.importPath + ".meta");
                        AssetDatabase.Refresh();
                    }
                }
                else
                {
                    if (GUILayout.Button("Install"))
                    {
                        sample.Import();
                    }
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
            GUILayout.EndScrollView();
        }
    }
}
