#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class TestClientMenu
{
    [MenuItem("CorruptPolice/Test Clients/Launch Test Host")]
    static void LaunchTestHost()
    {
        var launcher = EnsureLauncher();
        launcher.mode = TestClientMode.Host;
        launcher.playerName = "TestHost";
        launcher.serverAddress = "127.0.0.1";
        launcher.autoReady = true;
        launcher.autoPlay = true;
        launcher.autoStartGame = true;
        launcher.minPlayersToStart = 2;
        launcher.ApplyArgs(launcher.BuildArgsFromInspector());
        Debug.Log("[TestClient] Test host launcher configured. Enter Play Mode to start.");
    }

    [MenuItem("CorruptPolice/Test Clients/Launch Test Client")]
    static void LaunchTestClient()
    {
        var launcher = EnsureLauncher();
        launcher.mode = TestClientMode.Client;
        launcher.playerName = $"TestClient_{Random.Range(1000, 9999)}";
        launcher.serverAddress = "127.0.0.1";
        launcher.autoReady = true;
        launcher.autoPlay = true;
        launcher.autoStartGame = false;
        launcher.ApplyArgs(launcher.BuildArgsFromInspector());
        Debug.Log("[TestClient] Test client launcher configured. Enter Play Mode to start.");
    }

    [MenuItem("CorruptPolice/Test Clients/Clear Test Launcher")]
    static void ClearTestLauncher()
    {
        var existing = Object.FindObjectOfType<TestClientLauncher>();
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);
    }

    static TestClientLauncher EnsureLauncher()
    {
        var existing = Object.FindObjectOfType<TestClientLauncher>();
        if (existing != null)
            return existing;

        var launcherObject = new GameObject("TestClientLauncher");
        return launcherObject.AddComponent<TestClientLauncher>();
    }
}
#endif
