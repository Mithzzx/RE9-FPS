using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AssetInventory
{
    public sealed class OrphanedPreviewFilesValidator : Validator
    {
        public OrphanedPreviewFilesValidator()
        {
            Type = ValidatorType.FileSystem;
            Name = "Orphaned Preview Files";
            Description = "Scans the file system for preview files that are not referenced anymore.";
            FixCaption = "Remove";
        }

        public override async Task Validate()
        {
            CurrentState = State.Scanning;
            FileIssues = await GatherOrphanedPreviews();
            CurrentState = State.Completed;
        }

        public override async Task Fix()
        {
            CurrentState = State.Fixing;
            await RemoveOrphanedPreviews(FileIssues);
            CurrentState = State.Idle;
        }

        private static async Task<List<string>> GatherOrphanedPreviews()
        {
            List<string> result = new List<string>();
            List<string> files = IOUtils.GetFiles(AssetInventory.GetPreviewFolder(), new[]
            {
                "af-*.png"
            }, SearchOption.AllDirectories).ToList();

            // gather existing asset files in memory for faster processing
            Dictionary<int, AssetFile> existing = AssetImporter.ToIdDict(DBAdapter.DB.Table<AssetFile>());

            int progress = 0;
            int count = files.Count;
            int progressId = MetaProgress.Start("Gathering orphaned preview files");

            foreach (string file in files)
            {
                progress++;
                MetaProgress.Report(progressId, progress, count, file);
                if (progress % 50000 == 0) await Task.Yield();

                string[] arr = Path.GetFileNameWithoutExtension(file).Split('-');

                int assetFileId = int.Parse(arr[1]);
                if (!existing.ContainsKey(assetFileId)) result.Add(file);
            }
            MetaProgress.Remove(progressId);

            return result;
        }

        private static async Task RemoveOrphanedPreviews(List<string> files)
        {
            int progress = 0;
            int count = files.Count;
            int progressId = MetaProgress.Start("Removing orphaned preview files");

            foreach (string file in files)
            {
                progress++;
                MetaProgress.Report(progressId, progress, count, file);
                if (progress % 50 == 0) await Task.Yield();

                if (File.Exists(file)) File.Delete(file);
            }

            MetaProgress.Remove(progressId);
        }
    }
}
