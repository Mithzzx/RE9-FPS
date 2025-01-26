using System;
using UnityEditor;

namespace AssetInventory
{
    public sealed class MemoryObserver
    {
        public bool Enabled = true;

        private DateTime _lastClean;
        private readonly long _targetSize;
        private readonly int _interval;

        private long _curSize;

        public MemoryObserver(long targetSize, int interval = 1)
        {
            _lastClean = DateTime.Now;
            _interval = interval;
            _targetSize = targetSize;
        }

        public void Do(long size)
        {
            _curSize += size;

            if (!Enabled || _targetSize == 0 || _curSize < _targetSize || (DateTime.Now - _lastClean).TotalMinutes < _interval) return;
            EditorUtility.UnloadUnusedAssetsImmediate();

            _lastClean = DateTime.Now;
            _curSize = 0;
        }
    }
}
