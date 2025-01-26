using System;
using SQLite;

namespace AssetInventory
{
    [Serializable]
    public sealed class SystemData
    {
        [PrimaryKey] public string Key { get; set; }
        public string Name { get; set; }
        public string Model { get; set; }
        public string Type { get; set; }
        public string OS { get; set; }
        public DateTime LastUsed { get; set; }

        public SystemData()
        {
        }

        public override string ToString()
        {
            return $"System '{Key}' ({Name})";
        }
    }
}