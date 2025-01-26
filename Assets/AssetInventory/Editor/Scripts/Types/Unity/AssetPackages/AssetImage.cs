using System;

namespace AssetInventory
{
    [Serializable]
    public sealed class AssetImage
    {
        public int height;
        public int width;
        public string imageUrl;
        public string webpUrl;
        public string thumbnailUrl;
        public string type;

        public override string ToString()
        {
            return $"Asset Image ({type}, {width}x{height})";
        }
    }
}
