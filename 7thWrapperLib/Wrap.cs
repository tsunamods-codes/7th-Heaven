/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
  Heavy rework was done by Julian Xhokaxhiu <https://julianxhokaxhiu.com>
  Additional help and support on .NET internals + low level wiring by Benjamin Moir <https://github.com/DaZombieKiller>
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Diagnostics;

namespace _7thWrapperLib {
    public static unsafe class Wrap {
        private static RuntimeProfile _profile;

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct HostExports
        {
            public delegate* unmanaged<int> DetourTransactionBegin;
            public delegate* unmanaged<void**, void*, int> DetourAttach;
            public delegate* unmanaged<void**, void*, int> DetourDetach;
            public delegate* unmanaged<int> DetourTransactionCommit;
        }
        private static unsafe HostExports* _exports;

        [StructLayout(LayoutKind.Sequential)]
        struct Methods
        {
            public delegate* unmanaged<ushort*, uint, uint, void*, uint, uint, void*, void*> CreateFileW;
        }

        static int s_MainThreadId;
        static Methods s_Trampolines;
        static Methods s_Detours;

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
                System.Threading.Thread.Sleep(5000);
                DebugLogger.WriteLine("MONITOR:");
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

        public unsafe static void Run(IntPtr exports, Process currentProcess, string profileFile = ".7thWrapperProfile")
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US", false);
            try {
                _exports = (HostExports*)exports;

                RuntimeProfile profile;
                using (var fs = new FileStream(profileFile, FileMode.Open))
                {
                    profile = Iros._7th.Util.DeserializeBinary<RuntimeProfile>(fs);
                }
                
                File.Delete(profileFile);

                if (!String.IsNullOrWhiteSpace(profile.LogFile)) {
                    try {
                        try { File.Delete(profile.LogFile); } catch { } // ensure old log is deleted since new run

                        DebugLogger.Init(profile.LogFile);
                        DebugLogger.IsDetailedLogging = profile.Options.HasFlag(RuntimeOptions.DetailedLog);

                        DebugLogger.WriteLine("Logging debug output to " + profile.LogFile);
                    } catch (Exception ex) {
                        DebugLogger.WriteLine("Failed to log debug output: " + ex.ToString());
                    }
                }

                DebugLogger.WriteLine($"Wrap run... PName: {currentProcess.ProcessName} PID: {currentProcess.Id} Path: {profile.ModPath} Capture: {String.Join(", ", profile.MonitorPaths)}");
                _profile = profile;
                for (int i = _profile.MonitorPaths.Count - 1; i >= 0; i--) {
                    if (!_profile.MonitorPaths[i].EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
                        _profile.MonitorPaths[i] += System.IO.Path.DirectorySeparatorChar;
                    if (String.IsNullOrWhiteSpace(_profile.MonitorPaths[i])) _profile.MonitorPaths.RemoveAt(i);
                }

                foreach (var item in profile.Mods) {
                    DebugLogger.WriteLine($"  Mod: {item.BaseFolder} has {item.Conditionals.Count} conditionals");
                    DebugLogger.WriteLine("     Additional paths: " + String.Join(", ", item.ExtraFolders));
                    item.Startup();
                }

                try
                {
                    DetourTransaction.Initialize(_exports);
                    InitializeHooks(Environment.CurrentManagedThreadId);
                }
                catch (Exception ex)
                {
                    DebugLogger.WriteLine(ex.ToString());
                }

                if (profile.MonitorVars != null)
                    new System.Threading.Thread(MonitorThread) { IsBackground = true }.Start(profile);

                System.Threading.Thread.Sleep(1000);
                foreach (string LL in profile.Mods.SelectMany(m => m.GetLoadLibraries())) {
                    DebugLogger.WriteLine($"Loading library DLL {LL}");
                    NativeLibrary.Load(LL);
                }
                foreach (var mod in profile.Mods) {
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

                foreach (var mod in profile.Mods.AsEnumerable().Reverse()) {
                    foreach (string file in mod.GetPathOverrideNames("hext")) {
                        foreach (var of in mod.GetOverrides("hext\\" + file)) {
                            System.IO.Stream s;
                            if (of.Archive == null) {
                                s = new System.IO.FileStream(of.File, FileMode.Open, FileAccess.Read);
                            } else {
                                s = of.Archive.GetData(of.File);
                            }
                            DebugLogger.WriteLine($"Applying hext patch {file} from mod {mod.BaseFolder}");
                            try {
                                HexPatch.Apply(s);
                            } catch (Exception ex) {
                                DebugLogger.WriteLine("Error applying patch: " + ex.Message);
                            }
                        }
                    }
                }
            } catch (Exception e) {
                DebugLogger.WriteLine(e.ToString());
                return;
            }
        }

        public static void InitializeHooks(int mainThreadId)
        {
            s_MainThreadId = mainThreadId;
            s_Detours = new()
            {
                CreateFileW = &HCreateFileW,
            };

            // Ensure console is initialized before we hook WinAPI.
            _ = Console.In;
            _ = Console.Out;
            _ = Console.Error;

            fixed (Methods* targets = &s_Trampolines)
            {
                var kernel32 = NativeLibrary.Load("kernel32");
                using var transaction = new DetourTransaction();

                // Locate addresses
                *(void**)&targets->CreateFileW = (void*)NativeLibrary.GetExport(kernel32, "CreateFileW");

                // Attach detours
                transaction.Attach((void**)&targets->CreateFileW, s_Detours.CreateFileW);
            }
        }

        // ------------------------------------------------------------------------------------------------------
        private static Dictionary<string, OverrideFile> mappedFilesCache = new Dictionary<string, OverrideFile>();

        [UnmanagedCallersOnly]
        static void* HCreateFileW(ushort* lpFileName, uint dwDesiredAccess, uint dwShareMode, void*lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, void* hTemplateFile)
        {
            void* ret = null;
            try
            {
                string _lpFileName = new string((char*)lpFileName);

                // Usually this check should be enough...
                bool isFF7GameFile = _lpFileName.StartsWith(_profile.FF7Path, StringComparison.InvariantCultureIgnoreCase);
                // ...but if it fails, last resort is to check if the file exists in the game directory
                if (!isFF7GameFile && !_lpFileName.StartsWith("\\", StringComparison.InvariantCultureIgnoreCase) && !Path.IsPathRooted(_lpFileName))
                {
                    isFF7GameFile = _profile.gameFiles.Any(s => s.EndsWith(_lpFileName, StringComparison.InvariantCultureIgnoreCase));
                }

                // If a game file is found, process with replacing its content with relative mod file
                if (isFF7GameFile)
                {
                    _lpFileName = _lpFileName.Replace("\\/", "\\").Replace("/", "\\").Replace("\\\\", "\\");
                    DebugLogger.DetailedWriteLine($"CreateFileW for {_lpFileName}...");
                    if (_lpFileName.IndexOf('\\') < 0)
                    {
                        //DebugLogger.WriteLine("No path: curdir is {0}", System.IO.Directory.GetCurrentDirectory(), 0);
                        _lpFileName = Path.Combine(Directory.GetCurrentDirectory(), _lpFileName);
                    }

                    foreach (string path in _profile.MonitorPaths)
                    {
                        if (_lpFileName.StartsWith(path, StringComparison.InvariantCultureIgnoreCase))
                        {
                            string match = _lpFileName.Substring(path.Length);
                            OverrideFile mapped = LGPWrapper.MapFile(match, _profile);

                            //DebugLogger.WriteLine($"Attempting match '{match}' for {_lpFileName}...");

                            if (mapped == null)
                            {
                                // Attempt a second round, this time relaxing the path match replacing only the game folder path.
                                match = _lpFileName.Substring(_profile.FF7Path.Length + 1);
                                mapped = LGPWrapper.MapFile(match, _profile);

                                //DebugLogger.WriteLine($"Attempting match '{match}' for {_lpFileName}...");
                            }

                            if (mapped != null)
                            {
                                DebugLogger.WriteLine($"Remapping {_lpFileName} to {mapped.File} [ Matched: '{match}' ]");

                                if (mapped.Archive == null)
                                    _lpFileName = mapped.File;
                            }
                        }
                    }
                }
                else
                    DebugLogger.DetailedWriteLine($"Skipped file {_lpFileName}");

                if (ret == null)
                    ret = s_Trampolines.CreateFileW((ushort*)Marshal.StringToHGlobalAuto(_lpFileName).ToPointer(), dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);

                //DebugLogger.WriteLine("Hooked CreateFileW for {0} under {1}", _lpFileName, handle.ToInt32());
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret;
        }
    }
}
