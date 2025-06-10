# CorruptPolice Demo

This repository contains a small Unity project used to prototype a turn based board game.

## 12 Player Demo

The `GameManager` script now exposes settings that control how many players are created. By default it creates 8 regular police, 2 corrupt police and 2 thief players, totalling **12** participants. Players are distributed across teams automatically.

To run the demo open the `SampleScene` in Unity and press play. The project uses Unity 2021 or newer.

An EditMode test named `GameSetupTests` verifies that the `GameManager`
initializes the full roster of 12 participants. Run it via **Window → General → Test Runner**.
