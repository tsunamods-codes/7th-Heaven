/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace Iros._7th.Workshop
{
    public class Mod
    {
        public Mod()
        {
            // ensure lists are not null on creation
            Tags = new List<string>();
            Requirements = new List<ModRequirement>();
            Patches = new List<ModPatch>();
            LatestVersion = new ModVersion()
            {
                CompatibleGameVersions = GameVersions.All,
                Links = new List<string>(),
                PreviewImage = string.Empty,
                ReleaseDate = DateTime.Now,
                ReleaseNotes = string.Empty,
                Version = 1.00m,
            };
        }

        public Guid ID { get; set; }
        public string Author { get; set; }
        public string Link { get; set; }
        public string DonationLink { get; set; }
        public ModVersion LatestVersion { get; set; }
        public decimal MetaVersion { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public string Category { get; set; }

        public string SourceCatalogName { get; set; }

        public string SourceCatalogUrl { get; set; }

        public List<string> Tags { get; set; }
        [System.Xml.Serialization.XmlElement("Patch")]
        public List<ModPatch> Patches { get; set; }
        [System.Xml.Serialization.XmlElement("Requirement")]
        public List<ModRequirement> Requirements { get; set; }

        /// <summary>
        /// Set to true on import if mod has music files
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        public bool ContainsMusic { get; set; }


        /// <summary>
        /// Set to true on import if mod has movie files
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        public bool ContainsMovies { get; set; }

        /// <summary>
        /// Returns list of patches to download to update <paramref name="from"/> version to  <paramref name="to"/> version 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public List<ModPatch> GetPatchesFromTo(decimal from, decimal to)
        {
            List<ModPatch> patchesForVersion = new List<ModPatch>();

            if (Patches == null || Patches.Count == 0)
                return patchesForVersion;

            foreach (ModPatch patch in Patches)
            {
                if (patch == null)
                    continue;

                if (patch.VerTo == to && patch.VerFrom.Split(',').Select(s =>
                                                                    {
                                                                        if (decimal.TryParse(s, System.Globalization.NumberStyles.AllowDecimalPoint, new System.Globalization.CultureInfo(""), out decimal parsedVer))
                                                                            return parsedVer;
                                                                        else
                                                                            return 0;
                                                                    }).Contains(from))
                {
                    patchesForVersion.Add(patch);
                }
            }

            return patchesForVersion;
        }

        public int SearchRelevance(string text)
        {
            if (Name.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) >= 0) return 200;
            if (Description.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) >= 0) return 100;
            if (Author.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) >= 0) return 50;
            if (Category?.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) >= 0) return 25;
            return 0;
        }

        /// <summary>
        /// Compares properties to another Mod to see if 
        /// name, description, category, author, or link is different.
        /// </summary>
        /// <param name="other"></param>
        /// <returns> true if properties are different; false if they are equal. </returns>
        internal bool HasChanges(Mod other)
        {
            return (this.Name != other.Name ||
                    this.Description != other.Description ||
                    this.Category != other.Category ||
                    this.Author != other.Author ||
                    this.Link != other.Link ||
                    this.DonationLink != other.DonationLink);
        }

        public static Mod CopyMod(Mod modToCopy)
        {
            return new Mod()
            {
                Author = modToCopy.Author,
                Description = modToCopy.Description,
                Category = modToCopy.Category,
                ID = modToCopy.ID,
                Link = modToCopy.Link,
                DonationLink = modToCopy.DonationLink,
                Tags = modToCopy.Tags?.ToList(),
                Name = modToCopy.Name,
                ContainsMovies = modToCopy.ContainsMovies,
                ContainsMusic = modToCopy.ContainsMusic,
                MetaVersion = modToCopy.MetaVersion,
                SourceCatalogName = modToCopy.SourceCatalogName,
                SourceCatalogUrl = modToCopy.SourceCatalogUrl,
                Patches = modToCopy.Patches?.ToList(),
                Requirements = modToCopy.Requirements?.ToList(),
                LatestVersion = new ModVersion()
                {
                    CompatibleGameVersions = modToCopy.LatestVersion.CompatibleGameVersions,
                    Links = modToCopy.LatestVersion.Links?.ToList(),
                    PreviewImage = modToCopy.LatestVersion.PreviewImage,
                    ReleaseDate = modToCopy.LatestVersion.ReleaseDate,
                    ReleaseNotes = modToCopy.LatestVersion.ReleaseNotes,
                    Version = modToCopy.LatestVersion.Version,
                    DownloadSize = modToCopy.LatestVersion.DownloadSize,
                    ExtractInto = modToCopy.LatestVersion.ExtractInto,
                    ExtractSubFolder = modToCopy.LatestVersion.ExtractSubFolder,
                    PatchLinks = modToCopy.LatestVersion.PatchLinks?.ToList()
                }
            };
        }
    }
}
