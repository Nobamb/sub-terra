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
   - **Surface sell (prompt-B 39 / 102)**: Economy panel lists owned items only; select row → qty default 1 → preview credits = unit price × qty; Sell Selected reduces stack and increases gold; Sell All sells positive **non-rare mineral stacks only** under one busy span. Rare items, including engine fuel, remain in cargo. With rare items only, Sell All is disabled while selected sale remains available. Mine inventory must **not** expose sell buttons.
   - After layout refresh: menu `SubTerra/UI/Build Prompt-B Sell Panel Layout (SurfaceBase only)` if sell list/qty/action controls are missing under EconomyPanel.
4. Relaunch the same package and use Continue; verify world changes, facilities, progress, cargo settlement, and checkpoint state restore.
5. Check `%USERPROFILE%/AppData/LocalLow/DefaultCompany/sub-terra/Player.log` for new fatal errors.
6. Start with a temporary save directory fixture and verify migration/corrupt-save recovery before using a real player save.

## Prompt-B 102 engine fuel acceptance route

Use a backed-up disposable QA save slot and normal input from Bootstrap. Record the tested build hash, resolution, result, and any failure for every row. These are acceptance checks, not a record of tests already passed.

| Step | Action | Expected result |
| --- | --- | --- |
| 1 | Start a new game, mine common minerals, return and sell/upgrade | Core route works without debug tools; sell actions are available at Surface Base only |
| 2 | Approach the 36m seal before deep-zone unlock | The seal blocks progress; a locked resource cannot be mined and the HUD explains the deep-zone lock |
| 3 | Unlock the deep zone and approach the 38m sealed glyph | Gray stone replaces the purple X marker |
| 4 | Let Digger-Bot scan the glyph | Cyan point light and awakened glyph appear during the active scan pulse; the gray stone returns when the pulse expires. This is a pulse effect, not a permanent radius highlight |
| 5 | Attempt mining with drill level below 2 | Drill-level failure appears; the block and cargo remain unchanged. If normal progression already grants level 2, use the automated mining test for this independent guard |
| 6 | Attempt mining with drill level 2 and no room for 1 fuel unit | Cargo-full failure appears; the block remains and fuel is not granted |
| 7 | Free cargo space and finish mining | The block disappears, engine fuel quantity increases by exactly 1, and inventory shows a rare item with its own icon |
| 8 | Save while carrying the fuel; fully exit and Continue | Fuel quantity, deep unlock, and cargo weight restore. The mined glyph block does not regenerate |
| 9 | Return with both common minerals and fuel; use Sell All | Common minerals sell; fuel quantity stays unchanged. The rare-exclusion notice remains visible after the result |
| 10 | Save at Surface Base before selling fuel; exit and Continue, then re-enter the mine | Fuel survives the surface save and the previously mined glyph block remains absent |
| 11 | Select 1 engine fuel and sell | Fuel decreases by 1; base unit price is 100G and gold increases by the displayed preview. For the exact +100G check use a run without a gold-gain bonus; verify upgraded runs against their preview |
| 12 | Exit and Continue after sale | Sold fuel remains absent, gold persists, and the mined block does not return |
| 13 | Carry or store fuel at an outpost settlement console; try bulk and selected settlement | Bulk settlement preserves rare cargo/storage while selling common minerals; selected fuel settlement fails with a Surface Base instruction and no state change |

Automation covers selected sale, rare exclusion, drill/deep/capacity guards, and file save/load plus destroyed-tile restoration. The full normal-input route, pulse layering, HUD readability, and a separate PC without Unity still require runtime acceptance.

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
