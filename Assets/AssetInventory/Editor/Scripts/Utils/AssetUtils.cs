using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.Build;
#endif
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;

namespace AssetInventory
{
    public static class AssetUtils
    {
        private static readonly Regex NoSpecialChars = new Regex("[^a-zA-Z0-9 -]"); // private static Regex AssetStoreContext.s_InvalidPathCharsRegExp = new Regex("[^a-zA-Z0-9() _-]");
        private static readonly Dictionary<string, Texture2D> PreviewCache = new Dictionary<string, Texture2D>();

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T element in source)
            {
                action(element);
            }
        }

#if UNITY_2021_2_OR_NEWER
        private static List<string> GetCurrentDefines() => PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup)).Split(';').ToList();
        private static void SetCurrentDefines(IEnumerable<string> keywords) => PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup), string.Join(";", keywords));
#else
        private static List<string> GetCurrentDefines() => PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';').ToList();

        private static void SetCurrentDefines(IEnumerable<string> keywords) => PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", keywords));
#endif

        public static bool HasDefine(string keyword) => GetCurrentDefines().Contains(keyword);

        public static void AddDefine(string keyword) => SetCurrentDefines(GetCurrentDefines().Union(new List<string> {keyword}));
        public static void RemoveDefine(string keyword) => SetCurrentDefines(GetCurrentDefines().Where(d => d != keyword));

        public static int GetPageCount(int resultCount, int maxResults)
        {
            return (int)Math.Ceiling((double)resultCount / (maxResults > 0 ? maxResults : int.MaxValue));
        }

        public static void ClearCache()
        {
            PreviewCache.Clear();
        }

        public static string RemoveTrailing(this string source, string text)
        {
            if (source == null)
            {
                Debug.LogError("This should not happen, source path is null");
                return null;
            }

            while (source.EndsWith(text)) source = source.Substring(0, source.Length - text.Length);
            return source;
        }

        public static int RemoveMissingScripts(this Transform obj)
        {
            int result = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj.gameObject);
            for (int i = 0; i < obj.childCount; i++)
            {
                result += RemoveMissingScripts(obj.GetChild(i));
            }
            return result;
        }

        public static async Task<AudioClip> LoadAudioFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            // workaround for Unity not supporting loading local files with # or + or unicode chars in the name
            if (filePath.Contains("#") || filePath.Contains("+") || filePath.IsUnicode())
            {
                string newName = Path.Combine(Application.temporaryCachePath, "AIAudioPreview" + Path.GetExtension(filePath));
                File.Copy(filePath, newName, true);
                filePath = newName;
            }

            // use uri form to support network shares
            string fileUri;
            try
            {
                fileUri = new Uri(filePath).AbsoluteUri;
            }
            catch (UriFormatException e)
            {
                Debug.LogError($"Could not convert path to URI '{filePath}': {e.Message}");
                return null;
            }

            // select appropriate audio type from extension where UNKNOWN heuristic can fail, especially for AIFF
            AudioType type = AudioType.UNKNOWN;
            switch (Path.GetExtension(filePath).ToLowerInvariant())
            {
                case ".aiff":
                    type = AudioType.AIFF;
                    break;
            }

            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(fileUri, type))
            {
                ((DownloadHandlerAudioClip)uwr.downloadHandler).streamAudio = true;
                uwr.timeout = AssetInventory.Config.timeout;
                UnityWebRequestAsyncOperation request = uwr.SendWebRequest();
                while (!request.isDone) await Task.Yield();

#if UNITY_2020_1_OR_NEWER
                if (uwr.result != UnityWebRequest.Result.Success)
#else
                if (uwr.isNetworkError || uwr.isHttpError)
#endif
                {
                    Debug.LogError($"Error fetching '{filePath} ({fileUri})': {uwr.error}");
                    return null;
                }

                DownloadHandlerAudioClip dlHandler = (DownloadHandlerAudioClip)uwr.downloadHandler;
                dlHandler.streamAudio = false; // otherwise tracker files won't work
                if (dlHandler.isDone)
                {
                    // can fail if FMOD encounters incorrect file, will return zero-length then, error cannot be suppressed
                    AudioClip clip = dlHandler.audioClip;
                    if (AssetInventory.Config.LogAudioParsing && (clip == null || (clip.channels == 0 && clip.length == 0)))
                    {
                        Debug.LogError($"Unity could not load incompatible audio clip '{filePath} ({fileUri})'");
                    }

                    return clip;
                }
            }

            return null;
        }

        public static async void LoadTextures(List<AssetInfo> infos, CancellationToken ct, Action<int, Texture2D> callback = null)
        {
            for (int i = 0; i < infos.Count; i++)
            {
                AssetInfo info = infos[i];
                if (ct.IsCancellationRequested) break;

                if (info.ParentInfo != null)
                {
                    await LoadPackageTexture(info.ParentInfo);
                    info.PreviewTexture = info.ParentInfo.PreviewTexture;
                }
                else
                {
                    await LoadPackageTexture(info);
                }
                callback?.Invoke(i, info.PreviewTexture);
            }
        }

        public static async Task LoadPackageTexture(AssetInfo info, bool useCache = true)
        {
            string file = info.ToAsset().GetPreviewFile(AssetInventory.GetPreviewFolder());
            if (string.IsNullOrEmpty(file)) return;

            Texture2D texture;
            if (useCache && PreviewCache.TryGetValue(file, out Texture2D pt) && pt != null)
            {
                texture = pt;
            }
            else
            {
                texture = await LoadLocalTexture(file, true);
                if (texture != null) PreviewCache[file] = texture;
            }
            if (texture != null) info.PreviewTexture = texture;
        }

        public static void RemoveFromPreviewCache(string file)
        {
            if (PreviewCache.ContainsKey(file)) PreviewCache.Remove(file);
        }

        public static async Task<Texture2D> LoadLocalTexture(string file, bool useCache, int upscale = 0)
        {
            if (useCache && PreviewCache.TryGetValue(file, out Texture2D texture) && texture != null) return texture;

            try
            {
                UnityWebRequest uwr = UnityWebRequestTexture.GetTexture("file://" + file);
                uwr.timeout = AssetInventory.Config.timeout;
                UnityWebRequestAsyncOperation request = uwr.SendWebRequest();
                while (!request.isDone) await Task.Yield();

                Texture2D result = DownloadHandlerTexture.GetContent(uwr);
                if (upscale > 0 && result.width < upscale && result.height < upscale) result = result.Resize(upscale);
                if (useCache) PreviewCache[file] = result;

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading texture '{file}': {e.Message}");
                return null;
            }
        }

        public static async Task<T> FetchAPIData<T>(string uri, string token = null, string etag = null, Action<string> eTagCallback = null, int retries = 1, Action<long> responseIssueCodeCallback = null, bool suppressErrors = false)
        {
            Restart:
            using (UnityWebRequest uwr = UnityWebRequest.Get(uri))
            {
                if (!string.IsNullOrEmpty(token)) uwr.SetRequestHeader("Authorization", "Bearer " + token);
                if (!string.IsNullOrEmpty(etag)) uwr.SetRequestHeader("If-None-Match", etag);
                uwr.timeout = AssetInventory.Config.timeout;
                UnityWebRequestAsyncOperation request = uwr.SendWebRequest();
                while (!request.isDone) await Task.Yield();

#if UNITY_2020_1_OR_NEWER
                if (uwr.result == UnityWebRequest.Result.ConnectionError)
#else
                if (uwr.isNetworkError)
#endif
                {
                    if (retries > 0)
                    {
                        retries--;
                        goto Restart;
                    }
                    if (!suppressErrors) Debug.LogError($"Could not fetch API data from {uri} due to network issues: {uwr.error}");
                }
#if UNITY_2020_1_OR_NEWER
                else if (uwr.result == UnityWebRequest.Result.ProtocolError)
#else
                else if (uwr.isHttpError)
#endif
                {
                    responseIssueCodeCallback?.Invoke(uwr.responseCode);
                    if (uwr.responseCode == (int)HttpStatusCode.Unauthorized)
                    {
                        if (!suppressErrors) Debug.LogError($"Invalid or expired API Token when contacting {uri}");
                    }
                    else
                    {
                        if (!suppressErrors) Debug.LogError($"Error fetching API data from {uri} ({uwr.responseCode}): {uwr.downloadHandler.text}");
                    }
                }
                else
                {
                    if (typeof (T) == typeof (string))
                    {
                        return (T)Convert.ChangeType(uwr.downloadHandler.text, typeof (T));
                    }

                    string newEtag = uwr.GetResponseHeader("ETag");
                    if (!string.IsNullOrEmpty(newEtag) && newEtag != etag) eTagCallback?.Invoke(newEtag);

                    try
                    {
                        return JsonConvert.DeserializeObject<T>(uwr.downloadHandler.text);
                    }
                    catch (Exception e)
                    {
                        // can happen if deserializers in local project have been added/altered
                        Debug.LogError($"Error parsing API data from {uri}: {e.Message}");
                    }
                }
            }

            return default(T);
        }

        public static async Task LoadImageAsync(string imageUrl, string targetFile)
        {
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(imageUrl))
            {
                // Send the request and wait for the response without blocking the main thread
                uwr.timeout = AssetInventory.Config.timeout;
                UnityWebRequestAsyncOperation request = uwr.SendWebRequest();
                while (!request.isDone) await Task.Yield();

#if UNITY_2020_1_OR_NEWER
                if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
#else
                if (uwr.isNetworkError || uwr.isHttpError)
#endif
                {
                    if (AssetInventory.Config.LogMediaDownloads) Debug.LogWarning($"Failed to download image from {imageUrl}: {uwr.error}");
                }
                else
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                    byte[] imageBytes = texture.EncodeToPNG();

                    if (!Directory.Exists(Path.GetDirectoryName(targetFile))) Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
                    int retries = 3;
                    do
                    {
                        try
                        {
#if UNITY_2021_2_OR_NEWER
                            await File.WriteAllBytesAsync(targetFile, imageBytes);
                            break;
#else
                            File.WriteAllBytes(targetFile, imageBytes);
                            break;
#endif
                        }
                        catch (Exception e)
                        {
                            if (AssetInventory.Config.LogMediaDownloads) Debug.LogWarning($"Could not download image to {targetFile}, retrying: {e.Message}");

                            // can happen if file is locked (sharing violation)
                            retries--;
                            await Task.Delay(100);
                        }
                    } while (retries > 0);
                }
            }
        }

        // https://forum.unity.com/threads/handle-cannot-create-fmod-on-unitywebrequestmultimedia-getaudioclip.1139980/
        public static bool IsMp3File(string filePath)
        {
            byte[] mp3Header = {0xFF, 0xFB}; // Typical MP3 frame sync bits.
            byte[] id3Header = {0x49, 0x44, 0x33}; // 'ID3' in ASCII.
            byte[] bytes = new byte[3]; // Read the first three bytes of the file.

            using (FileStream file = File.OpenRead(filePath))
            {
                if (file.Length < 3)
                {
                    return false;
                }

                file.Read(bytes, 0, 3);
            }

            // Return true if we found an MP3 frame header or an ID3v2 tag.
            return bytes.SequenceEqual(mp3Header) || bytes.SequenceEqual(id3Header);
        }

        public static string GuessSafeName(string name, string replacement = "")
        {
            // remove special characters like Unity does when saving to disk
            // This will work in 99% of cases but sometimes items get renamed and
            // Unity will keep the old safe name so this needs to be synced with the 
            // download info API.
            string clean = name;

            // remove special characters
            clean = NoSpecialChars.Replace(clean, replacement);

            // remove duplicate spaces
            clean = Regex.Replace(clean, @"\s+", " ");

            return clean.Trim();
        }

        public static List<AssetInfo> Guid2File(string guid)
        {
            string query = "select * from AssetFile inner join Asset on Asset.Id = AssetFile.AssetId where Guid=?";
            List<AssetInfo> files = DBAdapter.DB.Query<AssetInfo>($"{query}", guid);
            return files;
        }

        public static string ExtractGuidFromFile(string path)
        {
            string guid = null;
            try
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.StartsWith("guid:"))
                        {
                            guid = line;
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading guid from '{path}': {e.Message}");
                return null;
            }

            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"Could not find guid in meta file: {path}");
                return null;
            }

            return guid.Substring(6);
        }

        public static bool IsUrl(string url)
        {
            return Uri.IsWellFormedUriString(url, UriKind.Absolute);
        }

        public static bool IsOnURP()
        {
            RenderPipelineAsset rpa = GraphicsSettings.defaultRenderPipeline;
            if (rpa == null) return false;

            return rpa.GetType().Name.Contains("UniversalRenderPipelineAsset");
        }

        public static List<string> ExtractIncludedFiles(string shaderCode)
        {
            List<string> result = new List<string>();
            string includePattern = @"#include\s*""(.+?)"""; // Regex to match include lines and capture file names

            MatchCollection matches = Regex.Matches(shaderCode, includePattern);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    result.Add(match.Groups[1].Value);
                }
            }

            return result;
        }

        public static List<string> ExtractCustomEditors(string shaderCode)
        {
            List<string> result = new List<string>();
            string customEditorPattern = @"CustomEditor\s*""(.+?)"""; // Regex to match custom editor lines and capture names

            MatchCollection matches = Regex.Matches(shaderCode, customEditorPattern);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    result.Add(match.Groups[1].Value);
                }
            }

            return result;
        }

        public static bool IsUnityProject(string folder)
        {
            return Directory.Exists(Path.Combine(folder, "Assets"))
                && Directory.Exists(Path.Combine(folder, "Library"))
                && Directory.Exists(Path.Combine(folder, "Packages"))
                && Directory.Exists(Path.Combine(folder, "ProjectSettings"));
        }
    }
}