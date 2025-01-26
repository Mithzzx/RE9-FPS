using System;
using SQLite;
using UnityEngine;

namespace AssetInventory
{
    [Serializable]
    public class AssetMedia
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [Indexed] public int AssetId { get; set; }
        [Indexed] public string Type { get; set; }
        public int Order { get; set; }
        public string Url { get; set; }
        public string ThumbnailUrl { get; set; }
        public string WebpUrl { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        // runtime
        [Ignore] public bool IsDownloading { get; set; }
        [Ignore] public Texture2D ThumbnailTexture { get; set; }
        [Ignore] public Texture2D Texture { get; set; }

        public string GetUrl(bool nonEmbedded = true)
        {
            // convert to non-embedded form
            // input https://www.youtube.com/embed/EWV-dhi61Yo?feature=oembed
            // output https://www.youtube.com/watch?v=EWV-dhi61Yo

            if (nonEmbedded && Type == "youtube" && Url.Contains("youtube.com/embed/"))
            {
                string url = Url.Replace("https://www.youtube.com/embed/", "https://www.youtube.com/watch?v=");
                url = url.Replace("?feature=oembed", "");
                return url;
            }
            return Url;
        }

        public override string ToString()
        {
            return $"Asset Media '{Type}' ({Width}x{Height})";
        }
    }
}
