// AppLauncher.cpp : Defines the entry point for the application.
//

#include <windows.h>
#include "Resource.h"

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{
    // Initialize the process start information
    STARTUPINFO si;
    PROCESS_INFORMATION pi;
    ZeroMemory(&si, sizeof(si));
    si.cb = sizeof(si);
    ZeroMemory(&pi, sizeof(pi));

    // Start the process
    if (!CreateProcess(L"ff7_en.exe", NULL, NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi)) return 1;

    // Wait for the process to finish
    WaitForSingleObject(pi.hProcess, INFINITE);

    // Close process and thread handles
    CloseHandle(pi.hProcess);
    CloseHandle(pi.hThread);

	return 0;
}
