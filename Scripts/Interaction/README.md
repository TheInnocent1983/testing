# Interaction System

> **What this folder is:** the code that lets the player look at something in the world, press
> **E**, and have that something react — a key you grab, a door that opens, a lever you pull.
>
> **Where the editor setup steps live:** [`/INTERACTION.md`](../../INTERACTION.md) at the project
> root. This file explains the *thinking*; that file is the click-by-click checklist.

---

## 1. Why this exists

A parkour level is not only geometry you run across. At some point you need a goal object, a door
that blocks a route, a button that changes the map. The naive way to build that is one script per
puzzle:

```csharp
// The way we are NOT doing it
public partial class GoldKey : Area3D
{
    [Export] private MeshInstance3D _theRedDoor;

    public override void _Process(double delta)
    {
        if (PlayerIsNear() && Input.IsActionJustPressed("interact"))
        {
            _theRedDoor.Visible = false;   // hard-coded to one specific door
            QueueFree();
        }
    }
}
```

That works exactly once. The second key needs a copy of the file, the door cannot be opened by a
lever as well, the raycast/input logic is duplicated in every prop, and none of it can show a HUD
prompt without every prop knowing about the HUD.

This folder replaces that with **one system, three separable roles**:

| Role | Question it answers | Lives on |
| --- | --- | --- |
| **Sensor** | *What am I looking at, and can I use it?* | The player |
| **Interactable** | *I am usable. Someone just used me.* | The prop |
| **Reactor** | *Something happened, so I do my thing.* | Whatever should change |

The player never knows what a door is. The key never knows what a door is. The door never knows
what a key is. They are introduced to each other in the editor, with a signal.

---

## 2. The pieces

```
Scripts/Interaction/
├── IInteractable.cs          the contract
├── Interactable.cs           THE BASE — put it on any prop
├── PickupInteractable.cs     a specialisation of the base
├── Door.cs                   a reactor
├── InteractionComponent.cs   the player's sensor
├── InteractionPromptUI.cs    the HUD hint
└── InteractionUtil.cs        shared helper
```

### `IInteractable.cs` — the contract

A C# interface with three members:

```csharp
string Prompt { get; }                      // "Grab key"
bool CanInteract(Node3D interactor);        // is it usable right now?
void Interact(Node3D interactor);           // do it
```

This is the *only* thing the player's sensor knows about. Anything in the game that implements
these three members is interactable, whatever else it may be. That is what keeps the player script
from growing a list of special cases.

### `Interactable.cs` — the base you will use most

A plain `Node` that you add **as a child of a collision body** (`StaticBody3D`, `Area3D`,
`RigidBody3D`). It follows the same pattern as the player's movement code: small components hanging
off a body, not one giant class.

```
StaticBody3D              ← what the ray physically hits
├── CollisionShape3D
├── MeshInstance3D
└── Interactable          ← "this body is usable"
```

Exports: `Prompt`, `Enabled`, `OneShot`, `Cooldown`.
Signal: `interacted(interactor)`.

For most props you write **no code at all** — you drop this node in, type a prompt, and connect its
signal to whatever should react.

### `PickupInteractable.cs` — a specialisation

Subclasses `Interactable` and overrides one method:

```csharp
protected override void OnInteract(Node3D interactor)
{
    // hide the prop, disable its collision, emit picked_up
}
```

That is the extension point. `OnInteract` is called by the base *after* it has already checked
`Enabled`, `OneShot` and `Cooldown`, and the base still emits `interacted` afterwards. So a
specialisation only ever describes the behaviour unique to it.

Exports: `PickupRoot`, `RemoveDelay`, `FreeAfterPickup`.

It hides rather than frees by default, because a freed node cannot run the animation or signal you
will want later.

### `Door.cs` — a reactor

Not interactable. It exposes methods anyone can call:

```csharp
door.Open();
door.Close();
door.Toggle();
door.OnInteracted(interactor);   // signal-shaped version, see §5
```

Right now "open" means *hide the visual and disable the collision*. Assign an `AnimationPlayer` to
its **Animator** export and it plays the `open` / `close` animation instead — no caller changes.
That is the whole point of keeping the reactor separate: the placeholder and the finished version
have the same interface.

### `InteractionComponent.cs` — the player's sensor

Sits under `MovementComponents` with the other player components. Every physics frame it:

1. Casts a ray from the camera, forward, `Range` metres.
2. Resolves whatever it hit into an `IInteractable` (or nothing).
3. Emits `focus_changed(interactable, prompt)` **only when the focus actually changes**.

And on the `interact` action it calls `Interact()` on whatever is focused.

Exports: `Camera`, `Interactor`, `Range`, `CollisionMask`, `DetectAreas`, `SearchDepth`,
`InteractAction`, `RequireCapturedMouse`.

### `InteractionPromptUI.cs` — the HUD

Listens to `focus_changed`, shows/hides a label reading `[E] Grab key`. The key name is read from
the `InputMap`, so it stays correct after the player rebinds `interact` in the settings menu.

### `InteractionUtil.cs` — the helper

One static method, `SetCollisionEnabled(root, enabled)`. It walks every `CollisionShape3D` under a
node and toggles it **deferred** — Godot forbids enabling or disabling shapes from inside a physics
callback, and an interaction happens inside one. Both the pickup and the door need this, so it is
shared rather than copy-pasted.

---

## 3. How a frame actually flows

```
                    ┌─────────────────────────────┐
   every physics    │   InteractionComponent      │
   frame            │   (child of the player)     │
                    └──────────────┬──────────────┘
                                   │ 1. ray from camera, Range metres
                                   ▼
                          ┌─────────────────┐
                          │  hit a collider │
                          └────────┬────────┘
                                   │ 2. Resolve(): is there an
                                   │    Interactable on it or near it?
                                   ▼
                       ┌───────────────────────┐
                       │  Interactable found   │
                       │  CanInteract() == true│
                       └───────────┬───────────┘
                                   │ 3. focus changed?
                                   ▼
                     emit focus_changed(node, "Grab key")
                                   │
                                   ▼
                       InteractionPromptUI shows "[E] Grab key"


   player presses E
                                   │
                                   ▼
                    Interactable.Interact(player)
                                   │
                  ┌────────────────┴────────────────┐
                  ▼                                 ▼
        OnInteract() — the prop's        emit interacted(player)
        own behaviour (hide itself)                 │
                                                    ▼
                                      Door.OnInteracted() → Open()
```

### Why a code raycast instead of a `RayCast3D` node

The player scene already has `RayCast3D` nodes for wall-running. Interaction uses
`PhysicsRayQueryParameters3D` in code instead, for two reasons: the ray must follow the camera's
pitch (which is rotated separately from the body), and it needs to exclude the player's own
collider, which is fiddlier to keep correct on a node.

### Why the collision mask defaults to `0b101` (layers 1 + 3)

Layer 3 is where interactables live. Layer 1 is `SolidEnvironment` — it is in the mask **on
purpose**, so that a wall between you and a key stops the ray. Without layer 1 you could grab
things through walls.

### Why `Resolve()` searches around the hit node

The ray reports a *collider*, but the `Interactable` is a child component of that collider — and
sometimes the collider is itself a child of the prop's root. So `Resolve()` checks the hit node,
then its direct children, then walks up to `SearchDepth` parents doing the same. The depth limit
exists so it cannot wander up into the level root and find an unrelated prop's component.

### Why `CanInteract()` is checked during focus, not only on press

A prompt that appears for something you cannot use is a lie. A used-up one-shot key stops showing
its prompt automatically, with no extra bookkeeping in the HUD.

---

## 4. Building a new interactable

**Nine times out of ten, no code.** Add an `Interactable` node under the prop's collision body, set
its `Prompt`, connect `interacted` to something.

**When the prop needs its own behaviour**, subclass the base:

```csharp
using Godot;

namespace Parkour.Interaction;

[GlobalClass]
public partial class LeverInteractable : Interactable
{
	[ExportGroup("Lever")]
	[Export] public float ResetAfter { get; set; } = 2.0f;

	protected override void OnInteract(Node3D interactor)
	{
		// Only what makes a lever a lever. Enabled / OneShot / Cooldown / the
		// interacted signal are all handled by the base class.
		GD.Print($"{interactor.Name} pulled the lever");
	}
}
```

`[GlobalClass]` is what makes it appear by name in Godot's **Add Child Node** dialog. Build once
(`dotnet build testing.sln`) before looking for it there.

**When something needs to react**, give it a public method and connect a signal to it. It does not
need to be in this folder and does not need to know the interaction system exists.

---

## 5. Two rules that will bite you

**Signal arity.** Godot refuses to connect a signal to a method that takes *fewer* parameters than
the signal emits. `interacted` emits one argument, so its target must accept one. That is the only
reason `Door.OnInteracted(Node3D)` exists next to the plain `Door.Open()` — `Open()` is for calling
from code, `OnInteracted` is for connecting in the editor.

**Deferred collision.** Never set `CollisionShape3D.Disabled` directly during an interaction. Use
`InteractionUtil.SetCollisionEnabled` — Godot is inside a physics step and will complain.

---

## 6. Multiplayer note

This is currently **local only**: the interaction runs on the machine of whoever pressed E. That is
consistent with the project's deliberate client-authority movement model, but interactions are
world state rather than personal state, so they will eventually need to be authoritative.

The seam is already in one place: `InteractionComponent.TryInteract()` is the single call site that
turns an input into an interaction. Routing that through an RPC — client asks, host validates and
calls `Interact()` on every peer — touches that method and nothing else. No prop script changes.
