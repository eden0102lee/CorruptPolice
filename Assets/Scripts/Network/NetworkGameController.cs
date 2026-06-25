using Unity.Netcode;
using UnityEngine;

public class NetworkGameController : NetworkBehaviour
{
    public static NetworkGameController Instance;

    public override void OnNetworkSpawn()
    {
        Instance = this;
    }

    public bool CanLocalPlayerAct(PlayerData player)
    {
        if (player == null || NetworkManager.Singleton == null)
            return false;

        return player.IsLocalPlayer(NetworkManager.Singleton.LocalClientId);
    }

    [ClientRpc]
    public void BeginNetworkGameClientRpc(int seed, NetworkRosterEntry[] rosterPayload)
    {
        if (GameManager.Instance == null || RoomManager.Instance == null)
            return;

        var roster = BuildRosterFromPayload(rosterPayload);
        GameManager.Instance.useNetworkMode = true;
        GameManager.Instance.InitializeFromRoster(roster, RoomManager.Instance.ActiveConfig);

        if (MapManager.Instance != null)
            MapManager.Instance.BuildMap(seed);

        GameManager.Instance.BeginPlacement();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitPlacementServerRpc(int nodeId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer || GameManager.Instance == null)
            return;

        var player = GameManager.Instance.GetPlayerByClientId(rpcParams.Receive.SenderClientId);
        if (player == null || GameManager.Instance.CurrentTurnPlayer != player)
            return;

        player.currentNodeId = nodeId;
        if (PlayerTokenManager.Instance != null)
            PlayerTokenManager.Instance.UpdateTokenPosition(player);

        ConfirmPlacementClientRpc(player.playerName, nodeId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitActionServerRpc(int nodeId, int actionType, ServerRpcParams rpcParams = default)
    {
        if (!IsServer || GameManager.Instance == null)
            return;

        var player = GameManager.Instance.GetPlayerByClientId(rpcParams.Receive.SenderClientId);
        if (player == null || GameManager.Instance.CurrentTurnPlayer != player)
            return;

        var action = (ActionType)actionType;
        if (!GameManager.Instance.TryExecuteAction(player, nodeId, action, out _, out _))
            return;

        BroadcastActionClientRpc(
            player.playerName,
            nodeId,
            actionType,
            GameManager.Instance.CurrentRound,
            GameManager.Instance.TreasuresCollected,
            (int)GameManager.Instance.Result,
            GameManager.Instance.CurrentTurnPlayer != null ? GameManager.Instance.CurrentTurnPlayer.playerName : string.Empty
        );
    }

    [ClientRpc]
    void ConfirmPlacementClientRpc(string playerName, int nodeId)
    {
        var player = GameManager.Instance?.GetPlayerByName(playerName);
        if (player == null)
            return;

        player.currentNodeId = nodeId;
        if (PlayerTokenManager.Instance != null)
            PlayerTokenManager.Instance.UpdateTokenPosition(player);

        if (GameManager.Instance.CurrentTurnPlayer == player && PlayerInputController.Instance != null)
            PlayerInputController.Instance.ApplyPlacementResult(nodeId);

        GameManager.Instance?.ConfirmPlacement();
    }

    [ClientRpc]
    void BroadcastActionClientRpc(string playerName, int nodeId, int actionType, int round, int treasuresCollected, int resultCode, string nextPlayerName)
    {
        if (IsServer)
            return;

        ApplyRemoteState(playerName, nodeId, (ActionType)actionType, nextPlayerName, (GameResult)resultCode);
    }

    void ApplyRemoteState(string actingPlayerName, int nodeId, ActionType action, string nextPlayerName, GameResult result)
    {
        if (GameManager.Instance == null)
            return;

        var actingPlayer = GameManager.Instance.GetPlayerByName(actingPlayerName);
        if (actingPlayer != null && PlayerInputController.Instance != null)
        {
            if (GameManager.Instance.CurrentTurnPlayer == actingPlayer)
                PlayerInputController.Instance.ApplyRemoteAction(nodeId, action);
            else if (PlayerTokenManager.Instance != null)
                PlayerTokenManager.Instance.UpdateTokenPosition(actingPlayer);
        }

        if (!string.IsNullOrEmpty(nextPlayerName))
        {
            var nextPlayer = GameManager.Instance.GetPlayerByName(nextPlayerName);
            if (nextPlayer != null)
            {
                GameManager.Instance.SetCurrentTurnPlayer(nextPlayer);
                if (PlayerInputController.Instance != null)
                    PlayerInputController.Instance.SetCurrentPlayer(nextPlayer);
            }
        }

        if (result != GameResult.None)
            GameManager.Instance.SetFlowState(GameFlowState.GameOver);
    }

    static List<PlayerData> BuildRosterFromPayload(NetworkRosterEntry[] payload)
    {
        var roster = new List<PlayerData>();
        foreach (var entry in payload)
        {
            roster.Add(new PlayerData(entry.PlayerName.ToString(), (PlayerRole)entry.Role, entry.TeamIndex)
            {
                clientId = entry.ClientId
            });
        }
        return roster;
    }
}
