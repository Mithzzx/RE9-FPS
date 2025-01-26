using System;

namespace AssetInventory
{
    [Serializable]
    public sealed class DownloadInfoResultDetails
    {
        public DownloadInfo download;

        public override string ToString()
        {
            return "Download Info Result Details";
        }
    }
}