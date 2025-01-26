using System;

namespace AssetInventory
{
    [Serializable]
    public sealed class Category
    {
        public string id;
        public string name;
        public string slug;

        public override string ToString()
        {
            return $"Category ({name})";
        }
    }
}