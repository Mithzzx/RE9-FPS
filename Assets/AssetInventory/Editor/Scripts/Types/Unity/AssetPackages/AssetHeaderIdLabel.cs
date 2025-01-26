using System;

namespace AssetInventory
{
    [Serializable]
    public sealed class AssetHeaderIdLabel
    {
        public string id;
        public string label;

        public override string ToString()
        {
            return $"Asset Header Label ({id}, {label})";
        }
    }
}