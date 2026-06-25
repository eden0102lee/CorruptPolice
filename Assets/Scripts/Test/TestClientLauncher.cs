using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class TestClientLauncher : MonoBehaviour
{
    [Header("Mode")]
    public TestClientMode mode = TestClientMode.None;
    public string serverAddress = "127.0.0.1";
    public string playerName = "TestClient";

    [Header("Automation")]
    public bool autoReady = true;
    public bool autoPlay = true;
    public bool autoStartGame = true;
    public int minPlayersToStart = 2;
    public float actionDelaySeconds = 0.75f;
    public float connectRetrySeconds = 0.5f;
    public int maxConnectAttempts = 60;

    private TestClientArgs activeArgs;
    private bool hasJoinedLobby;
    private bool hasMarkedReady;
    private bool hasRequestedGameStart;
    private Coroutine connectRoutine;
    private Coroutine actionRoutine;

    void Start()
    {
        if (mode == TestClientMode.None || connectRoutine != null)
            return;

        activeArgs = BuildArgsFromInspector();
        connectRoutine = StartCoroutine(BootstrapRoutine());
    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnChanged += HandleTurnChanged;
            GameManager.Instance.OnFlowStateChanged += HandleFlowStateChanged;
        }
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnChanged -= HandleTurnChanged;
            GameManager.Instance.OnFlowStateChanged -= HandleFlowStateChanged;
        }
    }

    public void ApplyArgs(TestClientArgs args)
    {
        activeArgs = args;
        mode = args.mode;
        serverAddress = args.serverAddress;
        playerName = args.playerName;
        autoReady = args.autoReady;
        autoPlay = args.autoPlay;
        autoStartGame = args.autoStartGame;
        minPlayersToStart = args.minPlayersToStart;
        actionDelaySeconds = args.actionDelaySeconds;
        connectRetrySeconds = args.connectRetrySeconds;
        maxConnectAttempts = args.maxConnectAttempts;

        if (connectRoutine == null && isActiveAndEnabled)
            connectRoutine = StartCoroutine(BootstrapRoutine());
    }

    public TestClientArgs BuildArgsFromInspector()
    {
        return new TestClientArgs
        {
            mode = mode,
            serverAddress = serverAddress,
            playerName = playerName,
            autoReady = autoReady,
            autoPlay = autoPlay,
            autoStartGame = autoStartGame,
            minPlayersToStart = minPlayersToStart,
            actionDelaySeconds = actionDelaySeconds,
            connectRetrySeconds = connectRetrySeconds,
            maxConnectAttempts = maxConnectAttempts
        };
    }

    IEnumerator BootstrapRoutine()
    {
        if (activeArgs == null)
            activeArgs = BuildArgsFromInspector();

        PrepareNetworkSession();

        if (NetworkBootstrap.Instance == null)
        {
            var bootstrapObject = new GameObject("NetworkBootstrap");
            bootstrapObject.AddComponent<NetworkBootstrap>();
        }

        bool started = activeArgs.mode == TestClientMode.Host
            ? NetworkBootstrap.Instance.StartHostGame()
            : NetworkBootstrap.Instance.StartClientGame(activeArgs.serverAddress);

        if (!started)
        {
            Debug.LogError("[TestClient] Failed to start network session.");
            yield break;
        }

        int attempts = 0;
        while (attempts < activeArgs.maxConnectAttempts)
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsConnectedClient &&
                NetworkRoomManager.Instance != null &&
                NetworkRoomManager.Instance.IsSpawned)
            {
                break;
            }

            attempts++;
            yield return new WaitForSeconds(activeArgs.connectRetrySeconds);
        }

        if (NetworkRoomManager.Instance == null || !NetworkRoomManager.Instance.IsSpawned)
        {
            Debug.LogError("[TestClient] NetworkRoomManager did not become available.");
            yield break;
        }

        if (activeArgs.mode == TestClientMode.Host)
        {
            NetworkRoomManager.Instance.RegisterLocalHostPlayer(activeArgs.playerName);
            hasJoinedLobby = true;
            Debug.Log($"[TestClient] Host ready as {activeArgs.playerName}");
        }
        else
        {
            NetworkRoomManager.Instance.JoinRoomServerRpc(new FixedString32Bytes(activeArgs.playerName));
            hasJoinedLobby = true;
            Debug.Log($"[TestClient] Joined lobby as {activeArgs.playerName}");
        }

        if (activeArgs.autoReady)
            yield return MarkReadyWhenJoined();

        if (activeArgs.mode == TestClientMode.Host && activeArgs.autoStartGame)
            StartCoroutine(AutoStartGameRoutine());
    }

    IEnumerator MarkReadyWhenJoined()
    {
        yield return new WaitForSeconds(activeArgs.actionDelaySeconds);

        if (hasMarkedReady || NetworkRoomManager.Instance == null)
            yield break;

        NetworkRoomManager.Instance.SetReadyServerRpc(true);
        hasMarkedReady = true;
        Debug.Log($"[TestClient] {activeArgs.playerName} is ready.");
    }

    IEnumerator AutoStartGameRoutine()
    {
        while (!hasRequestedGameStart)
        {
            if (NetworkRoomManager.Instance == null)
            {
                yield return new WaitForSeconds(activeArgs.connectRetrySeconds);
                continue;
            }

            int playerCount = NetworkRoomManager.Instance.GetConnectedPlayers().Count;
            if (playerCount >= activeArgs.minPlayersToStart)
            {
                yield return new WaitForSeconds(activeArgs.actionDelaySeconds);
                NetworkRoomManager.Instance.StartGameServerRpc();
                hasRequestedGameStart = true;
                Debug.Log($"[TestClient] Host auto-started game with {playerCount} players.");
                yield break;
            }

            yield return new WaitForSeconds(activeArgs.connectRetrySeconds);
        }
    }

    void HandleFlowStateChanged(GameFlowState state)
    {
        if (!activeArgs.autoPlay || state != GameFlowState.Placement)
            return;

        TryScheduleBotAction(GameManager.Instance != null ? GameManager.Instance.CurrentTurnPlayer : null);
    }

    void HandleTurnChanged(PlayerData player)
    {
        if (!activeArgs.autoPlay)
            return;

        TryScheduleBotAction(player);
    }

    void TryScheduleBotAction(PlayerData player)
    {
        if (player == null || NetworkManager.Singleton == null)
            return;

        if (!player.IsLocalPlayer(NetworkManager.Singleton.LocalClientId))
            return;

        if (actionRoutine != null)
            StopCoroutine(actionRoutine);

        actionRoutine = StartCoroutine(PerformBotActionRoutine(player));
    }

    IEnumerator PerformBotActionRoutine(PlayerData player)
    {
        yield return new WaitForSeconds(activeArgs.actionDelaySeconds);

        if (player == null || NetworkGameController.Instance == null || GameManager.Instance == null)
            yield break;

        if (!player.IsLocalPlayer(NetworkManager.Singleton.LocalClientId))
            yield break;

        if (GameManager.Instance.FlowState == GameFlowState.Placement &&
            GameManager.Instance.CurrentPhase == GamePhase.Placement)
        {
            if (TestClientBot.TryGetPlacementNode(player, out int placementNode))
            {
                Debug.Log($"[TestClient] {player.playerName} placing at node {placementNode}");
                NetworkGameController.Instance.SubmitPlacementServerRpc(placementNode);
            }
            yield break;
        }

        if (GameManager.Instance.FlowState != GameFlowState.Playing)
            yield break;

        if (TestClientBot.TryGetTurnAction(player, out int targetNodeId, out ActionType action))
        {
            Debug.Log($"[TestClient] {player.playerName} performing {action} on node {targetNodeId}");
            NetworkGameController.Instance.SubmitActionServerRpc(targetNodeId, (int)action);
        }
    }

    static void PrepareNetworkSession()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.useNetworkMode = true;
            GameManager.Instance.ResetGameState();
            GameManager.Instance.SetFlowState(GameFlowState.Lobby);
        }

        if (MapManager.Instance != null)
            MapManager.Instance.autoBuildOnStart = false;
    }
}
