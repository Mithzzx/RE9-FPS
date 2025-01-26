using System.Linq;
using System.Threading.Tasks;

namespace AssetInventory
{
    public sealed class OrphanedPackagesValidator : Validator
    {
        public OrphanedPackagesValidator()
        {
            Type = ValidatorType.DB;
            Name = "Orphaned Packages";
            Description = "Scans the database for custom packages (not from the Asset Store or a registry) where the referenced file does not exist anymore.";
            FixCaption = "Remove";
        }

        public override async Task Validate()
        {
            CurrentState = State.Scanning;

            await Task.Yield();

            DBIssues = AssetInventory.LoadAssets().Where(a => a.AssetSource != Asset.Source.AssetStorePackage && a.AssetSource != Asset.Source.RegistryPackage && !a.Downloaded).ToList();

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
