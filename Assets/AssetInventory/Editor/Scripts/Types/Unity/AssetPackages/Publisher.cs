using System;

namespace AssetInventory
{
    [Serializable]
    public sealed class Publisher
    {
        public string id;
        public string name;
        public string externalRef;
        public string supportUrl;
        public string supportEmail;
        public string url;
        public string gaAccount;
        public string gaPrefix;

        public override string ToString()
        {
            return $"Publisher ({name})";
        }
    }
}