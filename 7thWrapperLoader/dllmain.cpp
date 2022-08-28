#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <stdio.h>
#include <detours/detours.h>
#include <nethost.h>
#include <hostfxr.h>
#include <coreclr_delegates.h>

#pragma comment(linker, "/export:DirectInputCreateA=C:\\Windows\\System32\\dinput.DirectInputCreateA,@1")

#define X(n) n##_fn n;
#include "hostfxr.x.h"
#include "delegates.x.h"
#undef X

#define MAIN_ASM_NAME L"7thWrapperProxy"
#define MAIN_TYP_NAME L"_7thWrapperProxy.Proxy"
#define MAIN_FUN_NAME L"Main"

// APIs to export from the host to managed code
struct host_exports
{
#define X(n) decltype(&n) n;
#include "host_exports.x.h"
#undef X
} exports =
{
    #define X(n) &n,
    #include "host_exports.x.h"
    #undef X
};

// API to initialize 7th Heaven.
HRESULT(WINAPI* HostInitialize)(const host_exports*);

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpReserved)
{
    if (fdwReason != DLL_PROCESS_ATTACH)
        return TRUE;

    static auto target = &GetCommandLineA;
    static decltype(target) detour = []()
    {
        DetourTransactionBegin();
        DetourDetach((void**)&target, detour);
        DetourTransactionCommit();

        size_t buffer_size = 0;
        get_hostfxr_path(nullptr, &buffer_size, nullptr);

        auto buffer = new char_t[buffer_size];
        get_hostfxr_path(buffer, &buffer_size, nullptr);

        auto hostfxr = LoadLibraryW(buffer);
        delete[] buffer;

#define X(n) *(void**)&n = GetProcAddress(hostfxr, #n);
#include "hostfxr.x.h"
#undef X

        hostfxr_handle context = nullptr;
        hostfxr_set_error_writer([](auto message) { OutputDebugString(message); });
        hostfxr_initialize_for_runtime_config(MAIN_ASM_NAME L".runtimeconfig.json", nullptr, &context);

#define X(n) hostfxr_get_runtime_delegate(context, hdt_##n, (void**)&n);
#include "delegates.x.h"
#undef X

        hostfxr_close(context);

        load_assembly_and_get_function_pointer(MAIN_ASM_NAME L".dll", MAIN_TYP_NAME L", " MAIN_ASM_NAME, MAIN_FUN_NAME, UNMANAGEDCALLERSONLY_METHOD, nullptr, (void**)&HostInitialize);
        HostInitialize(&exports);

        return target();
    };

    DisableThreadLibraryCalls(hinstDLL);
    DetourTransactionBegin();
    DetourAttach((void**)&target, detour);
    DetourTransactionCommit();

    return TRUE;
}
