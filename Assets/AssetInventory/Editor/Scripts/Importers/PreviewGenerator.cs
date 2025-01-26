using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetInventory
{
    public static class PreviewGenerator
    {
        private const string PREVIEW_FOLDER = "_AssetInventoryPreviewsTemp";
        private const int MIN_PREVIEW_CACHE_SIZE = 200;
        private const float PREVIEW_TIMEOUT = 25f;
        private const float MIN_PREVIEW_WAIT = 6f;
        private const int BREAK_INTERVAL = 30;
        private static readonly List<PreviewRequest> _requests = new List<PreviewRequest>();

        public static void Init(int expectedFileCount)
        {
            AssetPreview.SetPreviewTextureCacheSize(Mathf.Max(MIN_PREVIEW_CACHE_SIZE, expectedFileCount + 100));
        }

        public static int ActiveRequestCount() => _requests.Count;

        public static string GetPreviewWorkFolder()
        {
            string targetDir = Path.Combine(Application.dataPath, PREVIEW_FOLDER);
            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

            return targetDir;
        }

        public static void RegisterPreviewRequest(int id, string sourceFile, string previewDestination, Action<PreviewRequest> onSuccess, bool useSourceDirectly = false)
        {
            PreviewRequest request = new PreviewRequest
            {
                Id = id, SourceFile = sourceFile, DestinationFile = previewDestination, OnSuccess = onSuccess
            };

            if (useSourceDirectly)
            {
                request.TempFile = sourceFile;
            }
            else
            {
                string targetDir = GetPreviewWorkFolder();
                request.TempFile = Path.Combine(targetDir, id + Path.GetExtension(sourceFile));
                try
                {
                    File.Copy(sourceFile, request.TempFile, true);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"File is inaccessible. Preview could not be generated for '{sourceFile}': {e.Message}");
                    return;
                }
            }

            request.TempFileRel = request.TempFile;
            if (request.TempFileRel.StartsWith(Application.dataPath))
            {
                request.TempFileRel = "Assets" + request.TempFileRel.Substring(Application.dataPath.Length);
            }
            if (!File.Exists(request.TempFileRel)) AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport); // can happen in very rare cases, not yet clear why
            if (!File.Exists(request.TempFileRel))
            {
                Debug.LogWarning($"Preview could not be generated for: {sourceFile}");
                return;
            }
            AssetDatabase.ImportAsset(request.TempFileRel);

            // trigger creation, fetch later as it takes a while
            request.Obj = AssetDatabase.LoadAssetAtPath<Object>(request.TempFileRel);
            if (request.Obj != null)
            {
                request.TimeStarted = Time.realtimeSinceStartup;
                AssetPreview.GetAssetPreview(request.Obj);
                _requests.Add(request);
            }
            else
            {
                Debug.LogError($"Queuing preview request failed for: {sourceFile}");
            }
        }

        public static void EnsureProgress()
        {
            // Unity is so buggy when creating previews, you need to hammer the GetAssetPreview call
            for (int i = _requests.Count - 1; i >= 0; i--)
            {
                PreviewRequest req = _requests[i];
                if (req.Icon != null) continue;

                req.Icon = AssetPreview.GetAssetPreview(req.Obj);
                if (req.Icon == null && AssetPreview.IsLoadingAssetPreview(req.Obj.GetInstanceID()))
                {
                    AssetPreview.GetAssetPreview(req.Obj);
                }
            }
        }

        public static async Task ExportPreviews(int limit = 0)
        {
            while (_requests.Count > limit)
            {
                await Task.Yield();
                for (int i = _requests.Count - 1; i >= 0; i--)
                {
                    PreviewRequest req = _requests[i];
                    if (req.Icon == null)
                    {
                        req.Icon = AssetPreview.GetAssetPreview(req.Obj);
                        if (req.Icon == null && AssetPreview.IsLoadingAssetPreview(req.Obj.GetInstanceID()))
                        {
                            AssetPreview.GetAssetPreview(req.Obj);
                            if (Time.realtimeSinceStartup - req.TimeStarted < PREVIEW_TIMEOUT) continue;
                        }
                        if (req.Icon == null) req.Icon = AssetPreview.GetAssetPreview(req.Obj);
                        if (req.Icon == null && Time.realtimeSinceStartup - req.TimeStarted < MIN_PREVIEW_WAIT) continue;
                    }

                    // still will not return something for all assets
                    if (req.Icon != null && req.Icon.isReadable)
                    {
                        byte[] bytes = req.Icon.EncodeToPNG();
                        if (bytes != null)
                        {
                            string targetDir = Path.GetDirectoryName(req.DestinationFile);
                            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                            File.WriteAllBytes(req.DestinationFile, bytes);
                        }
                    }
                    req.OnSuccess?.Invoke(req);

                    // check if file is directly in temp folder (means had no dependencies) or in a subdirectory (means had dependencies so cannot be deleted if there are more requests still in the queue)
                    if (Path.GetFileName(Path.GetDirectoryName(req.TempFile)) == PREVIEW_FOLDER)
                    {
                        // delete asset again, this will also null the Obj field
                        if (!AssetDatabase.DeleteAsset(req.TempFileRel))
                        {
                            await IOUtils.DeleteFileOrDirectory(req.TempFile);
                            await IOUtils.DeleteFileOrDirectory(req.TempFile + ".meta");
                        }
                    }
                    _requests.RemoveAt(i);
                    if (i % BREAK_INTERVAL == 0) await Task.Yield(); // let editor breath in case many files are already indexed
                }
            }
        }

        public static void Clear()
        {
            _requests.Clear();

            string targetDir = Path.Combine(Application.dataPath, PREVIEW_FOLDER);
            if (!Directory.Exists(targetDir)) return;

            try
            {
                Directory.Delete(targetDir, true);
                FileUtil.DeleteFileOrDirectory(targetDir + ".meta");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not remove temporary preview folder '{targetDir}'. Please do so manually: {e.Message}");
            }

            AssetDatabase.Refresh();
        }

        public static bool IsPreviewable(string file, bool includeProblematic)
        {
            if (file.Contains("__MACOSX")) return false;
            if (includeProblematic)
            {
                // TODO: will need dependency materialization
                // Materials will not have correct texture references but stock shaders and colors will work
                // Prefabs always have a reference to another object and will always be broken
                return AssetInventory.IsFileType(file, "Audio")
                    || AssetInventory.IsFileType(file, "Images")
                    || AssetInventory.IsFileType(file, "Models")
                    || AssetInventory.IsFileType(file, "Prefabs")
                    || AssetInventory.IsFileType(file, "Materials");
            }
            return AssetInventory.IsFileType(file, "Audio")
                || AssetInventory.IsFileType(file, "Images")
                || AssetInventory.IsFileType(file, "Models");
        }
    }

    public sealed class PreviewRequest
    {
        public int Id;
        public string SourceFile;
        public string TempFile;
        public string TempFileRel;
        public string DestinationFile;
        public Object Obj;
        public Action<PreviewRequest> OnSuccess;

        // runtime properties
        public float TimeStarted;
        public Texture2D Icon;
    }
}
