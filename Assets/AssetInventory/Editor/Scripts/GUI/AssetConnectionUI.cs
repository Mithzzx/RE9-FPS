using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetInventory
{
    public sealed class AssetConnectionUI : PopupWindowContent
    {
        private string _url;
        private Action<AssetDetails> _callback;
        private bool _invalidInput;
        private bool _focusDone;
        private AssetDetails _resolvedAsset;

        public void Init(Action<AssetDetails> callback)
        {
            _callback = callback;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(450, 160);
        }

        public override void OnGUI(Rect rect)
        {
            if (Event.current.isKey && Event.current.keyCode == KeyCode.Return) CheckURL(_url);

            EditorGUILayout.LabelField("Connect Free-Floating Asset to Asset Store Metadata", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("URL/ID:", GUILayout.Width(50));
            GUI.SetNextControlName("URLEntry");
            _url = EditorGUILayout.TextField(_url, GUILayout.ExpandWidth(true));
            if (!_focusDone) GUI.FocusControl("URLEntry");
            if (GUILayout.Button("Verify", GUILayout.ExpandWidth(false))) CheckURL(_url);
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();
            if (string.IsNullOrWhiteSpace(_url))
            {
                _resolvedAsset = null;
                _invalidInput = false;

                EditorGUILayout.HelpBox("Enter the URL to the asset on the Asset Store. Do not use short URLs but those of the form below or alternatively the Id, e.g. 226927", MessageType.Info);
                if (GUILayout.Button("https://assetstore.unity.com/packages/tools/utilities/asset-inventory-226927", EditorStyles.linkLabel)) Application.OpenURL("https://assetstore.unity.com/packages/tools/utilities/asset-inventory-226927");
            }

            if (_invalidInput) EditorGUILayout.HelpBox("The entered URL could not be resolved correctly.", MessageType.Error);

            if (_resolvedAsset != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Resolved Asset", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"{_resolvedAsset.displayName} - {_resolvedAsset.version}");
                EditorGUILayout.LabelField($"by {_resolvedAsset.productPublisher.name}, {_resolvedAsset.state}");
                EditorGUILayout.Space();
                if (GUILayout.Button("Connect"))
                {
                    _callback?.Invoke(_resolvedAsset);
                    editorWindow.Close();
                }
            }
        }

        private async void CheckURL(string url)
        {
            _invalidInput = false;
            _resolvedAsset = null;

            if (string.IsNullOrWhiteSpace(url)) return;

            string idPart = url.Split('-').Last();
            if (int.TryParse(idPart, out int id))
            {
                _resolvedAsset = await AssetStore.RetrieveAssetDetails(id);
                if (_resolvedAsset != null) return;
            }
            _invalidInput = true;
        }
    }
}
