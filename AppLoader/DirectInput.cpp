#define WIN32_LEAN_AND_MEAN
#define DIRECTINPUT_VERSION 0x0700
#include <Windows.h>
#include <dinput.h>
#include <string>

#pragma comment(linker, "/export:DirectInputCreateA=_ForwardDirectInputCreateA@16,@1")

namespace
{
    using CreateDirectInput = HRESULT (WINAPI *)(HINSTANCE, DWORD, LPDIRECTINPUTA*, LPUNKNOWN);

    CreateDirectInput ResolveDirectInput()
    {
        std::wstring path(32768, L'\0');
        const auto length = GetModuleFileNameW(nullptr, path.data(), static_cast<DWORD>(path.size()));
        if (length == 0 || length >= path.size())
            return nullptr;

        const auto separator = path.find_last_of(L"\\/", length - 1);
        if (separator == std::wstring::npos)
            return nullptr;

        // The launcher stages the system DLL beside the game under a separate name.
        // With .Local redirection, even a full system path to dinput.dll can load AppLoader again.
        path.resize(separator + 1);
        path += L"7thHeaven.SystemDirectInput.dll";

        std::wstring mappedPath(32768, L'\0');
        const auto module = LoadLibraryExW(path.c_str(), nullptr, LOAD_LIBRARY_SEARCH_SYSTEM32);
        if (!module)
            return nullptr;

        const auto mappedLength = GetModuleFileNameW(module, mappedPath.data(), static_cast<DWORD>(mappedPath.size()));
        if (mappedLength == path.size() &&
            CompareStringOrdinal(mappedPath.data(), static_cast<int>(mappedLength), path.data(),
                static_cast<int>(path.size()), TRUE) == CSTR_EQUAL)
        {
            const auto create = reinterpret_cast<CreateDirectInput>(GetProcAddress(module, "DirectInputCreateA"));
            if (create)
                return create; // Keep the module loaded while the export can be called.
        }

        FreeLibrary(module);
        return nullptr;
    }
}

extern "C" HRESULT WINAPI ForwardDirectInputCreateA(
    HINSTANCE instance, DWORD version, LPDIRECTINPUTA* input, LPUNKNOWN outer) noexcept
{
    try
    {
        static const auto create = ResolveDirectInput();
        return create ? create(instance, version, input, outer) : DIERR_GENERIC;
    }
    catch (...)
    {
        return DIERR_GENERIC;
    }
}
