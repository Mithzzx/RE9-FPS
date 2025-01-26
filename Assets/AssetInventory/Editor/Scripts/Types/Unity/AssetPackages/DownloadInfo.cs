using System;

namespace AssetInventory
{
    [Serializable]
    public sealed class DownloadInfo
    {
        public string id;
        public string filename_safe_category_name;
        public string filename_safe_package_name;
        public string filename_safe_publisher_name;
        public string key;
        public string url;
        public string upload_id;

        public override string ToString()
        {
            return $"Download Info ({filename_safe_package_name})";
        }
    }
}