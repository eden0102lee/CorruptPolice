# CorruptPolice Demo

This repository contains a small Unity project used to prototype a turn based board game.

## 12 Player Demo

The `GameManager` script exposes settings that control how many players are created. By default it creates 8 regular police, 2 corrupt police and 2 thief players, totalling **12** participants. Players are distributed across teams automatically.

To run the local demo open the `SampleScene` in Unity and press play. The project uses Unity 2021 or newer.

An EditMode test named `GameSetupTests` verifies that the `GameManager`
initializes the full roster of 12 participants. Run it via **Window → General → Test Runner**.

## Network Multiplayer

The project includes a Unity Netcode for GameObjects client/server flow for online play.

### Setup

1. Open `SampleScene`.
2. Add a UI `Canvas` with the `LobbyUI` component (or wire an existing lobby panel).
3. Optionally add `GameFlowUI` to display phase, round, and game-over state.
4. On `GameManager`, enable **Use Network Mode** to skip the local auto-start and wait in the lobby.

`GameSystemsBootstrap` and `RoomManager` are already attached to the `GameManager` prefab.

### Host a game

1. Enter a player name and room settings.
2. Click **Host** to start a host on port `7777`.
3. Wait for other players to join.
4. Click **Start Game** when ready (minimum 2 players).

### Join a game

1. Enter a player name and the host IP address (default `127.0.0.1`).
2. Click **Join**.
3. Click **Ready** when prepared to play.

### Game flow

1. **Lobby** – players connect and configure the room.
2. **Placement** – each player chooses a starting node in turn order.
3. **Playing** – thieves move first each round, then police teams take turns.
4. **Game Over** – police win if all thieves are arrested or rounds expire; thieves win if they collect enough treasure.

Only the active player can submit actions. The host/server validates all moves and broadcasts state to every client.

### Scripts

| Script | Purpose |
|--------|---------|
| `RoomManager` | Local room configuration and role assignment |
| `NetworkBootstrap` | Creates `NetworkManager` and starts host/client |
| `NetworkRoomManager` | Syncs lobby players and starts the networked match |
| `NetworkGameController` | Server-authoritative placement and action handling |
| `LobbyUI` | Host/join/ready/start controls |
| `GameFlowUI` | Phase, turn, and result display |

## Test Clients

Automated test clients connect to a host, join the lobby, mark ready, and play without manual input. Useful for validating network flow with multiple instances.

### Unity Editor

Use the menu **CorruptPolice → Test Clients**:

- **Launch Test Host** – configures an auto-host that starts the game when enough players join
- **Launch Test Client** – configures a bot client that connects to `127.0.0.1`
- **Clear Test Launcher** – removes the launcher object from the scene

Enter Play Mode after choosing a menu item. For multi-client testing, build the game and run multiple executables, or use several Editor/Build instances.

### Command Line (Build)

```bash
# Test host (auto-starts when 2+ players join)
./CorruptPolice.exe -testhost -name TestHost -autostart true -minplayers 2

# Test client
./CorruptPolice.exe -testclient -address 127.0.0.1 -name TestClient_1 -autoready true -autoplay true
```

Supported flags:

| Flag | Description |
|------|-------------|
| `-testhost` / `-testclient` | Run as automated test host or client |
| `-address` | Server address (clients only, default `127.0.0.1`) |
| `-name` | Player display name |
| `-autoready` | Automatically mark ready in lobby (`true`/`false`) |
| `-autoplay` | Automatically perform placement and turn actions |
| `-autostart` | Host auto-starts game when enough players join |
| `-minplayers` | Minimum players before host auto-starts (default `2`) |
| `-delay` | Seconds between bot actions (default `0.75`) |

### Launch Script

After building the game, run:

```bash
chmod +x Assets/Scripts/Test/launch_test_clients.sh
./Assets/Scripts/Test/launch_test_clients.sh /path/to/CorruptPolice.exe 3
```

This starts one test host and three automated test clients.
