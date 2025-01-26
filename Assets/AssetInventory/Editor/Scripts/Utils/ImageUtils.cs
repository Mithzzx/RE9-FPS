// adapted from https://stackoverflow.com/questions/30103425/find-dominant-color-in-an-image

using System;
using System.Collections.Generic;
using System.IO;
#if UNITY_2021_2_OR_NEWER && UNITY_EDITOR_WIN
using System.Drawing;
using System.Drawing.Imaging;
using Graphics = System.Drawing.Graphics;
#endif
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Color = UnityEngine.Color;

namespace AssetInventory
{
    public static class ImageUtils
    {
        public static readonly List<string> SYSTEM_IMAGE_TYPES = new List<string> {"jpg", "jpeg", "png", "bmp", "gif", "tiff", "tif"};

        // palette adapted from http://eastfarthing.com/blog/2016-05-06-palette/
        private static Color[] PALETTE_32 =
        {
            FromHex("#d6a090"),
            FromHex("#fe3b1e"),
            FromHex("#a12c32"),
            FromHex("#fa2f7a"),
            FromHex("#fb9fda"),
            FromHex("#e61cf7"),
            FromHex("#992f7c"),
            FromHex("#47011f"),
            FromHex("#051155"),
            FromHex("#4f02ec"),
            FromHex("#2d69cb"),
            FromHex("#00a6ee"),
            FromHex("#6febff"),
            FromHex("#08a29a"),
            FromHex("#2a666a"),
            FromHex("#063619"),
            FromHex("#000000"),
            FromHex("#4a4957"),
            FromHex("#8e7ba4"),
            FromHex("#b7c0ff"),
            FromHex("#ffffff"),
            FromHex("#acbe9c"),
            FromHex("#827c70"),
            FromHex("#5a3b1c"),
            FromHex("#ae6507"),
            FromHex("#f7aa30"),
            FromHex("#f4ea5c"),
            FromHex("#9b9500"),
            FromHex("#566204"),
            FromHex("#11963b"),
            FromHex("#51e113"),
            FromHex("#08fdcc")
        };

        private static Color UNITY_BACKGROUND = new Color(0.321568638f, 0.321568638f, 0.321568638f, 1f);

        public static Color FromHex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color result))
            {
                return result;
            }
            return Color.clear;
        }

        public static Color GetNearestColor(Color inputColor)
        {
            double inputRed = Convert.ToDouble(inputColor.r);
            double inputGreen = Convert.ToDouble(inputColor.g);
            double inputBlue = Convert.ToDouble(inputColor.b);

            Color nearestColor = Color.clear;
            double distance = 500.0;
            foreach (Color color in PALETTE_32)
            {
                // Compute Euclidean distance between the two colors
                double testRed = Math.Pow(Convert.ToDouble(color.r) - inputRed, 2.0);
                double testGreen = Math.Pow(Convert.ToDouble(color.g) - inputGreen, 2.0);
                double testBlue = Math.Pow(Convert.ToDouble(color.b) - inputBlue, 2.0);
                double tempDistance = Math.Sqrt(testBlue + testGreen + testRed);
                if (tempDistance == 0.0) return color;
                if (tempDistance < distance)
                {
                    distance = tempDistance;
                    nearestColor = color;
                }
            }
            return nearestColor;
        }

        public static Color GetMostUsedColor(Texture2D texture)
        {
            Dictionary<Color, int> colorCount = new Dictionary<Color, int>();
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    Color pixelColor = texture.GetPixel(x, y);
                    if (pixelColor == UNITY_BACKGROUND) continue;
                    if (pixelColor.a == 0) continue;

                    if (colorCount.Keys.Contains(pixelColor))
                    {
                        colorCount[pixelColor]++;
                    }
                    else
                    {
                        colorCount.Add(pixelColor, 1);
                    }
                }
            }
            if (colorCount.Count == 0) return Color.clear;

            return colorCount.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value).First().Key;
        }

        public static float GetHue(Texture2D source)
        {
            if (source == null) return -1;

            Color32[] texColors = source.GetPixels32();
            int total = texColors.Length;
            float r = 0;
            float g = 0;
            float b = 0;
            float count = 0;

            for (int i = 0; i < total; i++)
            {
                Color32 pixelColor = texColors[i];
                if (pixelColor.a > .25f)
                {
                    count++;
                    r += pixelColor.r;
                    g += pixelColor.g;
                    b += pixelColor.b;
                }
            }
            return RGBToHue(r / 256f / count, g / 256f / count, b / 256f / count);
        }

        public static float ToHue(this Color color) => RGBToHue(color.r, color.g, color.b);

        // adapted from https://stackoverflow.com/questions/23090019/fastest-formula-to-get-hue-from-rgb
        public static float RGBToHue(float r, float g, float b)
        {
            float min = Mathf.Min(Mathf.Min(r, g), b);
            float max = Mathf.Max(Mathf.Max(r, g), b);
            if (min == max) return 0;
            float delta = max - min;

            float hue = 0;
            if (r == max)
            {
                hue = (g - b) / delta;
            }
            else if (g == max)
            {
                hue = 2 + (b - r) / delta;
            }
            else if (b == max)
            {
                hue = 4 + (r - g) / delta;
            }
            hue *= 60;

            if (hue < 0.0f) hue += 360;

            return hue;
        }

        public static Texture FillTexture(this Texture2D texture, Color color)
        {
            Color[] pixels = texture.GetPixels();

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }

        public static Tuple<int, int> GetDimensions(string file)
        {
            #if UNITY_2021_2_OR_NEWER && UNITY_EDITOR_WIN
            Image originalImage; // leave here as otherwise temp files will be created by FromFile() for yet unknown reasons 
            #endif
            try
            {
                #if UNITY_2021_2_OR_NEWER && UNITY_EDITOR_WIN
                using (originalImage = Image.FromFile(file))
                {
                    return new Tuple<int, int>(originalImage.Width, originalImage.Height);
                }
                #else
                Texture2D tmpTexture = new Texture2D(1, 1);
                byte[] assetContent = File.ReadAllBytes(file);
                if (tmpTexture.LoadImage(assetContent))
                {
                    return new Tuple<int, int>(tmpTexture.width, tmpTexture.height);
                }
                return null;
                #endif
            }
            catch (Exception e)
            {
                if (AssetInventory.Config.LogImageExtraction)
                {
                    Debug.LogWarning($"Could not determine image dimensions for '{file}': {e.Message}");
                }
                return null;
            }
        }

        #if UNITY_2021_2_OR_NEWER && UNITY_EDITOR_WIN
        public static bool ResizeImage(string originalFile, string outputFile, int maxSize, bool scaleBeyondSize = true, ImageFormat format = null)
        {
            Image originalImage; // leave here as otherwise temp files will be created by FromFile() for yet unknown reasons 
            try
            {
                using (originalImage = Image.FromFile(originalFile))
                {
                    int originalWidth = originalImage.Width;
                    int originalHeight = originalImage.Height;

                    // Calculate the scaling
                    double ratioX = (double)maxSize / originalWidth;
                    double ratioY = (double)maxSize / originalHeight;
                    double ratio = Math.Min(ratioX, ratioY);

                    int newWidth = Mathf.Max(1, (int)(originalWidth * ratio));
                    int newHeight = Mathf.Max(1, (int)(originalHeight * ratio));

                    if (!scaleBeyondSize && (newWidth > originalWidth || newHeight > originalHeight))
                    {
                        newWidth = originalWidth;
                        newHeight = originalHeight;
                    }

                    // Create a new empty image with the new dimensions
                    using (Bitmap newImage = new Bitmap(newWidth, newHeight))
                    {
                        using (Graphics graphics = Graphics.FromImage(newImage))
                        {
                            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                            graphics.DrawImage(originalImage, 0, 0, newWidth, newHeight);
                        }

                        // Save the resized image
                        string dir = Path.GetDirectoryName(outputFile);
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        newImage.Save(outputFile, format != null ? format : ImageFormat.Png); // Adjust the format based on your needs
                    }
                }
            }
            catch (Exception e)
            {
                if (AssetInventory.Config.LogImageExtraction)
                {
                    Debug.LogWarning($"Could not resize image '{originalFile}': {e.Message}");
                }
                return false;
            }
            return true;
        }
        #endif

        public static Texture2D Resize(this Texture2D texture, int size)
        {
            int targetX = size;
            int targetY = size;

            if (texture.width > texture.height) targetY = (int)(targetX * ((float)texture.height / texture.width));
            if (texture.height > texture.width) targetX = (int)(targetY * ((float)texture.width / texture.height));

            RenderTexture rt = RenderTexture.GetTemporary(targetX, targetY, 24, GraphicsFormat.R32G32B32A32_SFloat, 1);
            RenderTexture.active = rt;
            UnityEngine.Graphics.Blit(texture, rt);
            Texture2D result = new Texture2D(targetX, targetY, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0);
            result.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            result.Apply();
            RenderTexture.ReleaseTemporary(rt);

            return result;
        }
    }
}
