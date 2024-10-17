// DEFINE ----------------------------------------
#define WIN32_LEAN_AND_MEAN

#define MAIN_ASM_NAME L"AppProxy"
#define MAIN_TYP_NAME L"AppProxy.Proxy"
#define MAIN_FUN_NAME L"Main"

// PRAGMA ----------------------------------------

#pragma comment(linker, "/export:DirectInputCreateA=C:\\Windows\\System32\\dinput.DirectInputCreateA,@1")

// INCLUDE ---------------------------------------

#include <iostream>
#include <fstream>
#include <nlohmann/json.hpp>
using json = nlohmann::json;

#include <Windows.h>
#include <stdio.h>
#include <detours/detours.h>
#include <nethost.h>
#include <hostfxr.h>
#include <coreclr_delegates.h>
#include <TlHelp32.h>
#include <StackWalker.h>
#include <plog/Log.h>
#include <plog/Initializers/RollingFileInitializer.h>
#include "plog.formatter.h"

#define X(n) n##_fn n;
#include "hostfxr.x.h"
#include "delegates.x.h"
#undef X

// UTILS

// trim from start (in place)
static inline void ltrim(std::string& s) {
    s.erase(s.begin(), std::find_if(s.begin(), s.end(), [](unsigned char ch) {
        return !std::isspace(ch);
        }));
}

// trim from end (in place)
static inline void rtrim(std::string& s) {
    s.erase(std::find_if(s.rbegin(), s.rend(), [](unsigned char ch) {
        return !std::isspace(ch);
        }).base(), s.end());
}

// trim from both ends (in place)
static inline void trim(std::string& s) {
    rtrim(s);
    ltrim(s);
}

// EXPORTS ---------------------------------------

struct host_exports
{
    void (*Shutdown)();
#define X(n) decltype(&n) n;
#include "host_exports.x.h"
#undef X
} exports;

// IMPORTS ---------------------------------------

// API to initialize AppProxy
static HRESULT(WINAPI* HostInitialize)(host_exports*) = nullptr;

// CreateFileW
static HANDLE(WINAPI* TrueCreateFileW)(LPCWSTR lpFileName, DWORD dwDesiredAccess, DWORD dwShareMode, LPSECURITY_ATTRIBUTES lpSecurityAttributes, DWORD dwCreationDisposition, DWORD dwFlagsAndAttributes, HANDLE hTemplateFile) = CreateFileW;

// ReadFile
static BOOL(WINAPI* TrueReadFile)(HANDLE hFile, LPVOID lpBuffer, DWORD nNumberOfBytesToRead, LPDWORD lpNumberOfBytesRead, LPOVERLAPPED lpOverlapped) = ReadFile;

// FindFirstFileW
static HANDLE(WINAPI* TrueFindFirstFileW)(LPCWSTR lpFileName, LPWIN32_FIND_DATAW lpFindFileData) = FindFirstFileW;

// SetFilePointer
static DWORD(WINAPI* TrueSetFilePointer)(HANDLE hFile, LONG lDistanceToMove, PLONG lpDistanceToMoveHigh, DWORD dwMoveMethod) = SetFilePointer;

// SetFilePointerEx
static BOOL(WINAPI* TrueSetFilePointerEx)(HANDLE hFile, LARGE_INTEGER liDistanceToMove, PLARGE_INTEGER lpNewFilePointer, DWORD dwMoveMethod) = SetFilePointerEx;

// CloseHandle
static BOOL(WINAPI* TrueCloseHandle)(HANDLE hObject) = CloseHandle;

// GetFileType
static DWORD(WINAPI* TrueGetFileType)(HANDLE hFile) = GetFileType;

// GetFileInformationByHandle
static BOOL(WINAPI* TrueGetFileInformationByHandle)(HANDLE hFile, LPBY_HANDLE_FILE_INFORMATION lpFileInformation) = GetFileInformationByHandle;

// DuplicateHandle
static BOOL(WINAPI* TrueDuplicateHandle)(HANDLE hSourceProcessHandle, HANDLE hSourceHandle, HANDLE hTargetProcessHandle, LPHANDLE lpTargetHandle, DWORD dwDesiredAccess, BOOL bInheritHandle, DWORD dwOptions) = DuplicateHandle;

// GetFileSize
static DWORD(WINAPI* TrueGetFileSize)(HANDLE hFile, LPDWORD lpFileSizeHigh) = GetFileSize;

// GetFileSizeEx
static BOOL(WINAPI* TrueGetFileSizeEx)(HANDLE hFile, PLARGE_INTEGER lpFileSize) = GetFileSizeEx;

// PostQuitMessage
static VOID(WINAPI* TruePostQuitMessage)(int nExitCode) = PostQuitMessage;

// VARS ------------------------------------------

DWORD currentMainThreadId = 0;
HANDLE currentMainThread = nullptr;
BOOL inDotNetCode = false;

// FUNCTIONS -------------------------------------

HANDLE WINAPI _CreateFileW(LPCWSTR lpFileName, DWORD dwDesiredAccess, DWORD dwShareMode, LPSECURITY_ATTRIBUTES lpSecurityAttributes, DWORD dwCreationDisposition, DWORD dwFlagsAndAttributes, HANDLE hTemplateFile)
{
    HANDLE ret = nullptr;

    if (exports.CreateFileW)
    {
        if (!inDotNetCode)
        {
            inDotNetCode = true;
            ret = exports.CreateFileW(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
            inDotNetCode = false;
        }
    }

    if (ret == nullptr)
        ret = TrueCreateFileW(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);

    return ret;
}

BOOL WINAPI _ReadFile(HANDLE hFile, LPVOID lpBuffer, DWORD nNumberOfBytesToRead, LPDWORD lpNumberOfBytesRead, LPOVERLAPPED lpOverlapped)
{
    BOOL ret = FALSE;

    if (exports.ReadFile)
    {
        ret = exports.ReadFile(hFile, lpBuffer, nNumberOfBytesToRead, lpNumberOfBytesRead, lpOverlapped);
    }

    if (ret == FALSE)
        ret = TrueReadFile(hFile, lpBuffer, nNumberOfBytesToRead, lpNumberOfBytesRead, lpOverlapped);

    return ret;
}

HANDLE WINAPI _FindFirstFileW(LPCWSTR lpFileName, LPWIN32_FIND_DATAW lpFindFileData)
{
    if (exports.FindFirstFileW)
    {
        exports.FindFirstFileW(lpFileName, lpFindFileData);
    }

    return TrueFindFirstFileW(lpFileName, lpFindFileData);
}

DWORD WINAPI _SetFilePointer(HANDLE hFile, LONG lDistanceToMove, PLONG lpDistanceToMoveHigh, DWORD dwMoveMethod)
{
    DWORD ret = INVALID_SET_FILE_POINTER;

    if (exports.SetFilePointer)
    {
        ret = exports.SetFilePointer(hFile, lDistanceToMove, lpDistanceToMoveHigh, dwMoveMethod);
    }

    if (ret == INVALID_SET_FILE_POINTER)
        ret = TrueSetFilePointer(hFile, lDistanceToMove, lpDistanceToMoveHigh, dwMoveMethod);

    return ret;
}

BOOL WINAPI _SetFilePointerEx(HANDLE hFile, LARGE_INTEGER liDistanceToMove, PLARGE_INTEGER lpNewFilePointer, DWORD dwMoveMethod)
{
    BOOL ret = FALSE;

    if (exports.SetFilePointerEx)
    {
        ret = exports.SetFilePointerEx(hFile, liDistanceToMove, lpNewFilePointer, dwMoveMethod);
    }

    if (ret == FALSE)
        ret = TrueSetFilePointerEx(hFile, liDistanceToMove, lpNewFilePointer, dwMoveMethod);

    return ret;
}

BOOL WINAPI _CloseHandle(HANDLE hObject)
{
    if (exports.CloseHandle)
    {
        if (GetCurrentThreadId() == currentMainThreadId)
        {
            exports.CloseHandle(hObject);
        }
    }

    return TrueCloseHandle(hObject);
}

DWORD WINAPI _GetFileType(HANDLE hFile)
{
    DWORD ret = FILE_TYPE_UNKNOWN;

    if (exports.GetFileType)
    {
        ret = exports.GetFileType(hFile);
    }

    if (ret == FILE_TYPE_UNKNOWN)
        ret = TrueGetFileType(hFile);

    return ret;
}

BOOL WINAPI _GetFileInformationByHandle(HANDLE hFile, LPBY_HANDLE_FILE_INFORMATION lpFileInformation)
{
    BOOL ret = FALSE;

    if (exports.GetFileInformationByHandle)
    {
        if (!inDotNetCode)
        {
            inDotNetCode = true;
            ret = exports.GetFileInformationByHandle(hFile, lpFileInformation);
            inDotNetCode = false;
        }
    }

    if (ret == FALSE)
        ret = TrueGetFileInformationByHandle(hFile, lpFileInformation);

    return ret;
}

BOOL WINAPI _DuplicateHandle(HANDLE hSourceProcessHandle, HANDLE hSourceHandle, HANDLE hTargetProcessHandle, LPHANDLE lpTargetHandle, DWORD dwDesiredAccess, BOOL bInheritHandle, DWORD dwOptions)
{
    BOOL ret = TrueDuplicateHandle(hSourceProcessHandle, hSourceHandle, hTargetProcessHandle, lpTargetHandle, dwDesiredAccess, bInheritHandle, dwOptions);

    if (exports.DuplicateHandle)
    {
        if (GetCurrentThreadId() == currentMainThreadId)
        {
            exports.DuplicateHandle(hSourceProcessHandle, hSourceHandle, hTargetProcessHandle, lpTargetHandle, dwDesiredAccess, bInheritHandle, dwOptions);
        }
    }

    return ret;
}

DWORD WINAPI _GetFileSize(HANDLE hFile, LPDWORD lpFileSizeHigh)
{
    DWORD ret = INVALID_FILE_SIZE;

    if (exports.GetFileSize)
    {
        ret = exports.GetFileSize(hFile, lpFileSizeHigh);
    }

    if (ret == INVALID_FILE_SIZE)
        ret = TrueGetFileSize(hFile, lpFileSizeHigh);

    return ret;
}

BOOL WINAPI _GetFileSizeEx(HANDLE hFile, PLARGE_INTEGER lpFileSize)
{
    BOOL ret = FALSE;

    if (exports.GetFileSizeEx)
    {
        ret = exports.GetFileSizeEx(hFile, lpFileSize);
    }

    if (ret == FALSE)
        ret = TrueGetFileSizeEx(hFile, lpFileSize);

    return ret;
}

VOID WINAPI _PostQuitMessage(int nExitCode)
{
    if (GetCurrentThreadId() == currentMainThreadId)
    {
        // Unhook Win32 APIs
        DetourTransactionBegin();
        DetourUpdateThread(GetCurrentThread());
        // ------------------------------------
        DetourDetach((PVOID*)&TrueCreateFileW, _CreateFileW);
        DetourDetach((PVOID*)&TrueReadFile, _ReadFile);
        DetourDetach((PVOID*)&TrueFindFirstFileW, _FindFirstFileW);
        DetourDetach((PVOID*)&TrueSetFilePointer, _SetFilePointer);
        DetourDetach((PVOID*)&TrueSetFilePointerEx, _SetFilePointerEx);
        DetourDetach((PVOID*)&TrueCloseHandle, _CloseHandle);
        DetourDetach((PVOID*)&TrueGetFileType, _GetFileType);
        DetourDetach((PVOID*)&TrueGetFileInformationByHandle, _GetFileInformationByHandle);
        DetourDetach((PVOID*)&TrueDuplicateHandle, _DuplicateHandle);
        DetourDetach((PVOID*)&TrueGetFileSize, _GetFileSize);
        DetourDetach((PVOID*)&TrueGetFileSizeEx, _GetFileSizeEx);
        DetourDetach((PVOID*)&TruePostQuitMessage, _PostQuitMessage);
        // ------------------------------------
        DetourTransactionCommit();

        // Ask the .NET code to gracefully shutdown
        if (exports.Shutdown) exports.Shutdown();
    }

    // Continue with the usual execution
    TruePostQuitMessage(nExitCode);
}

// MAIN ------------------------------------------

class _7thStackWalker : public StackWalker
{
public:
    _7thStackWalker(bool muted = false) : StackWalker(), _baseAddress(0), _size(0), _muted(muted) {}
    DWORD64 getBaseAddress() const {
        return _baseAddress;
    }
    DWORD getSize() const {
        return _size;
    }
protected:
    virtual void OnLoadModule(LPCSTR img, LPCSTR mod, DWORD64 baseAddr,
        DWORD size, DWORD result, LPCSTR symType, LPCSTR pdbName,
        ULONGLONG fileVersion
    )
    {
        if (_baseAddress == 0 && _size == 0)
        {
            _baseAddress = baseAddr;
            _size = size;
        }
        StackWalker::OnLoadModule(
            img, mod, baseAddr, size, result, symType, pdbName, fileVersion
        );
    }

    virtual void OnDbgHelpErr(LPCSTR szFuncName, DWORD gle, DWORD64 addr)
    {
        // Silence is golden.
    }

    virtual void OnOutput(LPCSTR szText)
    {
        if (!_muted)
        {
            std::string tmp(szText);
            trim(tmp);
            PLOGV << tmp;
        }
    }
private:
    DWORD64 _baseAddress;
    DWORD _size;
    bool _muted;
};

LONG WINAPI ExceptionHandler(EXCEPTION_POINTERS* ep)
{
    PLOGV << "*** Exception 0x" << std::hex << ep->ExceptionRecord->ExceptionCode << ", address 0x" << std::hex << ep->ExceptionRecord->ExceptionAddress << " ***";
    
    _7thStackWalker sw;
    sw.ShowCallstack(
        GetCurrentThread(),
        ep->ContextRecord
    );

    PLOGE << "Unhandled Exception. See dumped information above.";

    // This exception is mostly called for this reason, hint the user
    std::ifstream f(MAIN_ASM_NAME L".runtimeconfig.json");
    json data = json::parse(f);

    std::string version = data["runtimeOptions"]["framework"]["version"];
    std::string msg = "Could not start the .NET Desktop Runtime version " + version + ".\n\nPlease make sure you have both the x86 and x64 editions installed. Try using the 7th Heaven exe installer, or visit https://dotnet.microsoft.com for more information.";
    MessageBoxA(NULL, msg.c_str(), "Error", MB_OK | MB_ICONERROR);

    // let OS handle the crash
    SetUnhandledExceptionFilter(0);
    return EXCEPTION_CONTINUE_EXECUTION;
}

#ifndef MAKEULONGLONG
#define MAKEULONGLONG(ldw, hdw) ((ULONGLONG(hdw) << 32) | ((ldw) & 0xFFFFFFFF))
#endif

DWORD GetCurrentProcessMainThreadId()
{
    DWORD dwMainThreadID = 0;
    ULONGLONG ullMinCreateTime = MAXULONGLONG;

    HANDLE hThreadSnap = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, 0);
    if (hThreadSnap != INVALID_HANDLE_VALUE) {
        THREADENTRY32 th32;
        th32.dwSize = sizeof(THREADENTRY32);
        BOOL bOK = TRUE;
        for (bOK = Thread32First(hThreadSnap, &th32); bOK; bOK = Thread32Next(hThreadSnap, &th32))
        {
            if (th32.th32OwnerProcessID == GetCurrentProcessId())
            {
                HANDLE hThread = OpenThread(THREAD_QUERY_INFORMATION, TRUE, th32.th32ThreadID);
                if (hThread)
                {
                    FILETIME afTimes[4] = { 0 };
                    if (GetThreadTimes(hThread, &afTimes[0], &afTimes[1], &afTimes[2], &afTimes[3]))
                    {
                        ULONGLONG ullTest = MAKEULONGLONG(afTimes[0].dwLowDateTime, afTimes[0].dwHighDateTime);
                        if (ullTest && ullTest < ullMinCreateTime)
                        {
                            ullMinCreateTime = ullTest;
                            dwMainThreadID = th32.th32ThreadID; // let it be main... :)
                        }
                    }
                    CloseHandle(hThread);
                }
            }
        }
#ifndef UNDER_CE
        CloseHandle(hThreadSnap);
#else
        CloseToolhelp32Snapshot(hThreadSnap);
#endif
    }

    return dwMainThreadID;
}

DWORD WINAPI StartProxy(LPVOID lpParam) {
    HINSTANCE hinstDLL = (HINSTANCE)lpParam;

    // Begin the detouring
    static auto target = &GetCommandLineA;
    static decltype(target) detour = []()
        {
            DetourTransactionBegin();
            DetourUpdateThread(GetCurrentThread());
            // ------------------------------------
            DetourDetach((void**)&target, detour);
            // ------------------------------------
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

            // Get main entry point and load the assembly
            load_assembly_and_get_function_pointer(MAIN_ASM_NAME L".dll", MAIN_TYP_NAME L", " MAIN_ASM_NAME, MAIN_FUN_NAME, UNMANAGEDCALLERSONLY_METHOD, nullptr, (void**)&HostInitialize);

            // Start the AppProxy process
            HostInitialize(&exports);

            // Hook Win32 APIs
            DetourTransactionBegin();
            DetourUpdateThread(GetCurrentThread());
            // ------------------------------------
            DetourAttach((PVOID*)&TrueCreateFileW, _CreateFileW);
            DetourAttach((PVOID*)&TrueReadFile, _ReadFile);
            DetourAttach((PVOID*)&TrueFindFirstFileW, _FindFirstFileW);
            DetourAttach((PVOID*)&TrueSetFilePointer, _SetFilePointer);
            DetourAttach((PVOID*)&TrueSetFilePointerEx, _SetFilePointerEx);
            DetourAttach((PVOID*)&TrueCloseHandle, _CloseHandle);
            DetourAttach((PVOID*)&TrueGetFileType, _GetFileType);
            DetourAttach((PVOID*)&TrueGetFileInformationByHandle, _GetFileInformationByHandle);
            DetourAttach((PVOID*)&TrueDuplicateHandle, _DuplicateHandle);
            DetourAttach((PVOID*)&TrueGetFileSize, _GetFileSize);
            DetourAttach((PVOID*)&TrueGetFileSizeEx, _GetFileSizeEx);
            DetourAttach((PVOID*)&TruePostQuitMessage, _PostQuitMessage);
            // ------------------------------------
            DetourTransactionCommit();

            PLOGI << "AppLoader started successfully";

            return target();
        };

    DisableThreadLibraryCalls(hinstDLL);
    DetourTransactionBegin();
    DetourUpdateThread(GetCurrentThread());
    // ------------------------------------
    DetourAttach((void**)&target, detour);
    // ------------------------------------
    DetourTransactionCommit();

    return 0;
}

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpReserved)
{
    // Move on if the current process is an helper process or the reason is not attach
    if (fdwReason != DLL_PROCESS_ATTACH) return TRUE;
    if (DetourIsHelperProcess()) return TRUE;

    // Setup logging layer
    remove("AppLoader.log");
    plog::init<plog::_7thFormatter>(plog::verbose, "AppLoader.log");
    PLOGI << "AppLoader init log";

    // Log unhandled exceptions
    SetUnhandledExceptionFilter(ExceptionHandler);

    // Save current main thread if for FF7.exe
    currentMainThreadId = GetCurrentProcessMainThreadId();

    // Get current process name
    CHAR parentName[1024];
    GetModuleFileNameA(NULL, parentName, sizeof(parentName));
    _strlwr(parentName);

    // Use the create thread approach for the Steam exe, otherwise run directly the code for 1998 edition
    if (strstr(parentName, "ff7_en.exe") != NULL)
    {
        PLOGI << "Detected Steam edition. Using CreateThread approach...";

        CreateThread(NULL, 0, StartProxy, hinstDLL, 0, NULL);
    }
    else
    {
        PLOGI << "Detected 1998 edition. Using direct call approach...";

        StartProxy((LPVOID)hinstDLL);
    }

    return TRUE;
}
