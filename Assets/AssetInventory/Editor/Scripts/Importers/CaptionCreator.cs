using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AssetInventory
{
    public sealed class CaptionCreator : AssetImporter
    {
        public async Task Index()
        {
            ResetState(false);
            int progressId = MetaProgress.Start("Creating AI captions");

            string previewFolder = AssetInventory.GetPreviewFolder();

            string query = "select *, AssetFile.Id as Id from AssetFile inner join Asset on Asset.Id = AssetFile.AssetId where Asset.Exclude = false and AssetFile.Type = \"prefab\" and AssetFile.AICaption is null and (AssetFile.PreviewState = ? or AssetFile.PreviewState = ?) order by Asset.Id desc";
            List<AssetFile> files = DBAdapter.DB.Query<AssetFile>(query, AssetFile.PreviewOptions.Custom, AssetFile.PreviewOptions.Supplied).ToList();

            bool toolChainWorking = true;
            for (int i = 0; i < files.Count; i++)
            {
                if (CancellationRequested) break;
                await Cooldown.Do();
                await Task.Delay(AssetInventory.Config.aiPause * 1000); // crashes system otherwise after a while

                AssetFile file = files[i];
                MetaProgress.Report(progressId, i + 1, files.Count, file.FileName);
                SubCount = files.Count;
                CurrentSub = $"Captioning {file.FileName}";
                SubProgress = i + 1;

                string previewFile = ValidatePreviewFile(file, previewFolder);
                if (string.IsNullOrEmpty(previewFile)) continue;

                await Task.Run(() =>
                {
                    string caption = CaptionImage(previewFile);
                    if (!string.IsNullOrWhiteSpace(caption))
                    {
                        file.AICaption = caption;
                        Persist(file);
                    }
                    else if (i == 0)
                    {
                        toolChainWorking = false;
                    }
                });
                if (!toolChainWorking) break;
            }
            MetaProgress.Remove(progressId);
            ResetState(true);
        }

        public static string CaptionImage(string filename)
        {
            string blipType = AssetInventory.Config.blipType == 1 ? "--large" : "";
            string result = IOUtils.ExecuteCommand("blip-caption", $"{blipType} --json {filename}");
            if (string.IsNullOrWhiteSpace(result)) return null;

            return JsonConvert.DeserializeObject<List<BlipResult>>(result).First().caption;
        }
    }

    public class BlipResult
    {
        public string path;
        public string caption;
    }
}
