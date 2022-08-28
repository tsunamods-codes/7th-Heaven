using System;
using static _7thWrapperLib.Wrap;

namespace _7thWrapperLib
{
    unsafe struct DetourTransaction : IDisposable
    {
        static HostExports* s_Exports;

        internal static void Initialize(HostExports* exports)
        {
            s_Exports = exports;
        }

        public DetourTransaction()
        {
            s_Exports->DetourTransactionBegin();
        }

        public void Attach(void** target, void* detour)
        {
            s_Exports->DetourAttach(target, detour);
        }

        public void Detach(void** target, void* detour)
        {
            s_Exports->DetourDetach(target, detour);
        }

        public void Dispose()
        {
            s_Exports->DetourTransactionCommit();
        }
    }
}