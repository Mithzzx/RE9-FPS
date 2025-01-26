using System.Collections.Generic;
#if UNITY_2021_2_OR_NEWER && UNITY_EDITOR_WIN
using System.Drawing.Imaging;
#endif
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace AssetInventory
{
    public sealed class PreviewImporter : AssetImporter
    {
        private const int MAX_REQUESTS = 50;
        private const int OPEN_REQUESTS = 5;

        public async Task<int> RecreatePreviews(Asset asset, List<AssetInfo> allAssets)
        {
            string query = "select *, AssetFile.Id as Id from AssetFile inner join Asset on Asset.Id = AssetFile.AssetId where Asset.Exclude = false and AssetFile.PreviewState=? " + (asset != null ? " and Asset.Id=" + asset.Id : "") + " order by Asset.Id";
            List<AssetInfo> files = DBAdapter.DB.Query<AssetInfo>(query, AssetFile.PreviewOptions.Redo).ToList();
            AssetInventory.ResolveParents(files, allAssets);

            return await RecreatePreviews(files);
        }

        public async Task<bool> RecreatePreview(AssetInfo info)
        {
            return await RecreatePreviews(new List<AssetInfo> {info}) > 0;
        }

        public async Task<int> RecreatePreviews(List<AssetInfo> files)
        {
            int created = 0;

            ResetState(false);
            int progressId = MetaProgress.Start("Recreating previews");

            SubCount = files.Count;

            PreviewGenerator.Init(files.Count);
            string previewPath = AssetInventory.GetPreviewFolder();
            foreach (AssetInfo info in files.OrderBy(info => info.AssetId))
            {
                SubProgress++;
                CurrentSub = $"Creating preview for {info.FileName}";
                MetaProgress.Report(progressId, SubProgress, SubCount, string.Empty);
                if (CancellationRequested) break;
                await Cooldown.Do();
                if (SubProgress % 5000 == 0) await Task.Yield(); // let editor breath in case there are many non-previewable files 

                if (!info.Downloaded)
                {
                    Debug.Log($"Could not recreate preview for '{info}' since the package is not downloaded.");
                    continue;
                }

                // check if previewable at all
                if (!PreviewGenerator.IsPreviewable(info.FileName, true))
                {
                    if (info.PreviewState != AssetFile.PreviewOptions.Supplied)
                    {
                        DBAdapter.DB.Execute("update AssetFile set PreviewState=? where Id=?", AssetFile.PreviewOptions.None, info.Id);
                    }
                    continue;
                }

                string previewFile = info.GetPreviewFile(previewPath);
                string sourcePath = await AssetInventory.EnsureMaterializedAsset(info);
                if (sourcePath == null)
                {
                    if (info.PreviewState != AssetFile.PreviewOptions.Supplied)
                    {
                        DBAdapter.DB.Execute("update AssetFile set PreviewState=? where Id=?", AssetFile.PreviewOptions.Error, info.Id);
                    }
                    continue;
                }

                if (SubProgress % 10 == 0) await Task.Yield(); // let editor breath

                // from Unity 2021.2+ we can take a shortcut for images since the drawing library is supported in C#
                #if UNITY_2021_2_OR_NEWER && UNITY_EDITOR_WIN
                if (ImageUtils.SYSTEM_IMAGE_TYPES.Contains(info.Type))
                {
                    // take shortcut for images and skip Unity importer
                    if (ImageUtils.ResizeImage(sourcePath, previewFile, AssetInventory.Config.upscaleSize, !AssetInventory.Config.upscaleLossless, ImageFormat.Png))
                    {
                        StorePreviewResult(new PreviewRequest {DestinationFile = previewFile, Id = info.Id, Icon = Texture2D.grayTexture, SourceFile = sourcePath});
                        created++;
                    }
                    else
                    {
                        // try to use original preview
                        string originalPreviewFile = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(sourcePath)), "preview.png");
                        if (File.Exists(originalPreviewFile))
                        {
                            File.Copy(originalPreviewFile, previewFile, true);
                            StorePreviewResult(new PreviewRequest {DestinationFile = previewFile, Id = info.Id, Icon = Texture2D.grayTexture, SourceFile = originalPreviewFile});
                            info.PreviewState = AssetFile.PreviewOptions.Supplied;
                            DBAdapter.DB.Execute("update AssetFile set PreviewState=? where Id=?", AssetFile.PreviewOptions.Supplied, info.Id);
                            created++;
                        }
                    }
                }
                else
                {
                #endif
                    // import through Unity
                    if (AssetInventory.NeedsDependencyScan(info.Type))
                    {
                        if (info.DependencyState == AssetInfo.DependencyStateOptions.Unknown) await AssetInventory.CalculateDependencies(info);
                        if (info.Dependencies.Count > 0) sourcePath = await AssetInventory.CopyTo(info, PreviewGenerator.GetPreviewWorkFolder(), true);
                    }

                    PreviewGenerator.RegisterPreviewRequest(info.Id, sourcePath, previewFile, req =>
                    {
                        StorePreviewResult(req);
                        if (req.Icon != null) created++;
                    }, info.Dependencies?.Count > 0);

                    PreviewGenerator.EnsureProgress();
                    if (PreviewGenerator.ActiveRequestCount() > MAX_REQUESTS) await PreviewGenerator.ExportPreviews(OPEN_REQUESTS);
                #if UNITY_2021_2_OR_NEWER && UNITY_EDITOR_WIN
                }
                #endif
            }
            await PreviewGenerator.ExportPreviews();
            PreviewGenerator.Clear();

            MetaProgress.Remove(progressId);
            ResetState(true);

            return created;
        }

        public static void ScheduleRecreatePreviews(Asset asset, bool missingOnly, bool retryErroneous, string[] types = null)
        {
            List<string> wheres = new List<string>();
            List<object> args = new List<object>();
            args.Add(AssetFile.PreviewOptions.Redo);

            if (retryErroneous)
            {
                if (asset != null)
                {
                    DBAdapter.DB.Execute("update AssetFile set PreviewState = ? where PreviewState = ? and AssetId = ?", AssetFile.PreviewOptions.None, AssetFile.PreviewOptions.Error, asset.Id);
                }
                else
                {
                    DBAdapter.DB.Execute("update AssetFile set PreviewState = ? where PreviewState = ?", AssetFile.PreviewOptions.None, AssetFile.PreviewOptions.Error);
                }
            }

            // sqlite does not support binding lists, parameters must be spelled out
            List<string> paramCount = new List<string>();
            IEnumerable<string> finalTypes = types ?? AssetInventory.TypeGroups["Audio"].Union(AssetInventory.TypeGroups["Images"]).Union(AssetInventory.TypeGroups["Models"]).Union(AssetInventory.TypeGroups["Prefabs"]).Union(AssetInventory.TypeGroups["Materials"]);
            foreach (string t in finalTypes)
            {
                paramCount.Add("?");
                args.Add(t);
            }
            wheres.Add("AssetFile.Type in (" + string.Join(",", paramCount) + ")");

            if (missingOnly)
            {
                wheres.Add("PreviewState = ?");
                args.Add(AssetFile.PreviewOptions.None);

                if (asset != null)
                {
                    wheres.Add("AssetId=?");
                    args.Add(asset.Id);
                }
            }
            else
            {
                if (asset == null)
                {
                    // base query only
                    Debug.LogError("This is not supported yet as it would require reparsing unity packages as well.");
                    return;
                }
                wheres.Add("AssetId=?");
                args.Add(asset.Id);
            }

            string where = wheres.Count > 0 ? "where " + string.Join(" and ", wheres) : "";
            DBAdapter.DB.Execute($"update AssetFile set PreviewState=? {where}", args.ToArray());
        }

        public static void StorePreviewResult(PreviewRequest req)
        {
            if (!File.Exists(req.DestinationFile)) return;
            AssetFile af = DBAdapter.DB.Find<AssetFile>(req.Id);
            if (af == null) return;

            if (req.Obj != null)
            {
                if (req.Obj is Texture2D tex)
                {
                    af.Width = tex.width;
                    af.Height = tex.height;
                }
                if (req.Obj is AudioClip clip)
                {
                    af.Length = clip.length;
                }
            }

            // do not remove originally supplied previews even in case of error
            af.PreviewState = req.Icon != null ? AssetFile.PreviewOptions.Custom : (af.PreviewState != AssetFile.PreviewOptions.Supplied ? AssetFile.PreviewOptions.Error : AssetFile.PreviewOptions.Supplied);
            af.Hue = -1;

            DBAdapter.DB.Update(af);
        }
    }
}
