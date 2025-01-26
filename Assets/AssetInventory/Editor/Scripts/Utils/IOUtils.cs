using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#if UNITY_2021_2_OR_NEWER
using SharpCompress.Archives;
using SharpCompress.Common;
#endif
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace AssetInventory
{
    public static class IOUtils
    {
        public static bool PathContainsInvalidChars(string path)
        {
            return !string.IsNullOrEmpty(path) && path.IndexOfAny(Path.GetInvalidPathChars()) >= 0;
        }
        
        public static string RemoveInvalidChars(string path)
        {
            return string.Concat(path.Split(Path.GetInvalidFileNameChars()));
        }
        
        public static string CamelCaseToWords(string input)
        {
            string pattern = @"(?<=[a-z])(?=[A-Z])|(?<=[0-9])(?=[A-Z])|(?<=[A-Z])(?=[0-9])|(?<=[0-9])(?=[a-z])";
            string result = Regex.Replace(input, pattern, " ");
            
            // Further refinement to handle cases with consecutive uppercase letters properly:
            // Ensure space before the start of a new word starting with an uppercase letter followed by lowercase letters
            result = Regex.Replace(result, @"(?<= [A-Z])(?=[A-Z][a-z])", " ");
            
            // Handle special characters (parentheses)
            result = Regex.Replace(result, @"(?<=[^\s])(?=[(])|(?<=[)])(?=[^\s])", " ");
            
            // Split the result into words
            string[] words = result.Split(' ');
            
            // Capitalize the first letter of each word, but keep acronyms in upper case
            for (int i = 0; i < words.Length; i++)
            {
                words[i] = CapitalizeFirstLetter(words[i]);
            }
            
            return string.Join(" ", words);
        }
        
        private static string CapitalizeFirstLetter(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                return word;
            }
            
            // Preserve the case of the rest of the word
            return char.ToUpper(word[0]) + word.Substring(1);
        }
        
        public static string GetShortHash(string input, int length = 6)
        {
            if (length < 1 || length > 10)
            {
                throw new ArgumentOutOfRangeException(nameof (length), "Length must be between 1 and 10.");
            }
            
            // Compute a simple hash from the input string.
            int hash = 0;
            foreach (char c in input)
            {
                hash = (hash * 31 + c); // Use a prime number multiplier
            }
            
            // Calculate the modulus based on the desired length
            int mod = (int)Math.Pow(10, length);
            
            // Reduce the hash to a number with the desired length
            int shortHash = Math.Abs(hash) % mod;
            
            // Return the hash as a string, padded with leading zeros if necessary
            return shortHash.ToString($"D{length}");
        }
        
        // faster helper method using also fewer allocations 
        public static string GetExtensionWithoutDot(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }
            
            int dotIndex = path.LastIndexOf('.');
            if (dotIndex >= 0 && dotIndex < path.Length - 1) // Ensure there is an extension and it's not the last character
            {
                return path.Substring(dotIndex + 1);
            }
            
            return string.Empty;
        }
        
        public static string GetFileName(string path, bool returnOriginalOnError = true, bool quiet = true)
        {
            try
            {
                return Path.GetFileName(path);
            }
            catch (Exception e)
            {
                if (!quiet) Debug.LogError($"Illegal characters in path '{path}': {e}");
                return returnOriginalOnError ? path : null;
            }
        }
        
        public static bool IsUnicode(this string input)
        {
            return input.ToCharArray().Any(c => c > 255);
        }
        
        public static string StripTags(string input)
        {
            return Regex.Replace(input, "<.*?>", string.Empty);
        }
        
        public static string StripUnicode(string input)
        {
            return Regex.Replace(input, "&#.*?;", string.Empty);
        }
        
        public static string ReadFirstLine(string path)
        {
            string result = null;
            try
            {
                using (StreamReader reader = new StreamReader(path))
                {
                    result = reader.ReadLine();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading file '{path}': {e.Message}");
            }
            
            return result;
        }
        
        public static string ToLabel(string input)
        {
            string result = input;
            
            // Normalize line breaks to \n
            result = Regex.Replace(result, @"\r\n?|\n", "\n");
            
            // Translate some HTML tags
            result = result.Replace("<br>", "\n");
            result = result.Replace("</br>", "\n");
            result = result.Replace("<p>", "\n\n");
            result = result.Replace("<p >", "\n\n");
            result = result.Replace("<li>", "\n* ");
            result = result.Replace("<li >", "\n* ");
            result = result.Replace("&nbsp;", " ");
            result = result.Replace("&amp;", "&");
            
            // Remove remaining tags and also unicode tags
            result = StripUnicode(StripTags(result));
            
            // Remove whitespace from empty lines
            result = Regex.Replace(result, @"[ \t]+\n", "\n");
            
            // Ensure at max two consecutive line breaks
            result = Regex.Replace(result, @"\n{3,}", "\n\n");
            
            return result.Trim();
        }
        
        public static string ToLowercaseFirstLetter(this string input)
        {
            if (string.IsNullOrEmpty(input) || char.IsLower(input[0]))
            {
                return input;
            }
            
            return char.ToLower(input[0]) + input.Substring(1);
        }
        
        public static bool TryDeleteFile(string path)
        {
            try
            {
                File.Delete(path);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        public static async Task DeleteFileOrDirectory(string path, int retries = 3)
        {
            while (retries >= 0)
            {
                try
                {
                    FileUtil.DeleteFileOrDirectory(path); // use Unity method to circumvent unauthorized access that can happen every now and then
                    break;
                }
                catch
                {
                    retries--;
                    if (retries >= 0) await Task.Delay(200);
                }
            }
        }
        
        public static void Populate<T>(this T[] arr, T value)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = value;
            }
        }
        
        // Regex version
        public static IEnumerable<string> GetFiles(string path, string searchPatternExpression = "", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            Regex reSearchPattern = new Regex(searchPatternExpression, RegexOptions.IgnoreCase);
            return Directory.EnumerateFiles(path, "*", searchOption)
                .Where(file => reSearchPattern.IsMatch(Path.GetExtension(file)));
        }
        
        // Takes multiple patterns and executes in parallel
        public static IEnumerable<string> GetFiles(string path, IEnumerable<string> searchPatterns, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            return searchPatterns.AsParallel()
                .SelectMany(searchPattern => Directory.EnumerateFiles(path, searchPattern, searchOption));
        }
        
        public static bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }
        
        public static bool IsSameDirectory(string path1, string path2)
        {
            DirectoryInfo di1 = new DirectoryInfo(path1);
            DirectoryInfo di2 = new DirectoryInfo(path2);
            
            return string.Equals(di1.FullName, di2.FullName, StringComparison.OrdinalIgnoreCase);
        }
        
        public static void CopyDirectory(string sourceDir, string destDir, bool includeSubDirs = true)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDir);
            DirectoryInfo[] dirs = dir.GetDirectories();
            if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
            
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDir, file.Name);
                file.CopyTo(tempPath, false);
            }
            
            if (includeSubDirs)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string tempPath = Path.Combine(destDir, subDir.Name);
                    CopyDirectory(subDir.FullName, tempPath, includeSubDirs);
                }
            }
        }
        
        public static string GetEnvVar(string key)
        {
            string value = Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process);
            if (string.IsNullOrWhiteSpace(value)) value = Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.User);
            if (string.IsNullOrWhiteSpace(value)) value = Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Machine);
            
            return value;
        }
        
        public static async Task<long> GetFolderSize(string folder, bool async = true)
        {
            if (!Directory.Exists(folder)) return 0;
            DirectoryInfo dirInfo = new DirectoryInfo(folder);
            try
            {
                if (async)
                {
                    // FIXME: this can crash Unity
                    return await Task.Run(() => dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length));
                }
                return dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
            }
            catch
            {
                return 0;
            }
        }
        
        /// <summary>
        /// Returns a combined path with unified slashes
        /// </summary>
        /// <returns></returns>
        public static string PathCombine(params string[] path)
        {
            return Path.GetFullPath(Path.Combine(path));
        }
        
        public static string ExecuteCommand(string command, string arguments)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo(command, arguments)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            try
            {
                using (Process process = new Process {StartInfo = processStartInfo})
                {
                    process.Start();
                    string result = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return result;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error executing command '{command}': {e.Message}");
                return null;
            }
        }
        
#if UNITY_2021_2_OR_NEWER
        public static void ExtractArchive(string archiveFile, string targetFolder)
        {
            if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);
            
            try
            {
                using (IArchive archive = ArchiveFactory.Open(archiveFile))
                {
                    foreach (IArchiveEntry entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Key)) continue;
                        
                        if (!entry.IsDirectory)
                        {
                            string fullOutputPath = Path.Combine(targetFolder, entry.Key);
                            string directoryName = Path.GetDirectoryName(fullOutputPath);
                            if (!Directory.Exists(directoryName)) Directory.CreateDirectory(directoryName);
                            
                            entry.WriteToDirectory(targetFolder, new ExtractionOptions
                            {
                                Overwrite = true,
                                ExtractFullPath = true
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not extract file from archive '{archiveFile}'. The process was either interrupted or the file is corrupted: {e.Message}");
            }
        }
#endif
    }
}
