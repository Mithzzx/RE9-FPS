using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AssetInventory
{
    public sealed class OrphanedTagAssignmentsValidator : Validator
    {
        public OrphanedTagAssignmentsValidator()
        {
            Type = ValidatorType.DB;
            Name = "Orphaned Tag Assignments";
            Description = "Scans the database for tags assigned to packages that don't exist anymore.";
            FixCaption = "Remove";
        }

        public override async Task Validate()
        {
            CurrentState = State.Scanning;
            DBIssues = new List<AssetInfo>();

            await Task.Yield();

            List<AssetInfo> assets = AssetInventory.LoadAssets();
            List<TagAssignment> tagAssignments = DBAdapter.DB.Table<TagAssignment>().ToList();

            // abuse AssetInfo for now to store the tag assignments
            foreach (TagAssignment assignment in tagAssignments.Where(ta => ta.TagTarget == TagAssignment.Target.Package))
            {
                if (assets.Find(a => a.AssetId == assignment.TargetId) == null)
                {
                    DBIssues.Add(new AssetInfo
                    {
                        Id = assignment.Id,
                        Path = assignment.TargetId.ToString()
                    });
                }
            }

            // TODO: scan also for asset files

            CurrentState = State.Completed;
        }

        public override async Task Fix()
        {
            CurrentState = State.Fixing;

            foreach (AssetInfo issue in DBIssues)
            {
                DBAdapter.DB.Execute("delete from TagAssignment where Id = ?", issue.Id);
                await Task.Yield();
            }

            await Validate();
        }
    }
}
