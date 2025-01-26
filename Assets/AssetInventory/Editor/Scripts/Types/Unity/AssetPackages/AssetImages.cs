using System;

namespace AssetInventory
{
    [Serializable]
    public sealed class AssetImages
    {
        public string big;
        public string icon;
        public string icon25;
        public string icon75;
        public string small;
        public string url;
        public string facebook;
        public string small_v2;
        public string big_v2;

        public override string ToString()
        {
            return "Asset Images";
        }
    }
}