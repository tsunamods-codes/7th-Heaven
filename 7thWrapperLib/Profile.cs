/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using Iros._7th.Workshop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace _7thWrapperLib
{

    public class ProfileConfig
    {
        public string OptionID { get; set; }
        public int OptionValue { get; set; }
    }

    [Flags]
    public enum ProfileItemOptions
    {
        None = 0,
        Active = 0x1,
    }

    [Serializable]
    public class ProfileItem
    {
        public string Folder { get; set; }
        public ProfileItemOptions Options { get; set; }
        public string CacheLink { get; set; }

        public List<ProfileConfig> ConfigOptions { get; set; }

        public ProfileItem()
        {
            ConfigOptions = new List<ProfileConfig>();
        }

        private static string[] _comparison = new[] { "=", "!=", "<", ">", "<=", ">=" };
        private static List<string> _comparisonL = _comparison.ToList();

        public bool IsConfigActive(string spec)
        {
            if (String.IsNullOrWhiteSpace(spec)) return true;

            string[] parts = spec.Split(_comparison, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) return false;
            parts = new[] { parts[0].Trim(), spec.Substring(parts[0].Length, spec.Length - parts[1].Length - parts[0].Length), parts[1].Trim() };

            var conf = ConfigOptions.Find(c => c.OptionID.Equals(parts[0], StringComparison.InvariantCultureIgnoreCase));
            if (conf == null) return false;

            int val;
            if (!int.TryParse(parts[2], out val)) return false;

            switch (_comparisonL.IndexOf(parts[1]))
            {
                case 0:
                    return val == conf.OptionValue;
                case 1:
                    return val != conf.OptionValue;
                case 2:
                    return val < conf.OptionValue;
                case 3:
                    return val > conf.OptionValue;
                case 4:
                    return val <= conf.OptionValue;
                case 5:
                    return val >= conf.OptionValue;
                default:
                    return false;
            }
        }
    }

    [Serializable]
    public class Profile
    {
        public Profile()
        {
            Items = new List<ProfileItem>();
        }

        public List<ProfileItem> Items { get; set; }
    }

    //Base: DBFD38

    [Flags]
    public enum RuntimeOptions
    {
        None = 0,
        DetailedLog = 0x1,
    }

    [Serializable]
    public class RuntimeParams
    {
        public string ProfileFile { get; set; }
    }

    [Serializable]
    public class RuntimeProfile
    {
        public string LogFile { get; set; }
        public RuntimeOptions Options { get; set; }
        public string ModPath { get; set; }
        public string FF7Path { get; set; }
        public string[] gameFiles { get; set; }
        public List<string> MonitorPaths { get; set; }

        public List<RuntimeMod> Mods { get; set; }

        public List<Tuple<string, string>> MonitorVars { get; set; }
    }

    public static class XmlUtil
    {
        public static string NodeText(this XmlNode n, string def = "")
        {
            return (n == null) ? def : n.InnerText;
        }
    }

    public class LoaderContext
    {
        public Dictionary<string, string> VarAliases { get; set; }
    }

    [Serializable]
    public class RuntimeMod
    {
        public string BaseFolder { get; private set; }
        public List<string> ExtraFolders { get; private set; }
        public List<ConditionalFolder> Conditionals { get; private set; }
        public List<string> LoadLibraries { get; private set; }
        public List<string> LoadAssemblies { get; private set; }
        public List<string> LoadPlugins { get; private set; }
        public List<ProgramInfo> LoadPrograms { get; private set; }
        public List<FFNxFlag> FFNxConfig { get; private set; }
        public List<Variable> Variables { get; private set; }

        /* NON SERIALIZED */
        [NonSerialized]
        public Wpf32Window WpfWindowInterop;        
        [NonSerialized]
        private HashSet<string> _activated;

        public RuntimeMod(string folder, IEnumerable<ConditionalFolder> conditionalFolders, IEnumerable<string> extraFolders, ModInfo modInfo)
        {
            BaseFolder = folder;
            Conditionals = conditionalFolders.ToList();
            ExtraFolders = extraFolders.ToList();
            LoadLibraries = modInfo.LoadLibraries.ToList();
            LoadAssemblies = modInfo.LoadAssemblies.ToList();
            LoadPlugins = modInfo.LoadPlugins.ToList();
            LoadPrograms = modInfo.LoadPrograms.ToList();
            FFNxConfig = modInfo.FFNxConfig;
            Variables = modInfo.Variables;
        }

        /// <summary>
        /// Ensure the <see cref="_archive"/> is loaded if mod is an .iro
        /// </summary>
        public void Startup()
        {
            _activated = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            DirectoryInfo di = new DirectoryInfo(BaseFolder);

            foreach(FileInfo fi in di.GetFiles("*", SearchOption.AllDirectories))
            {
                if (!_activated.Contains(fi.DirectoryName))
                {
                    _activated.Add(fi.DirectoryName);
                    DebugLogger.DetailedWriteLine("      Added folder " + fi.DirectoryName);
                }
                    
                _activated.Add(fi.FullName);
                DebugLogger.DetailedWriteLine("      Added file " + fi.FullName);
            }
        }

        private static bool SameData(byte[] b1, byte[] b2)
        {
            if (b1.Length != b2.Length) return false;
            foreach (int i in Enumerable.Range(0, b1.Length))
                if (b1[i] != b2[i]) return false;
            return true;
        }

        private void WriteIfNecessary(string path, byte[] data)
        {
            if (System.IO.File.Exists(path))
            {
                byte[] exist = System.IO.File.ReadAllBytes(path);
                if (!SameData(data, exist))
                    System.IO.File.WriteAllBytes(path, data);
            }
            else
                System.IO.File.WriteAllBytes(path, data);
        }

        private IEnumerable<string> GetDlls(IEnumerable<string> dlls, bool loadaside)
        {
            return dlls.Select(s => System.IO.Path.Combine(BaseFolder, s));
        }

        public IEnumerable<string> GetLoadLibraries()
        {
            return GetDlls(LoadLibraries, false);
        }
        public IEnumerable<string> GetLoadAssemblies()
        {
            return GetDlls(LoadAssemblies, true);
        }
        public IEnumerable<string> GetLoadPlugins()
        {
            return GetDlls(LoadPlugins, true);
        }

        public IEnumerable<ProgramInfo> GetLoadPrograms()
        {
            LoadPrograms.ForEach(s => s.PathToProgram = Path.Combine(BaseFolder, s.PathToProgram));
            return LoadPrograms.ToList();
        }

        public bool DirExists(string dir)
        {
            return _activated.Contains(dir);
        }

        public bool OverridesFolder(string which)
        {
            string file = System.IO.Path.Combine(BaseFolder, which);
            if (DirExists(file)) return true;
            foreach (string extra in ExtraFolders)
                if (DirExists(System.IO.Path.Combine(BaseFolder, extra, which))) return true;
            foreach (var cf in Conditionals)
            {
                file = System.IO.Path.Combine(BaseFolder, cf.Folder, which);
                if (DirExists(file)) return true;
            }
           
            return false;
        }

        private bool FileExists(string file)
        {
            return _activated.Contains(file);
        }

        public bool HasFile(string file)
        {
            return _activated.Contains(file);
        }

        public System.IO.Stream Read(string file)
        {
            return new System.IO.FileStream(System.IO.Path.Combine(BaseFolder, file), System.IO.FileMode.Open, System.IO.FileAccess.Read);
        }

        public string GetOverride(string path)
        {
            string ret = null;

            string file;
            foreach (var cf in Conditionals)
            {
                file = System.IO.Path.Combine(BaseFolder, cf.Folder, path);
                if (FileExists(file) && cf.IsActive(path))
                {
                    ret = file;
                    break;
                }
            }
            if (ret == null)
            {
                foreach (string extra in ExtraFolders)
                {
                    file = System.IO.Path.Combine(BaseFolder, extra, path);
                    if (FileExists(file))
                    {
                        ret = file;
                        break;
                    }
                }
            }

            if (ret == null)
            {
                file = System.IO.Path.Combine(BaseFolder, path);
                if (FileExists(file)) ret = file;
            }

            return ret;
        }

        public IEnumerable<string> GetOverrides(string path)
        {
            string file;
            foreach (var cf in Conditionals)
            {
                file = System.IO.Path.Combine(BaseFolder, cf.Folder, path);
                if (FileExists(file)) yield return file;
            }
            foreach (string extra in ExtraFolders)
            {
                file = System.IO.Path.Combine(BaseFolder, extra, path);
                if (FileExists(file)) yield return file;
            }
            file = System.IO.Path.Combine(BaseFolder, path);
            if (FileExists(file)) yield return file;
        }

        private Dictionary<string, string[]> _pathOverrideNames = new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase);

        public IEnumerable<string> GetPathOverrideNames(string path)
        {
            string[] cache;
            if (_pathOverrideNames.TryGetValue(path, out cache)) return cache;
            cache = GetPathOverrideNamesInt(path).ToArray();
            _pathOverrideNames[path] = cache;
            return cache;
        }

        private IEnumerable<string> GetPathOverrideNamesInt(string path)
        {
            string spath = System.IO.Path.Combine(BaseFolder, path);
            if (DirExists(spath))
                foreach (string file in System.IO.Directory.GetFiles(spath, "*", System.IO.SearchOption.AllDirectories))
                {
                    yield return file.Substring(spath.Length).TrimStart('\\');
                }
            foreach (string extra in ExtraFolders)
            {
                spath = System.IO.Path.Combine(BaseFolder, extra, path);
                if (DirExists(spath))
                    foreach (string file in System.IO.Directory.GetFiles(spath, "*", System.IO.SearchOption.AllDirectories))
                    {
                        yield return file.Substring(spath.Length).TrimStart('\\');
                    }
            }
            foreach (var cf in Conditionals)
            {
                spath = System.IO.Path.Combine(BaseFolder, cf.Folder, path);
                if (DirExists(spath))
                    foreach (string file in System.IO.Directory.GetFiles(spath, "*", System.IO.SearchOption.AllDirectories))
                    {
                        yield return file.Substring(spath.Length).TrimStart('\\');
                    }
            }
        }
    }

    public abstract class ActiveWhen
    {
        public class And : ActiveWhen
        {
            private List<ActiveWhen> _children;

            public List<ActiveWhen> Children { get => _children; }

            protected override void Load(XmlNode source)
            {
                _children = source.SelectNodes("*").Cast<XmlNode>().Select(n => Create(n)).ToList();
            }
            public override bool IsActive(Func<string, bool> check)
            {
                return _children.All(a => a.IsActive(check));
            }
        }
        public class Or : ActiveWhen
        {
            private List<ActiveWhen> _children;
            public List<ActiveWhen> Children { get => _children; }

            protected override void Load(XmlNode source)
            {
                _children = source.SelectNodes("*").Cast<XmlNode>().Select(n => Create(n)).ToList();
            }
            public override bool IsActive(Func<string, bool> check)
            {
                return _children.Any(a => a.IsActive(check));
            }
        }
        public class Not : ActiveWhen
        {
            private ActiveWhen _child;

            public ActiveWhen Child
            {
                get => _child;
            }

            protected override void Load(XmlNode source)
            {
                _child = Create(source.SelectSingleNode("*"));
            }
            public override bool IsActive(Func<string, bool> check)
            {
                return !_child.IsActive(check);
            }
        }
        public class Option : ActiveWhen
        {
            [XmlElement("Option")]
            public string Condition { get; set; }

            protected override void Load(XmlNode source)
            {
                Condition = source.InnerText;
            }
            public override bool IsActive(Func<string, bool> check)
            {
                return check(Condition);
            }
        }

        private static Dictionary<string, Func<ActiveWhen>> _types;

        static ActiveWhen()
        {
            _types = new Dictionary<string, Func<ActiveWhen>>(StringComparer.InvariantCultureIgnoreCase);
            _types["Or"] = () => new Or();
            _types["And"] = () => new And();
            _types["Not"] = () => new Not();
            _types["Option"] = () => new Option();
        }

        protected abstract void Load(System.Xml.XmlNode source);
        public abstract bool IsActive(Func<string, bool> check);

        private static ActiveWhen Create(System.Xml.XmlNode source)
        {
            ActiveWhen aw = _types[source.LocalName]();
            aw.Load(source);
            return aw;
        }

        private static ActiveWhen Compat(string condition)
        {
            return new Option() { Condition = condition };
        }

        public static ActiveWhen Parse(XmlNode source)
        {
            var xAW = source.SelectSingleNode("ActiveWhen");
            if (xAW != null)
                return Create(xAW.SelectSingleNode("*"));
            if (source.Attributes["ActiveWhen"] != null)
                return Compat(source.Attributes["ActiveWhen"].Value);
            return null;
        }

        /// <summary>
        /// Recursively write the correct xml structure for the abstract class based on the inherited types
        /// </summary>
        internal void WriteXml(XmlWriter writer)
        {
            if (this is ActiveWhen.Option)
            {
                writer.WriteElementString("Option", (this as Option).Condition);
            }
            else if (this is ActiveWhen.Not)
            {
                writer.WriteStartElement("Not");

                (this as Not).Child.WriteXml(writer);

                writer.WriteEndElement();
            }
            else if (this is ActiveWhen.And)
            {
                writer.WriteStartElement("And");

                List<ActiveWhen> children = (this as ActiveWhen.And).Children;
                foreach (ActiveWhen child in children)
                {
                    child.WriteXml(writer);
                }

                writer.WriteEndElement();
            }
            else if (this is ActiveWhen.Or)
            {
                writer.WriteStartElement("Or");

                var children = (this as ActiveWhen.Or).Children;
                foreach (ActiveWhen child in children)
                {
                    child.WriteXml(writer);
                }

                writer.WriteEndElement();
            }
        }
    }



    public class ModFolder : IXmlSerializable
    {
        public string Folder { get; set; }

        public ActiveWhen ActiveWhen { get; set; }

        public ModFolder(XmlNode source)
        {
            Folder = source.Attributes["Folder"].NodeText();
            ActiveWhen = ActiveWhen.Parse(source);
        }

        public ModFolder()
        {
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            // This method should not be used for de-serialization... see ModInfo.Load()
            XmlDocument doc = new XmlDocument();
            doc.ReadNode(reader);

            if (doc != null)
            {
                ModFolder parsed = new ModFolder(doc.SelectSingleNode("*"));
                Folder = parsed.Folder;
                ActiveWhen = parsed.ActiveWhen;
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("Folder", Folder);
            if (ActiveWhen is ActiveWhen.Option)
            {
                // ActiveWhen is treated as an attribute when it is just an Option
                writer.WriteAttributeString("ActiveWhen", (ActiveWhen as ActiveWhen.Option).Condition);
            }
            else
            {
                // ActiveWhen is an xml element
                writer.WriteStartElement("ActiveWhen");

                if (ActiveWhen is ActiveWhen.Not)
                {
                    writer.WriteStartElement("Not");

                    ActiveWhen child = (ActiveWhen as ActiveWhen.Not).Child;
                    child.WriteXml(writer);

                    writer.WriteEndElement();
                }
                else if (ActiveWhen is ActiveWhen.And)
                {
                    writer.WriteStartElement("And");

                    List<ActiveWhen> children = (ActiveWhen as ActiveWhen.And).Children;

                    foreach (var child in children)
                    {
                        child.WriteXml(writer);
                    }

                    writer.WriteEndElement();
                }
                else if (ActiveWhen is ActiveWhen.Or)
                {
                    writer.WriteStartElement("Or");

                    List<ActiveWhen> children = (ActiveWhen as ActiveWhen.Or).Children;

                    foreach (var child in children)
                    {
                        child.WriteXml(writer);
                    }

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }
        }
    }

    public class CompatEntry
    {
        [XmlAttribute]
        public Guid ModID { get; set; }
        public string Description { get; set; }

        [XmlIgnore]
        public List<decimal> Versions { get; set; }

        [XmlAttribute(AttributeName = "Versions")]
        public string VersionsAttribute
        {
            get
            {
                if (Versions == null || Versions.Count == 0)
                    return "";

                return string.Join(",", Versions);
            }
        }

        public CompatEntry(XmlNode n)
        {
            Description = n.InnerText;
            ModID = Guid.Parse(n.Attributes["ModID"].Value);
            if (n.Attributes["Versions"] != null)
                Versions = n.Attributes["Versions"].Value
                    .Split(',')
                    .Select(s =>
                    {
                        decimal.TryParse(s, NumberStyles.AllowDecimalPoint, new CultureInfo("") ,out decimal parsedVer);
                        return parsedVer;
                    })
                    .ToList();
            else
                Versions = new List<decimal>();
        }

        public CompatEntry()
        {
        }
    }

    public class CompatSetting
    {
        public string MyID { get; set; } //can be blank, in which case, whenever this mod is active at all, the other mod setting must be as declared
        public int MyValue { get; set; }
        public Guid ModID { get; set; }
        public string TheirID { get; set; }
        public int? Require { get; set; }
        public List<int> Forbid { get; set; }

        public CompatSetting(XmlNode n)
        {
            if (n.SelectSingleNode("MyID") != null)
            {
                MyID = n.SelectSingleNode("MyID").InnerText;
                MyValue = int.Parse(n.SelectSingleNode("MyValue").InnerText);
            }
            ModID = Guid.Parse(n.SelectSingleNode("ModID").InnerText);
            TheirID = n.SelectSingleNode("TheirID").InnerText;
            if (n.SelectSingleNode("Require") != null)
                Require = int.Parse(n.SelectSingleNode("Require").InnerText);
            Forbid = n.SelectNodes("Forbid")
                .Cast<XmlNode>()
                .Select(x => int.Parse(x.InnerText))
                .ToList();
        }

        public CompatSetting()
        {
        }
    }

    public class Compatibility
    {
        public List<CompatEntry> Requires { get; set; }
        public List<CompatEntry> Forbids { get; set; }
        public List<CompatSetting> Settings { get; set; }

        public Compatibility(XmlNode n)
        {
            if (n == null)
            {
                Requires = new List<CompatEntry>();
                Forbids = new List<CompatEntry>();
                Settings = new List<CompatSetting>();
            }
            else
            {
                Requires = n.SelectNodes("Require")
                    .Cast<XmlNode>()
                    .Select(x => new CompatEntry(x))
                    .ToList();
                Forbids = n.SelectNodes("Forbid")
                    .Cast<XmlNode>()
                    .Select(x => new CompatEntry(x))
                    .ToList();
                Settings = n.SelectNodes("Setting")
                    .Cast<XmlNode>()
                    .Select(x => new CompatSetting(x))
                    .ToList();
            }
        }

        public Compatibility()
        {
        }
    }

    public class ModInfo
    {
        public ModInfo(string filePath, LoaderContext ctx)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);
            Load(doc, ctx);
        }

        private void Load(XmlDocument doc, LoaderContext ctx)
        {
            Conditionals = new List<ConditionalFolder>();
            ModFolders = new List<ModFolder>();
            Options = new List<ConfigOption>();
            LoadLibraries = new List<string>();
            LoadAssemblies = new List<string>();
            LoadPlugins = new List<string>();
            LoadPrograms = new List<ProgramInfo>();
            FFNxConfig = new List<FFNxFlag>();
            Variables = new List<Variable>();

            Guid.TryParse(doc.SelectSingleNode("/ModInfo/ID").NodeText(), out Guid parsedId);
            ID = parsedId;
            
            Name = doc.SelectSingleNode("/ModInfo/Name").NodeText();
            Author = doc.SelectSingleNode("/ModInfo/Author").NodeText();

            string versionText = doc.SelectSingleNode("/ModInfo/Version").NodeText("0.00").Replace(',', '.'); // in-case Xml has "1,7" format then replace with "1.70"
            if (decimal.TryParse(versionText, NumberStyles.AllowDecimalPoint, new CultureInfo("") ,out decimal parsedVersion))
            {
                Version = parsedVersion;
            }

            ReleaseNotes = doc.SelectSingleNode("/ModInfo/ReleaseNotes").NodeText();
            Description = doc.SelectSingleNode("/ModInfo/Description").NodeText();
            Category = doc.SelectSingleNode("/ModInfo/Category").NodeText();
            Link = doc.SelectSingleNode("/ModInfo/Link").NodeText();
            PreviewFile = doc.SelectSingleNode("/ModInfo/PreviewFile").NodeText();

            DateTime.TryParse(doc.SelectSingleNode("/ModInfo/ReleaseDate").NodeText(), out DateTime parsedDate);
            ReleaseDate = parsedDate;

            foreach (XmlNode cond in doc.SelectNodes("/ModInfo/Conditional"))
                Conditionals.Add(new ConditionalFolder(cond, ctx));
            foreach (XmlNode modf in doc.SelectNodes("/ModInfo/ModFolder"))
                ModFolders.Add(new ModFolder(modf));
            foreach (XmlNode conf in doc.SelectNodes("/ModInfo/ConfigOption"))
                Options.Add(new ConfigOption(conf));
            foreach (XmlNode LL in doc.SelectNodes("/ModInfo/LoadLibrary"))
                LoadLibraries.Add(LL.InnerText);
            foreach (XmlNode LA in doc.SelectNodes("/ModInfo/LoadAssembly"))
                LoadAssemblies.Add(LA.InnerText);
            foreach (XmlNode LP in doc.SelectNodes("/ModInfo/LoadPlugin"))
                LoadPlugins.Add(LP.InnerText);
            foreach (XmlNode xmlNode in doc.SelectNodes("/ModInfo/FFNxConfig"))
            {
                foreach(XmlNode child in xmlNode)
                {
                    var flag = new FFNxFlag();

                    flag.Key = child.Name;
                    if (child.ChildNodes.Count > 1)
                    {
                        List<string> items = new List<string>();
                        foreach (XmlNode item in child.ChildNodes)
                        {
                            items.Add(item.InnerText);
                        }
                        flag.Values = items;
                    }
                    else
                    {
                        flag.Value = child.InnerText;
                    }

                    foreach (XmlAttribute attr in child.Attributes)
                        flag.Attributes.Add(attr.Name, int.Parse(attr.Value));

                    FFNxConfig.Add(flag);
                }
                    
            }

            foreach (XmlNode xmlNode in doc.SelectNodes("/ModInfo/Variable"))
            {

                Variable var = new Variable();

                var.Name = xmlNode.Attributes.GetNamedItem("Name").Value;
                var.Value = xmlNode.InnerText.Trim();

                Variables.Add(var);
            }

            XmlNode loadPrograms = doc.SelectSingleNode("/ModInfo/LoadPrograms");

            if (loadPrograms != null)
            {
                foreach (XmlNode pi in loadPrograms.ChildNodes)
                {
                    string programPath = pi.SelectSingleNode("PathToProgram").NodeText();
                    string programArgs = pi.SelectSingleNode("ProgramArgs").NodeText();
                    bool closeAll = pi.SelectSingleNode("CloseAllInstances").NodeText().Equals("true", StringComparison.InvariantCultureIgnoreCase);

                    string windowTitle = pi.SelectSingleNode("WindowTitle").NodeText();
                    bool waitToShow = pi.SelectSingleNode("WaitForWindowToShow").NodeText().Equals("true", StringComparison.InvariantCultureIgnoreCase);
                    string waitTimeoutTxt = pi.SelectSingleNode("WaitTimeOutInSeconds").NodeText();
                    int.TryParse(waitTimeoutTxt, out int waitTimeout);


                    LoadPrograms.Add(new ProgramInfo()
                    {
                        PathToProgram = programPath,
                        ProgramArgs = programArgs,
                        CloseAllInstances = closeAll,
                        WindowTitle = windowTitle,
                        WaitForWindowToShow = waitToShow,
                        WaitTimeOutInSeconds = waitTimeout
                    });
                }
            }


            OrderBefore = doc.SelectNodes("/ModInfo/OrderConstraints/Before")
                .Cast<XmlNode>()
                .Select(n => Guid.Parse(n.InnerText))
                .ToList();
            OrderAfter = doc.SelectNodes("/ModInfo/OrderConstraints/After")
                .Cast<XmlNode>()
                .Select(n => Guid.Parse(n.InnerText))
                .ToList();

            Compatibility = new Compatibility(doc.SelectSingleNode("/ModInfo/Compatibility"));
        }

        public ModInfo(XmlDocument doc, LoaderContext ctx)
        {
            Load(doc, ctx);
        }

        public ModInfo()
        {
            Conditionals = new List<ConditionalFolder>();
            ModFolders = new List<ModFolder>();
            Options = new List<ConfigOption>();
            LoadLibraries = new List<string>();
            LoadAssemblies = new List<string>();
            LoadPlugins = new List<string>();
            LoadPrograms = new List<ProgramInfo>();
            PreviewFile = Author = Description = Link = String.Empty;
            Version = 0.00m;
            OrderAfter = new List<Guid>();
            OrderBefore = new List<Guid>();
            Compatibility = null;
            FFNxConfig = new List<FFNxFlag>();
            Variables = new List<Variable>();
        }

        public Guid ID { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }

        public string Author { get; set; }
        public decimal Version { get; set; }
        public string Description { get; set; }
        public string ReleaseNotes { get; set; }
        public DateTime ReleaseDate { get; set; }

        public string Link { get; set; }
        public string DonationLink { get; set; }
        public string PreviewFile { get; set; }

        [XmlElement(ElementName = "ModFolder")]
        public List<ModFolder> ModFolders { get; private set; }
        public List<ConditionalFolder> Conditionals { get;  set; }

        [XmlElement(ElementName = "ConfigOption")]
        public List<ConfigOption> Options { get;  set; }
        public List<string> LoadLibraries { get;  set; }
        public List<string> LoadAssemblies { get;  set; }
        public List<string> LoadPlugins { get;  set; }
        public List<ProgramInfo> LoadPrograms { get;  set; }
        public Compatibility Compatibility { get; set; }
        public List<Guid> OrderBefore { get; set; }
        public List<Guid> OrderAfter { get; set; }
        public List<FFNxFlag> FFNxConfig { get; set; }
        public List<Variable> Variables { get; set; }
    }

    [Serializable]
    public class ProgramInfo
    {
        public string PathToProgram { get; set; }
        public string ProgramArgs { get; set; }

        /// <summary>
        /// If set to true then all Processes with the same name as the <see cref="PathToProgram"/>
        /// will be closed.
        /// </summary>
        public bool CloseAllInstances { get; set; }

        /// <summary>
        /// If set to true then the launch process wil wait until the program window is visible
        /// and will force minimize it.
        /// </summary>
        public bool WaitForWindowToShow { get; set; }

        /// <summary>
        /// This will be the Main Window Title of the Window displayed.
        /// Used to find the window handle and wait for it before closing
        /// </summary>
        public string WindowTitle { get; set; }

        /// <summary>
        /// Defaults to 0 if missing from mod.xml; If set to 0, then no time out. 
        /// Otherwise will wait for this amount of time before continuing the launch process
        /// </summary>
        public int WaitTimeOutInSeconds { get; set; }

        public ProgramInfo()
        {
            WaitForWindowToShow = false;
            WaitTimeOutInSeconds = 0;
            CloseAllInstances = false;
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            //       
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237  
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //

            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return PathToProgram.Equals((obj as ProgramInfo).PathToProgram, StringComparison.InvariantCultureIgnoreCase);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public enum OptionType
    {
        Bool,
        List,
    }

    public class OptionValue
    {
        [XmlAttribute]
        public int Value { get; set; }
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string PreviewFile { get; set; }
        [XmlAttribute]
        public string PreviewAudio { get; set; }

        public OptionValue() { }

        public OptionValue(XmlNode source)
        {
            Value = int.Parse(source.Attributes["Value"].NodeText());
            Name = source.Attributes["Name"].NodeText();
            PreviewFile = source.Attributes["PreviewFile"].NodeText();
            PreviewAudio = source.Attributes["PreviewAudio"].NodeText();
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class ConfigOption
    {
        public OptionType Type { get; set; }

        [XmlElement(ElementName = "Option")]
        public List<OptionValue> Values { get; set; }
        public int Default { get; set; }
        public string Name { get; set; }
        public string ID { get; set; }
        public string Description { get; set; }

        public ConfigOption() { }

        public ConfigOption(XmlNode source)
        {
            Type = (OptionType)Enum.Parse(typeof(OptionType), source.SelectSingleNode("Type").NodeText());
            Default = int.Parse(source.SelectSingleNode("Default").NodeText());
            Name = source.SelectSingleNode("Name").NodeText();
            ID = source.SelectSingleNode("ID").NodeText();
            Description = source.SelectSingleNode("Description").NodeText();
            Values = source.SelectNodes("Option").Cast<XmlNode>().Select(n => new OptionValue(n)).ToList();
        }

        public override string ToString()
        {
            return Name;
        }
    }

    [Serializable]
    public class ConditionalFolder
    {
        [NonSerialized]
        private ActiveWhen _activeWhen;

        public string Folder { get; set; }

        public ActiveWhen ActiveWhen { get { return _activeWhen; } set { _activeWhen = value; } }

        private Dictionary<string, Conditional> _conditions;
        //private bool _active = false;

        public bool IsActive(string path)
        {
            Conditional c;
            if (!_conditions.TryGetValue(path, out c))
                _conditions.TryGetValue(String.Empty, out c);
            bool active = c.IsActive();

            string activeStr = active ? "ACTIVE" : "INACTIVE";
            DebugLogger.WriteLine($"Folder {Folder} file {path} has become {activeStr}");
            return active;
        }

        public ConditionalFolder(XmlNode node, LoaderContext ctx)
        {
            Folder = node.Attributes["Folder"].InnerText;
            ActiveWhen = ActiveWhen.Parse(node);
            _conditions = new Dictionary<string, Conditional>(StringComparer.InvariantCultureIgnoreCase);
            foreach (XmlNode n in node.SelectNodes("./*"))
            {
                if (!n.LocalName.Equals("ActiveWhen", StringComparison.InvariantCultureIgnoreCase))
                {
                    var c = Conditional.Load(n, ctx);
                    _conditions[n.Attributes["ApplyTo"].NodeText()] = c;
                }
            }
        }

        public ConditionalFolder()
        {
        }
    }

    [Serializable]
    public abstract class Conditional
    {
        protected abstract void Init(XmlNode source, LoaderContext ctx);
        public abstract bool IsActive();

        private static Dictionary<string, Func<Conditional>> _types;

        static Conditional()
        {
            _types = new Dictionary<string, Func<Conditional>>(StringComparer.InvariantCultureIgnoreCase);
            _types["And"] = () => new CAnd();
            _types["Or"] = () => new COr();
            _types["Not"] = () => new CNot();
            _types["RuntimeVar"] = () => new CRuntimeVar();
        }

        public static Conditional Load(XmlNode source, LoaderContext ctx)
        {
            var c = _types[source.LocalName]();
            c.Init(source, ctx);
            return c;
        }
    }

    [Serializable]
    class CRuntimeVar : Conditional
    {

        [NonSerialized]
        private Func<bool> _eval;

        private string _var, _values;

        protected override void Init(XmlNode source, LoaderContext ctx)
        {
            _var = source.Attributes["Var"].InnerText; // get friendly name of variable from xml e.g. "Time"

            if (ctx.VarAliases.TryGetValue(_var, out string varAlias))
            {
                // replace friendly name of variable with alias if found e.g. "Byte:0xDC08EB"
                _var = varAlias;
            }

            _values = source.Attributes["Values"].InnerText;
            _eval = RuntimeVar.MakeRuntimeVar(_var, _values);
        }

        public override bool IsActive()
        {
            if (_eval == null)
                _eval = RuntimeVar.MakeRuntimeVar(_var, _values);
            return _eval();
        }
    }

    [Serializable]
    class CAnd : Conditional
    {

        private List<Conditional> _children;

        protected override void Init(XmlNode source, LoaderContext ctx)
        {
            _children = new List<Conditional>();
            foreach (XmlNode child in source.ChildNodes)
                _children.Add(Conditional.Load(child, ctx));
        }

        public override bool IsActive()
        {
            return _children.All(c => c.IsActive());
        }
    }

    [Serializable]
    class COr : Conditional
    {
        private List<Conditional> _children;

        protected override void Init(XmlNode source, LoaderContext ctx)
        {
            _children = new List<Conditional>();
            foreach (XmlNode child in source.ChildNodes)
                _children.Add(Conditional.Load(child, ctx));
        }

        public override bool IsActive()
        {
            return _children.Any(c => c.IsActive());
        }
    }

    [Serializable]
    class CNot : Conditional
    {
        private Conditional _child;

        protected override void Init(XmlNode source, LoaderContext ctx)
        {
            _child = Conditional.Load(source.SelectSingleNode("./*"), ctx);
        }

        public override bool IsActive()
        {
            return !_child.IsActive();
        }
    }

    [Serializable]
    public class FFNxFlag
    {
        public string Key;
        public string Value;
        public List<string> Values;
        public SerializableDictionary<string, int> Attributes;

        public FFNxFlag()
        {
            Key = String.Empty;
            Value = String.Empty;
            Values = new List<string>();
            Attributes = new SerializableDictionary<string, int>();
        }
    }

    [Serializable]
    public class Variable
    {
        [XmlAttribute("Name")]
        public string Name;

        [XmlElement("Value")]
        public string Value;
    }
}
