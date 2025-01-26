using System;
using UnityEditor;
using UnityEngine;

namespace AssetInventory
{
    public sealed class NameUI : PopupWindowContent
    {
        private string _text;
        private Action<string> _callback;
        private bool _firstRunDone;

        public void Init(string text, Action<string> callback)
        {
            _text = text;
            _callback = callback;
        }

        public override void OnGUI(Rect rect)
        {
            editorWindow.maxSize = new Vector2(200, 45);

            GUI.SetNextControlName("TextField");
            _text = EditorGUILayout.TextField(_text, GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            if ((Event.current.isKey && Event.current.keyCode == KeyCode.Return) || GUILayout.Button("OK") && !string.IsNullOrWhiteSpace(_text))
            {
                _callback?.Invoke(_text);
                editorWindow.Close();
            }
            if (GUILayout.Button("Cancel")) editorWindow.Close();
            GUILayout.EndHorizontal();

            if (!_firstRunDone)
            {
                GUI.FocusControl("TextField");
                _firstRunDone = true;
            }
        }
    }
}