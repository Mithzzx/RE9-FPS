using System;
using Newtonsoft.Json;

namespace AssetInventory
{
    [Serializable]
    public sealed class DownloadState
    {
        [JsonProperty("in_progress")]
        public bool inProgress;
        public DownloadStateDetails download;
        
        public override string ToString()
        {
            return $"Download State (in progress: {inProgress})";
        }
    }
}
