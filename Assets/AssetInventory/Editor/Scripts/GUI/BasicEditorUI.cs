using UnityEditor;
using UnityEngine;

namespace AssetInventory
{
    public abstract class BasicEditorUI : EditorWindow
    {
        public static Texture2D Logo
        {
            get
            {
                if (_logo == null) _logo = UIStyles.LoadTexture("asset-inventory-logo");
                return _logo;
            }
        }

        private static Texture2D _logo;

        public virtual void OnGUI()
        {
            EditorGUILayout.Space();
        }
    }
}
