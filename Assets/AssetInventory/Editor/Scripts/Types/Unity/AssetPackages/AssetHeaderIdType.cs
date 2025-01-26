using System;

namespace AssetInventory
{
    [Serializable]
    public sealed class AssetHeaderIdType
    {
        public string id;
        public string type;

        public override string ToString()
        {
            return $"Asset Header Type ({id}, {type})";
        }
    }
}