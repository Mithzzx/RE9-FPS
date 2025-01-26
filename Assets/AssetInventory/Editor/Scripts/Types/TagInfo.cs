using System;
using UnityEngine;

namespace AssetInventory
{
    [Serializable]
    // used to contain results of join calls
    public sealed class TagInfo : TagAssignment
    {
        public string Name { get; set; }
        public string Color { get; set; }

        public Color GetColor()
        {
            if (ColorUtility.TryParseHtmlString(Color, out Color toUse)) return toUse;
            return Tag.DefaultColor;
        }

        public override string ToString()
        {
            return $"Tag Info '{Name}' ('{TagTarget}', {TargetId})";
        }
    }
}