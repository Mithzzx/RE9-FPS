using System.Collections.Generic;
using System.Threading.Tasks;

namespace AssetInventory
{
    public abstract class Validator
    {
        public enum ValidatorType
        {
            DB,
            FileSystem
        }

        public enum State
        {
            Idle,
            Scanning,
            Completed,
            Fixing
        }

        public ValidatorType Type { get; protected set; }
        public State CurrentState { get; protected set; }
        public string Name { get; protected set; }
        public string Description { get; protected set; }
        public bool Fixable { get; protected set; } = true;
        public string FixCaption { get; protected set; } = "Fix";
        public List<AssetInfo> DBIssues { get; protected set; }
        public List<string> FileIssues { get; protected set; }

        public int IssueCount { get { return Type == ValidatorType.DB ? DBIssues.Count : FileIssues.Count; } }

        public abstract Task Validate();
        public abstract Task Fix();
    }
}
