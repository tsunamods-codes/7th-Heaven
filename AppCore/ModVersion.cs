/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using System;
using System.Collections.Generic;

namespace Iros.Workshop
{
    public class ModVersion
    {
        public ModVersion()
        {
            // ensure lists are not null on creation
            PatchLinks = new List<string>();
            Links = new List<string>();
        }

        public int DownloadSize { get; set; }
        [System.Xml.Serialization.XmlElement("Link")]
        public List<string> Links { get; set; }
        [System.Xml.Serialization.XmlElement("ApplyPatch")]
        public List<string> PatchLinks { get; set; }
        public string ExtractSubFolder { get; set; }
        public string ExtractInto { get; set; }

        public decimal Version { get; set; }
        public DateTime ReleaseDate { get; set; }
        public GameVersions CompatibleGameVersions { get; set; }

        public string PreviewImage { get; set; }
        public string ReleaseNotes { get; set; }

    }
}
