using System;
using SQLite;

namespace AssetInventory
{
    [Serializable]
    public sealed class AppProperty
    {
        [PrimaryKey] public string Name { get; set; }
        public string Value { get; set; }

        public AppProperty()
        {
        }

        public AppProperty(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public override string ToString()
        {
            return $"App Property '{Name}' ({Value})";
        }
    }
}