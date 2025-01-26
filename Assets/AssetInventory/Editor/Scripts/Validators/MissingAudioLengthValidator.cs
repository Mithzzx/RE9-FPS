using System.Threading.Tasks;
using UnityEditor;

namespace AssetInventory
{
    public sealed class MissingAudioLengthValidator : Validator
    {
        public MissingAudioLengthValidator()
        {
            Type = ValidatorType.DB;
            Name = "Missing Audio Duration";
            Description = "Finds indexed audio files for which the duration has not been determined yet.";
            FixCaption = "Mark for Reindexing";
        }

        public override async Task Validate()
        {
            CurrentState = State.Scanning;

            await Task.Yield();

            // query all asset files that do not have an asset id that is contained in the asset table
            string query = "select * from AssetFile where Length = 0 and Type in ('" + string.Join("','", AssetInventory.TypeGroups["Audio"]) + "')";
            DBIssues = DBAdapter.DB.Query<AssetInfo>(query);

            CurrentState = State.Completed;
        }

        public override async Task Fix()
        {
            CurrentState = State.Fixing;

            int fileCount = AssetInventory.MarkAudioWithMissingLengthForIndexing();
            EditorUtility.DisplayDialog("Success", $"During the next index update, up to {fileCount} audio files will be reindexed to try to read the length again.", "OK");

            await Validate();
        }
    }
}
