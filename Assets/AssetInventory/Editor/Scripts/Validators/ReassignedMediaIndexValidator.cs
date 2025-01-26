using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AssetInventory
{
    public sealed class ReassignedMediaIndexValidator : Validator
    {
        public ReassignedMediaIndexValidator()
        {
            Type = ValidatorType.DB;
            Name = "Reassigned Media Index";
            Description = "Checks if media files were previously stored with no package assignment but have one in the meantime and will remove the references without package assignment.";
            FixCaption = "Remove";
        }

        public override async Task Validate()
        {
            CurrentState = State.Scanning;

            DBIssues = await GatherReassignedMediaIndexes();

            CurrentState = State.Completed;
        }

        public override async Task Fix()
        {
            CurrentState = State.Fixing;
            await RemoveReassignedMediaIndexes(DBIssues);
            await Validate();
        }

        private static async Task<List<AssetInfo>> GatherReassignedMediaIndexes()
        {
            List<AssetInfo> result = new List<AssetInfo>();
            Asset noAsset = DBAdapter.DB.Find<Asset>(a => a.SafeName == Asset.NONE);
            if (noAsset == null) return result;

            List<AssetInfo> files = DBAdapter.DB.Query<AssetInfo>("select *, AssetFile.Id as Id from AssetFile left join Asset on Asset.Id = AssetFile.AssetId where Asset.AssetSource=2");
            List<AssetInfo> unlinked = files.Where(f => f.AssetId == noAsset.Id).ToList();
            List<AssetInfo> linked = files.Where(f => f.AssetId != noAsset.Id).ToList();
            if (linked.Count == 0) return result;

            int progress = 0;
            int count = unlinked.Count;
            int progressId = MetaProgress.Start("Gathering duplicate indexed media files");

            foreach (AssetInfo file in unlinked)
            {
                progress++;
                MetaProgress.Report(progressId, progress, count, file.FileName);
                if (progress % 1000 == 0) await Task.Yield();

                // if file does not exist under a different asset continue
                if (linked.All(f => f.Path != file.Path)) continue;

                result.Add(file);
            }
            MetaProgress.Remove(progressId);

            return result;
        }

        private static async Task RemoveReassignedMediaIndexes(List<AssetInfo> duplicates)
        {
            int progress = 0;
            int count = duplicates.Count;
            int progressId = MetaProgress.Start("Removing duplicate indexed media files");

            foreach (AssetInfo file in duplicates)
            {
                progress++;
                MetaProgress.Report(progressId, progress, count, file.FileName);
                if (progress % 1000 == 0) await Task.Yield();

                DBAdapter.DB.Delete<AssetFile>(file.Id);
            }
            MetaProgress.Remove(progressId);
        }
    }
}
