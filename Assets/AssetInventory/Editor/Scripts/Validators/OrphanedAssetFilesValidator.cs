using System.Threading.Tasks;

namespace AssetInventory
{
    public sealed class OrphanedAssetFilesValidator : Validator
    {
        public OrphanedAssetFilesValidator()
        {
            Type = ValidatorType.DB;
            Name = "Orphaned Asset Files";
            Description = "Scans the database for asset files that do not have a valid package reference anymore.";
            FixCaption = "Remove";
        }

        public override async Task Validate()
        {
            CurrentState = State.Scanning;

            await Task.Yield();
            
            // query all asset files that do not have an asset id that is contained in the asset table
            string query = "SELECT * from AssetFile where AssetId is null or AssetId not in (SELECT Id from Asset)";
            DBIssues = DBAdapter.DB.Query<AssetInfo>(query);

            CurrentState = State.Completed;
        }

        public override async Task Fix()
        {
            CurrentState = State.Fixing;

            // delete all orphaned asset files
            foreach (AssetInfo issue in DBIssues)
            {
                DBAdapter.DB.Delete<AssetFile>(issue.Id);
            }

            await Validate();
        }
    }
}
