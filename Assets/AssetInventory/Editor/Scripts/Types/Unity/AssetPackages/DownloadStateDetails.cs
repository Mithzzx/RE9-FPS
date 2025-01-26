using System;

namespace AssetInventory
{
    [Serializable]
    public sealed class DownloadStateDetails
    {
        public string url;
        public string key;
        
        public override string ToString()
        {
            return $"Download State Details ({url})";
        }
    }
}
