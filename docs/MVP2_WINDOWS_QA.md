# sub-terra MVP2 Windows x64 QA

## Build profiles

| Profile | Define | Development Build | Debugger | Profiler |
| --- | --- | --- | --- | --- |
| Development | `SUBTERRA_BUILD_DEVELOPMENT` | Yes | Yes | Yes |
| QA | `SUBTERRA_BUILD_QA` | Yes | No | Yes |
| Release | `SUBTERRA_BUILD_RELEASE` | No | No | No |

Create each package in Unity with `SubTerra > Phase P > Build Windows`.
The build command uses Windows x64 and includes only Bootstrap, MainMenu, SurfaceBase, and Mine_Demo_Integration. It writes a ZIP, SHA-256 file, `README.txt`, `CHANGELOG.txt`, and `BUILD_MANIFEST.json` below `Builds/Windows-x64`.

## Required Windows QA

Record the package SHA-256 and `BUILD_MANIFEST.json` before testing. Run the same steps on the development PC and on a separate Windows x64 PC without Unity installed.

1. Start a new game from Bootstrap/MainMenu.
2. Complete the documented 40m demo route using normal input only.
3. Return normally, sell/upgrade at Surface Base, then exit the process fully.
   - **Surface sell (prompt-B 39)**: Economy panel lists owned minerals only; select row → qty default 1 → preview credits = unit price × qty; Sell Selected reduces stack and increases gold; Sell All sells every positive stack under one busy span. Mine inventory must **not** expose sell buttons.
   - After layout refresh: menu `SubTerra/UI/Build Prompt-B Sell Panel Layout (SurfaceBase only)` if sell list/qty/action controls are missing under EconomyPanel.
4. Relaunch the same package and use Continue; verify world changes, facilities, progress, cargo settlement, and checkpoint state restore.
5. Check `%USERPROFILE%/AppData/LocalLow/DefaultCompany/sub-terra/Player.log` for new fatal errors.
6. Start with a temporary save directory fixture and verify migration/corrupt-save recovery before using a real player save.

## Performance capture record

Target: Windows x64 QA build, 1920x1080, 60 FPS target. Capture at least 10 seconds of steady state and note the largest spike for each scenario.

| Scenario | CPU frame ms | GC alloc/frame | Peak spike ms | Result/notes |
| --- | ---: | ---: | ---: | --- |
| 40m world generation |  |  |  |  |
| Tilemap collider update |  |  |  |  |
| Structural recalculation/collapse |  |  |  |  |
| Facility power update |  |  |  |  |
| Hazard + HUD overlap |  |  |  |  |
| Save and reload |  |  |  |  |

The MVP target is no sustained frame above 16.7 ms at 60 FPS. Any measured spike, memory growth, or known limitation must be recorded rather than hidden.
