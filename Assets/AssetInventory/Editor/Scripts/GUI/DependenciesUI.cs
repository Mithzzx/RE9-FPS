using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetInventory
{
    public sealed class DependenciesUI : EditorWindow
    {
        private Vector2 _scrollPos;
        private AssetInfo _info;

        public static DependenciesUI ShowWindow()
        {
            DependenciesUI window = GetWindow<DependenciesUI>("Asset Dependencies");
            window.minSize = new Vector2(500, 200);

            return window;
        }

        public void Init(AssetInfo info)
        {
            _info = info;
            _info.Dependencies.ForEach(i => i.CheckIfInProject());
        }

        public void OnGUI()
        {
            EditorGUILayout.LabelField($"{_info.Dependencies.Count} dependencies of '{_info.FileName}' in asset '{_info.GetDisplayName()}' ({EditorUtility.FormatBytes(_info.Size)})", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("File Types: ", EditorStyles.boldLabel, GUILayout.MaxWidth(70));
            string types = string.Join(", ", _info.Dependencies.OrderBy(f => f.Type).GroupBy(f => f.Type).Select(g => g.Count() + " ." + g.Key + " (" + EditorUtility.FormatBytes(g.Sum(f => f.Size)) + ")"));
            EditorGUILayout.LabelField(types);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Total Size: ", EditorStyles.boldLabel, GUILayout.MaxWidth(70));
            EditorGUILayout.LabelField(EditorUtility.FormatBytes(_info.Dependencies.Sum(f => f.Size)));
            GUILayout.EndHorizontal();

            if (_info.Dependencies.Any(f => f.InProject))
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Remaining: ", EditorStyles.boldLabel, GUILayout.MaxWidth(70));
                EditorGUILayout.LabelField(EditorUtility.FormatBytes(_info.Dependencies.Where(f => !f.InProject).Sum(f => f.Size)));
                GUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandWidth(true));
            foreach (AssetFile info in _info.Dependencies.OrderBy(f => f.Path).ThenBy(f => f.Type))
            {
                GUILayout.BeginHorizontal();
                if (info.InProject)
                {
                    EditorGUILayout.LabelField(EditorGUIUtility.IconContent("Installed", "|Already in project"), GUILayout.Width(20));
                }
                else
                {
                    EditorGUILayout.LabelField(EditorGUIUtility.IconContent("Import", "|Needs to be imported"), GUILayout.Width(20));
                }
                EditorGUILayout.LabelField(new GUIContent(info.Path + " (" + EditorUtility.FormatBytes(info.Size) + ")", info.Guid), _info.ScriptDependencies.Contains(info) ? UIStyles.ColoredText(Color.yellow) : EditorStyles.label);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }
    }
}