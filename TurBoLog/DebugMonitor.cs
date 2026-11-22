namespace TurBoLog
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows.Forms;

    /// <summary>
    /// Delegate used when firing DebugMonitor.OnOutputDebug event
    /// </summary>
    public delegate void OnOutputDebugStringHandler(int pid, string text);

    /// <summary>
    /// This class captures all strings passed to <c>OutputDebugString</c> when
    /// the application is not debugged.
    /// </summary>
    /// <remarks>
    ///	This class is a port of Microsofts Visual Studio's C++ example "dbmon", which
    ///	can be found at <c>http://msdn.microsoft.com/library/default.asp?url=/library/en-us/vcsample98/html/vcsmpdbmon.asp</c>.
    /// </remarks>
    /// <remarks>
    ///		<code>
    ///			public static void Main(string[] args) {
    ///				DebugMonitor.Start();
    ///				DebugMonitor.OnOutputDebugString += new OnOutputDebugStringHandler(OnOutputDebugString);
    ///				Console.WriteLine("Press 'Enter' to exit.");
    ///				Console.ReadLine();
    ///				DebugMonitor.Stop();
    ///			}
    ///
    ///			private static void OnOutputDebugString(int pid, string text) {
    ///				Console.WriteLine(DateTime.Now + ": " + text);
    ///			}
    ///		</code>
    /// </remarks>
    public class DebugMonitor
    {
        #region Win32 API Imports

        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_DESCRIPTOR
        {
            public byte revision;
            public byte size;
            public short control;
            public IntPtr owner;
            public IntPtr group;
            public IntPtr sacl;
            public IntPtr dacl;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }

        [Flags]
        private enum PageProtection : uint
        {
            NoAccess = 0x01,
            Readonly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            Guard = 0x100,
            NoCache = 0x200,
            WriteCombine = 0x400,
        }

        private const int WAIT_OBJECT_0 = 0;
        private const uint INFINITE = 0xFFFFFFFF;
        private const int ERROR_ALREADY_EXISTS = 183;

        private const uint SECURITY_DESCRIPTOR_REVISION = 1;

        private const uint SECTION_MAP_READ = 0x0004;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr MapViewOfFile(
            IntPtr hFileMappingObject,
            uint dwDesiredAccess,
            uint dwFileOffsetHigh,
            uint dwFileOffsetLow,
            uint dwNumberOfBytesToMap
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool InitializeSecurityDescriptor(
            ref SECURITY_DESCRIPTOR sd,
            uint dwRevision
        );

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetSecurityDescriptorDacl(
            ref SECURITY_DESCRIPTOR sd,
            bool daclPresent,
            IntPtr dacl,
            bool daclDefaulted
        );

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateEvent(
            ref SECURITY_ATTRIBUTES sa,
            bool bManualReset,
            bool bInitialState,
            string lpName
        );

        [DllImport("kernel32.dll")]
        private static extern bool PulseEvent(IntPtr hEvent);

        [DllImport("kernel32.dll")]
        private static extern bool SetEvent(IntPtr hEvent);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFileMapping(
            IntPtr hFile,
            ref SECURITY_ATTRIBUTES lpFileMappingAttributes,
            PageProtection flProtect,
            uint dwMaximumSizeHigh,
            uint dwMaximumSizeLow,
            string lpName
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hHandle);

        [DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
        private static extern Int32 WaitForSingleObject(IntPtr handle, uint milliseconds);
        #endregion

        /// <summary>
        /// Fired if an application calls <c>OutputDebugString</c>
        /// </summary>
        public static event OnOutputDebugStringHandler OnOutputDebugString;

        /// <summary>
        /// Event handle for slot 'DBWIN_BUFFER_READY'
        /// </summary>
        private static IntPtr m_AckEvent = IntPtr.Zero;

        /// <summary>
        /// Event handle for slot 'DBWIN_DATA_READY'
        /// </summary>
        private static IntPtr m_ReadyEvent = IntPtr.Zero;

        /// <summary>
        /// Handle for our shared file
        /// </summary>
        private static IntPtr m_SharedFile = IntPtr.Zero;

        /// <summary>
        /// Handle for our shared memory
        /// </summary>
        private static IntPtr m_SharedMem = IntPtr.Zero;

        /// <summary>
        /// Our capturing thread
        /// </summary>
        private static Thread m_Capturer = null;

        /// <summary>
        /// Our synchronization root
        /// </summary>
        private static object m_SyncRoot = new object();

        /// <summary>
        /// Mutex for singleton check
        /// </summary>
        private static Mutex m_Mutex = null;

        /// <summary>
        /// Starts this debug monitor
        /// </summary>
        ///
        [STAThread]
        public static void Start()
        {
            lock (m_SyncRoot)
            {
                if (m_Capturer != null)
                    throw new ApplicationException("This DebugMonitor is already started.");

                // Check for supported operating system. Mono (at least with *nix) won't support
                // our P/Invoke calls.
                if (Environment.OSVersion.ToString().IndexOf("Microsoft") == -1)
                    throw new NotSupportedException(
                        "This DebugMonitor is only supported on Microsoft operating systems."
                    );

                // Check for multiple instances. As the README.TXT of the msdn
                // example notes it is possible to have multiple debug monitors
                // listen on OutputDebugString, but the message will be randomly
                // distributed among all running instances so this won't be
                // such a good idea.
                bool createdNew = false;
                m_Mutex = new Mutex(false, typeof(DebugMonitor).Namespace, out createdNew);
                if (!createdNew)
                    throw new ApplicationException(
                        "There is already an instance of 'TurBoLog' running."
                    );

                SECURITY_DESCRIPTOR sd = new SECURITY_DESCRIPTOR();

                // Initialize the security descriptor.
                if (!InitializeSecurityDescriptor(ref sd, SECURITY_DESCRIPTOR_REVISION))
                {
                    throw CreateApplicationException(
                        "Failed to initializes the security descriptor."
                    );
                }

                // Set information in a discretionary access control list
                if (!SetSecurityDescriptorDacl(ref sd, true, IntPtr.Zero, false))
                {
                    throw CreateApplicationException(
                        "Failed to initializes the security descriptor"
                    );
                }

                SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();

                // Create the event for slot 'DBWIN_BUFFER_READY'
                m_AckEvent = CreateEvent(ref sa, false, false, "DBWIN_BUFFER_READY");
                if (m_AckEvent == IntPtr.Zero)
                {
                    throw CreateApplicationException("Failed to create event 'DBWIN_BUFFER_READY'");
                }

                // Create the event for slot 'DBWIN_DATA_READY'
                m_ReadyEvent = CreateEvent(ref sa, false, false, "DBWIN_DATA_READY");
                if (m_ReadyEvent == IntPtr.Zero)
                {
                    throw CreateApplicationException("Failed to create event 'DBWIN_DATA_READY'");
                }

                // Get a handle to the readable shared memory at slot 'DBWIN_BUFFER'.
                m_SharedFile = CreateFileMapping(
                    new IntPtr(-1),
                    ref sa,
                    PageProtection.ReadWrite,
                    0,
                    4096,
                    "DBWIN_BUFFER"
                );
                if (m_SharedFile == IntPtr.Zero)
                {
                    throw CreateApplicationException(
                        "Failed to create a file mapping to slot 'DBWIN_BUFFER'"
                    );
                }

                // Create a view for this file mapping so we can access it
                m_SharedMem = MapViewOfFile(m_SharedFile, SECTION_MAP_READ, 0, 0, 512);
                if (m_SharedMem == IntPtr.Zero)
                {
                    throw CreateApplicationException(
                        "Failed to create a mapping view for slot 'DBWIN_BUFFER'"
                    );
                }

                // Start a new thread where we can capture the output
                // of OutputDebugString calls so we don't block here.
                m_Capturer = new Thread(new ThreadStart(Capture));
                m_Capturer.Start();
            }
        }

        /// <summary>
        /// Captures
        /// </summary>
        private static void Capture()
        {
            try
            {
                // Everything after the first DWORD is our debugging text
                IntPtr pString = new IntPtr(m_SharedMem.ToInt64() + Marshal.SizeOf(typeof(int)));

                while (true)
                {
                    SetEvent(m_AckEvent);

                    int ret = WaitForSingleObject(m_ReadyEvent, INFINITE);

                    // if we have no capture set it means that someone
                    // called 'Stop()' and is now waiting for us to exit
                    // this endless loop.
                    if (m_Capturer == null)
                        break;

                    if (ret == WAIT_OBJECT_0)
                    {
                        // The first DWORD of the shared memory buffer contains
                        // the process ID of the client that sent the debug string.
                        FireOnOutputDebugString(
                            Marshal.ReadInt32(m_SharedMem),
                            Marshal.PtrToStringAnsi(pString)
                        );
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Error starting debug view");
                throw;

                // Cleanup
            }
            finally
            {
                Dispose();
            }
        }

        private static void FireOnOutputDebugString(int pid, string text)
        {
            // Raise event if we have any listeners
            if (OnOutputDebugString == null)
                return;

#if !DEBUG
            try
            {
#endif
                OnOutputDebugString(pid, text);
#if !DEBUG
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "An 'OnOutputDebugString' handler failed to execute: " + ex.ToString()
                );
            }
#endif
        }

        /// <summary>
        /// Dispose all resources
        /// </summary>
        private static void Dispose()
        {
            // Close AckEvent
            if (m_AckEvent != IntPtr.Zero)
            {
                if (!CloseHandle(m_AckEvent))
                {
                    throw CreateApplicationException("Failed to close handle for 'AckEvent'");
                }
                m_AckEvent = IntPtr.Zero;
            }

            // Close ReadyEvent
            if (m_ReadyEvent != IntPtr.Zero)
            {
                if (!CloseHandle(m_ReadyEvent))
                {
                    throw CreateApplicationException("Failed to close handle for 'ReadyEvent'");
                }
                m_ReadyEvent = IntPtr.Zero;
            }

            // Close SharedFile
            if (m_SharedFile != IntPtr.Zero)
            {
                if (!CloseHandle(m_SharedFile))
                {
                    throw CreateApplicationException("Failed to close handle for 'SharedFile'");
                }
                m_SharedFile = IntPtr.Zero;
            }

            // Unmap SharedMem
            if (m_SharedMem != IntPtr.Zero)
            {
                if (!UnmapViewOfFile(m_SharedMem))
                {
                    throw CreateApplicationException(
                        "Failed to unmap view for slot 'DBWIN_BUFFER'"
                    );
                }
                m_SharedMem = IntPtr.Zero;
            }

            // Close our mutex
            if (m_Mutex != null)
            {
                m_Mutex.Close();
                m_Mutex = null;
            }
        }

        /// <summary>
        /// Stops this debug monitor. This call we block the executing thread
        /// until this debug monitor is stopped.
        /// </summary>
        public static void Stop()
        {
            lock (m_SyncRoot)
            {
                if (m_Capturer == null)
                    throw new ObjectDisposedException(
                        "DebugMonitor",
                        "This DebugMonitor is not running."
                    );
                m_Capturer = null;
                PulseEvent(m_ReadyEvent);
                while (m_AckEvent != IntPtr.Zero)
                    ;
            }
        }

        /// <summary>
        /// Helper to create a new application exception, which has automaticly the
        /// last win 32 error code appended.
        /// </summary>
        /// <param name="text">text</param>
        private static ApplicationException CreateApplicationException(string text)
        {
            if (text == null || text.Length < 1)
                throw new ArgumentNullException("text", "'text' may not be empty or null.");

            return new ApplicationException(
                string.Format("{0}. Last Win32 Error was {1}", text, Marshal.GetLastWin32Error())
            );
        }
    }
}
