using _7thWrapperLib;
using Iros._7th;
using Iros._7th.Workshop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace _7thHeaven.Code
{
    public class ModImporter
    {
        public delegate void OnImportProgressChanged(string message, double percentComplete);
        public event OnImportProgressChanged ImportProgressChanged;

        private static bool TryParseIDFromXmlDoc(Mod modRef, XmlDocument doc)
        {
            string modidstr = doc.SelectSingleNode("/ModInfo/ID").NodeTextS();
            if (!string.IsNullOrWhiteSpace(modidstr))
            {
                try
                {
                    modRef.ID = new Guid(modidstr);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return false;
        }

        private static bool TryParseVersionFromXmlDoc(Mod modRef, XmlDocument doc)
        {
            string versionText = doc.SelectSingleNode("/ModInfo/Version").NodeTextS().Replace(',', '.'); // in-case Xml has "1,7" format then replace with "1.7"
            if (decimal.TryParse(versionText, System.Globalization.NumberStyles.AllowDecimalPoint, new System.Globalization.CultureInfo(""), out decimal ver))
            {
                modRef.LatestVersion.Version = ver;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes guid from passed in string and replaces underscores '_' with spaces if guid is found in string.
        /// </summary>
        /// <param name="name"> name of mod with possible Guid in string </param>
        /// <returns> if Guid found then returns parsed string; other wise returns <paramref name="name"/></returns>
        public static string ParseNameFromFileOrFolder(string name)
        {
            string parsedName = name;

            Regex regex = new Regex(@"[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}");
            Match match = regex.Match(name);

            if (match.Success)
            {
                int index = name.IndexOf(match.Value) + match.Length;
                parsedName = name.Substring(index);
                parsedName = parsedName.Replace("_", " ").Trim();
            }

            return parsedName;
        }

        /// <summary>
        /// Returns guid from passed in file or folder name if guid is found in string; otherwise returns new guid
        /// </summary>
        /// <param name="name"> name of mod file/folder with possible Guid in string </param>
        /// <returns> if Guid found then returns guid; other wise returns new guid</returns>
        public static Guid ParseModGuidFromFileOrFolderName(string name)
        {
            Regex regex = new Regex(@"[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}");
            Match match = regex.Match(name);

            Guid parsedGuid = Guid.NewGuid();

            if (match.Success)
            {
                if (!Guid.TryParse(match.Value, out parsedGuid))
                {
                    parsedGuid = Guid.NewGuid(); // failed to parse, which sets to empty guid so generate a new one
                }
            }

            return parsedGuid;
        }

        /// <summary>
        /// Static method to import mod 
        /// </summary>
        /// <param name="source"> absolute path to .iro or folder to import</param>
        /// <param name="name"> name to give mod </param>
        /// <param name="iroMode"> true if importing .iro </param>
        /// <param name="noCopy"> does not copy the file/folder to the mods Library folder</param>
        /// <returns>Parsed and imported mod</returns>
        public static Mod ImportMod(string source, string name, bool iroMode, bool noCopy)
        {
            return new ModImporter().Import(source, name, iroMode, noCopy);
        }

        /// <summary>
        /// Imports a mod from file/folder into the Library
        /// </summary>
        /// <param name="source"> absolute path to .iro or folder to import</param>
        /// <param name="name"> name to give mod </param>
        /// <param name="iroMode"> true if importing .iro </param>
        /// <param name="noCopy"> does not copy the file/folder to the mods Library folder</param>
        /// <returns>Parsed and imported mod</returns>
        public Mod Import(string source, string name, bool iroMode, bool noCopy)
        {
            Mod m = ParseModXmlFromSource(source); // this will increment the progress changed value up to 50%

            if (string.IsNullOrWhiteSpace(m.Name))
            {
                m.Name = name;
            }

            // validate mod with same ID doesn't exist in library
            // ... if it already exists then check if it's newer version to update to
            InstalledItem existingItem = Sys.Library.GetItem(m.ID);

            if (existingItem != null)
            {
                if (existingItem.LatestInstalled.VersionDetails.Version >= m.LatestVersion.Version)
                {
                    throw new DuplicateModException($"A mod ({existingItem.CachedDetails.Name} v{existingItem.LatestInstalled.VersionDetails.Version}) with the same ID already exists in your library.");
                }
                else
                {
                    // mod is being updated so uninstall current versions
                    Sys.Library.DeleteAndRemoveInstall(existingItem, existingItem.Versions
                                                                                 .Where(v => Path.Combine(Sys.Settings.LibraryLocation, v.InstalledLocation) != source) // ensure we are not deleting the mod file we are trying to import (in the case that the new file is already copied to mods library location)
                                                                                 .Select(v => v.InstalledLocation));
                }
            }


            string destFileName = Path.GetFileName(source);

            if (!noCopy)
            {
                // copy .iro or mod files to library location
                destFileName = String.Format("{0}_{1}", m.ID, name);

                if (iroMode)
                {
                    RaiseProgressChanged("Copying .iro file to library", 75);
                    destFileName += ".iro";
                    File.Copy(source, Path.Combine(Sys.Settings.LibraryLocation, destFileName), true);
                }
                else
                {
                    int i = 1;
                    string[] allFiles = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
                    foreach (string file in allFiles)
                    {
                        string part = file.Substring(source.Length).Trim('\\', '/');
                        string absoluteDestinationPath = Path.Combine(Sys.Settings.LibraryLocation, destFileName, part);

                        Directory.CreateDirectory(Path.GetDirectoryName(absoluteDestinationPath));
                        File.Copy(file, absoluteDestinationPath, true);

                        double newProgress = 50.0 + (((double)i / allFiles.Length) * 40); // start at 50 and eventually increment to 90 (i.e. 50 + 40 = 90)
                        RaiseProgressChanged($"Copying files from folder {i} / {allFiles.Length}", newProgress);
                        i++;
                    }
                }
            }

            RaiseProgressChanged("Finalizing import", 95);

            Sys.Library.AddInstall(new InstalledItem()
            {
                CachedDetails = m,
                CachePreview = String.Empty,
                ModID = m.ID,
                UpdateType = Sys.Library.DefaultUpdate,
                Versions = new List<InstalledVersion>() { new InstalledVersion() { VersionDetails = m.LatestVersion, InstalledLocation = destFileName } },
            });

            if (!Sys.ActiveProfile.HasItem(m.ID))
            {
                Sys.ActiveProfile.AddItem(new Iros._7th.Workshop.ProfileItem() { ModID = m.ID, Name = m.Name, Settings = new List<ProfileSetting>(), IsModActive = false });
            }

            RaiseProgressChanged("Import complete", 100);
            return m;
        }

        /// <summary>
        /// Imports and applies patch file to installed mod.
        /// </summary>
        /// <param name="sourcePatchFile"> absolute path to .irop file</param>
        /// <returns> true if applied patch. </returns>
        public bool ImportModPatch(string sourcePatchFile)
        {
            XmlDocument doc = null;

            if (!File.Exists(sourcePatchFile))
            {
                RaiseProgressChanged($"Patch file does not exist at {sourcePatchFile}", 0);
                return false;
            }

            // check for mod.xml in .irop file and load xml to get mod id
            RaiseProgressChanged($"Getting mod info from .irop file", 25);
            using (var patch = new IrosArc(sourcePatchFile))
            {
                if (!patch.HasFile("mod.xml"))
                {
                    RaiseProgressChanged($"Failed to apply patch due to missing mod.xml file in .irop patch", 0);
                    return false; // no mod.xml found in patch
                }

                doc = new XmlDocument();
                doc.Load(patch.GetData("mod.xml"));
            }

            if (doc == null)
            {
                RaiseProgressChanged($"Failed to load mod.xml file from patch", 0);
                return false;
            }


            Mod parsedMod = new Mod();

            if (!TryParseIDFromXmlDoc(parsedMod, doc))
            {
                RaiseProgressChanged($"Failed to apply patch due to Mod ID is missing from mod.xml in patch", 0);
                return false;
            }

            if (!TryParseVersionFromXmlDoc(parsedMod, doc))
            {
                RaiseProgressChanged($"Failed to apply patch due to Mod Version is missing from mod.xml in patch", 0);
                return false;

            }


            // if no mod id or mod not installed then error
            InstalledItem installedMod = Sys.Library.GetItem(parsedMod.ID);

            if (installedMod == null)
            {
                RaiseProgressChanged($"No mod is installed with the ID {parsedMod.ID}", 0);
                return false; // mod is not installed
            }

            // stop patching if patch is older than already installed version
            if (installedMod.LatestInstalled.VersionDetails.Version >= parsedMod.LatestVersion.Version)
            {
                throw new DuplicateModException($"A mod ({installedMod.CachedDetails.Name} v{installedMod.LatestInstalled.VersionDetails.Version}) with the same ID already exists in your library.");
            }


            // open .iro for patching and apply patch
            RaiseProgressChanged($"Applying patch to '{installedMod.CachedDetails.Name}'", 50);

            string sourceFileName = installedMod.LatestInstalled.InstalledLocation;
            string sourceIro = Path.Combine(Sys.Settings.LibraryLocation, sourceFileName);

            using (var iro = new IrosArc(sourceIro, true))
            {
                using (var patch = new IrosArc(sourcePatchFile))
                {
                    iro.ApplyPatch(patch, (d, msg) =>
                    {
                        RaiseProgressChanged(msg, 50);
                    });
                }
            }

            // update the cached mod details by re-reading the mod.xml
            RaiseProgressChanged($"Updating cached details of '{installedMod.CachedDetails.Name}'", 75);

            Mod updatedMod = ParseModXmlFromSource(sourceIro, installedMod.CachedDetails);

            installedMod.CachedDetails = updatedMod;
            installedMod.Versions.Clear();
            installedMod.Versions.Add(new InstalledVersion() { VersionDetails = updatedMod.LatestVersion, InstalledLocation = sourceFileName });

            return true;



        }

        /// <summary>
        /// Parses mod.xml from a folder or .iro and returns the <see cref="Mod"/>
        /// </summary>
        /// <param name="sourceFileOrFolder">absolute path to folder or .iro file </param>
        /// <param name="defaultModIfMissing"> default mod properties to use if the mod.xml file is not found </param>
        /// <returns></returns>
        public Mod ParseModXmlFromSource(string sourceFileOrFolder, Mod defaultModIfMissing = null)
        {
            if (defaultModIfMissing == null)
            {
                defaultModIfMissing = new Mod()
                {
                    Author = String.Empty,
                    Description = "Imported mod",
                    Category = "Unknown",
                    ID = ParseModGuidFromFileOrFolderName(sourceFileOrFolder),
                    Link = String.Empty,
                    Tags = new List<string>(),
                    Name = ""
                };
            }


            Mod parsedMod = Mod.CopyMod(defaultModIfMissing);

            if (string.IsNullOrWhiteSpace(sourceFileOrFolder))
            {
                return parsedMod;
            }

            if (!File.Exists(sourceFileOrFolder) && !Directory.Exists(sourceFileOrFolder))
            {
                return parsedMod;
            }

            bool isIroFile = sourceFileOrFolder.EndsWith(".iro");
            XmlDocument doc = null;
            IrosArc arc = null;

            string[] musicFiles = FF7FileLister.GetMusicFiles();
            string[] movieFiles = FF7FileLister.GetMovieFiles().Keys.ToArray();


            if (isIroFile)
            {
                RaiseProgressChanged("Getting mod.xml data from .iro", 10);

                arc = new IrosArc(sourceFileOrFolder, patchable: false, (i, fileCount) =>
                {
                    double newProgress = 10.0 + ((double)i / fileCount) * 30.0;
                    RaiseProgressChanged($"Scanning .iro archive files {i} / {fileCount}", newProgress);
                });

                if (arc.HasFile("mod.xml"))
                {
                    doc = new XmlDocument();
                    doc.Load(arc.GetData("mod.xml"));
                }

                RaiseProgressChanged($"Scanning .iro archive files for movie and music files", 45);
                foreach (string file in arc.AllFileNames())
                {
                    if (musicFiles.Any(f => f.Equals(Path.GetFileName(file), StringComparison.InvariantCultureIgnoreCase)))
                    {
                        parsedMod.ContainsMusic = true;
                    }

                    if (movieFiles.Any(f => f.Equals(Path.GetFileName(file), StringComparison.InvariantCultureIgnoreCase)))
                    {
                        parsedMod.ContainsMovies = true;
                    }

                    if (parsedMod.ContainsMovies && parsedMod.ContainsMusic)
                    {
                        break; // break out of loop to stop scanning since confirmed both music and movie files exist in mod
                    }
                }
            }
            else
            {
                string pathToModXml = Path.Combine(sourceFileOrFolder, "mod.xml");

                RaiseProgressChanged("Getting mod.xml data from file", 10);
                if (File.Exists(pathToModXml))
                {
                    doc = new System.Xml.XmlDocument();
                    doc.Load(pathToModXml);
                }

                RaiseProgressChanged($"Scanning mod folder for movie and music files", 25);
                foreach (string file in FileUtils.GetAllFilesInDirectory(sourceFileOrFolder))
                {
                    if (musicFiles.Any(f => f.Equals(Path.GetFileName(file), StringComparison.InvariantCultureIgnoreCase)))
                    {
                        parsedMod.ContainsMusic = true;
                    }

                    if (movieFiles.Any(f => f.Equals(Path.GetFileName(file), StringComparison.InvariantCultureIgnoreCase)))
                    {
                        parsedMod.ContainsMovies = true;
                    }

                    if (parsedMod.ContainsMovies && parsedMod.ContainsMusic)
                    {
                        break; // break out of loop to stop scanning since confirmed both music and movie files exist in mod
                    }
                }
            }


            if (doc != null)
            {
                RaiseProgressChanged("Parsing information from mod.xml", 50);

                //If mod.xml contains an ID GUID, then use that instead of generating random one
                bool didParseId = TryParseIDFromXmlDoc(parsedMod, doc);

                if (!didParseId)
                {
                    Sys.Message(new WMessage("Invalid GUID found for Mod ID ... Using guid from file/folder name (or new guid).", WMessageLogLevel.LogOnly));
                    parsedMod.ID = ParseModGuidFromFileOrFolderName(sourceFileOrFolder);
                }

                parsedMod.Name = doc.SelectSingleNode("/ModInfo/Name").NodeTextS();
                if (string.IsNullOrWhiteSpace(parsedMod.Name))
                {
                    parsedMod.Name = defaultModIfMissing.Name;
                }

                parsedMod.Author = doc.SelectSingleNode("/ModInfo/Author").NodeTextS();
                if (string.IsNullOrWhiteSpace(parsedMod.Author))
                {
                    parsedMod.Author = defaultModIfMissing.Author;
                }


                parsedMod.Link = doc.SelectSingleNode("/ModInfo/Link").NodeTextS();
                if (string.IsNullOrWhiteSpace(parsedMod.Link))
                {
                    parsedMod.Link = defaultModIfMissing.Link;
                }

                parsedMod.Description = doc.SelectSingleNode("/ModInfo/Description").NodeTextS();
                if (string.IsNullOrWhiteSpace(parsedMod.Description))
                {
                    parsedMod.Description = defaultModIfMissing.Description;
                }

                parsedMod.Category = doc.SelectSingleNode("/ModInfo/Category").NodeTextS();
                if (string.IsNullOrWhiteSpace(parsedMod.Category))
                {
                    parsedMod.Category = defaultModIfMissing.Category;
                }

                parsedMod.LatestVersion.ReleaseNotes = doc.SelectSingleNode("/ModInfo/ReleaseNotes").NodeTextS();
                if (string.IsNullOrWhiteSpace(parsedMod.LatestVersion.ReleaseNotes))
                {
                    parsedMod.LatestVersion.ReleaseNotes = defaultModIfMissing.LatestVersion.ReleaseNotes;
                }


                if (DateTime.TryParse(doc.SelectSingleNode("/ModInfo/ReleaseDate").NodeTextS(), out DateTime parsedDate))
                {
                    parsedMod.LatestVersion.ReleaseDate = parsedDate;
                }
                else
                {
                    parsedMod.LatestVersion.ReleaseDate = defaultModIfMissing.LatestVersion.ReleaseDate;
                }

                bool didParse = TryParseVersionFromXmlDoc(parsedMod, doc);
                if (!didParse)
                {
                    parsedMod.LatestVersion.Version = defaultModIfMissing.LatestVersion.Version;
                }

                var pv = doc.SelectSingleNode("/ModInfo/PreviewFile");
                if (pv != null)
                {
                    // add the preview file to image cache and set the url prefixed with iros://Preview/Auto since it came from auto-import
                    byte[] data = null;

                    if (isIroFile)
                    {
                        data = arc.HasFile(pv.InnerText) ? arc.GetBytes(pv.InnerText) : null;
                    }
                    else
                    {
                        string file = Path.Combine(sourceFileOrFolder, pv.InnerText);
                        data = File.Exists(file) ? File.ReadAllBytes(file) : null;
                    }

                    if (data != null)
                    {
                        string url = $"iros://Preview/Auto/{parsedMod.ID}_{pv.InnerText.Replace('\\', '_')}";
                        parsedMod.LatestVersion.PreviewImage = url;
                        Sys.ImageCache.InsertManual(url, data);
                    }
                }
            }

            if (arc != null)
            {
                arc.Dispose();
            }

            return parsedMod;
        }

        private byte[] GetImageDataFromIrosArc(IrosArc arc, string innerText)
        {
            throw new NotImplementedException();
        }

        public void RaiseProgressChanged(string message, double percentComplete)
        {
            ImportProgressChanged?.Invoke(message, percentComplete);
        }

    }
}
