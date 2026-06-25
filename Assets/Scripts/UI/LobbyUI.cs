using System.Text;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject lobbyPanel;
    public GameObject inGamePanel;

    [Header("Inputs")]
    public TMP_InputField playerNameInput;
    public TMP_InputField serverAddressInput;
    public TMP_InputField maxPlayersInput;
    public TMP_InputField maxRoundsInput;
    public TMP_InputField treasureGoalInput;

    [Header("Buttons")]
    public Button hostButton;
    public Button joinButton;
    public Button readyButton;
    public Button startGameButton;
    public Button disconnectButton;

    [Header("Labels")]
    public TMP_Text statusLabel;
    public TMP_Text playerListLabel;

    private bool isReady;

    void Awake()
    {
        if (hostButton != null) hostButton.onClick.AddListener(OnHostClicked);
        if (joinButton != null) joinButton.onClick.AddListener(OnJoinClicked);
        if (readyButton != null) readyButton.onClick.AddListener(OnReadyClicked);
        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGameClicked);
        if (disconnectButton != null) disconnectButton.onClick.AddListener(OnDisconnectClicked);
    }

    void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.useNetworkMode)
            ShowLobby();
        else if (GameManager.Instance != null && !GameManager.Instance.useNetworkMode)
            ShowInGame();
        else
            ShowLobby();

        if (playerNameInput != null && string.IsNullOrWhiteSpace(playerNameInput.text))
            playerNameInput.text = $"Player{Random.Range(1000, 9999)}";

        if (serverAddressInput != null && string.IsNullOrWhiteSpace(serverAddressInput.text))
            serverAddressInput.text = "127.0.0.1";

        ApplyDefaultConfigToInputs();
        RefreshUI();
    }

    void OnEnable()
    {
        if (NetworkRoomManager.Instance != null)
            NetworkRoomManager.Instance.OnLobbyUpdated += RefreshUI;

        if (GameManager.Instance != null)
            GameManager.Instance.OnFlowStateChanged += HandleFlowStateChanged;
    }

    void OnDisable()
    {
        if (NetworkRoomManager.Instance != null)
            NetworkRoomManager.Instance.OnLobbyUpdated -= RefreshUI;

        if (GameManager.Instance != null)
            GameManager.Instance.OnFlowStateChanged -= HandleFlowStateChanged;
    }

    void Update()
    {
        if (NetworkManager.Singleton == null)
            return;

        if (NetworkManager.Singleton.IsConnectedClient &&
            !NetworkManager.Singleton.IsHost &&
            NetworkRoomManager.Instance != null &&
            NetworkRoomManager.Instance.IsSpawned &&
            !HasJoinedLobby())
        {
            JoinLobbyAsClient();
        }

        RefreshUI();
    }

    bool HasJoinedLobby()
    {
        if (NetworkRoomManager.Instance == null)
            return false;

        foreach (var slot in NetworkRoomManager.Instance.GetConnectedPlayers())
        {
            if (slot.ClientId == NetworkManager.Singleton.LocalClientId)
                return true;
        }

        return false;
    }

    void JoinLobbyAsClient()
    {
        NetworkRoomManager.Instance.JoinRoomServerRpc(GetPlayerName());
    }

    void OnHostClicked()
    {
        PrepareNetworkSession();

        ApplyConfigFromInputs();
        if (NetworkBootstrap.Instance == null)
        {
            var bootstrapObject = new GameObject("NetworkBootstrap");
            bootstrapObject.AddComponent<NetworkBootstrap>();
        }

        if (!NetworkBootstrap.Instance.StartHostGame())
            return;

        if (NetworkRoomManager.Instance != null)
            NetworkRoomManager.Instance.RegisterLocalHostPlayer(GetPlayerName());

        ShowLobby();
        RefreshUI();
    }

    void OnJoinClicked()
    {
        PrepareNetworkSession();

        ApplyConfigFromInputs();
        if (NetworkBootstrap.Instance == null)
        {
            var bootstrapObject = new GameObject("NetworkBootstrap");
            bootstrapObject.AddComponent<NetworkBootstrap>();
        }

        string address = serverAddressInput != null ? serverAddressInput.text : "127.0.0.1";
        if (!NetworkBootstrap.Instance.StartClientGame(address))
            return;

        ShowLobby();
        RefreshUI();
    }

    void OnReadyClicked()
    {
        isReady = !isReady;
        if (NetworkRoomManager.Instance != null)
            NetworkRoomManager.Instance.SetReadyServerRpc(isReady);
        RefreshUI();
    }

    void OnStartGameClicked()
    {
        if (NetworkRoomManager.Instance != null)
            NetworkRoomManager.Instance.StartGameServerRpc();
    }

    void OnDisconnectClicked()
    {
        NetworkBootstrap.Instance?.Shutdown();
        isReady = false;
        ShowLobby();
        RefreshUI();
    }

    void HandleFlowStateChanged(GameFlowState state)
    {
        if (state == GameFlowState.Placement || state == GameFlowState.Playing)
            ShowInGame();
        else if (state == GameFlowState.Lobby)
            ShowLobby();
    }

    void ShowLobby()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        if (inGamePanel != null) inGamePanel.SetActive(false);
    }

    void ShowInGame()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (inGamePanel != null) inGamePanel.SetActive(true);
    }

    void ApplyDefaultConfigToInputs()
    {
        if (RoomManager.Instance == null)
            return;

        var config = RoomManager.Instance.ActiveConfig;
        if (maxPlayersInput != null) maxPlayersInput.text = config.maxPlayers.ToString();
        if (maxRoundsInput != null) maxRoundsInput.text = config.maxRounds.ToString();
        if (treasureGoalInput != null) treasureGoalInput.text = config.treasureGoal.ToString();
    }

    void ApplyConfigFromInputs()
    {
        if (RoomManager.Instance == null)
            return;

        var config = RoomManager.Instance.ActiveConfig;
        if (maxPlayersInput != null && int.TryParse(maxPlayersInput.text, out int maxPlayers))
            config.maxPlayers = Mathf.Clamp(maxPlayers, 2, 12);
        if (maxRoundsInput != null && int.TryParse(maxRoundsInput.text, out int maxRounds))
            config.maxRounds = Mathf.Max(1, maxRounds);
        if (treasureGoalInput != null && int.TryParse(treasureGoalInput.text, out int treasureGoal))
            config.treasureGoal = Mathf.Max(1, treasureGoal);

        RoomManager.Instance.UpdateConfig(config);
        if (GameManager.Instance != null)
            config.ApplyTo(GameManager.Instance);
    }

    void PrepareNetworkSession()
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

    string GetPlayerName()
    {
        if (playerNameInput == null || string.IsNullOrWhiteSpace(playerNameInput.text))
            return $"Player{Random.Range(1000, 9999)}";
        return playerNameInput.text.Trim();
    }

    void RefreshUI()
    {
        bool connected = NetworkBootstrap.Instance != null && NetworkBootstrap.Instance.IsConnected;
        bool isHost = connected && NetworkBootstrap.Instance.IsHost;

        if (hostButton != null) hostButton.interactable = !connected;
        if (joinButton != null) joinButton.interactable = !connected;
        if (readyButton != null) readyButton.interactable = connected;
        if (startGameButton != null) startGameButton.interactable = connected && isHost;
        if (disconnectButton != null) disconnectButton.interactable = connected;

        if (statusLabel != null)
        {
            if (!connected)
                statusLabel.text = "尚未連線";
            else if (isHost)
                statusLabel.text = "已建立房間（Host）";
            else
                statusLabel.text = "已加入房間（Client）";
        }

        if (playerListLabel != null)
        {
            var builder = new StringBuilder();
            if (NetworkRoomManager.Instance != null && connected)
            {
                foreach (var slot in NetworkRoomManager.Instance.GetConnectedPlayers())
                {
                    builder.AppendLine($"{slot.PlayerName} {(slot.IsReady ? "[Ready]" : "")}");
                }
            }
            else if (RoomManager.Instance != null)
            {
                foreach (var player in RoomManager.Instance.LobbyPlayers)
                {
                    builder.AppendLine($"{player.playerName} {(player.isReady ? "[Ready]" : "")}");
                }
            }

            if (builder.Length == 0)
                builder.Append("等待玩家加入...");
            playerListLabel.text = builder.ToString();
        }
    }
}
