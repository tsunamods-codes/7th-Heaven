using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AppProxy
{
    static unsafe class Proxy
    {
        private static Assembly? lib = null;
        private static Type? t = null;

        private static MethodInfo? _mRun = null;
        private static MethodInfo? _mShutdown = null;
        private static MethodInfo? _mHCreateFileW = null;
        private static MethodInfo? _mHReadFile = null;
        private static MethodInfo? _mHFindFirstFileW = null;
        private static MethodInfo? _mHSetFilePointer = null;
        private static MethodInfo? _mHSetFilePointerEx = null;
        private static MethodInfo? _mHCloseHandle = null;
        private static MethodInfo? _mHGetFileType = null;
        private static MethodInfo? _mHGetFileInformationByHandle = null;
        private static MethodInfo? _mHDuplicateHandle = null;
        private static MethodInfo? _mHGetFileSize = null;
        private static MethodInfo? _mHGetFileSizeEx = null;

        [StructLayout(LayoutKind.Sequential)]
        public struct HostExports
        {
            public delegate* unmanaged<void> Shutdown;
            public delegate* unmanaged<ushort*, uint, uint, void*, uint, uint, void*, void*> CreateFileW;
            public delegate* unmanaged<void*, void*, uint, uint*, void*, int> ReadFile;
            //public delegate* unmanaged<void*, void*, uint, uint*, void*, int> WriteFile;
            public delegate* unmanaged<ushort*, void*, void*> FindFirstFileW;
            public delegate* unmanaged<void*, int, int*, uint, uint> SetFilePointer;
            public delegate* unmanaged<void*, long, void*, uint, int> SetFilePointerEx;
            public delegate* unmanaged<void*, int> CloseHandle;
            public delegate* unmanaged<void*, uint> GetFileType;
            public delegate* unmanaged<void*, void*, int> GetFileInformationByHandle;
            public delegate* unmanaged<void*, void*, void*, void**, uint, int, uint, int> DuplicateHandle;
            public delegate* unmanaged<void*, uint*, uint> GetFileSize;
            public delegate* unmanaged<void*, int*, int> GetFileSizeEx;
        }

        private static HostExports* _exports;

        [UnmanagedCallersOnly]
        public static int Main(void* exports)
        {
            try
            {
                _exports = (HostExports*)exports;

                _exports->Shutdown = &Shutdown;
                _exports->CreateFileW = &HCreateFileW;
                _exports->ReadFile = &HReadFile;
                _exports->FindFirstFileW = &HFindFirstFileW;
                _exports->SetFilePointer = &HSetFilePointer;
                _exports->SetFilePointerEx = &HSetFilePointerEx;
                _exports->CloseHandle = &HCloseHandle;
                _exports->GetFileType = &HGetFileType;
                _exports->GetFileInformationByHandle = &HGetFileInformationByHandle;
                _exports->DuplicateHandle = &HDuplicateHandle;
                _exports->GetFileSize = &HGetFileSize;
                _exports->GetFileSizeEx = &HGetFileSizeEx;

                lib = Assembly.LoadFrom(Path.Combine(Directory.GetCurrentDirectory(), "AppWrapper.dll"));
                t = lib.GetType("AppWrapper.Wrap");

                if (t != null)
                {
                    _mRun = t.GetMethod("Run", BindingFlags.Static | BindingFlags.Public);
                    _mShutdown = t.GetMethod("Shutdown", BindingFlags.Static | BindingFlags.Public);
                    _mHCreateFileW = t.GetMethod("HCreateFileW", BindingFlags.Static | BindingFlags.Public);
                    _mHReadFile = t.GetMethod("HReadFile", BindingFlags.Static | BindingFlags.Public);
                    _mHFindFirstFileW = t.GetMethod("HFindFirstFileW", BindingFlags.Static | BindingFlags.Public);
                    _mHSetFilePointer = t.GetMethod("HSetFilePointer", BindingFlags.Static | BindingFlags.Public);
                    _mHSetFilePointerEx = t.GetMethod("HSetFilePointerEx", BindingFlags.Static | BindingFlags.Public);
                    _mHCloseHandle = t.GetMethod("HCloseHandle", BindingFlags.Static | BindingFlags.Public);
                    _mHGetFileType = t.GetMethod("HGetFileType", BindingFlags.Static | BindingFlags.Public);
                    _mHGetFileInformationByHandle = t.GetMethod("HGetFileInformationByHandle", BindingFlags.Static | BindingFlags.Public);
                    _mHDuplicateHandle = t.GetMethod("HDuplicateHandle", BindingFlags.Static | BindingFlags.Public);
                    _mHGetFileSize = t.GetMethod("HGetFileSize", BindingFlags.Static | BindingFlags.Public);
                    _mHGetFileSizeEx = t.GetMethod("HGetFileSizeEx", BindingFlags.Static | BindingFlags.Public);
                }

                if (_mRun != null) _mRun.Invoke(null, new object[] { Process.GetCurrentProcess(), Type.Missing });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return 0;
        }

        [UnmanagedCallersOnly]
        public static void Shutdown()
        {
            try
            {
                if (_mShutdown != null) _mShutdown.Invoke(null, null);

                t = null;
                lib = null;

                _exports->Shutdown = null;
                _exports->CreateFileW = null;
                _exports->ReadFile = null;
                _exports->FindFirstFileW = null;
                _exports->SetFilePointer = null;
                _exports->SetFilePointerEx = null;
                _exports->CloseHandle = null;
                _exports->GetFileType = null;
                _exports->GetFileInformationByHandle = null;
                _exports->DuplicateHandle = null;
                _exports->GetFileSize = null;
                _exports->GetFileSizeEx = null;

                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.WaitForFullGCComplete();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        [UnmanagedCallersOnly]
        public static void* HCreateFileW(ushort* lpFileName, uint dwDesiredAccess, uint dwShareMode, void* lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, void* hTemplateFile)
        {
            IntPtr ret = IntPtr.Zero;

            try
            {
                if (_mHCreateFileW != null) ret = (IntPtr)(_mHCreateFileW.Invoke(null, new object[] { new string((char*)lpFileName), (System.IO.FileAccess)dwDesiredAccess, (System.IO.FileShare)dwShareMode, new IntPtr(lpSecurityAttributes), (System.IO.FileMode)dwCreationDisposition, (System.IO.FileAttributes)dwFlagsAndAttributes, new IntPtr(hTemplateFile) }) ?? IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret == IntPtr.Zero ? null : ret.ToPointer();
        }

        [UnmanagedCallersOnly]
        public static int HReadFile(void* handle, void* bytes, uint numBytesToRead, uint* numBytesRead, void* overlapped)
        {
            int ret = 0;

            try
            {
                if (_mHReadFile != null) ret = (int)(_mHReadFile.Invoke(null, new object[] { new IntPtr(handle), new IntPtr(bytes), numBytesToRead, new IntPtr(numBytesRead), new IntPtr(overlapped) }) ?? 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret;
        }

        [UnmanagedCallersOnly]
        public static void* HFindFirstFileW(ushort* lpFileName, void* lpFindFileData)
        {
            IntPtr ret = IntPtr.Zero;

            try
            {
                if (_mHFindFirstFileW != null) ret = (IntPtr)(_mHFindFirstFileW.Invoke(null, new object[] { new string((char*)lpFileName), new IntPtr(lpFindFileData) }) ?? IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret == IntPtr.Zero ? null : ret.ToPointer();
        }

        [UnmanagedCallersOnly]
        public static uint HSetFilePointer(void* hFile, int lDistanceTomove, int* lpDistanceToMoveHigh, uint dwMoveMethod)
        {
            uint ret = uint.MaxValue;

            try
            {
                if (_mHSetFilePointer != null) ret = (uint)(_mHSetFilePointer.Invoke(null, new object[] { new IntPtr(hFile), lDistanceTomove, new IntPtr(lpDistanceToMoveHigh), dwMoveMethod }) ?? 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret;
        }

        [UnmanagedCallersOnly]
        public static int HSetFilePointerEx(void* hFile, long liDistanceToMove, void* lpNewFilePointer, uint dwMoveMethod)
        {
            int ret = 0;

            try
            {
                if (_mHSetFilePointerEx != null) ret = (int)(_mHSetFilePointerEx.Invoke(null, new object[] { new IntPtr(hFile), liDistanceToMove, new IntPtr(lpNewFilePointer), dwMoveMethod }) ?? 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret;
        }

        [UnmanagedCallersOnly]
        public static int HCloseHandle(void* hObject)
        {
            int ret = 0;

            try
            {
                if (_mHCloseHandle != null) ret = (int)(_mHCloseHandle.Invoke(null, new object[] { new IntPtr(hObject) }) ?? 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret;
        }

        [UnmanagedCallersOnly]
        public static uint HGetFileType(void* hFile)
        {
            uint ret = 0;

            try
            {
                if (_mHGetFileType != null) ret = (uint)(_mHGetFileType.Invoke(null, new object[] { new IntPtr(hFile) }) ?? 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret;
        }

        [UnmanagedCallersOnly]
        public static int HGetFileInformationByHandle(void* hFile, void* lpFileInformation)
        {
            int ret = 0;

            try
            {
                if (_mHGetFileInformationByHandle != null) ret = (int)(_mHGetFileInformationByHandle.Invoke(null, new object[] { new IntPtr(hFile), new IntPtr(lpFileInformation) }) ?? 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret;
        }

        [UnmanagedCallersOnly]
        public static int HDuplicateHandle(void* hSourceProcessHandle, void* hSourceHandle, void* hTargetProcessHandle, void** lpTargetHandle, uint dwDesiredAccess, int bInheritHandle, uint dwOptions)
        {
            int ret = 0;

            try
            {
                if (_mHDuplicateHandle != null) ret = (int)(_mHDuplicateHandle.Invoke(null, new object[] { new IntPtr(hSourceProcessHandle), new IntPtr(hSourceHandle), new IntPtr(hTargetProcessHandle), new IntPtr(lpTargetHandle), dwDesiredAccess, bInheritHandle, dwOptions }) ?? 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret;
        }

        [UnmanagedCallersOnly]
        public static uint HGetFileSize(void* hFile, uint* lpFileSizeHigh)
        {
            uint ret = 0xFFFFFFFF;

            try
            {
                if (_mHGetFileSize != null) ret = (uint)(_mHGetFileSize.Invoke(null, new object[] { new IntPtr(hFile), new IntPtr(lpFileSizeHigh) }) ?? 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret;
        }

        [UnmanagedCallersOnly]
        public static int HGetFileSizeEx(void* hFile, int* lpFileSize)
        {
            int ret = 0;

            try
            {
                if (_mHGetFileSizeEx != null) ret = (int)(_mHGetFileSizeEx.Invoke(null, new object[] { new IntPtr(hFile), new IntPtr(lpFileSize) }) ?? 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret;
        }
    }
}