using System;
using System.IO;
using System.Linq;
using System.Text;
using Unity.SharpZipLib.GZip;
using Unity.SharpZipLib.Tar;
using UnityEngine;

namespace AssetInventory
{
    public static class TarUtil
    {
        public static void ExtractGz(string archiveFile, string targetFolder)
        {
            Stream rawStream = File.OpenRead(archiveFile);
            GZipInputStream gzipStream = new GZipInputStream(rawStream);

            try
            {
                TarArchive tarArchive = TarArchive.CreateInputTarArchive(IsZipped(archiveFile) ? gzipStream : rawStream, Encoding.Default);
                tarArchive.ExtractContents(targetFolder);
                tarArchive.Close();
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not extract archive '{archiveFile}'. The process was either interrupted or the file is corrupted: {e.Message}");
            }

            gzipStream.Close();
            rawStream.Close();
        }

        public static string ExtractGzFile(string archiveFile, string fileName, string targetFolder)
        {
            Stream rawStream = File.OpenRead(archiveFile);
            GZipInputStream gzipStream = new GZipInputStream(rawStream);

            string destFile = null;

            // fileName will be ID/asset, whole folder is needed though
            string folderName = fileName.Split(new[] {'/', '\\'}, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

            try
            {
                Stream inputStream = IsZipped(archiveFile) ? gzipStream : rawStream;

                using (TarInputStream tarStream = new TarInputStream(inputStream, Encoding.Default))
                {
                    TarEntry entry;
                    bool found = false;
                    while ((entry = tarStream.GetNextEntry()) != null)
                    {
                        if (entry.IsDirectory) continue;
                        if (entry.Name.Contains(folderName))
                        {
                            destFile = Path.Combine(targetFolder, entry.Name);
                            string directoryName = Path.GetDirectoryName(destFile);
                            if (!Directory.Exists(directoryName)) Directory.CreateDirectory(directoryName);

                            using (FileStream fileStream = File.Create(destFile))
                            {
                                tarStream.CopyEntryContents(fileStream);
                            }
                            found = true;
                        }
                        else if (found)
                        {
                            // leave the loop if the files were found and the next entry is not in the same folder
                            // assumption is the files appear consecutively
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not extract file from archive '{archiveFile}'. The process was either interrupted or the file is corrupted: {e.Message}");
            }

            gzipStream.Close();
            rawStream.Close();

            return destFile;
        }

        private static bool IsZipped(string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[2];
                fs.Read(buffer, 0, buffer.Length);
                return buffer[0] == 0x1F && buffer[1] == 0x8B;
            }
        }
    }
}
