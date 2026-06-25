# AGENTS.md

## Cursor Cloud specific instructions

This repo is a single **Unity 2022.3.20f1** game project (`CorruptPolice Demo`). There is no
backend, database, or web service — the only "service" is the Unity Editor running the project.
See `README.md` for the gameplay summary and `ProjectSettings/ProjectVersion.txt` for the pinned
editor version/changeset (`2022.3.20f1 (61c2feb0970d)`).

### Environment layout (already provisioned in the VM snapshot)

- Unity Editor binary: `~/unity-editor/Editor/Unity` (Linux build, matches the pinned version).
- Runtime libraries for the editor and `xvfb` are installed system-wide.
- The editor is headless-only here; always wrap commands in `xvfb-run -a` so it has a virtual
  display. Unity Package Manager packages from `Packages/manifest.json` restore automatically the
  first time the project is opened (a `Library/` folder is generated and gitignored).

### Unity license is required (non-obvious gotcha)

Unity will not compile, test, run, or build in batch mode without an activated license. A fresh VM
has no license; the editor exits with `No valid Unity Editor license found.` Activate once and the
license is cached under `~/.local/share/unity3d/` (persists in the snapshot, so future sessions
inherit it).

Activation options (pick one):

- **Personal license (free, `.ulf` file):** generate a manual activation file, upload it at
  <https://license.unity3d.com/manual> while logged into a Unity account, download the returned
  `.ulf`, then store it as a secret. To activate from a base64 secret named `UNITY_LICENSE_B64`:

  ```bash
  echo "$UNITY_LICENSE_B64" | base64 -d > /tmp/Unity_lic.ulf
  xvfb-run -a ~/unity-editor/Editor/Unity -batchmode -nographics \
    -manualLicenseFile /tmp/Unity_lic.ulf -logFile - || true   # exits non-zero even on success
  ```

  (To regenerate the manual activation file: `xvfb-run -a ~/unity-editor/Editor/Unity -batchmode
  -nographics -createManualActivationFile -logFile - -quit` → writes `Unity_v2022.3.20f1.alf`.)

- **Pro/Plus serial:** with secrets `UNITY_SERIAL`, `UNITY_EMAIL`, `UNITY_PASSWORD`:

  ```bash
  xvfb-run -a ~/unity-editor/Editor/Unity -batchmode -nographics -quit \
    -serial "$UNITY_SERIAL" -username "$UNITY_EMAIL" -password "$UNITY_PASSWORD" -logFile -
  ```

### Run / test / build (after the license is active)

All commands are headless via `xvfb-run`. Use a non-zero exit-tolerant invocation only where noted.

- **Open project / compile scripts + restore packages:**
  ```bash
  xvfb-run -a ~/unity-editor/Editor/Unity -batchmode -nographics -projectPath /workspace -quit -logFile -
  ```
- **Run EditMode tests** (Test Runner; see note below about test setup):
  ```bash
  xvfb-run -a ~/unity-editor/Editor/Unity -batchmode -projectPath /workspace \
    -runTests -testPlatform EditMode -testResults /workspace/EditMode-results.xml -logFile -
  ```
  PlayMode tests: swap `-testPlatform PlayMode`.
- **Build a Linux player** (headless build):
  ```bash
  xvfb-run -a ~/unity-editor/Editor/Unity -batchmode -nographics -projectPath /workspace \
    -quit -buildLinux64Player /workspace/Build/game.x86_64 -logFile -
  ```
- **Play the game (12-player demo):** the GUI flow is "open `Assets/Scenes/SampleScene.unity`,
  press Play". `Assets/Scripts/Test/TestLauncher.cs` auto-places 12 players and calls
  `ForceStartGame()` for a quick demo when attached to a GameObject in the scene.

### Testing notes (non-obvious)

- The `README.md` references an EditMode test `GameSetupTests` that does **not** exist in the repo.
  The only test files (`Assets/DataRenderer2D/Line/Editor/Test/*.cs`) are gated behind
  `#if TEST_ENABLE` and there are no test `.asmdef` assemblies, so the Test Runner has no
  ready-to-run tests out of the box without adding a test assembly / scripting define.
- There is no linter, no `package.json`, no Makefile, and no CI config. Code style/inspection
  only comes from an IDE (Rider/Visual Studio) if used.
