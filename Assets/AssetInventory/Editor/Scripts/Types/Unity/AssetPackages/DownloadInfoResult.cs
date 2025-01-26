using System;

namespace AssetInventory
{
    [Serializable]
    public sealed class DownloadInfoResult
    {
        public DownloadInfoResultDetails result;

        public override string ToString()
        {
            return "Download Info Result";
        }
    }
}