using System;
using UnityEngine;

public enum TestClientMode
{
    None,
    Host,
    Client
}

[Serializable]
public class TestClientArgs
{
    public TestClientMode mode = TestClientMode.None;
    public string serverAddress = "127.0.0.1";
    public string playerName = "";
    public bool autoReady = true;
    public bool autoPlay = true;
    public bool autoStartGame = true;
    public int minPlayersToStart = 2;
    public float actionDelaySeconds = 0.75f;
    public float connectRetrySeconds = 0.5f;
    public int maxConnectAttempts = 60;

    public static TestClientArgs Parse()
    {
        var args = new TestClientArgs();
        string[] commandLine = Environment.GetCommandLineArgs();

        for (int i = 0; i < commandLine.Length; i++)
        {
            string token = commandLine[i].ToLowerInvariant();
            switch (token)
            {
                case "-testclient":
                case "--test-client":
                    args.mode = TestClientMode.Client;
                    break;
                case "-testhost":
                case "--test-host":
                    args.mode = TestClientMode.Host;
                    break;
                case "-address":
                case "--address":
                    if (i + 1 < commandLine.Length)
                        args.serverAddress = commandLine[++i];
                    break;
                case "-name":
                case "--name":
                    if (i + 1 < commandLine.Length)
                        args.playerName = commandLine[++i];
                    break;
                case "-autoready":
                case "--auto-ready":
                    if (i + 1 < commandLine.Length)
                        args.autoReady = ParseBool(commandLine[++i], true);
                    break;
                case "-autoplay":
                case "--auto-play":
                    if (i + 1 < commandLine.Length)
                        args.autoPlay = ParseBool(commandLine[++i], true);
                    break;
                case "-autostart":
                case "--auto-start":
                    if (i + 1 < commandLine.Length)
                        args.autoStartGame = ParseBool(commandLine[++i], true);
                    break;
                case "-minplayers":
                case "--min-players":
                    if (i + 1 < commandLine.Length && int.TryParse(commandLine[++i], out int minPlayers))
                        args.minPlayersToStart = Mathf.Max(2, minPlayers);
                    break;
                case "-delay":
                case "--delay":
                    if (i + 1 < commandLine.Length && float.TryParse(commandLine[++i], out float delay))
                        args.actionDelaySeconds = Mathf.Max(0.1f, delay);
                    break;
            }
        }

        if (args.mode != TestClientMode.None && string.IsNullOrWhiteSpace(args.playerName))
        {
            args.playerName = args.mode == TestClientMode.Host
                ? "TestHost"
                : $"TestClient_{UnityEngine.Random.Range(1000, 9999)}";
        }

        return args;
    }

    static bool ParseBool(string value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        if (bool.TryParse(value, out bool parsed))
            return parsed;

        return value == "1" || value == "yes" || value == "on";
    }
}

public static class TestClientRuntime
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BootstrapFromCommandLine()
    {
        var args = TestClientArgs.Parse();
        if (args.mode == TestClientMode.None)
            return;

        if (UnityEngine.Object.FindObjectOfType<TestClientLauncher>() != null)
            return;

        var launcherObject = new GameObject("TestClientLauncher");
        var launcher = launcherObject.AddComponent<TestClientLauncher>();
        launcher.ApplyArgs(args);
    }
}
