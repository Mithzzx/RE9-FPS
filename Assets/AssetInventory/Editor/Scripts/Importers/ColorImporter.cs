using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite;
using UnityEngine;

namespace AssetInventory
{
    public sealed class ColorImporter : AssetImporter
    {
        public async Task Index()
        {
            ResetState(false);
            int progressId = MetaProgress.Start("Extracting color information");

            string previewFolder = AssetInventory.GetPreviewFolder();

            TableQuery<AssetFile> query = DBAdapter.DB.Table<AssetFile>()
                .Where(a => (a.PreviewState == AssetFile.PreviewOptions.Custom || a.PreviewState == AssetFile.PreviewOptions.Supplied) && a.Hue < 0);

            // skip audio files per default
            if (!AssetInventory.Config.extractAudioColors)
            {
                foreach (string t in AssetInventory.TypeGroups["Audio"])
                {
                    query = query.Where(a => a.Type != t);
                }
            }

            List<AssetFile> files = query.ToList();
            for (int i = 0; i < files.Count; i++)
            {
                if (CancellationRequested) break;
                await Cooldown.Do();

                AssetFile file = files[i];
                MetaProgress.Report(progressId, i + 1, files.Count, file.FileName);
                SubCount = files.Count;
                CurrentSub = $"Color extraction from {file.FileName}";
                SubProgress = i + 1;

                string previewFile = ValidatePreviewFile(file, previewFolder);
                if (string.IsNullOrEmpty(previewFile)) continue;

                Texture2D texture = await AssetUtils.LoadLocalTexture(previewFile, false);
                if (texture != null)
                {
                    file.Hue = ImageUtils.GetHue(texture);
                    Persist(file);
                }
            }
            MetaProgress.Remove(progressId);
            ResetState(true);
        }
    }
}
