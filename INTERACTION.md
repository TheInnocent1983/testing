# Interaction System — setup notes

Scripts live in `Scripts/Interaction/`. Everything below is done in the Godot editor.

## The pieces

| Script | Node type | Role |
| --- | --- | --- |
| `IInteractable.cs` | — | Interface. `Prompt`, `CanInteract`, `Interact`. |
| `Interactable.cs` | `Node` | **The base.** Child of any collision body → that body becomes interactable. Emits `interacted(interactor)`. |
| `PickupInteractable.cs` | `Node` | Subclass of the base — hides/frees the prop when grabbed. |
| `Door.cs` | `Node3D` | A *reactor*, not an interactable. `Open()` / `Close()` / `Toggle()` / `OnInteracted(interactor)`. |
| `InteractionComponent.cs` | `Node` | Player side. Camera ray + `interact` input + focus tracking. |
| `InteractionPromptUI.cs` | `Control` | HUD hint, e.g. `[E] Grab key`. |
| `InteractionUtil.cs` | — | Static helper that disables `CollisionShape3D`s deferred. |

All node scripts use `[GlobalClass]`, so they show up by name in **Add Child Node**.

## Order of work

1. Project settings (layer name)
2. Player scene (InteractionComponent + HUD)
3. Prop scenes (pickup, door)
4. Level wiring (connect the signal)
5. Later: swap the placeholder hide for an animation

---

## 1. Project settings

**Project → Project Settings → General → Layer Names → 3D Physics**

- Layer 3 → `Interactable`

(Layer 1 `SolidEnvironment` and layer 10 `RunnableWall` already exist. The `interact` action is
already bound to **E** under Input Map — nothing to add there.)

## 2. Player scene — `Scenes/Entities/Player/FPSController.tscn`

### 2a. The ray component

1. Select `MovementComponents`.
2. **Add Child Node** → search `InteractionComponent` → Create.
3. In the Inspector:
   - **Camera** → `Head/Camera3D`
   - **Interactor** → the root `CharacterBody3D`
   - **Range** → `3.0`
   - **Collision Mask** → tick **1** and **3** (layer 1 makes walls block the ray)
   - **Interact Action** → `interact`

### 2b. The HUD prompt

1. Select the root `CharacterBody3D` → **Add Child Node** → `CanvasLayer`, rename to `HUD`.
2. Select `HUD` → **Add Child Node** → search `InteractionPromptUI` → Create, rename to `InteractionPrompt`.
   - Layout → Anchors Preset → **Center**
   - Mouse → Filter → **Ignore**
3. Select `InteractionPrompt` → **Add Child Node** → `Label`, rename to `PromptLabel`.
4. Select `InteractionPrompt`, Inspector:
   - **Interaction** → `MovementComponents/InteractionComponent`
   - **Prompt Label** → the `PromptLabel` you just made

Save the scene.

## 3. Prop scenes

Put them in a new folder, e.g. `Scenes/Interactables/`.

### 3a. A pickup — `KeyPickup.tscn`

```
StaticBody3D  (root, "KeyPickup")   Collision Layer: 3 only
├── CollisionShape3D                shape: Box/Sphere
├── MeshInstance3D                  any mesh
└── PickupInteractable ("Interactable")
```

1. New Scene → **Other Node** → `StaticBody3D`, rename `KeyPickup`.
2. Collision Layer: untick 1, tick **3**. (Layer 3 only ⇒ the player walks through it, but the
   interaction ray still sees it.)
3. Add `CollisionShape3D` + a shape, add `MeshInstance3D` + a mesh.
4. Add Child Node → `PickupInteractable`, rename `Interactable`.
   - **Prompt** → `Grab key`
   - **One Shot** is already on
   - **Pickup Root** → leave empty (defaults to this scene's root)
   - **Remove Delay** → `0` for now; raise it later so a grab animation can play first
5. Save as `Scenes/Interactables/KeyPickup.tscn`.

### 3b. A door — `Door.tscn`

```
Door  (root, Node3D with Door.cs)
└── StaticBody3D                    Collision Layer: 1
    ├── CollisionShape3D
    └── MeshInstance3D
```

1. New Scene → **Other Node** → search `Door` → Create. Rename `Door`.
2. Add `StaticBody3D` under it, Collision Layer **1** (`SolidEnvironment`), with a
   `CollisionShape3D` and a `MeshInstance3D` (or instance a modular asset).
3. Select `Door` in the Inspector:
   - **Visual Root** → empty (defaults to itself)
   - **Collision Root** → empty (defaults to Visual Root)
   - **Animator** → empty for now
   - **Start Open** → off
4. Save as `Scenes/Interactables/Door.tscn`.

## 4. Wire it up in the level

In `Scenes/Levels/TestMaps/area_3d.tscn`:

1. Drag `KeyPickup.tscn` and `Door.tscn` into the scene, position them.
2. Select the pickup's `Interactable` node → **Node** dock → **Signals**.
3. Double-click `interacted(interactor: Node3D)` → pick the `Door` node → **Receiver Method**:
   `OnInteracted` → Connect.
   > `OnInteracted` takes a `Node3D` on purpose — Godot refuses to connect a signal to a method
   > that takes fewer arguments, which is why `Open()` is not the connection target.
4. Run the level (F6). Look at the key, press **E** → key disappears, door disappears.

### Making the door interactable directly instead

Add an `Interactable` node under the door's `StaticBody3D`, set **Prompt** to `Open door`, and
connect its `interacted` signal to the `Door` root's `OnInteracted`. Tick **Toggle On Interact**
on `Door` if E should open *and* close it.

## 5. Later — the real animation

1. Open `Door.tscn`, add an `AnimationPlayer` child.
2. Create animations named `open` and `close` (key rotation, position, whatever).
3. Select `Door` → **Animator** → the AnimationPlayer.

That is the whole change. `Door.Open()` plays `open` instead of hiding the mesh; nothing that
calls it needs to change.

**Known limitation:** collision is dropped the instant `Open()` is called, even when animating. If
the door should stay solid until the animation finishes, tick **Block While Open** and disable
collision from an animation track, or ask for a `CollisionFollowsAnimation` option.

---

## Gotchas

- **Nothing is detected** → check the prop's Collision Layer is inside the component's Collision
  Mask, and that the prop actually has a `CollisionShape3D`.
- **Area3D props** → `Detect Areas` is on by default; leave it on.
- **Prompt shows but E does nothing** → `Require Captured Mouse` is on, so click into the game
  window first (the mouse is released by Esc).
- The prompt only appears while `CanInteract()` is true, so used-up one-shots go quiet by design.

## Unrelated issue spotted

`project.godot` autoloads `HighLevelNetworkHandler` from `res://Scripts/Network/HighLevelNetworkHandler.cs`,
but that file does not exist on the `dev` branch (it lives on `feat/multiplayerNetwork`). Godot logs
an autoload error on every run until that is merged or the autoload entry is removed.
