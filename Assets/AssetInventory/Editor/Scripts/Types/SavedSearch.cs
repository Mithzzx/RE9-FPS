using System;

namespace AssetInventory
{
    [Serializable]
    public sealed class SavedSearch
    {
        public string name;
        public string color;
        public string searchPhrase;
        public string type;
        public int packageTypes;
        public string package;
        public string packageTag;
        public string fileTag;
        public string publisher;
        public string category;
        public string width;
        public string height;
        public string length;
        public string size;
        public bool checkMaxWidth;
        public bool checkMaxHeight;
        public bool checkMaxLength;
        public bool checkMaxSize;
        public int colorOption;
        public string searchColor;
    }
}