using UnityEditor;

namespace AssetInventory
{
    public static class MetaProgress
    {
        private static int _idx;

        public static int Start(string name, string description = null, int parentId = -1)
        {
            _idx++;
#if UNITY_2020_1_OR_NEWER
            return Progress.Start(name, description, Progress.Options.None, parentId);
#else
            return _idx;
#endif
        }

        public static void Report(int id, float progress, string description)
        {
#if UNITY_2020_1_OR_NEWER
            Progress.Report(id, progress, description);
#endif
        }

        public static void Report(int id, int currentStep, int totalSteps, string description)
        {
#if UNITY_2020_1_OR_NEWER
            Progress.Report(id, currentStep, totalSteps, description);
#endif
        }

        public static int Remove(int id)
        {
#if UNITY_2020_1_OR_NEWER
            return Progress.Remove(id);
#else
            return -1;
#endif
        }
    }
}
