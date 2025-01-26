using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace AssetInventory
{
    public sealed class AssetUsage : AssetProgress
    {
        public async Task<List<AssetInfo>> Calculate()
        {
            ResetState(false);

            List<AssetInfo> result = new List<AssetInfo>();

            // identify asset packages through guids lookup
            CurrentMain = "Phase 1/2: Gathering guids";
            List<string> guids = await GatherGuids(new[] {Application.dataPath});

            CurrentMain = "Phase 2/2: Looking up assets";
            MainCount = guids.Count;
            MainProgress = 0;

            foreach (string guid in guids)
            {
                if (CancellationRequested) break;

                MainProgress++;
                if (MainProgress % 100 == 0) await Task.Yield();

                List<AssetInfo> files = AssetUtils.Guid2File(guid);
                if (files.Count == 0)
                {
                    // found non-indexed asset
                    AssetInfo ai = new AssetInfo();
                    ai.Guid = guid;
                    ai.CurrentState = Asset.State.Unknown;
                    result.Add(ai);
                    continue;
                }
                if (files.Count > 1)
                {
                    Debug.LogWarning("Duplicate guids found: " + string.Join(", ", files.Select(ai => ai.Path)));
                    continue;
                }
                result.Add(files[0]);
            }
            ResetState(true);

            return result;
        }

        private async Task<List<string>> GatherGuids(IEnumerable<string> folders)
        {
            List<string> result = new List<string>();

            foreach (string folder in folders)
            {
                string[] assets = Directory.GetFiles(folder, "*.meta", SearchOption.AllDirectories);
                MainCount = assets.Length;
                foreach (string asset in assets)
                {
                    if (CancellationRequested) break;

                    MainProgress++;
                    if (MainProgress % 100 == 0) await Task.Yield();

                    string guid = AssetUtils.ExtractGuidFromFile(asset);
                    if (string.IsNullOrEmpty(guid)) continue;

                    result.Add(guid);
                }
            }

            return result;
        }
    }
}