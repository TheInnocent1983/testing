# Project Rules

First-person parkour game built in Godot 4.7 with C#.

## Stack

- Godot **4.7** (.NET build), `Godot.NET.Sdk/4.7.1`, `net8.0`
- Physics: **Godot Jolt**
- Rendering: Forward+, D3D12 on Windows
- Language: **C# only**. GDScript is not used here — do not suggest it, not
  even as a temporary solution.

The Godot version is pinned in three places: `testing.csproj`,
`project.godot` (`config/features`) and `.github/workflows/build.yml`
(`version: 4.7.1`). Never bump one without the others.

**Verify every code change with `dotnet build testing.sln`.** It must finish
with 0 errors and 0 warnings. The IDE may show stale
`'Node.SignalName' does not contain a definition for ...` errors after adding a
`[Signal]` — that is the Godot source generator lagging behind; trust the build.

## Layout

```
Scripts/Player/PlayerMovementStates/   player movement components
Scripts/Interaction/                   interactables, doors, pickups
Scenes/Entities/                       gameplay entities (player, enemies)
Scenes/Levels/                         levels and test maps
Scenes/ModularAssets/                  reusable level geometry (.glb wrappers)
Scenes/UI/Menus/                       main menu, pause, settings shell
Scenes/UI/Settings/Tabs/               one folder per settings tab
Scenes/UI/Keybinds/                    keybind rows and managers
Scenes/UI/Components/                  reusable UI controls
Scenes/UI/Resources/                   shared .tres styles
Data/                                  descriptions.json and other data
Assets/                                art, icons, audio
```

New code goes in the folder matching its namespace. Do not invent new
top-level folders without asking.

## Namespaces

- `Parkour.Movement` — player movement
- `Parkour.Interaction` — interactables and their reactors
- `Parkour.UI`, `Parkour.UI.Settings`, `Parkour.UI.Settings.Video` — interface
- `Parkour.AI` — enemies (planned, no code yet)

Use file-scoped namespaces: `namespace Parkour.Movement;` — no braces.

Six files currently have **no** namespace: `RestartLevelComponent.cs`,
`SlideMovementComponent.cs`, `IsolatedScrollContainer.cs`, `KeybindManager.cs`,
`KeybindRow.cs`, `ControllerKeybindRow.cs`. Add the correct one when you touch
such a file, but do not sweep through the project renaming things.

## Code conventions

- Every node class is `partial`.
- Indentation: **tabs** (16 of 18 existing scripts). `CameraController.cs`,
  `CrouchComponent.cs` and `GroundMovementComponent.cs` use spaces — match the
  file you are editing, use tabs for new files.
- All comments and documentation in **English**.
- Methods that may not succeed return `bool` and are named `TryXxx`.
- Tunables are `[Export]` properties grouped with `[ExportGroup("...")]`,
  each with a default and a **trailing comment** explaining what it does:
  ```csharp
  [Export] public float WallStickForce { get; set; } = 3.0f;   // Pull into the wall so you don't peel off
  ```
- `[Signal]` on its own line above the delegate, as in `KeybindRow.cs`.
- `[GlobalClass]` on node scripts meant to be added from the editor's
  **Add Child Node** dialog (used throughout `Scripts/Interaction/`).

Two wiring styles exist in the project. Match the area you work in:

- Gameplay (`Parkour.Movement`, `Parkour.Interaction`): public auto-properties,
  `[Export] public CameraController CameraComp { get; private set; }`,
  with a `GetNodeOrNull<T>(...)` fallback in `_Ready()`.
- UI: private underscore fields, `[Export] private DescriptionPanel _panel;`,
  grouped under `[ExportGroup("External Dependencies")]` and
  `[ExportGroup("Internal Sections")]`.

## Component pattern — mandatory for gameplay

Mechanics are never written inside a controller. Each is a separate `Node`
component that receives its owner as a parameter:

```csharp
public partial class WallRunComponent : Node
{
	public bool TryWallRun(FpsController player, float delta) { ... }
}
```

The controller holds `[Export]` references and calls components from
`_PhysicsProcess`. Build every new entity the same way, not as a monolith.

**One writer per resource.** Only `FpsController` calls `MoveAndSlide()`;
only `ApplyStance()` resizes the capsule; only `CameraController.ApplyRoll()`
touches camera roll. Components state what they want every frame rather than
setting and resetting. Preserve this when adding mechanics.

## Prefer engine features over code

If built-in Godot features solve the task — signals, `AnimationTree`,
navigation, `Area3D` triggers, resources — use them and explain why. Reason:
such solutions are covered by the official docs, so the whole team can follow
them, not just the author.

## Required response format

End every answer containing code with two blocks:

**Editor setup** — which nodes to add, where, which properties to set and to
what values. Concrete, step by step. In Godot half the work happens in the
inspector; without this block the code stays unwired.

**How to verify** — what should appear on screen or in the console so I know
it works.

If a task genuinely does not need these (a one-line fix), say so explicitly.

## Uncertainty

- Never invent API. If unsure about a property or signal name, say so plainly.
- You know Godot's C# API less well than GDScript — fewer examples exist.
  Do not carry snake_case over from GDScript; C# uses PascalCase
  (`target_position` → `TargetPosition`, `link_reached` → `LinkReached`).
- Mark anything uncertain with `// TODO VERIFY:` and state what needs checking.
- If a request is ambiguous, ask one clarifying question before writing code.
- If you think an idea is architecturally wrong, say so before implementing it.

## Task scope

- One task = one coherent chunk. If asked for too much at once, propose a
  split and do the first chunk.
- Touching more than one file: plan first, code second.
- Do not refactor or rename anything you were not asked about.
- Explain the reasoning behind a change before and after making it, and stop
  for confirmation rather than batching several steps.

## Do not

- Do not edit `.tscn` or `.tres` as text. Describe what to do by hand instead.
- Do not touch or delete `.cs.uid` files — Godot 4.7 generates them.
- Do not add NuGet packages without permission.
- Do not write a placeholder silently; say when something is temporary.
- Do not change `export_presets.cfg` or the CI workflow without asking —
  the release builds depend on the exact preset names
  ("Windows Desktop", "Linux").
- Do not add `Co-Authored-By` or any Claude/Anthropic signature to commits or
  PR bodies. Plain messages only.

## Git

Work happens on `dev`; features branch off as `feat/<name>` and merge back.
`main` is release. CI runs only on `main`/`master`, so a green local build is
the real gate on `dev`.

## Cross-cutting rules

**Adding an input action.** It must be registered in the `[input]` section of
`project.godot` **and** added to `KeybindManager._customActions` with a display
name, or it will not appear in the rebinding UI.
Currently missing there: `interact` — it exists in `project.godot` (bound to E)
but is not rebindable yet.

**Adding a setting.** Add its description text to `Data/descriptions.json`
under the `en` key — the file is structured by language, so new UI copy goes
there rather than being hardcoded.

**Physics layers.** Named in `project.godot` under `[layer_names]`:
layer 1 `SolidEnvironment`, layer 10 `RunnableWall`. Reuse the named layers;
if a new one is needed, name it there first. Interactables are intended to live
on layer 3 (`Interactable` — needs naming in Project Settings).

**Interaction ray masks** must include layer 1, otherwise the player can
interact through walls.

## Domain documentation

Deeper design notes live in separate files — read the relevant one before
working in that area, and update it in the same task that changes the code:

- `Scripts/Player/README.md` — movement architecture, the priority chain, the
  Quake-style acceleration model, tuning cheat-sheet
- `Scripts/Interaction/README.md` — why the interaction system is split into
  sensor / interactable / reactor, and how to add new ones
- `INTERACTION.md` — click-by-click Godot editor setup for interactables

Planned, not yet written: `docs/enemies.md`, `docs/levels.md`.

## Multiplayer context

Not on `dev` yet — the work lives on `feat/multiplayerNetwork`. Decisions that
are not derivable from the code:

- **Listen server + client authority, deliberately.** Movement-heavy parkour
  needs zero input latency and the cheat surface is acceptable for co-op with
  friends. Revisit only for public leaderboards, and then by validating run
  times, not per-frame movement.
- **Steam (prebuilt GodotSteam GDExtension) is the shipping transport.**
  Compiling GodotSteam from source was explicitly rejected — no C++ compiler
  available, the prebuilt zip is a drop-in.
- **The ENet path stays forever** as the local dev transport, because two Steam
  accounts cannot easily run on one machine and that would break the
  "Run Multiple Instances" test loop.
- All transport setup is confined to `HighLevelNetworkHandler` on purpose.

Interaction is currently local-only. `InteractionComponent.TryInteract()` is the
single seam where an RPC would go.

## Known open questions

Verified, real, and deliberately not fixed. **Ask before changing any of these.**

- `project.godot` autoloads `HighLevelNetworkHandler` from
  `res://Scripts/Network/HighLevelNetworkHandler.cs`, which does not exist on
  `dev`. Godot logs an autoload error every run until it is merged.
- `FpsController._PhysicsProcess`: `wallClimbing` is computed but never used, so
  air gravity applies on top of wall-climbing unless a wall-run is also active.
- `WallRunComponent.WallCollisionMask` defaults to `2` (layer 2, unused) and
  `_Ready()` overwrites both wall rays with it, while runnable walls are on
  `collision_layer = 513` (layers 1 + 10).
- The crouch toggle is tracked twice, in `GroundMovementComponent` and in
  `CrouchComponent`, and the two copies can drift apart.
- `FpsController.AutoNoclip` is never read; `NoclipComponent.AutoNoclip` is the
  real flag.
- `NoclipComponent` looks up `%CollisionShape3D`, but that node does not have
  *Access as Unique Name* ticked, so the lookup returns null.
