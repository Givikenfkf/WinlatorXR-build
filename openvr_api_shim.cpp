// openvr_api_shim.cpp
// Build as a Win32/x64 DLL project in Visual Studio. Export name: openvr_api.dll

#include <windows.h>
#include <stdint.h>

extern "C" {

__declspec(dllexport) const char* VR_GetVRInitErrorAsSymbol(int e) {
    return "OK";
}

__declspec(dllexport) void* VR_Init(int* peError, int eType) {
    static int fakeHandle = 1;
    (void)peError; (void)eType;
    return &fakeHandle;
}

__declspec(dllexport) void VR_Shutdown() {
    // TODO: call winlatorxr_shutdown() if implemented
}

__declspec(dllexport) void* VR_GetGenericInterface(const char* pchInterfaceVersion, int* peError) {
    (void)pchInterfaceVersion; (void)peError;
    return nullptr;
}

}

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved) {
    switch (fdwReason) {
        case DLL_PROCESS_ATTACH:
            break;
        case DLL_PROCESS_DETACH:
            break;
    }
    return TRUE;
}