using UnityEditor;
using UnityEngine;

namespace AssetInventory
{
    public partial class IndexUI
    {
        private void DrawAboutTab()
        {
            EditorGUILayout.Space(30);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Box(Logo, EditorStyles.centeredGreyMiniLabel, GUILayout.MaxWidth(300), GUILayout.MaxHeight(300));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("A tool by Impossible Robert", UIStyles.whiteCenter);
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Developer: Robert Wetzold", UIStyles.whiteCenter);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("www.wetzold.com/tools", UIStyles.centerLinkLabel)) Application.OpenURL("https://www.wetzold.com/tools");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EditorGUILayout.LabelField($"Version {AssetInventory.TOOL_VERSION}", UIStyles.whiteCenter);
            EditorGUILayout.Space(30);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.HelpBox("If you like this asset please consider leaving a review on the Unity Asset Store. Thanks a million!", MessageType.Info);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Write Review")) Application.OpenURL(AssetInventory.ASSET_STORE_LINK);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (ShowAdvanced() && GUILayout.Button("Create Debug Support Report")) CreateDebugReport();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            if (AssetInventory.DEBUG_MODE && GUILayout.Button("Get Token", GUILayout.ExpandWidth(false))) Debug.Log(CloudProjectSettings.accessToken);
            if (AssetInventory.DEBUG_MODE && GUILayout.Button("Reload Lookups")) ReloadLookups();
        }
    }
}
