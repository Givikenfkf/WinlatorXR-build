// winlator_c_wrapper.cpp
// Thin C ABI wrapper exposing simple functions for P/Invoke from C#.

#include <windows.h>

extern "C" {

__declspec(dllexport) int winlator_init() {
    // Implement actual initialization against your WinlatorXR C++ API.
    return 0; // 0 == success
}

__declspec(dllexport) void winlator_shutdown() {
    // Implement shutdown
}

__declspec(dllexport) bool winlator_get_head_pose(float* px, float* py, float* pz, float* rx, float* ry, float* rz, float* rw) {
    if (!px || !py || !pz || !rx || !ry || !rz || !rw) return false;
    *px = 0.0f; *py = 1.6f; *pz = 0.0f; *rx = 0.0f; *ry = 0.0f; *rz = 0.0f; *rw = 1.0f;
    return true;
}

__declspec(dllexport) bool winlator_get_controller_pose(int idx, float* px, float* py, float* pz, float* rx, float* ry, float* rz, float* rw) {
    (void)idx;
    if (!px || !py || !pz || !rx || !ry || !rz || !rw) return false;
    *px = (idx == 0) ? -0.2f : 0.2f;
    *py = 1.45f; *pz = 0.3f; *rx = 0.0f; *ry = 0.0f; *rz = 0.0f; *rw = 1.0f;
    return true;
}

}