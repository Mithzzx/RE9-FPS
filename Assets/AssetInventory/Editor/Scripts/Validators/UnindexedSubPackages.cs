using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AssetInventory
{
    public sealed class UnindexedSubPackagesValidator : Validator
    {
        public UnindexedSubPackagesValidator()
        {
            Type = ValidatorType.DB;
            Name = "Unindexed Sub-Packages";
            Description = "Scans all packages if they contain other .unitypackage or .zip/.rar/.7z files which are not yet indexed as sub-packages.";
            FixCaption = "Mark for Reindexing";
        }

        public override async Task Validate()
        {
            CurrentState = State.Scanning;

            await Task.Yield();

            DBIssues = new List<AssetInfo>();
            List<AssetInfo> assets = AssetInventory.LoadAssets().Where(a => a.ParentId > 0).ToList();
            List<AssetInfo> candidates = DBAdapter.DB.Query<AssetInfo>("select *, AssetFile.Id as Id from AssetFile left join Asset on Asset.Id = AssetFile.AssetId where Asset.SafeName != ? AND (Type = 'unitypackage' OR Type = 'zip' OR Type = 'rar' OR Type = '7z')", Asset.NONE);
            HashSet<string> assetLocations = new HashSet<string>(assets.Select(a => a.Location));

            int count = candidates.Count;
            int progressId = MetaProgress.Start("Gathering unindexed sub-packages");

            for (int i = 0; i < count; i++)
            {
                AssetInfo file = candidates[i];
                if (i % 50 == 0) await Task.Yield();
                MetaProgress.Report(progressId, i, count, file.Path);

                string segment = $"|{file.Path}";
                if (!assetLocations.Any(location => location.EndsWith(segment)))
                {
                    DBIssues.Add(file);
                }
            }
            MetaProgress.Remove(progressId);

            CurrentState = State.Completed;
        }

        public override async Task Fix()
        {
            CurrentState = State.Fixing;

            List<AssetInfo> assets = DBIssues.GroupBy(asset => asset.AssetId).Select(group => group.First()).ToList();
            foreach (AssetInfo asset in assets)
            {
                if (asset.CurrentState != Asset.State.Done) continue;
                asset.CurrentState = Asset.State.SubInProcess;
                DBAdapter.DB.Execute("update Asset set CurrentState=? where Id=?", asset.CurrentState, asset.AssetId);

                await Task.Yield();
            }

            CurrentState = State.Idle;
        }
    }
}