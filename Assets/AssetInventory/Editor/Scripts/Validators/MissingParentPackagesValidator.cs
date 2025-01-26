using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AssetInventory
{
    public sealed class MissingParentPackagesValidator : Validator
    {
        public MissingParentPackagesValidator()
        {
            Type = ValidatorType.DB;
            Name = "Missing Parent Packages";
            Description = "Scans the database for packages where the referenced parent does not exist anymore.";
            FixCaption = "Remove";
        }

        public override async Task Validate()
        {
            CurrentState = State.Scanning;

            await Task.Yield();

            List<AssetInfo> assets = AssetInventory.LoadAssets();
            DBIssues = assets.Where(a => a.ParentId > 0 && !assets.Any(a2 => a2.Id == a.ParentId)).ToList();

            CurrentState = State.Completed;
        }

        public override async Task Fix()
        {
            CurrentState = State.Fixing;

            foreach (AssetInfo issue in DBIssues)
            {
                AssetInventory.RemovePackage(issue, false);
                await Task.Yield();
            }

            await Validate();
        }
    }
}
