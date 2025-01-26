using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AssetInventory
{
    public sealed class ArchiveImporter : AssetImporter
    {
        private const int BREAK_INTERVAL = 30;

        public async Task Index(FolderSpec spec)
        {
            ResetState(false);

            if (string.IsNullOrEmpty(spec.location)) return;

            string[] files = IOUtils.GetFiles(spec.GetLocation(true), new[] {"*.zip", "*.rar", "*.7z"}, SearchOption.AllDirectories).ToArray();

            MainCount = files.Length;
            MainProgress = 1; // small hack to trigger UI update in the end

            int progressId = MetaProgress.Start("Updating archives index");
            for (int i = 0; i < files.Length; i++)
            {
                if (CancellationRequested) break;
                if (i % BREAK_INTERVAL == 0) await Task.Yield(); // let editor breath in case many files are already indexed
                await Cooldown.Do();

                string package = files[i];
                Asset asset = HandlePackage(package);
                if (asset == null) continue;

                MetaProgress.Report(progressId, i + 1, files.Length, package);
                MainCount = files.Length;
                CurrentMain = package + " (" + EditorUtility.FormatBytes(asset.PackageSize) + ")";
                MainProgress = i + 1;

                await Task.Yield();
                await IndexPackage(asset, spec);
                await Task.Yield();

                if (CancellationRequested) break;

                if (spec.assignTag && !string.IsNullOrWhiteSpace(spec.tag))
                {
                    AssetInventory.AddTagAssignment(new AssetInfo(asset), spec.tag, TagAssignment.Target.Package);
                }
            }
            MetaProgress.Remove(progressId);
            ResetState(true);
        }

        private static Asset HandlePackage(string package, Asset parent = null, AssetFile subPackage = null)
        {
            Asset asset = new Asset();
            if (parent == null)
            {
                asset.SafeName = Path.GetFileNameWithoutExtension(package);
                asset.SetLocation(AssetInventory.MakeRelative(package));
            }
            else
            {
                // package inherits nearly everything from parent
                asset.CopyFrom(parent);
                asset.ForeignId = 0; // will otherwise override metadata when syncing with store
                asset.ParentId = parent.Id;
                asset.SafeName = Path.GetFileNameWithoutExtension(package);

                string relPackage = $"{parent.Location}{Asset.SUB_PATH}{subPackage.Path}";
                asset.SetLocation(relPackage);
            }
            asset.DisplayName = IOUtils.CamelCaseToWords(asset.SafeName.Replace("_", " ")).Trim();
            asset.AssetSource = Asset.Source.Archive;

            Asset existing = DBAdapter.DB.Table<Asset>().FirstOrDefault(a => a.Location == asset.Location);

            long size; // determine late for performance, especially with many exclusions
            FileInfo fInfo;
            if (existing != null)
            {
                if (existing.Exclude) return null;

                fInfo = new FileInfo(package);
                size = fInfo.Length;
                if (existing.CurrentState == Asset.State.Done && existing.PackageSize == size && existing.Location == asset.Location) return null;

                asset = existing;
            }
            else
            {
                fInfo = new FileInfo(package);
                size = fInfo.Length;
            }
            asset.PackageSize = size;
            asset.LastRelease = fInfo.LastWriteTime;
            Persist(asset);
            ApplyOverrides(asset);

            return asset;
        }

        public async Task IndexDetails(Asset asset)
        {
            ResetState(false);

            MainCount = 1;
            CurrentMain = "Indexing archive";

            FolderSpec importSpec = GetDefaultImportSpec();
            importSpec.createPreviews = true; // TODO: derive from additional folder settings
            await IndexPackage(asset, importSpec);

            ResetState(true);
        }

        private static async Task IndexPackage(Asset asset, FolderSpec spec)
        {
            await RemovePersistentCacheEntry(asset);
            string tempPath = await AssetInventory.ExtractAsset(asset);
            if (string.IsNullOrEmpty(tempPath))
            {
                Debug.LogError($"{asset} could not be indexed due to issues extracting it: {asset.Location}");
                return;
            }

            FolderSpec importSpec = GetDefaultImportSpec();
            importSpec.location = tempPath;
            importSpec.createPreviews = spec.createPreviews;
            await new MediaImporter().Index(importSpec, asset, true, true);
            RemoveWorkFolder(asset, tempPath);

            // update only individual fields to not override potential changes in metadata during indexing
            asset.CurrentState = Asset.State.Done;
            DBAdapter.DB.Execute("update Asset set CurrentState=? where Id=?", asset.CurrentState, asset.Id);
        }

        public static async Task ProcessSubArchives(Asset asset, List<AssetFile> subArchives)
        {
            // index sub-packages while extracted
            if (AssetInventory.Config.indexSubPackages && subArchives.Count > 0)
            {
                int subProgressId2 = MetaProgress.Start("Indexing sub-archives");
                for (int i = 0; i < subArchives.Count; i++)
                {
                    if (CancellationRequested) break;

                    CurrentMain = "Indexing sub-archives";
                    MainCount = subArchives.Count;
                    MainProgress = i + 1;

                    AssetFile subArchive = subArchives[i];
                    string path = await AssetInventory.EnsureMaterializedAsset(asset, subArchive);
                    if (path == null)
                    {
                        Debug.LogError($"Could materialize sub-archive '{subArchive.Path}' for '{asset.DisplayName}'");
                        continue;
                    }
                    Asset subAsset = HandlePackage(path, asset, subArchive);
                    if (subAsset == null) continue;

                    // index immediately
                    FolderSpec importSpec = GetDefaultImportSpec();
                    importSpec.createPreviews = true; // TODO: derive from additional folder settings

                    await IndexPackage(subAsset, importSpec);
                    subAsset.CurrentState = Asset.State.Done;
                    Persist(subAsset);
                }
                MetaProgress.Remove(subProgressId2);
            }
        }
    }
}