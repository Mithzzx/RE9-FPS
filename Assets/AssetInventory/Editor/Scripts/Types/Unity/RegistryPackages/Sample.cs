using System;

namespace AssetInventory
{
    [Serializable]
    public sealed class Sample
    {
        public string displayName;
        public string description;
        public string path;
        public bool interactiveImport;

        public override string ToString()
        {
            return $"Package Sample '{displayName}' ({path})";
        }
    }
}