using System;
using UnityEditor.PackageManager;

namespace AssetInventory
{
    [Serializable]
    public sealed class Repository
    {
        public string type;
        public string url;
        public string revision;
        public string path;

        public Repository()
        {
        }

#if UNITY_2020_1_OR_NEWER
        public Repository(RepositoryInfo repository)
        {
            type = repository.type;
            url = repository.url;
            revision = repository.revision;
            path = repository.path;
        }
#endif
        public override string ToString()
        {
            return $"Repository '{type}' ({url})";
        }
    }
}