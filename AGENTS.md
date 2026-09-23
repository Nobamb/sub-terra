# AGENTS.md

## Project

Sub-Terra is a Unity 6000.5.4f1 project.

The Git repository root is not the Unity project root.
The Unity project is located at:

sub-terra/

Project code and assets primarily live under:

sub-terra/Assets/\_Project/

## Sources of truth

Before making changes, use these documents as authoritative:

- `init/rule.md` — binding agent/work rules
- `init/PRD.md` — game design and gameplay values
- `docs/INTEGRATION_GUIDE.md` — runtime integration architecture
- relevant files under `work_process/` — feature-specific requirements

Do not load unrelated documents unless needed for the task.

## Critical rules

- Do not modify `ProjectSettings/*`.
- Do not modify `Packages/manifest.json` or `packages-lock.json`.
- Do not change Unity or package versions.
- Do not edit scene or prefab YAML manually when an Editor/builder workflow exists.
- Do not commit, push, or use destructive Git commands without explicit user approval.
- Preserve `.meta` GUIDs when moving Unity assets.
- Keep changes strictly within the requested scope.

## Architecture

- `SubTerra.App` must never reference `SubTerra.Gameplay.*`.
- Gameplay must never reference `SubTerra.App`.
- Cross-boundary integration belongs under `Scripts/App/Integration/`
  or Shared interfaces/DTOs.
- `SubTerra.Shared` must not depend on `UnityEngine`.

## UI

For UI work, use the installed UI skills when applicable.

- `.agents/skills/ui`
- `.agents/skills/ui-ugui`

Inspect the existing hierarchy, prefab, builder, and related scripts before modifying UI.
Preserve the current visual language unless a redesign is explicitly requested.

## Verification

After making changes:

1. Check `git status`.
2. Check the relevant diff.
3. Run the narrowest relevant tests.
4. Report changed files, verification performed, and unresolved issues.

Do not make unrelated cleanup changes.
