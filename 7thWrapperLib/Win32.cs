/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace _7thWrapperLib {
    static class Win32 {
        public enum EMoveMethod : uint
        {
            Begin = 0,
            Current = 1,
            End = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        };

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct BY_HANDLE_FILE_INFORMATION
        {
            public uint FileAttributes;
            public FILETIME CreationTime;
            public FILETIME LastAccessTime;
            public FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        public struct OVERLAPPED {
            public UIntPtr Internal;
            public UIntPtr InternalHigh;
            public uint Offset;
            public uint OffsetHigh;
            public IntPtr EventHandle;
        }

        // -------------------------------------------------------------------------------------------------------------------------------------------

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal delegate void ReadFileCompletionDelegate(int dwErrorCode, int dwNumBytesTransferred, ref OVERLAPPED lpOverlapped);

        // -------------------------------------------------------------------------------------------------------------------------------------------

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static internal extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateFileW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
            [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
            [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
            IntPtr lpSecurityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
            [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DuplicateHandle(IntPtr hSourceProcessHandle,
           IntPtr hSourceHandle, IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle,
           uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwOptions);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        static internal extern unsafe int ReadFile(IntPtr handle, IntPtr bytes, uint numBytesToRead, ref uint numBytesRead, IntPtr overlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        static internal extern unsafe int ReadFile(IntPtr handle, IntPtr bytes, uint numBytesToRead, ref uint numBytesRead, ref OVERLAPPED overlapped);

        [DllImport("kernel32.dll")]
        static extern bool ReadFileEx(IntPtr hFile, [Out] byte[] lpBuffer,
           uint nNumberOfBytesToRead, [In] ref System.Threading.NativeOverlapped lpOverlapped,
           ReadFileCompletionDelegate lpCompletionRoutine);

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static internal extern unsafe int SetFilePointer(
            [In] IntPtr hFile,
            [In] int lDistanceToMove,
            [In] IntPtr lpDistanceToMoveHigh,
            [In] EMoveMethod dwMoveMethod);

    }

}
