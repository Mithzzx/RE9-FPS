using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AssetInventory
{
    public sealed class OrphanedPreviewFoldersValidator : Validator
    {
        public OrphanedPreviewFoldersValidator()
        {
            Type = ValidatorType.FileSystem;
            Name = "Orphaned Preview Folders";
            Description = "Scans the file system for preview folders that are not referenced anymore.";
            FixCaption = "Remove";
        }

        public override async Task Validate()
        {
            CurrentState = State.Scanning;
            FileIssues = await GatherOrphanedPreviewFolders();
            CurrentState = State.Completed;
        }

        public override async Task Fix()
        {
            CurrentState = State.Fixing;
            await RemoveOrphanedPreviewFolders(FileIssues);
            await Validate();
        }

        private static async Task<List<string>> GatherOrphanedPreviewFolders()
        {
            List<string> result = new List<string>();
            string[] folders = Directory.GetDirectories(AssetInventory.GetPreviewFolder());

            // gather existing assets for faster processing
            List<AssetInfo> assets = AssetInventory.LoadAssets();

            int progress = 0;
            int count = folders.Length;
            int progressId = MetaProgress.Start("Gathering orphaned preview folders");

            foreach (string folder in folders)
            {
                progress++;
                MetaProgress.Report(progressId, progress, count, folder);
                if (progress % 50 == 0) await Task.Yield();

                if (int.TryParse(Path.GetFileName(folder), out int assetId))
                {
                    if (!assets.Any(a => a.AssetId == assetId)) result.Add(folder);
                }
                else
                {
                    // non-numeric folders are always considered orphaned
                    result.Add(folder);
                }
            }
            MetaProgress.Remove(progressId);

            return result;
        }

        private static async Task RemoveOrphanedPreviewFolders(List<string> folders)
        {
            int progress = 0;
            int count = folders.Count;
            int progressId = MetaProgress.Start("Removing orphaned preview folders");

            foreach (string folder in folders)
            {
                progress++;
                MetaProgress.Report(progressId, progress, count, folder);
                if (progress % 10 == 0) await Task.Yield();

                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }

            MetaProgress.Remove(progressId);
        }
    }
}
