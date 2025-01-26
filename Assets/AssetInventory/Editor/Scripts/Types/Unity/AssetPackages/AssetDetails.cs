using System;
using System.Collections.Generic;

namespace AssetInventory
{
    [Serializable]
    public sealed class AssetDetails
    {
        public string id;
        public string createdBy;
        public string updatedBy;
        public DateTime createdTime;
        public DateTime updatedTime;
        public string packageId;
        public string slug;
        public int revision;
        public string packageName;
        public string ownerId;
        public string ownerType;
        public Dictionary<string, string> properties;
        public string name;
        public string displayName;
        public string description;
        public string aiDescription;
        public string elevatorPitch;
        public string keyFeatures;
        public string compatibilityInfo;
        public string[] supportedUnityVersions;
        public string[] keyWords;
        public AssetVersion version;
        public AssetReview productReview;
        public string originPrice;
        public AssetPrice[] productRatings;
        public Publisher productPublisher;
        public Category category;
        public AssetImages mainImage;
        public AssetImages mainImageWebp;
        public AssetImage[] images;
        public string publisherId;
        public string publishNotes;
        public string[] requirements;
        public string state;
        public Dictionary<string, UploadInfo> uploads;
        public string[] supportLinks;

        // runtime
        public string ETag { get; set; }

        public override string ToString()
        {
            return $"Asset Details '{name}' ({state})";
        }
    }
}
