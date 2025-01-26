using System;
using System.Collections.Generic;

namespace AssetInventory
{
    [Serializable]
    public sealed class AssetPurchase
    {
        public string id;
        public string orderId;
        public string grantTime;
        public int packageId;
        public List<string> tagging;
        public string displayName;

        public string CalculatedSafeName => AssetUtils.GuessSafeName(displayName);

        public bool isPublisherAsset;
        public bool isHidden;

        public Asset ToAsset()
        {
            Asset result = new Asset();
            result.AssetSource = Asset.Source.AssetStorePackage;
            result.DisplayName = displayName;
            result.ForeignId = packageId;
            result.IsHidden = isHidden;

            return result;
        }

        public override string ToString()
        {
            return $"Asset Purchase ({displayName})";
        }
    }
}