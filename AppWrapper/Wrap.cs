/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
  Heavy rework was done by Julian Xhokaxhiu <https://julianxhokaxhiu.com>
  Additional help and support on .NET internals + low level wiring by Benjamin Moir <https://github.com/DaZombieKiller>
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using Iros;

namespace AppWrapper {
    public static class Wrap {
        private static Dictionary<IntPtr, VArchiveData> _varchives = new();
        private static Dictionary<string, List<OverrideFile>> _mappedFiles = new();
        private static RuntimeProfile _profile;
        private static Process _process;

        private static void MonitorThread(object rpo)
        {
            RuntimeProfile rp = (RuntimeProfile)rpo;
            var accessors = rp.MonitorVars
                .Select(t => new { Name = t.Item1, Data = t.Item2.Split(':') })
                .Select(a => new { Type = (VarType)Enum.Parse(typeof(VarType), a.Data[0]), Addr = new IntPtr(RuntimeVar.Parse(a.Data[1])), Name = a.Name, Mask = a.Data.Length < 3 ? -1 : (int)RuntimeVar.Parse(a.Data[2]) })
                .Where(a => (int)a.Type <= 2)
                .ToList();
            int[] values = accessors.Select(_ => 247834893).ToArray();

            do
            {
                DebugLogger.WriteLine(">> MONITOR:");
                for (int i = 0; i < accessors.Count; i++)
                {
                    int value;
                    switch (accessors[i].Type)
                    {
                        case VarType.Int:
                            value = System.Runtime.InteropServices.Marshal.ReadInt32(accessors[i].Addr);
                            break;
                        case VarType.Short:
                            value = System.Runtime.InteropServices.Marshal.ReadInt16(accessors[i].Addr);
                            break;
                        case VarType.Byte:
                            value = System.Runtime.InteropServices.Marshal.ReadByte(accessors[i].Addr);
                            break;
                        default:
                            continue;
                    }
                    value = value & accessors[i].Mask;
                    if (value != values[i])
                    {
                        values[i] = value;
                        DebugLogger.WriteLine($"  {accessors[i].Name} = {value}");
                    }
                }
            } while (true);
        }

        public static void Run(Process currentProcess, string profileFile = ".7thWrapperProfile")
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US", false);

            try {
                using (var fs = new FileStream(profileFile, FileMode.Open))
                {
                    _profile = Iros.Util.DeserializeBinary<RuntimeProfile>(fs);
                    _process = currentProcess;
                }

                File.Delete(profileFile);

                if (!String.IsNullOrWhiteSpace(_profile.LogFile)) {
                    try {
                        try { File.Delete(_profile.LogFile); } catch { } // ensure old log is deleted since new run

                        DebugLogger.Init(_profile.LogFile);
                        DebugLogger.IsDetailedLogging = _profile.Options.HasFlag(RuntimeOptions.DetailedLog);

                        DebugLogger.WriteLine("Logging debug output to " + _profile.LogFile);
                    } catch (Exception ex) {
                        DebugLogger.WriteLine("Failed to log debug output: " + ex.ToString());
                    }
                }

                DebugLogger.WriteLine($"Starting wrapper now:");

                DebugLogger.WriteLine($">> PName: {_process.ProcessName}");

                bool isDebuggerAttached = false;
                if (!Win32.CheckRemoteDebuggerPresent(_process.Handle, ref isDebuggerAttached))
                {
                    DebugLogger.WriteLine(">> Debugger detection failed!");
                }
                else
                {
                    if (isDebuggerAttached)
                        DebugLogger.WriteLine(">> Attached debugger detected!");
                    else
                        DebugLogger.WriteLine(">> No attached debugger detected.");
                }

                DebugLogger.WriteLine($">> PID: {_process.Id}");
                DebugLogger.WriteLine($">> FF7Path: {_profile.FF7Path}");
                DebugLogger.WriteLine($">> ModPath: {_profile.ModPath}");
                DebugLogger.WriteLine($">> MonitorPaths:");
                foreach(string monPath in _profile.MonitorPaths)
                    DebugLogger.WriteLine($"   - {monPath}");
                for (int i = _profile.MonitorPaths.Count - 1; i >= 0; i--) {
                    if (!_profile.MonitorPaths[i].EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
                        _profile.MonitorPaths[i] += System.IO.Path.DirectorySeparatorChar;
                    if (String.IsNullOrWhiteSpace(_profile.MonitorPaths[i])) _profile.MonitorPaths.RemoveAt(i);
                }

                DebugLogger.WriteLine($"\nLoading mods:");

                foreach (var item in _profile.Mods) {
                    DebugLogger.WriteLine($">> Mod: {item.BaseFolder}");
                    DebugLogger.WriteLine($"   - Conditionals: {item.Conditionals.Count}");
                    DebugLogger.WriteLine($"   - Extra Folders:");
                    foreach (string extraFolder in item.ExtraFolders)
                        DebugLogger.WriteLine($"     - {extraFolder}");
                    item.Startup();
                }

                if (_profile.MonitorVars != null)
                    new System.Threading.Thread(MonitorThread) { IsBackground = true }.Start(_profile);

                foreach (string LL in _profile.Mods.SelectMany(m => m.GetLoadLibraries())) {
                    DebugLogger.WriteLine($"Loading library DLL {LL}");
                    NativeLibrary.Load(LL);
                }

                foreach (var mod in _profile.Mods) {
                    foreach (string LA in mod.GetLoadAssemblies()) {
                        DebugLogger.WriteLine($"Loading assembly DLL {LA}");
                        var asm = System.Reflection.Assembly.LoadFrom(LA);
                        try {
                            string path = mod.BaseFolder;
                            asm.GetType("_7thHeaven.Main")
                                .GetMethod("Init", new[] { typeof(RuntimeMod) })
                                .Invoke(null, new object[] { mod });
                        } catch { }
                    }
                }

                DebugLogger.WriteLine($"\nLoading hext patches:");

                foreach (var mod in _profile.Mods.AsEnumerable().Reverse()) {
                    foreach (string file in mod.GetPathOverrideNames("hext")) {
                        foreach (var of in mod.GetOverrides("hext\\" + file)) {
                            System.IO.Stream s;
                            if (of.Archive == null) {
                                s = new System.IO.FileStream(of.File, FileMode.Open, FileAccess.Read);
                            } else {
                                s = of.Archive.GetData(of.File);
                            }
                            DebugLogger.WriteLine($">> Applying hext patch {file} from mod {mod.BaseFolder}");
                            try {
                                HexPatch.Apply(s);
                            } catch (Exception ex) {
                                DebugLogger.WriteLine("   - Error applying patch: " + ex.Message);
                            }
                        }
                    }
                }

                foreach (var mod in _profile.Mods)
                {
                    foreach (var cFolder in mod.Conditionals)
                    {
                        var archive = mod.getArchive();
                        if (archive == null)
                            AddFolderFilesToMappedFiles(Path.Combine(mod.BaseFolder, cFolder.Folder), cFolder);
                        else
                            AddIROFilesToMappedFiles(cFolder.Folder, cFolder, archive);
                    }

                    foreach (var folder in mod.ExtraFolders)
                    {
                        var archive = mod.getArchive();
                        if (archive == null)
                            AddFolderFilesToMappedFiles(Path.Combine(mod.BaseFolder, folder), null);
                        else
                            AddIROFilesToMappedFiles(folder, null, archive);
                    }

                    if (mod.Conditionals.Count + mod.ExtraFolders.Count == 0)
                    {
                        var archive = mod.getArchive();
                        if (archive == null)
                            AddFolderFilesToMappedFiles(mod.BaseFolder, null);
                        else
                            AddIROFilesToMappedFiles("", null, archive);
                    }
                }

                DebugLogger.WriteLine($"\nWrapper startup complete.");
                DebugLogger.WriteLine($"\nListening for game actions...");
            } catch (Exception e) {
                DebugLogger.WriteLine(e.ToString());
                return;
            }
        }

        public static void Shutdown()
        {
            foreach(var mf in _mappedFiles)
            {
                foreach(var of in mf.Value)
                {
                    of.Archive.Dispose();
                }
            }

            _varchives.Clear();
            _mappedFiles.Clear();

            _varchives = null;
            _mappedFiles = null;
            _profile = null;
            _process = null;

            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.WaitForFullGCComplete();
        }

        private static void AddIROFilesToMappedFiles(string folderPath, ConditionalFolder cFolder, IrosArc archive)
        {
            foreach (string filename in archive.AllFileNames())
            {
                if (filename.StartsWith(folderPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    int pathOffset = 1;
                    if (folderPath.Length == 0) // Fix the offset when folderPath is empty string (no need to skip "/")
                        pathOffset = 0;
                    string fileKey = filename.Substring(folderPath.Length + pathOffset).ToLower();
                    if (!_mappedFiles.ContainsKey(fileKey))
                        _mappedFiles.Add(fileKey, new List<OverrideFile>());

                    if (_mappedFiles.TryGetValue(fileKey, out List<OverrideFile> overrideFiles))
                    {
                        overrideFiles.Add(new OverrideFile()
                        {
                            File = filename,
                            CFolder = cFolder,
                            CName = fileKey,
                            Size = archive.GetFileSize(filename),
                            Archive = archive
                        });
                    }
                }
            }
        }

        private static void AddFolderFilesToMappedFiles(string folderPath, ConditionalFolder conditionalFolder)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(folderPath);

                foreach (FileInfo fi in di.GetFiles("*", SearchOption.AllDirectories))
                {
                    string fileKey = fi.FullName.Substring(folderPath.Length + 1).ToLower();
                    if (!_mappedFiles.ContainsKey(fileKey))
                        _mappedFiles.Add(fileKey, new List<OverrideFile>());

                    if (_mappedFiles.TryGetValue(fileKey, out List<OverrideFile> overrideFiles))
                    {
                        overrideFiles.Add(new OverrideFile()
                        {
                            File = fi.FullName,
                            CFolder = conditionalFolder,
                            CName = fileKey
                        });
                    }
                }
            }
            catch (DirectoryNotFoundException e)
            {
                DebugLogger.WriteLine(e.ToString());
            }
        }


        // ------------------------------------------------------------------------------------------------------
        public static int HCloseHandle(IntPtr hObject)
        {
            int ret = 0;

            if (_varchives.ContainsKey(hObject))
            {
                _varchives.Remove(hObject);
                DebugLogger.WriteLine($">> Closing dummy handle {hObject}");
            }

            return ret;
        }

        public static uint HGetFileType(IntPtr hFile)
        {            
            uint ret = 0;

            DebugLogger.DetailedWriteLine($">> GetFileType on {hFile}");
            if (_varchives.ContainsKey(hFile))
            {
                //DebugLogger.WriteLine(" ---faking dummy file");
                ret = 1;
            }

            return ret;
        }

        public static uint HSetFilePointer(IntPtr hFile, int lDistanceTomove, IntPtr lpDistanceToMoveHigh, uint dwMoveMethod)
        {
            //DebugLogger.WriteLine("SetFilePointer on {0} to {1} by {2}", handle, lDistanceTomove, dwMoveMethod);
            uint ret = uint.MaxValue;

            long offset = lDistanceTomove;

            if (lpDistanceToMoveHigh != IntPtr.Zero)
                offset |= ((long)Marshal.ReadInt32(lpDistanceToMoveHigh) << 32);

            if (_varchives.ContainsKey(hFile))
            {
                ret = _varchives[hFile].SetFilePointer(offset, (Win32.EMoveMethod)dwMoveMethod);
            }
            
            return ret;
        }

        public static int HReadFile(IntPtr handle, IntPtr bytes, uint numBytesToRead, IntPtr numBytesRead, IntPtr overlapped)
        {
            int ret = 0;

            uint _numBytesRead = 0;
            if (_varchives.ContainsKey(handle))
            {
                ret = _varchives[handle].ReadFile(bytes, numBytesToRead, ref _numBytesRead);
                byte[] tmp = BitConverter.GetBytes(_numBytesRead);
                Util.CopyToIntPtr(tmp, numBytesRead, tmp.Length);
                return ret;
            }

            return ret;
        }

        public static IntPtr CreateVA(OverrideFile of) {
            VArchiveData va = new VArchiveData(of.Archive.GetBytes(of.File));
            IntPtr dummy = of.Archive.GetDummyHandle(_process);
            DebugLogger.WriteLine($">> Creating dummy file handle {dummy} to access {of.Archive}{of.File}");
            _varchives[dummy] = va;

            return dummy;
        }

        public static IntPtr HCreateFileW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
            [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
            [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
            IntPtr lpSecurityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
            [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
            IntPtr hTemplateFile)
        {
            IntPtr ret = IntPtr.Zero;

            // Usually this check should be enough...
            bool isFF7GameFile = lpFileName.StartsWith(_profile.FF7Path, StringComparison.InvariantCultureIgnoreCase);
            // ...but if it fails, last resort is to check if the file exists in the game directory
            if (!isFF7GameFile && !lpFileName.StartsWith("\\", StringComparison.InvariantCultureIgnoreCase) && !Path.IsPathRooted(lpFileName))
            {
                isFF7GameFile = _profile.gameFiles.Any(s => s.EndsWith(lpFileName, StringComparison.InvariantCultureIgnoreCase));
            }

            // If a game file is found, process with replacing its content with relative mod file
            if (isFF7GameFile)
            {
                lpFileName = lpFileName.Replace("\\/", "\\").Replace("/", "\\").Replace("\\\\", "\\");
                DebugLogger.DetailedWriteLine($">> CreateFileW for {lpFileName}...");
                if (lpFileName.IndexOf('\\') < 0)
                {
                    //DebugLogger.WriteLine("No path: curdir is {0}", System.IO.Directory.GetCurrentDirectory(), 0);
                    lpFileName = Path.Combine(Directory.GetCurrentDirectory(), lpFileName);
                }

                foreach (string path in _profile.MonitorPaths)
                {
                    if (lpFileName.StartsWith(path, StringComparison.InvariantCultureIgnoreCase))
                    {
                        string match = lpFileName.Substring(path.Length);
                        OverrideFile mapped = LGPWrapper.MapFile(match, _mappedFiles);

                        //DebugLogger.WriteLine($"Attempting match '{match}' for {lpFileName}...");

                        if (mapped == null)
                        {
                            // Attempt a second round, this time relaxing the path match replacing only the game folder path.
                            match = lpFileName.Substring(_profile.FF7Path.Length + 1);
                            mapped = LGPWrapper.MapFile(match, _mappedFiles);

                            //DebugLogger.WriteLine($"Attempting match '{match}' for {lpFileName}...");
                        }

                        if (mapped != null)
                        {
                            DebugLogger.WriteLine($"   - Remapping {lpFileName} to {mapped.File} [ Matched: '{match}' ]");

                            if (mapped.Archive == null)
                            {
                                lpFileName = mapped.File;
                            }
                            else
                            {
                                ret = CreateVA(mapped);
                                break;
                            }
                        }
                    }
                }
            }
            else
                DebugLogger.DetailedWriteLine($">> Skipped file {lpFileName}");

            if (ret == IntPtr.Zero)
                ret = Win32.CreateFileW(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);

            //DebugLogger.WriteLine("Hooked CreateFileW for {0} under {1}", lpFileName, handle.ToInt32());

            return ret;
        }

        public static IntPtr HFindFirstFileW(string lpFileName, IntPtr lpFindFileData)
        {
            DebugLogger.WriteLine(">> FindFirstFile for " + lpFileName);

            return IntPtr.Zero;
        }

        public static int HGetFileInformationByHandle(IntPtr hFile, IntPtr lpFileInformation)
        {
            Win32.BY_HANDLE_FILE_INFORMATION _lpFileInformation;

            bool result = Win32.GetFileInformationByHandle(hFile, out _lpFileInformation);

            byte[] tmp = Util.StructToBytes(_lpFileInformation);
            Util.CopyToIntPtr(tmp, lpFileInformation, tmp.Length);

            if (result && _varchives.ContainsKey(hFile))
            {
                DebugLogger.DetailedWriteLine($">> Overriding GetFileInformationByHandle for dummy file {hFile}");
                _lpFileInformation.FileSizeHigh = (uint)(_varchives[hFile].Size >> 32);
                _lpFileInformation.FileSizeLow = (uint)(_varchives[hFile].Size & 0xffffffff);

                // Update again the struct
                tmp = Util.StructToBytes(_lpFileInformation);
                Util.CopyToIntPtr(tmp, lpFileInformation, tmp.Length);
            }

            return result ? 1 : 0;
        }

        public static int HDuplicateHandle(IntPtr hSourceProcessHandle, IntPtr hSourceHandle, IntPtr hTargetProcessHandle, IntPtr lpTargetHandle, uint dwDesiredAccess, int bInheritHandle, uint dwOptions)
        {
            // DebugLogger.DetailedWriteLine("DuplicateHandle on {0}", hSourceHandle);

            if (_varchives.ContainsKey(hSourceHandle))
            {
                _varchives[lpTargetHandle] = _varchives[hSourceHandle];
                DebugLogger.DetailedWriteLine($">> Duplicating dummy handle {hSourceHandle} to {lpTargetHandle}");
            }

            return 1;
        }

        public static uint HGetFileSize(IntPtr hFile, IntPtr lpFileSizeHigh)
        {
            uint ret = uint.MaxValue;

            if (_varchives.ContainsKey(hFile))
            {
                DebugLogger.WriteLine($">> GetFileSize on dummy handle {hFile}");
                ret = _varchives[hFile].GetFileSize(lpFileSizeHigh);
            }

            return ret;
        }

        public static int HGetFileSizeEx(IntPtr hFile, IntPtr lpFileSize)
        {
            int ret = 0;

            if (_varchives.ContainsKey(hFile))
            {
                DebugLogger.WriteLine($">> GetFileSizeEx on dummy handle {hFile}");
                byte[] tmp = BitConverter.GetBytes(_varchives[hFile].Size);
                Util.CopyToIntPtr(tmp, lpFileSize, tmp.Length);

                ret = 1;
            }

            return ret;
        }

        public static int HSetFilePointerEx(IntPtr hFile, long liDistanceToMove, IntPtr lpNewFilePointer, uint dwMoveMethod)
        {
            int ret = 0;

            if (_varchives.ContainsKey(hFile))
                ret = _varchives[hFile].SetFilePointerEx(hFile, liDistanceToMove, lpNewFilePointer, (uint)dwMoveMethod);

            return ret;
        }
    }
}
