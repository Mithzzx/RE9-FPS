using System;

namespace AssetInventory
{
    [Serializable]
    public sealed class Author
    {
        public string name;
        public string email;
        public string url;

        public override string ToString()
        {
            return $"Package Author '{name}'";
        }
    }
}