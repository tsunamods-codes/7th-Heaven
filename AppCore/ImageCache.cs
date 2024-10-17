/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using AppCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Iros._7th.Workshop {

    public class ImageCacheEntry {
        public string Url { get; set; }
        public string File { get; set; }
        public DateTime Updated { get; set; }
    }

    public class ImageCacheData {
        public List<ImageCacheEntry> Entries { get; set; }
    }

    public class ImageCache {

        private string _folder;
        private Dictionary<string, ImageCacheEntry> _entries;
        private HashSet<string> _usedFiles;

        private object _entryLock = new object();

        public ImageCache(string folder) {
            Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, "cache.xml");
            
            if (File.Exists(file)) {
                ImageCacheData data = Util.Deserialize<ImageCacheData>(file);
                _entries = data.Entries.ToDictionary(e => e.Url, e => e, StringComparer.InvariantCultureIgnoreCase);
            } 
            else
            {
                _entries = new Dictionary<string, ImageCacheEntry>(StringComparer.InvariantCultureIgnoreCase);
            }

            _usedFiles = new HashSet<string>(_entries.Select(e => e.Value.File), StringComparer.InvariantCultureIgnoreCase);
            _folder = folder;

            DeleteUnusedCacheFiles();
        }

        public void Save() {
            string file = Path.Combine(_folder, "cache.xml");
            ImageCacheData data;

            lock (_entryLock) {
                data = new ImageCacheData() { Entries = _entries.Values.ToList() };
            }

            using (var fs = new FileStream(file, FileMode.Create))
            {
                Util.Serialize(data, fs);
            }
        }

        private void TriggerDownload(string url, Guid modID)
        {
            ImageCacheEntry e;

            lock (_entryLock)
            {
                if (!_entries.TryGetValue(url, out e))
                {
                    e = new ImageCacheEntry();
                    e.Url = url;
                    _entries[url] = e;
                }
            }

            e.Updated = DateTime.Now;
            string file = e.File;

            if (string.IsNullOrWhiteSpace(file))
            {
                file = GetCacheFileName(url);
            }

            string pathToTempDownload = Path.Combine(_folder, file + ".tmp");

            DownloadItem download = new DownloadItem()
            {
                Links = new List<string> { LocationUtil.FormatHttpUrl(url) },
                SaveFilePath = pathToTempDownload,
                ItemName = $"[{StringKey.DownloadingPreviewImage}]",
                ItemNameTranslationKey = StringKey.DownloadingPreviewImage,
                Category = DownloadCategory.Image
            };

            download.IProc = new Install.InstallProcedureCallback(ae =>
            {
                if (ae.Error == null)
                {
                    lock (_entryLock)
                    {
                        e.File = file;
                    }

                    try
                    {
                        try
                        {
                            // delete existing cache file and rename .tmp file to match cache file name
                            string f = Path.Combine(_folder, file);

                            if (File.Exists(f)) File.Delete(f);

                            File.Move(pathToTempDownload, f);

                        }
                        catch (IOException)
                        {
                            // this happens while the file is in-use. the file will be renamed and image cache entry updated to point to new file instead
                            file = GetCacheFileName(url);
                            File.Move(pathToTempDownload, file);
                            e.File = file;
                        }

                        Sys.PingInfoChange(modID);

                    }
                    catch (Exception ex)
                    {
                        Sys.Message(new WMessage(StringKey.FailedToGetPreviewImage, WMessageLogLevel.Error, ex));
                    }

                }
                else
                {
                    Sys.Message(new WMessage(StringKey.FailedToGetPreviewImage, WMessageLogLevel.LogOnly, ae.Error));
                }

            });

            Sys.Downloads.AddToDownloadQueue(download);
        }

        /// <summary>
        /// Generates a file name based on the hash code of the url.
        /// file name is added to <see cref="_usedFiles"/> to keep track of file names already generated
        /// </summary>
        private string GetCacheFileName(string url)
        {
            string file = "";
            if (String.IsNullOrEmpty(file))
            {
                string prefix = url.GetHashCode().ToString("x");
                int count = 0;
                do
                {
                    file = prefix + count.ToString();
                    count++;
                } while (_usedFiles.Contains(file));
                _usedFiles.Add(file);
            }

            return file;
        }

        public void InsertManual(string url, byte[] data) {
            ImageCacheEntry e;
            lock (_entryLock) {
                if (!_entries.TryGetValue(url, out e)) {
                    e = new ImageCacheEntry();
                    e.Url = url;
                    _entries[url] = e;
                }
            }

            e.Updated = DateTime.Now;

            if (String.IsNullOrWhiteSpace(e.File)) {
                string prefix = url.GetHashCode().ToString("x");
                int count = 0;
                do {
                    e.File = prefix + count.ToString();
                    count++;
                } while (_usedFiles.Contains(e.File));
                _usedFiles.Add(e.File);
            }

            try
            {
                File.WriteAllBytes(Path.Combine(_folder, e.File), data);
            }
            catch (IOException ioex)
            {
                // IOException can be thrown if image is in-use at the time e.g. being previewed
                Sys.Message(new WMessage($"Failed to update image cache file: {ioex.Message}", WMessageLogLevel.LogOnly));
            }
            catch (Exception ex)
            {
                Sys.Message(new WMessage("Failed to update image cache file", WMessageLogLevel.LogOnly, ex));
            }
        }

        /// <summary>
        /// Returns absolute path to the cached image file if it exists.
        /// Triggers the image download if it does not exist.
        /// </summary>
        /// <param name="url"> image download url</param>
        /// <param name="modID"> ID of Mod for cached image to look up </param>
        public string GetImagePath(string url, Guid modID)
        {
            if (String.IsNullOrWhiteSpace(url)) return null;

            bool gotValue = false;
            string pathToImage = null;
            ImageCacheEntry e;

            lock (_entryLock)
            {
                gotValue = _entries.TryGetValue(url, out e);
            }

            if (gotValue && e.File != null)
            {
                string file = Path.Combine(_folder, e.File);
                if (File.Exists(file))
                {
                    pathToImage = file;
                }

                // re-download image if older than a day to keep cache updated (exlude images that came from auto import)
                if ((!File.Exists(file) || e.Updated < DateTime.Now.AddDays(-5)) && !url.StartsWith("iros://Preview/Auto", StringComparison.InvariantCultureIgnoreCase))
                {
                    TriggerDownload(url, modID);
                }
            }
            else if (!url.StartsWith("iros://Preview/Auto", StringComparison.InvariantCultureIgnoreCase))
            {
                TriggerDownload(url, modID);
            }

            return pathToImage;
        }

        /// <summary>
        /// Deletes any files in cache folder that are not referenced by <see cref="_entries"/>
        /// </summary>
        public void DeleteUnusedCacheFiles()
        {
            foreach (string file in Directory.GetFiles(_folder))
            {
                FileInfo info = new FileInfo(file);

                if (info.Name == "cache.xml" || info.Extension != "")
                    continue; // skip the xml file or files with extensions (image cache files have no extension)

                if (!_entries.Values.Any(c => c.File == info.Name))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Sys.Message(new WMessage($"Failed to delete old image cache entry {file}", WMessageLogLevel.LogOnly, ex));
                    }
                }
            }
        }
    }
}
