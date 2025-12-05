// WinlatorXRBridge.cs
// Place this DLL into BepInEx/plugins/WinlatorXRBridge/WinlatorXRBridge.dll
using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx;
using HarmonyLib;
using UnityEngine;

[BepInPlugin("com.yourname.winlatorxrbridge", "WinlatorXR Bridge", "0.1.0")]
public class WinlatorXRBridge : BaseUnityPlugin
{
    private Harmony _harmony;
    private bool _winlatorInitialized = false;
    private float _poseUpdateInterval = 1f / 90f; // aim for 90Hz

    void Awake()
    {
        Logger.LogInfo("WinlatorXRBridge Awake");
        _harmony = new Harmony("com.yourname.winlatorxrbridge.harmony");
        TryPatchXRInit();
        StartCoroutine(DelayedInit());
    }

    void OnDestroy()
    {
        Logger.LogInfo("WinlatorXRBridge OnDestroy");
        if (_harmony != null) _harmony.UnpatchAll(_harmony.Id);
        if (_winlatorInitialized) WinlatorNative.Winlator_Shutdown();
        _winlatorInitialized = false;
    }

    private IEnumerator DelayedInit()
    {
        yield return new WaitForSeconds(1.0f);
        Logger.LogInfo("Attempting to initialize WinlatorXR native runtime...");
        try
        {
            int rc = WinlatorNative.Winlator_Init();
            if (rc == 0)
            {
                _winlatorInitialized = true;
                Logger.LogInfo("WinlatorXR native init succeeded.");
                StartCoroutine(PoseLoop());
            }
            else
            {
                Logger.LogWarning("WinlatorXR native init returned non-zero: " + rc);
            }
        }
        catch (DllNotFoundException e)
        {
            Logger.LogError("Winlator native library not found: " + e.Message);
            Logger.LogError("Make sure the WinlatorXR native DLL (winlatorxr.dll or your name) is placed next to the game exe or in PATH.");
        }
        catch (Exception e)
        {
            Logger.LogError("Exception initializing Winlator native: " + e);
        }
    }

    private IEnumerator PoseLoop()
    {
        while (_winlatorInitialized)
        {
            WinlatorNative.Pose headPose = new WinlatorNative.Pose();
            WinlatorNative.Pose leftPose = new WinlatorNative.Pose();
            WinlatorNative.Pose rightPose = new WinlatorNative.Pose();

            bool okHead = WinlatorNative.Winlator_GetHeadPose(ref headPose);
            bool okLeft = WinlatorNative.Winlator_GetControllerPose(0, ref leftPose);
            bool okRight = WinlatorNative.Winlator_GetControllerPose(1, ref rightPose);

            if (okHead)
            {
                InjectPoseToUnity(headPose, leftPose, rightPose);
            }
            else
            {
                Logger.LogWarning("Winlator_GetHeadPose failed");
            }

            yield return new WaitForSeconds(_poseUpdateInterval);
        }
    }

    private void InjectPoseToUnity(WinlatorNative.Pose head, WinlatorNative.Pose left, WinlatorNative.Pose right)
    {
        try
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(head.positionX, head.positionY, head.positionZ);
                cam.transform.rotation = new Quaternion(head.rotX, head.rotY, head.rotZ, head.rotW);
            }

            var leftHandObj = GameObject.Find("LeftHandModel") ?? GameObject.Find("LeftHand");
            var rightHandObj = GameObject.Find("RightHandModel") ?? GameObject.Find("RightHand");

            if (leftHandObj != null)
            {
                leftHandObj.transform.position = new Vector3(left.positionX, left.positionY, left.positionZ);
                leftHandObj.transform.rotation = new Quaternion(left.rotX, left.rotY, left.rotZ, left.rotW);
            }
            if (rightHandObj != null)
            {
                rightHandObj.transform.position = new Vector3(right.positionX, right.positionY, right.positionZ);
                rightHandObj.transform.rotation = new Quaternion(right.rotX, right.rotY, right.rotZ, right.rotW);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning("InjectPoseToUnity exception: " + ex);
        }
    }

    private void TryPatchXRInit()
    {
        Logger.LogInfo("Trying to patch XR initialization entry points...");
        try
        {
            Type xrGeneral = Type.GetType("UnityEngine.XR.Management.XRGeneralSettings, UnityEngine.XR.Management");
            if (xrGeneral != null)
            {
                MethodInfo initMethod = xrGeneral.GetMethod("InitManagerOnStart", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (initMethod != null)
                {
                    var prefix = typeof(WinlatorXRBridge).GetMethod(nameof(XR_Init_Prefix), BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(initMethod, new HarmonyMethod(prefix));
                    Logger.LogInfo("Patched XRGeneralSettings.InitManagerOnStart");
                    return;
                }
            }

            Type openxrLoader = Type.GetType("Unity.XR.OpenXR.OpenXRLoader, Unity.XR.OpenXR");
            if (openxrLoader != null)
            {
                MethodInfo startMethod = openxrLoader.GetMethod("Initialize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (startMethod != null)
                {
                    var prefix = typeof(WinlatorXRBridge).GetMethod(nameof(XR_Init_Prefix), BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(startMethod, new HarmonyMethod(prefix));
                    Logger.LogInfo("Patched OpenXRLoader.Initialize");
                    return;
                }
            }

            Logger.LogInfo("No XR init method found to patch automatically. Proceeding without patch.");
        }
        catch (Exception e)
        {
            Logger.LogError("Exception while trying to patch XR init: " + e);
        }
    }

    private static bool XR_Init_Prefix()
    {
        try
        {
            BepInEx.Logging.Logger.Sources["com.yourname.winlatorxrbridge"].LogInfo("Blocked Unity XR init (prefix). WinlatorXR will be used instead.");
        }
        catch { }
        return false; // prevent original XR init
    }
}

internal static class WinlatorNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Pose
    {
        public float positionX;
        public float positionY;
        public float positionZ;
        public float rotX;
        public float rotY;
        public float rotZ;
        public float rotW;
    }

    private const string DllName = "winlatorxr"; // TODO: replace with exact DLL name (e.g., "winlatorxr.dll")

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Winlator_Init();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Winlator_Shutdown();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool Winlator_GetHeadPose(ref Pose outPose);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool Winlator_GetControllerPose(int controllerIndex, ref Pose outPose);
}