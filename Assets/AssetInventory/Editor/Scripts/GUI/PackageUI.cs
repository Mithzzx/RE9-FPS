using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetInventory
{
    public sealed class PackageUI : EditorWindow
    {
        private Vector2 _scrollPos;
        private AssetInfo _info;
        private Asset _asset;
        private Action<Asset> _onSave;
        private Vector2 _scrollDescr;

        public static PackageUI ShowWindow()
        {
            PackageUI window = GetWindow<PackageUI>("Package Data");
            window.minSize = new Vector2(400, 430);

            return window;
        }

        public void Init(AssetInfo info, Action<Asset> onSave)
        {
            _info = info;
            _asset = DBAdapter.DB.Find<Asset>(info.AssetId); // load fresh from DB and store that exact copy later again
            _asset.PreviewTexture = info.PreviewTexture;
            if (_asset.PreviewTexture == null)
            {
                // create grey texture
                _asset.PreviewTexture = new Texture2D(100, 100);
                _asset.PreviewTexture.SetPixel(0, 0, Color.grey);
                _asset.PreviewTexture.Apply();
            }
            _onSave = onSave;
        }

        public void OnGUI()
        {
            if (string.IsNullOrEmpty(_asset?.Location))
            {
                Close();
                return;
            }
            int labelWidth = 125;

            EditorGUILayout.HelpBox("Change the values below to update the package data. The technical names are mandatory if you want filters or selection dropdowns to work properly.", MessageType.Info);
            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Location", EditorStyles.boldLabel, GUILayout.MaxWidth(labelWidth));
            EditorGUILayout.LabelField(_asset.Location, EditorStyles.wordWrappedLabel);

            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();
            int tWidth = 65;
            GUILayout.Box(_asset.PreviewTexture, EditorStyles.centeredGreyMiniLabel, GUILayout.MaxWidth(tWidth), GUILayout.MaxHeight(tWidth));
            if (GUILayout.Button("Change...", GUILayout.MaxWidth(tWidth))) ChangePreview();
            EditorGUILayout.Space();
            GUILayout.EndVertical();
            EditorGUILayout.Space();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(UIStyles.Content("Name", "Overrides the technical name"), EditorStyles.boldLabel, GUILayout.MaxWidth(labelWidth));
            _asset.DisplayName = EditorGUILayout.TextField(_asset.DisplayName);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Technical Name", EditorStyles.boldLabel, GUILayout.MaxWidth(labelWidth));
            EditorGUI.BeginDisabledGroup(true);
            _asset.SafeName = EditorGUILayout.TextField(_asset.SafeName);
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(UIStyles.Content("Publisher", "Overrides the technical publisher name"), EditorStyles.boldLabel, GUILayout.MaxWidth(labelWidth));
            _asset.DisplayPublisher = EditorGUILayout.TextField(_asset.DisplayPublisher);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Technical Publisher", EditorStyles.boldLabel, GUILayout.MaxWidth(labelWidth));
            _asset.SafePublisher = EditorGUILayout.TextField(_asset.SafePublisher);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(UIStyles.Content("Category", "Overrides the technical category name"), EditorStyles.boldLabel, GUILayout.MaxWidth(labelWidth));
            _asset.DisplayCategory = EditorGUILayout.TextField(_asset.DisplayCategory);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Technical Category", EditorStyles.boldLabel, GUILayout.MaxWidth(labelWidth));
            _asset.SafeCategory = EditorGUILayout.TextField(_asset.SafeCategory);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Version", EditorStyles.boldLabel, GUILayout.MaxWidth(labelWidth));
            _asset.Version = EditorGUILayout.TextField(_asset.Version);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Unity Versions", EditorStyles.boldLabel, GUILayout.MaxWidth(labelWidth));
            _asset.SupportedUnityVersions = EditorGUILayout.TextField(_asset.SupportedUnityVersions);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("License", EditorStyles.boldLabel, GUILayout.MaxWidth(labelWidth));
            _asset.License = EditorGUILayout.TextField(_asset.License);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("License Location", EditorStyles.boldLabel, GUILayout.MaxWidth(labelWidth));
            _asset.LicenseLocation = EditorGUILayout.TextField(_asset.LicenseLocation);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Price EUR", EditorStyles.boldLabel, GUILayout.MaxWidth(labelWidth));
            _asset.PriceEur = EditorGUILayout.FloatField(_asset.PriceEur);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Price USD", EditorStyles.boldLabel, GUILayout.MaxWidth(labelWidth));
            _asset.PriceUsd = EditorGUILayout.FloatField(_asset.PriceUsd);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Price CNY", EditorStyles.boldLabel, GUILayout.MaxWidth(labelWidth));
            _asset.PriceCny = EditorGUILayout.FloatField(_asset.PriceCny);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Description", EditorStyles.boldLabel, GUILayout.MaxWidth(labelWidth));
            _scrollDescr = EditorGUILayout.BeginScrollView(_scrollDescr, GUILayout.ExpandHeight(true));
            _asset.Description = EditorGUILayout.TextArea(_asset.Description, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save", GUILayout.Height(40)))
            {
                SaveData();
                Close();
            }
        }

        private void ChangePreview()
        {
            string assetPreviewFile = EditorUtility.OpenFilePanel("Select image", "", "png");
            if (string.IsNullOrEmpty(assetPreviewFile)) return;

            try
            {
                // load immediately
                byte[] fileData = File.ReadAllBytes(assetPreviewFile);
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(fileData);

                // copy file
                string targetDir = Path.Combine(AssetInventory.GetPreviewFolder(), _asset.Id.ToString());
                string targetFile = Path.Combine(targetDir, "a-" + _asset.Id + Path.GetExtension(assetPreviewFile));
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                File.Copy(assetPreviewFile, targetFile, true);
                AssetUtils.RemoveFromPreviewCache(targetFile);

                // set once all critical parts are done
                _asset.PreviewTexture = tex;
                _info.PreviewTexture = tex;
            }
            catch (Exception e)
            {
                Debug.LogError("Error loading image: " + e.Message);
            }
        }

        private void SaveData()
        {
            if (string.IsNullOrWhiteSpace(_asset.DisplayName) && string.IsNullOrWhiteSpace(_asset.SafeName))
            {
                EditorUtility.DisplayDialog("Error", "Either name or technical name must be set.", "OK");
                return;
            }

            DBAdapter.DB.Update(_asset);

            _onSave?.Invoke(_asset);
        }
    }
}