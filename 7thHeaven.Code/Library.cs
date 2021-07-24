/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using _7thHeaven.Code;
using _7thWrapperLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Iros._7th.Workshop
{
    public class Library
    {
        private Dictionary<Guid, InstalledItem> _lookup;

        public UpdateType DefaultUpdate { get; set; }
        public List<InstalledItem> Items { get; set; }

        private object _pendingLock = new object();


        public List<string> PendingDelete { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public List<DownloadItem> PendingInstall { get; set; }

        public void QueuePendingDelete(IEnumerable<string> files)
        {
            if (PendingDelete == null)
            {
                PendingDelete = new List<string>();
            }

            lock (_pendingLock)
                PendingDelete.AddRange(files);
        }

        public void QueuePendingInstall(DownloadItem downloadedItem)
        {
            if (PendingInstall == null)
            {
                PendingInstall = new List<DownloadItem>();
            }

            if (HasPendingInstall(downloadedItem))
            {
                return;
            }

            lock (_pendingLock)
                PendingInstall.Add(downloadedItem);
        }

        public bool HasPendingInstall(DownloadItem pendingDownload)
        {
            return PendingInstall?.Any(d => d.UniqueId == pendingDownload.UniqueId) == true;
        }


        public void RemovePendingInstall(DownloadItem downloadedItem)
        {
            if (PendingInstall == null || PendingInstall.Count == 0)
            {
                return;
            }

            if (!HasPendingInstall(downloadedItem))
            {
                return;
            }

            lock (_pendingLock)
                PendingInstall.RemoveAll(i => i.UniqueId == downloadedItem.UniqueId);
        }

        public InstalledItem GetItem(Guid modID)
        {
            if (_lookup == null)
            {
                _lookup = Items.ToDictionary(i => i.ModID, i => i);
            }
            InstalledItem ii;
            _lookup.TryGetValue(modID, out ii);
            return ii;
        }

        public void AddInstall(InstalledItem ii)
        {
            var exist = GetItem(ii.ModID); //forces lookup creation
            if (exist != null) Items.Remove(exist);
            Items.Add(ii);
            _lookup[ii.ModID] = ii;
            PendingDelete.RemoveAll(s => s.Equals(ii.LatestInstalled.InstalledLocation, StringComparison.InvariantCultureIgnoreCase));
            Sys.SetStatus(ii.ModID, ModStatus.Installed);
            Sys.rebuildVars();
            Sys.SaveLibrary();
        }

        public void RemoveInstall(InstalledItem ii)
        {
            var exist = GetItem(ii.ModID); //forces lookup creation
            if (exist != null) Items.Remove(exist);
            _lookup.Remove(ii.ModID);
            Sys.SetStatus(ii.ModID, ModStatus.NotInstalled);
            Sys.rebuildVars();
            Sys.SaveLibrary();
        }

        public void DeleteAndRemoveInstall(InstalledItem ii)
        {
            DeleteAndRemoveInstall(ii, ii.Versions.Select(v => v.InstalledLocation));
        }

        public void DeleteAndRemoveInstall(InstalledItem ii, IEnumerable<string> versionFilesToDelete)
        {
            QueuePendingDelete(versionFilesToDelete);
            RemoveInstall(ii);
            AttemptDeletions();
        }

        public void AttemptDeletions()
        {
            lock (_pendingLock)
            {
                int oldCount = PendingDelete.Count;
                for (int i = PendingDelete.Count - 1; i >= 0; i--)
                {
                    string path = Path.Combine(Sys.Settings.LibraryLocation, PendingDelete[i]);
                    try
                    {
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                        else if (Directory.Exists(path))
                        {
                            Directory.Delete(path, true);
                        }
                    }
                    catch (IOException ioex)
                    {
                        Sys.Message(new WMessage($"File {path} could not be accessed - in use? Leaving in list.", WMessageLogLevel.LogOnly, ioex));
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Sys.Message(new WMessage($"Unknown exception deleting {path} - Leaving in list.", WMessageLogLevel.LogOnly, ex));
                        continue;
                    }

                    Sys.Message(new WMessage($"File {path} successfully deleted.", WMessageLogLevel.LogOnly));
                    PendingDelete.RemoveAt(i);
                }

                if (oldCount != PendingDelete.Count)
                {
                    Sys.SaveLibrary();
                }
            }
        }

        public void AttemptInstalls()
        {
            if (PendingInstall == null || PendingInstall.Count == 0)
            {
                return;
            }

            lock (_pendingLock)
            {
                for (int i = PendingInstall.Count - 1; i >= 0; i--)
                {
                    PendingInstall[i].IProc.Schedule();
                }
            }
        }

        public Library()
        {
            DefaultUpdate = UpdateType.Notify;
            Items = new List<InstalledItem>();
            PendingDelete = new List<string>();
        }
    }

    public enum UpdateType
    {
        Notify,
        Install,
        Ignore,
    }

    public class InstalledItem
    {
        /// <summary>
        /// Dictionary to cache ModInfo instances during runtime. This is to avoid having to open .iro or mod.xml file from disk everytime
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        private static Dictionary<string, ModInfo> _infoCache = new Dictionary<string, _7thWrapperLib.ModInfo>(StringComparer.InvariantCultureIgnoreCase);

        [System.Xml.Serialization.XmlIgnore]
        private bool? _hasReadMe;

        public Guid ModID { get; set; }
        public Mod CachedDetails { get; set; }
        public string CachePreview { get; set; }
        public UpdateType UpdateType { get; set; }

        public List<InstalledVersion> Versions { get; set; }
        public InstalledVersion LatestInstalled { get { return Versions.OrderBy(v => v.VersionDetails.Version).Last(); } }

        [System.Xml.Serialization.XmlIgnore]
        public bool HasReadmeFile
        {
            get
            {
                if (_hasReadMe == null && LatestInstalled != null)
                    _hasReadMe = LatestInstalled.HasFile("readme.md") || LatestInstalled.HasFile("readme.html") || LatestInstalled.HasFile("readme.txt");

                return _hasReadMe.GetValueOrDefault(false);
            }
            set
            {
                _hasReadMe = value;
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public bool IsUpdateAvailable
        {
            get
            {
                Mod cat = Sys.GetModFromCatalog(this.ModID);

                if (cat == null)
                {
                    return false;
                }

                return cat.LatestVersion?.Version > Versions?.Max(v => v.VersionDetails.Version);
            }
        }

        public bool ModExistsOnFileSystem()
        {
            string fn = Path.Combine(Sys.Settings.LibraryLocation, LatestInstalled.InstalledLocation);
            return Directory.Exists(fn) || File.Exists(fn);
        }

        public ModInfo GetModInfo()
        {
            ModInfo info;
            string pathToModFileOrFolder = Path.Combine(Sys.Settings.LibraryLocation, LatestInstalled.InstalledLocation);

            try
            {
                if (!_infoCache.TryGetValue(pathToModFileOrFolder, out info))
                {
                    if (pathToModFileOrFolder.EndsWith(".iro", StringComparison.InvariantCultureIgnoreCase))
                    {
                        using (IrosArc arc = new IrosArc(pathToModFileOrFolder))
                        {
                            if (arc.HasFile("mod.xml"))
                            {
                                var doc = new System.Xml.XmlDocument();
                                doc.Load(arc.GetData("mod.xml"));
                                info = new ModInfo(doc, Sys._context);
                            }
                        }
                    }
                    else
                    {
                        string file = Path.Combine(pathToModFileOrFolder, "mod.xml");
                        if (File.Exists(file))
                        {
                            info = new ModInfo(file, Sys._context);
                        }
                    }
                    _infoCache.Add(pathToModFileOrFolder, info);
                }
            }
            catch (Exception ex)
            {
                Sys.Message(new WMessage($"WARNING: failed to get mod info due to unknown exception (commonly caused by incorrect xml): {ex.Message}", WMessageLogLevel.LogOnly, ex));
                return null;
            }

            return info;
        }

        public static void RemoveFromInfoCache(string mfile)
        {
            if (_infoCache.ContainsKey(mfile))
            {
                _infoCache.Remove(mfile);
            }
        }
    }

    public class InstalledVersion
    {
        public ModVersion VersionDetails { get; set; }
        public string InstalledLocation { get; set; }

        public Stream GetData(string name)
        {
            string path = Path.Combine(Sys.Settings.LibraryLocation, InstalledLocation);

            if (InstalledLocation.EndsWith(".iro", StringComparison.InvariantCultureIgnoreCase) && File.Exists(path))
            {
                using (var arc = new _7thWrapperLib.IrosArc(path))
                    return arc.GetData(name);
            }
            else
            {
                path = Path.Combine(path, name);
                if (File.Exists(path))
                    return new FileStream(path, FileMode.Open, FileAccess.Read);
            }

            return null;
        }

        public bool HasData(string name)
        {
            using (Stream s = GetData(name))
            {
                return s != null;
            }
        }

        public bool HasFile(string name)
        {
            string path = Path.Combine(Sys.Settings.LibraryLocation, InstalledLocation);

            if (InstalledLocation.EndsWith(".iro", StringComparison.InvariantCultureIgnoreCase) && File.Exists(path))
            {
                using (var arc = new _7thWrapperLib.IrosArc(path))
                    return arc.HasFile(name);
            }
            else
            {
                path = Path.Combine(path, name);
                return File.Exists(path);
            }
        }
    }

    public enum ModStatus
    {
        NotInstalled,
        Downloading,
        Installed,
        Updating,
        InfoChanged,
        PendingInstall
    }
}
