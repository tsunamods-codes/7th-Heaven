/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using AppCore;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Iros.Workshop
{
    public static class Install
    {
        public static string SafeStr(string s)
        {
            return new string(s.Select(c => Char.IsLetterOrDigit(c) ? c : '_').ToArray());
        }

        public static void DownloadAndInstall(Mod m, bool isUpdatingMod = false)
        {
            // validate mod is not already downloading/updating
            ModStatus status = Sys.GetStatus(m.ID);
            if (status == ModStatus.Downloading)
            {
                Sys.Message(new WMessage($"{m.Name} [{StringKey.IsAlreadyDownloading}]", true) { TextTranslationKey = StringKey.IsAlreadyDownloading });
                return;
            }

            if (status == ModStatus.Updating)
            {
                Sys.Message(new WMessage($"{m.Name} [{StringKey.IsAlreadyUpdating}]", true) { TextTranslationKey = StringKey.IsAlreadyUpdating });
                return;
            }

            if (status == ModStatus.Installed && !isUpdatingMod)
            {
                Sys.Message(new WMessage($"{m.Name} [{StringKey.IsAlreadyDownloadedAndInstalled}]", true) { TextTranslationKey = StringKey.IsAlreadyDownloadedAndInstalled });
                return;
            }

            Action onCancel = () => Sys.RevertStatus(m.ID);
            Action<Exception> onError = ex =>
            {
                Sys.Message(new WMessage($"[{StringKey.ErrorOccurredDownloadingInstalling}] {m.Name} - {ex.Message}", WMessageLogLevel.Error, ex) { TextTranslationKey = StringKey.ErrorOccurredDownloadingInstalling });
                Sys.RevertStatus(m.ID);
            };

            string file = String.Format("{0}_{1}_{2}.iro", m.ID, SafeStr(m.Name), m.LatestVersion.Version);
            string temppath = Path.Combine(Sys.Settings.LibraryLocation, "temp");
            Directory.CreateDirectory(temppath);

            var install = Sys.Library.GetItem(m.ID);
            if (install != null)
            {
                // mod is installed so download update files for updating mod
                Sys.SetStatus(m.ID, ModStatus.Updating);

                List<ModPatch> patches = m.GetPatchesFromTo(install.LatestInstalled.VersionDetails.Version, m.LatestVersion.Version);
                if (patches.Any())
                {
                    // download patches to update the mod
                    string pfile = String.Format("{0}_{1}_{2}.irop", m.ID, SafeStr(m.Name), m.LatestVersion.Version);
                    DownloadItem download = new DownloadItem()
                    {
                        Links = patches.Select(p => p.Link).ToList(),
                        SaveFilePath = Path.Combine(temppath, pfile),
                        Category = DownloadCategory.ModPatch,
                        ItemName = $"[{StringKey.Downloading}] {m.Name} patch {install.LatestInstalled.VersionDetails.Version} -> {m.LatestVersion.Version}",
                        ItemNameTranslationKey = StringKey.Downloading,
                        OnCancel = onCancel
                    };

                    download.IProc = new PatchModProcedure()
                    {
                        Filename = pfile,
                        Install = install,
                        Mod = m,
                        Error = onError
                    };

                    Sys.Downloads.AddToDownloadQueue(download);
                    return;
                }
                else
                {
                    // no patches available to update so download entire new mod version .iro
                    DownloadAndInstallMod(m, temppath, file);
                    return;
                }
            }


            Sys.SetStatus(m.ID, ModStatus.Downloading);

            if (m.Patches.Any())
            {
                // mod is not installed and the latest version has patches available
                // so first download and install mod then download all patches for mod

                Guid downloadId = DownloadAndInstallMod(m, temppath, file);

                int pCount = 0;
                foreach (IGrouping<decimal, ModPatch> linksPerVersion in m.Patches.GroupBy(mp => mp.VerTo))
                {
                    string pfile = String.Format("{0}_{1}_{2}_patch{3}.irop", m.ID, SafeStr(m.Name), m.LatestVersion.Version, pCount);

                    DownloadItem patchDownload = new DownloadItem()
                    {
                        ParentUniqueID = downloadId, // this is set to know these patch downloads are dependent of the main mod download or previous patch.
                        Links = linksPerVersion.Select(mp => mp.Link).ToList(),
                        SaveFilePath = Path.Combine(temppath, pfile),
                        ItemName = $"[{StringKey.Downloading}] {m.Name} patch {linksPerVersion.Key}",
                        ItemNameTranslationKey = StringKey.Downloading,
                        Category = DownloadCategory.ModPatch,
                        OnCancel = onCancel
                    };

                    patchDownload.IProc = new PatchModProcedure()
                    {
                        Filename = pfile,
                        Error = onError,
                        Mod = m
                    };

                    downloadId = patchDownload.UniqueId; // get the id of the patch download so it can be set as the parent for the following patch

                    Sys.Downloads.AddToDownloadQueue(patchDownload);
                    pCount++;
                }

            }
            else
            {
                // mod is not installed in library so just download using the links of latest version
                DownloadAndInstallMod(m, temppath, file);
            }
        }

        private static Guid DownloadAndInstallMod(Mod m, string pathToFolder, string fileName)
        {
            DownloadItem installDownload = new DownloadItem()
            {
                Links = m.LatestVersion.Links,
                SaveFilePath = Path.Combine(pathToFolder, fileName),
                Category = DownloadCategory.Mod,
                ItemName = $"[{StringKey.Downloading}] {m.Name}",
                ItemNameTranslationKey = StringKey.Downloading
            };

            installDownload.IProc = new InstallModProcedure()
            {
                FileName = fileName,
                Mod = m,
                ExtractSubFolder = m.LatestVersion.ExtractSubFolder,
                ExtractInto = m.LatestVersion.ExtractInto
            };

            installDownload.OnCancel = () =>
            {
                Sys.Library.RemovePendingInstall(installDownload);
                Sys.RevertStatus(m.ID);
            };

            installDownload.IProc.Error = ex =>
            {
                Sys.Library.QueuePendingInstall(installDownload);
                Sys.Message(new WMessage($"Error installing {m.Name} - The mod is queued for install.", WMessageLogLevel.Error, ex) { TextTranslationKey = StringKey.ErrorOccurredDownloadingInstalling });
                Sys.SetStatus(m.ID, ModStatus.PendingInstall);
            };

            Sys.Downloads.AddToDownloadQueue(installDownload);

            return installDownload.UniqueId;
        }

        public abstract class InstallProcedure
        {
            public Action<int> SetPercentComplete;

            public Action Complete;

            public Action<Exception> Error;

            /// <summary>
            /// Called after Complete() has run and should tidy everything up, notify user, etc.
            /// </summary>
            /// <param name="e"></param>
            public abstract void DownloadComplete(System.ComponentModel.AsyncCompletedEventArgs e);

            /// <summary>
            /// Needs to process the downloaded file - such as extracting compressed iro- and afterwards, call Complete() (or Error())
            /// </summary>
            public abstract void Schedule();
        }

        public class InstallProcedureCallback : InstallProcedure
        {
            private Action<System.ComponentModel.AsyncCompletedEventArgs> _callback;

            public InstallProcedureCallback(Action<System.ComponentModel.AsyncCompletedEventArgs> callback)
            {
                try
                {
                    _callback = callback;
                }
                catch (Exception e)
                {
                    Error(e);
                }
            }

            public override void Schedule()
            {
                SetPercentComplete(100);
                Complete();
            }

            public override void DownloadComplete(System.ComponentModel.AsyncCompletedEventArgs e)
            {
                _callback(e);
            }
        }

        internal class PatchModProcedure : InstallProcedure
        {
            public string Filename { get; set; }
            public Mod Mod { get; set; }
            public InstalledItem Install { get; set; }

            private void ProcessDownload(object state)
            {
                string patchfile = Path.Combine(Sys.Settings.LibraryLocation, "temp", Filename);
                try
                {
                    if (Install == null)
                    {
                        Install = Sys.Library.GetItem(Mod.ID);

                        if (Install == null)
                        {
                            // don't go any further since mod is not installed
                            Error(new Exception($"{Mod.Name} not installed"));
                            return;
                        }
                    }

                    string source = Path.Combine(Sys.Settings.LibraryLocation, Install.LatestInstalled.InstalledLocation);
                    using (var iro = new AppWrapper.IrosArc(source, true))
                    {
                        using (var patch = new AppWrapper.IrosArc(patchfile))
                        {
                            iro.ApplyPatch(patch, (d, msg) =>
                            {
                                Sys.Message(new WMessage(msg, WMessageLogLevel.LogOnly));
                                SetPercentComplete((int)(100 * d));
                            });
                        }
                    }

                    File.Delete(patchfile);
                }
                catch (Exception e)
                {
                    Error(e);
                    return;
                }

                SetPercentComplete(100);
                Complete();
            }

            public override void Schedule()
            {
                System.Threading.ThreadPool.QueueUserWorkItem(ProcessDownload);
            }
            public override void DownloadComplete(System.ComponentModel.AsyncCompletedEventArgs e)
            {
                if (e.Error != null)
                {
                    Sys.Message(new WMessage($"[{StringKey.ErrorUpdating}] {Mod.Name}") { TextTranslationKey = StringKey.ErrorUpdating, LoggedException = e.Error });
                    Sys.RevertStatus(Mod.ID);
                }
                else
                {
                    // update the cached mod details by re-reading the mod.xml
                    string sourceFileName = Install.LatestInstalled.InstalledLocation;
                    string sourcePath = Path.Combine(Sys.Settings.LibraryLocation, sourceFileName);
                    Mod updatedMod = new ModImporter().ParseModXmlFromSource(sourcePath, Mod);

                    Install.CachedDetails = updatedMod;
                    Install.Versions.Clear();
                    Install.Versions.Add(new InstalledVersion() { VersionDetails = updatedMod.LatestVersion, InstalledLocation = sourceFileName });

                    Sys.Message(new WMessage($"[{StringKey.Updated}] {Mod.Name}") { TextTranslationKey = StringKey.Updated });
                    Sys.SetStatus(Mod.ID, ModStatus.Installed);
                }

                Sys.SaveLibrary();
            }
        }

        internal class InstallModProcedure : InstallProcedure
        {
            public string FileName { get; set; }
            public Mod Mod { get; set; }
            public string ExtractSubFolder { get; set; }
            public string ExtractInto { get; set; }
            private string _dest;
            private bool HasProcessed { get; set; } = false;


            private void ProcessDownload(object state)
            {
                // check if already finished processing download and call Complete()
                if (HasProcessed)
                {
                    SetPercentComplete?.Invoke(100);
                    Complete();
                    return;
                }

                try
                {
                    string source = Path.Combine(Sys.Settings.LibraryLocation, "temp", FileName);
                    _dest = Path.Combine(Sys.Settings.LibraryLocation, FileName);

                    byte[] sig = new byte[4];
                    int isig = 0;
                    using (var fs = new FileStream(source, FileMode.Open, FileAccess.Read))
                    {
                        fs.Read(sig, 0, 4);
                        isig = BitConverter.ToInt32(sig, 0);
                        fs.Close();
                    }

                    if (isig == AppWrapper.IrosArc.SIG)
                    {
                        //plain IRO file, just move into place
                        using (var iro = new AppWrapper.IrosArc(source))
                            if (!iro.CheckValid())
                                throw new Exception("IRO archive appears to be invalid: corrupted download?");

                        SetPercentComplete?.Invoke(50);

                        File.Copy(source, _dest, overwrite: true);
                        File.Delete(source);
                    }
                    else
                    {
                        using (var fs = new FileStream(source, FileMode.Open, FileAccess.Read))
                        {
                            var archive = ArchiveFactory.Open(fs);
                            var iroEnt = archive.Entries.FirstOrDefault(e => Path.GetExtension(e.Key).Equals(".iro", StringComparison.InvariantCultureIgnoreCase));
                            if (iroEnt != null)
                            {
                                SetPercentComplete?.Invoke(50);
                                iroEnt.WriteToFile(_dest, new SharpCompress.Common.ExtractionOptions() { Overwrite = true });
                            }
                            else
                            {
                                //extract entire archive...
                                if (_dest.EndsWith(".iro", StringComparison.InvariantCultureIgnoreCase)) _dest = _dest.Substring(0, _dest.Length - 4);
                                string extractTo = _dest;
                                if (!String.IsNullOrEmpty(ExtractInto)) extractTo = Path.Combine(extractTo, ExtractInto);
                                Directory.CreateDirectory(extractTo);

                                using (var reader = archive.ExtractAllEntries())
                                {
                                    var entries = archive.Entries.ToArray();
                                    int count = 0;
                                    while (reader.MoveToNextEntry())
                                    {
                                        if (!reader.Entry.IsDirectory)
                                        {
                                            reader.WriteEntryToDirectory(extractTo, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                                        }

                                        count++;
                                        float prog = (float)count / (float)entries.Length;
                                        SetPercentComplete?.Invoke((int)(50 * prog) + 50); // start at 50% go up to 100%
                                    }
                                }
                            }
                            fs.Close();
                        }

                        File.Delete(source);
                    }
                }
                catch (Exception e)
                {
                    Error(e);
                    return;
                }


                HasProcessed = true; // if reached this point then successfully processed the download with no error
                SetPercentComplete?.Invoke(100);
                Complete();
            }

            public override void DownloadComplete(System.ComponentModel.AsyncCompletedEventArgs e)
            {
                if (e.Error != null)
                {
                    Sys.Message(new WMessage($"[{StringKey.ErrorDownloading}] {Mod.Name}") { TextTranslationKey = StringKey.ErrorDownloading, LoggedException = e.Error });
                    Sys.RevertStatus(Mod.ID);
                    return;
                }

                InstalledItem inst = Sys.Library.GetItem(Mod.ID);
                bool isIro = Path.GetExtension(_dest) == ".iro";
                string modName = Mod?.Name;

                if (string.IsNullOrWhiteSpace(modName))
                {
                    modName = ModImporter.ParseNameFromFileOrFolder(_dest);
                }

                try
                {
                    Mod = ModImporter.ImportMod(_dest, modName, isIro, noCopy: true); // noCopy set to true because ProcessDownload() already copied the downloaded mod to the 'mods' library folder
                }
                catch (Exception ex)
                {
                    base.Error(ex);
                    return;
                }

                if (inst == null)
                {
                    Sys.Message(new WMessage($"[{StringKey.Installed}] {Mod.Name}") { TextTranslationKey = StringKey.Installed });
                }
                else
                {
                    Sys.Message(new WMessage($"[{StringKey.Updated}] {Mod.Name}") { TextTranslationKey = StringKey.Updated });
                }

                Sys.SetStatus(Mod.ID, ModStatus.Installed);
                Sys.SaveLibrary();
            }

            public override void Schedule()
            {
                System.Threading.ThreadPool.QueueUserWorkItem(ProcessDownload);
            }
        }


    }
}
