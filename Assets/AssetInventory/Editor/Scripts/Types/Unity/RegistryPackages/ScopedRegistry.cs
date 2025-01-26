using System;
using System.Reflection;
using UnityEditor.PackageManager;

namespace AssetInventory
{
    [Serializable]
    public sealed class ScopedRegistry
    {
        public string name;
        public string url;
        public string[] scopes;

        public ScopedRegistry()
        {
        }

        public ScopedRegistry(RegistryInfo registry)
        {
            name = registry.name;
            url = registry.url;
            scopes = (string[]) registry.GetType().GetProperty("scopes", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(registry);
        }

        public override string ToString()
        {
            return $"Scoped Registry '{name}' ({url})";
        }
    }
}