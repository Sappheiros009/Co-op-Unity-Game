using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace SlimeCoop.Prototype.Editor
{
    // Headless: direct Editor invocation WITHOUT -quit. No client secrets or paid packages.
    public static class PrototypePackageInstaller
    {
        private static AddAndRemoveRequest _request;
        private static double _deadline;
        public static void Install()
        {
            _request = Client.AddAndRemove(new[] { "com.unity.modules.video@1.0.0" }, new string[0]);
            _deadline = EditorApplication.timeSinceStartup + 180;
            EditorApplication.update += Poll;
        }
        public static void InstallNetworking()
        {
            // Let Package Manager resolve the latest compatible release for the pinned Editor, not registry latest.
            _request = Client.AddAndRemove(new[] { "com.unity.netcode.gameobjects" }, new string[0]);
            _deadline = EditorApplication.timeSinceStartup + 600;
            EditorApplication.update += Poll;
        }
        private static void Poll()
        {
            if (!_request.IsCompleted)
            {
                if (EditorApplication.timeSinceStartup > _deadline) { Debug.LogError("[PrototypePackages] Timeout"); EditorApplication.Exit(2); }
                return;
            }
            EditorApplication.update -= Poll;
            if (_request.Status == StatusCode.Success)
            {
                foreach (var package in _request.Result)
                    if (package.name == "com.unity.netcode.gameobjects" || package.name == "com.unity.transport" || package.name == "com.unity.modules.video")
                        Debug.Log("[PrototypePackages] Installed " + package.name + "@" + package.version);
                EditorApplication.Exit(0);
            }
            else { Debug.LogError("[PrototypePackages] " + _request.Error?.message); EditorApplication.Exit(1); }
        }
    }
}
