using System;
using System.Collections.Generic;

namespace AssetInventory
{
    [Serializable]
    public sealed class Package
    {
        public string name;
        public string version;

        public string description;
        public string displayName;

        public string unity;

        public Author author;
        public string changelogUrl;
        public string documentationUrl;
        public bool hideInEditor;
        public string[] keywords;
        public Dictionary<string, string> dependencies;
        public Sample[] samples;
        public string license;
        public string licensesUrl;
        public string type;
        public string unityRelease;

        public override string ToString()
        {
            return $"Package '{name}' ({version})";
        }
    }
}