# AGENTS.md

## Cursor Cloud specific instructions

This repo is a single **Unity 2022.3.20f1** game project (`CorruptPolice Demo`, a turn-based board
game). There is **no backend, database, web service, or networking** — the only "service" is the
Unity Editor opening/running the project. See `README.md` for gameplay and
`ProjectSettings/ProjectVersion.txt` for the pinned editor version/changeset
(`2022.3.20f1 (61c2feb0970d)`). There is no `package.json`, `Makefile`, Docker, linter, or CI.

### Environment layout (provisioned by the VM snapshot / update script)

- Unity Editor binary: `~/unity-editor/Editor/Unity` (Linux build, matches the pinned version).
  The update script self-heals this download if it is missing.
- The editor is headless-only here; always wrap commands in `xvfb-run -a` so it has a virtual
  display. Unity Package Manager packages from `Packages/manifest.json` restore automatically the
  first time the project is opened (generates a gitignored `Library/` folder).

### Unity license is REQUIRED before anything works (non-obvious gotcha)

Unity will not compile, test, run, or build in batch mode without an activated license. A fresh VM
has no license and the editor exits with `No valid Unity Editor license found.` Activate once and
the license is cached under `~/.local/share/unity3d/` (persists in the snapshot). Provide one of:

- **Personal license (free `.ulf`):** generate the manual activation file, upload it at
  <https://license.unity3d.com/manual> while logged into a Unity account, download the returned
  `.ulf`, store it as a base64 secret `UNITY_LICENSE_B64`, then:

  ```bash
  echo "$UNITY_LICENSE_B64" | base64 -d > /tmp/Unity_lic.ulf
  xvfb-run -a ~/unity-editor/Editor/Unity -batchmode -nographics \
    -manualLicenseFile /tmp/Unity_lic.ulf -logFile - || true   # exits non-zero even on success
  ```

  (Regenerate the activation file with: `xvfb-run -a ~/unity-editor/Editor/Unity -batchmode
  -nographics -createManualActivationFile -logFile - -quit` → writes `Unity_v2022.3.20f1.alf`.)

- **Pro/Plus serial:** with secrets `UNITY_SERIAL`, `UNITY_EMAIL`, `UNITY_PASSWORD`:

  ```bash
  xvfb-run -a ~/unity-editor/Editor/Unity -batchmode -nographics -quit \
    -serial "$UNITY_SERIAL" -username "$UNITY_EMAIL" -password "$UNITY_PASSWORD" -logFile -
  ```

### Run / test / build (after the license is active)

All commands are headless via `xvfb-run`.

- **Open project / compile scripts + restore packages:**
  ```bash
  xvfb-run -a ~/unity-editor/Editor/Unity -batchmode -nographics -projectPath /workspace -quit -logFile -
  ```
- **Run EditMode tests** (see testing note below):
  ```bash
  xvfb-run -a ~/unity-editor/Editor/Unity -batchmode -projectPath /workspace \
    -runTests -testPlatform EditMode -testResults /workspace/EditMode-results.xml -logFile -
  ```
  PlayMode tests: swap `-testPlatform PlayMode`.
- **Build a Linux player:**
  ```bash
  xvfb-run -a ~/unity-editor/Editor/Unity -batchmode -nographics -projectPath /workspace \
    -quit -buildLinux64Player /workspace/Build/game.x86_64 -logFile -
  ```
- **Play the game (12-player demo):** GUI flow is "open `Assets/Scenes/SampleScene.unity`, press
  Play". `Assets/Scripts/Test/TestLauncher.cs` (attach to a scene GameObject) auto-places 12
  players and calls `GameManager.ForceStartGame()` for a quick demo.

### Testing notes (non-obvious)

- The `README.md` references an EditMode test `GameSetupTests` that does **not** exist in the repo.
  The only test files (`Assets/DataRenderer2D/Line/Editor/Test/*.cs`) are gated behind
  `#if TEST_ENABLE` and there are no test `.asmdef` assemblies, so the Test Runner has **no
  ready-to-run tests** out of the box without first adding a test assembly / scripting define.
- No linter / formatter is configured; code style only comes from an IDE (Rider/Visual Studio).
