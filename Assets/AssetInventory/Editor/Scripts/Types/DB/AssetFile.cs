using System;
using System.IO;
using SQLite;
using UnityEditor;

namespace AssetInventory
{
    [Serializable]
    public class AssetFile : TreeElement
    {
        public enum PreviewOptions
        {
            None = 0,
            Supplied = 1,
            Redo = 2,
            Custom = 3,
            Error = 4
        }

        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [Indexed] public int AssetId { get; set; }
        [Indexed] public string Guid { get; set; }
        [Indexed] public string Path { get; set; }
        [Indexed] public string FileName { get; set; }
        public string SourcePath { get; set; }
        [Indexed] public string Type { get; set; }
        public PreviewOptions PreviewState { get; set; }
        public float Hue { get; set; } = -1f;
        public long Size { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public float Length { get; set; }
        public string AICaption { get; set; }

        // runtime
        [Ignore] public string ProjectPath { get; set; }
        [Ignore] public bool InProject => !string.IsNullOrWhiteSpace(ProjectPath);

        [Ignore] public string ShortPath => !string.IsNullOrEmpty(Path) && Path.StartsWith("Assets/") ? Path.Substring(7) : Path;

        public void CheckIfInProject()
        {
            // check if file already exists in project, and work-around issue that Unity reports deleted assets still back
            ProjectPath = AssetDatabase.GUIDToAssetPath(Guid);
            if (!string.IsNullOrEmpty(ProjectPath) && (ProjectPath.StartsWith($"Assets/{AssetInventory.TEMP_FOLDER}") || !File.Exists(ProjectPath))) ProjectPath = null;
        }

        public bool HasPreview()
        {
            return PreviewState == PreviewOptions.Custom || PreviewState == PreviewOptions.Supplied;
        }

        public bool IsPackage()
        {
            return Type == "unitypackage";
        }

        public bool IsArchive()
        {
            return Type == "zip" || Type == "rar" || Type == "7z";
        }

        public string GetPreviewFile(string previewFolder)
        {
            return System.IO.Path.Combine(previewFolder, AssetId.ToString(), $"af-{Id}.png");
        }

        public string GetPath(bool expanded)
        {
            return expanded ? AssetInventory.DeRel(Path) : Path;
        }

        public void SetPath(string path)
        {
            Path = path?.Replace("\\", "/");
        }

        public string GetSourcePath(bool expanded)
        {
            return expanded ? AssetInventory.DeRel(SourcePath) : SourcePath;
        }

        public void SetSourcePath(string sourcePath)
        {
            SourcePath = sourcePath?.Replace("\\", "/");
        }

        public override string ToString()
        {
            return $"Asset File '{Path}' ({Size})";
        }
    }
}
