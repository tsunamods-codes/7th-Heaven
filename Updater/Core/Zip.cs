using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater.Core
{
    public class Zip
    {
        public delegate void ZipExtractProgressEventHandler(int percent, int curr, int total, string fileCompleted);
        public delegate void ZipExtractCompleteEventHandler();

        public event ZipExtractProgressEventHandler ZipExtractProgress;
        public event ZipExtractCompleteEventHandler ZipExtractComplete;

        public void ExtractZipFileToDirectory(string sourceZipFilePath, string destinationDirectoryName, bool overwrite)
        {
            using (var archive = ZipFile.Open(sourceZipFilePath, ZipArchiveMode.Read))
            {
                if (!overwrite)
                {
                    archive.ExtractToDirectory(destinationDirectoryName);
                    return;
                }

                DirectoryInfo di = Directory.CreateDirectory(destinationDirectoryName);
                string destinationDirectoryFullPath = di.FullName;

                int totalFiles = archive.Entries.Count;
                int current = 0;

                string excludeFromPath = "";

                foreach (ZipArchiveEntry file in archive.Entries)
                {
                    current++;
                    if (current > 1)
                    {
                        string completeFileName = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, file.FullName)).Replace(excludeFromPath, "");

                        if (!completeFileName.StartsWith(destinationDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new IOException("Trying to extract file outside of destination directory. See this link for more info: https://snyk.io/research/zip-slip-vulnerability");
                        }

                        if (file.Name == "")
                        {
                            if (!Directory.Exists(Path.GetDirectoryName(completeFileName)))
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                            }
                            continue;
                        }
                        file.ExtractToFile(completeFileName, true);
                        File.SetCreationTimeUtc(completeFileName, file.LastWriteTime.UtcDateTime);
                        ZipExtractProgress((current / totalFiles) * 100, current, totalFiles, file.FullName);
                    }
                    else
                    {
                        excludeFromPath = file.FullName.Replace("/", "\\");
                    }
                }
                ZipExtractComplete();
            }
        }
    }
}
