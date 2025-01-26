using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AssetInventory
{
    public class TextureNameSuggester
    {
        private readonly Dictionary<string, string[]> suffixPatterns = new Dictionary<string, string[]>
        {
            {"albedo", new[] {"", "_diffuse", "_albedo", "_color", "_albedotransparency", "_dif", "_diff"}}, // Assuming the base file name is for albedo
            {"normal", new[] {"_n", "_normal", "_norm", "_nrm", "_bump"}},
            {"specular", new[] {"_spec", "_specular"}},
            {"metal", new[] {"_m", "_metal", "_metalness", "_metallicsmoothness"}},
            {"occlusion", new[] {"_ao", "_ambient", "_occlusion", "_ambientocclusion"}},
            {"displacement", new[] {"_disp", "_displacement"}},
            {"height", new[] {"_h", "_height"}},
            {"emission", new[] {"_e", "_emissive", "_glow"}},
            {"reflection", new[] {"_refl", "_reflection"}},
            {"alpha", new[] {"_alpha", "_opacity", "_transparency"}},
            {"mask", new[] {"_mask", "_maods", "_maskmap"}},
            {"roughness", new[] {"_r", "_rough", "_roughness"}}
        };

        public Dictionary<string, string> SuggestFileNames(string inputFile, Func<string, string> validationCallback)
        {
            string extension = Path.GetExtension(inputFile);
            string baseFileName = ExtractBaseFileName(inputFile);
            string baseFileNameWithoutExtension = baseFileName.Substring(0, baseFileName.Length - extension.Length);

            Dictionary<string, string> suggestions = new Dictionary<string, string>();
            suggestions.Add("original", inputFile);

            foreach (KeyValuePair<string, string[]> item in suffixPatterns)
            {
                string type = item.Key;
                foreach (string suffix in item.Value)
                {
                    string suggestedFileName = baseFileNameWithoutExtension + suffix + extension;
                    if (suggestedFileName.ToLowerInvariant() == inputFile.ToLowerInvariant())
                    {
                        suggestions[type] = inputFile;
                        break;

                    }
                    string validation = validationCallback(suggestedFileName);
                    if (!string.IsNullOrWhiteSpace(validation))
                    {
                        suggestions[type] = validation;
                        break; // Stop searching after the first match for this type
                    }
                }
            }

            return suggestions;
        }

        private string ExtractBaseFileName(string inputFileName)
        {
            List<string> knownSuffixes = suffixPatterns.SelectMany(p => p.Value).ToList();

            string extension = Path.GetExtension(inputFileName);
            string fileNameWithoutExtension = inputFileName.Substring(0, inputFileName.Length - extension.Length);

            // Attempt to remove any known suffix from the file name, start with longest
            foreach (string suffix in knownSuffixes.Where(s => !string.IsNullOrWhiteSpace(s)).OrderByDescending(s => s.Length))
            {
                // Check if the file name ends with the suffix
                if (fileNameWithoutExtension.ToLowerInvariant().EndsWith(suffix.ToLowerInvariant()))
                {
                    // Remove the suffix and return the base file name
                    return fileNameWithoutExtension.Substring(0, fileNameWithoutExtension.Length - suffix.Length) + extension;
                }
            }

            return inputFileName;
        }
    }
}
