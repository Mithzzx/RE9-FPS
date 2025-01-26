using System;
using SQLite;

namespace AssetInventory
{
    [Serializable]
    public class TagAssignment
    {
        public enum Target
        {
            Package = 0,
            Asset = 1
        }

        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [Indexed] public int TagId { get; set; }
        [Indexed] public Target TagTarget { get; set; }
        [Indexed] public int TargetId { get; set; }

        public TagAssignment()
        {
        }

        public TagAssignment(int tagId, Target tagTarget, int targetId)
        {
            TagId = tagId;
            TagTarget = tagTarget;
            TargetId = targetId;
        }

        public override string ToString()
        {
            return $"Tag Assignment '{TagTarget}' ({TagId})";
        }
    }
}