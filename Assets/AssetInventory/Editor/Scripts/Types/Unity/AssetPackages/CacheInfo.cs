using System;

namespace AssetInventory
{
    [Serializable]
    public sealed class CacheInfo
    {
        public string upload_id;

        public override string ToString()
        {
            return $"Cache Info ({upload_id})";
        }
    }
}
