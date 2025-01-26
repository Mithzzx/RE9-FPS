using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetInventory
{
    public sealed class DropDownUI : PopupWindowContent
    {
        private List<Tuple<int, string>> _data;
        private Vector2 _scrollPos;
        private Action<int> _callback;
        private int _current;

        private void Init(List<Tuple<int, string>> data, int current, Action<int> callback)
        {
            _data = data;
            _current = current;
            _callback = callback;
        }

        public void Init(int min, int max, int current, string prefix, string suffix, Action<int> callback)
        {
            List<Tuple<int, string>> data = new List<Tuple<int, string>>();

            if (prefix == null) prefix = "";
            if (suffix == null) suffix = "";
            for (int i = min; i <= max; i++)
            {
                data.Add(new Tuple<int, string>(i, prefix + i + suffix));
            }

            Init(data, current, callback);
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(150, 300);
        }

        public override void OnGUI(Rect rect)
        {
            if (_data == null) return;

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, false);
            foreach (Tuple<int, string> tuple in _data)
            {
                EditorGUI.BeginDisabledGroup(tuple.Item1 == _current);
                if (GUILayout.Button(tuple.Item2))
                {
                    _callback?.Invoke(tuple.Item1);
                    editorWindow.Close();
                }
                EditorGUI.EndDisabledGroup();
            }
            GUILayout.EndScrollView();
        }
    }
}