using System;

namespace AssetInventory
{
    [Serializable]
    public sealed class PackageOverrides
    {
        // default package properties
        public string displayName;
        public string displayCategory;
        public string safeCategory;
        public string displayPublisher;
        public string safePublisher;
        public int publisherId;
        public string slug;
        public int revision;
        public string description;
        public string keyFeatures;
        public string compatibilityInfo;
        public string supportedUnityVersions;
        public string keywords;
        public string version;
        public string latestVersion;
        public string license; // SPDX identifier format: https://spdx.org/licenses/
        public string licenseLocation;
        public DateTime purchaseDate;
        public DateTime firstRelease;
        public DateTime lastRelease;
        public string assetRating;
        public int ratingCount;
        public float hotness;
        public float priceEur;
        public float priceUsd;
        public float priceCny;
        public string registry;
        public string repository;
        public string requirements;
        public string releaseNotes;
        public string officialState;

        // additional properties
        public string[] tags;

        public PackageOverrides()
        {
        }
    }
}