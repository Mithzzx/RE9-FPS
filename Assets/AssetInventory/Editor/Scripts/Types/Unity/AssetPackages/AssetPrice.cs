using System;

namespace AssetInventory
{
    [Serializable]
    public sealed class AssetPrice
    {
        public string currency;
        public string originalPrice;
        public string finalPrice;
        public string entitlementType;

        public override string ToString()
        {
            return $"Asset Price ({finalPrice} {currency})";
        }
    }
}
