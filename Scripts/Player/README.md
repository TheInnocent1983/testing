# Player & Movement System

> **What this folder is:** everything that turns key presses into a first-person parkour
> character — walking, sprinting, jumping, crouching, sliding, wall-running, wall-climbing, the
> camera, and a noclip debug mode.
>
> **Scene it drives:** `Scenes/Entities/Player/FPSController.tscn`

---

## 1. Why it is built this way

A first-person controller grows fast. Walking needs friction, jumping needs gravity, sliding needs
slope maths, wall-running needs raycasts and camera tilt. Written as one class it becomes a
2000-line file where changing the slide breaks the crouch, because everything shares the same
`Velocity` and the same `if` ladder.

So the character is split into **one body plus many small components**:

```
CharacterBody3D  (FpsController)      ← owns Velocity, calls MoveAndSlide() once
└── MovementComponents/
    ├── CameraController               ← look, headbob, roll
    ├── GroundMovementComponent        ← walk / sprint / crouch / slide states
    ├── AirMovementComponent           ← gravity + air strafing
    ├── WallRunComponent               ← run along walls
    ├── WallClimbComponent             ← climb up walls
    ├── SlideMovementComponent         ← slide momentum & slopes
    ├── CrouchComponent                ← capsule + head height
    ├── NoclipComponent                ← debug fly
    └── RestartLevelComponent          ← R to reload
```

Every component is a plain `Node` with `[Export]` tuning values. None of them inherit from each
other. They are handed the player as an argument (`UpdateGroundPhysics(FpsController player, ...)`)
rather than reaching for it, which is why they can be read one at a time.

**The rule that keeps this sane:** components *read and modify* `player.Velocity`, but only
`FpsController` calls `MoveAndSlide()`, and only once, at the very end of the frame. Nothing else
moves the character. (Noclip is the one exception — it writes `GlobalPosition` directly, which is
exactly why it has to bypass everything else.)

---

## 2. The pieces

| File | What it decides |
| --- | --- |
| `FpsController.cs` | The body. Owns velocity, reads the move stick, runs the priority chain, jumps, applies stance. |
| `CameraController.cs` | Mouse/stick look, FOV, headbob, camera roll. The only thing that touches camera rotation. |
| `GroundMovementComponent.cs` | The Stand/Crouch/Slide state machine, plus ground friction & acceleration. |
| `AirMovementComponent.cs` | Gravity and air strafing. |
| `WallRunComponent.cs` | Attaching to a side wall, running along it, wall-jumping off it. |
| `WallClimbComponent.cs` | Attaching to a wall in front, climbing up it, jumping off. |
| `SlideMovementComponent.cs` | Slide momentum, slope acceleration, uphill penalty. |
| `CrouchComponent.cs` | Capsule height, head height, ceiling check. |
| `NoclipComponent.cs` | Debug fly — wipes collision and moves by position. |
| `RestartLevelComponent.cs` | `R` reloads the scene. |

### `FpsController.cs` — the body

Three things live here and nowhere else:

- **`WishDir`** — the direction the player is asking to go, in world space:
  ```csharp
  InputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backwards").Normalized();
  WishDir  = GlobalTransform.Basis * new Vector3(InputDir.X, 0.0f, InputDir.Y);
  ```
  Computed once per frame so every component agrees on it. Note it uses the **body's** basis, not
  the camera's — pitch does not tilt your movement.
- **`ApplyStance(headYOffset, capsuleHeight, lerpSpeed, delta)`** — the single place the capsule and
  head are resized. Crouch, slide and stand all route through it, so they can never fight.
- **The priority chain** in `_PhysicsProcess` (see §3).

Also worth knowing: pitch is applied to the **camera**, yaw to the **body** (`CameraController.HandleMouseLook`).
That is why `WishDir` stays horizontal for free and why camera roll can be layered on top.

### `CameraController.cs`

`ApplyRoll(targetRoll, delta)` is called *every frame by whoever is in charge* — ground movement
passes `0`, sliding passes `-8°`, wall-running passes `±15°`. Nobody has to "reset" the roll,
because the current owner of the frame always states what it should be. Same idea as the stance.

The lerp everywhere uses the frame-rate-independent form:

```csharp
float blend = 1.0f - Mathf.Pow(0.5f, delta * lerpSpeed);
```

which means "close half the remaining distance every `1/lerpSpeed` seconds" and behaves identically
at 60 and 144 fps, unlike a raw `Lerp(a, b, speed * delta)`.

---

## 3. How one physics frame flows

This is the heart of the system. `FpsController._PhysicsProcess`:

```
                    read input → WishDir
                            │
                            ▼
                    ┌───────────────┐
                    │ Noclip on?    │──yes──▶ move by position, Velocity = 0
                    └───────┬───────┘
                            │ no
                            ▼
                    ┌───────────────┐
              ┌─────│ IsOnFloor()?  │─────┐
         yes  │     └───────────────┘     │  no
              ▼                           ▼
   GroundMovementComponent        WallClimbComponent.TryWallClimb()
   (Stand / Crouch / Slide)                │
              │                            ▼
              ▼                   WallRunComponent.TryWallRun()
      jump pressed? → Velocity.Y            │
                                            ▼
                                   wall-running? ──no──▶ AirMovementComponent
                            │
                            ▼
                      MoveAndSlide()   ← once, always, for every branch
```

**Read it as a priority list, not a state machine.** Each layer gets a chance to claim the frame:

1. **Noclip wins outright.** It returns `true` from `_HandleNoclip` and everything else is skipped.
2. **On the floor** → ground movement, then the jump check. Jump is checked *after* friction has
   already been applied this frame.
3. **In the air** → wall-climb is offered the frame, then wall-run. `TryWallRun` returns `true` if
   it took over; only if it did **not** does air physics (gravity + strafing) run.

The `Try…` naming is the convention: *"here is a frame, take it if you can, tell me whether you
did."*

---

## 4. The movement model — Quake-style, and why

The acceleration code is not "velocity = direction × speed". It is the Quake/Source model, and the
difference is the whole feel of the game.

### Ground acceleration (`GroundMovementComponent.Accelerate`)

```csharp
float currentSpeedInWishDir = velocity.Dot(wishDir);      // how fast am I ALREADY going that way?
float addSpeedTillCap = targetSpeed - currentSpeedInWishDir;
if (addSpeedTillCap <= 0.0f) return;                      // already at the cap in that direction

float accelSpeed = Mathf.Min(GroundAcceleration * targetSpeed * delta, addSpeedTillCap);
velocity += accelSpeed * wishDir;
```

The cap applies to the **projection of velocity onto `wishDir`**, not to total speed. That single
detail is what makes bunny-hopping and air-strafing possible: if you turn while moving, your
existing velocity is no longer aligned with `wishDir`, the projection drops, the cap is no longer
reached, and you get to add speed again.

### Ground friction (`ApplyGroundFriction`)

```csharp
float control  = Mathf.Max(currentSpeed, GroundDeceleration);  // floor, so slow speeds still stop
float drop     = control * friction * delta;
velocity *= Mathf.Max(currentSpeed - drop, 0.0f) / currentSpeed;
```

Proportional drag with a minimum rate, so you glide when fast but do not creep forever when slow.
It scales the whole vector including Y — harmless, because it only runs while grounded where Y is
effectively zero.

### Air control (`AirMovementComponent`)

Same shape, radically different numbers:

| | Ground | Air |
| --- | --- | --- |
| Cap | `7` walk / `11` sprint | `AirCap = 1.0` |
| Acceleration | `14` | `AirAcceleration = 150` |

A cap of **1 m/s** with acceleration of **150** means: you can barely add speed in the direction
you already move, but you can add it *very fast* in a new direction. Strafe + turn = you keep
almost all your speed and redirect it. That is air-strafing, and it is entirely a consequence of
those two numbers.

Gravity comes from Project Settings (`physics/3d/default_gravity`) and is applied **only here** —
which is why wall-run and wall-climb can substitute their own, much gentler, gravity.

### Bunny-hopping

```csharp
if (Input.IsActionJustPressed("jump") || (AutoBunnyHop && Input.IsActionPressed("jump")))
```

`AutoBunnyHop` (on by default on the player scene, **off** in the test level) means holding space
re-jumps the instant you land, so ground friction only gets one frame to bite. Combined with the
projection cap above, that is how speed accumulates.

---

## 5. The ground state machine

`GroundMovementComponent` holds `PlayerState { Stand, Crouch, Slide }`:

```
        slide/crouch pressed AND fast enough (≥ MinSlideStartSpeed = 8)
   Stand ──────────────────────────────────────────▶ Slide
     │  ▲                                              │ SlideTimer ≤ 0
     │  │ released & no ceiling                        ▼
     └──┴─────────────────────────────────────────── Crouch
        slide/crouch pressed but too slow
```

- **Slide requires speed.** Under `MinSlideStartSpeed` you just crouch. Sliding is a reward for
  carrying momentum, not a free crouch.
- **Ceiling check keeps you down.** `CrouchComponent.IsCeilingBlocked()` raycasts up; while it hits,
  Stand is refused, so you cannot grow into geometry.
- **Slopes matter.** `SlideMovementComponent` reads `GetFloorAngle()` and `GetFloorNormal()`:
  downhill past 8° *adds* momentum and *refills* the timer (so a long hill is a long slide), flat or
  uphill drains both, uphill costing an extra `UphillPenalty`.

`SlideDir` is captured once on entry and then projected onto the floor plane each frame — you commit
to a direction when you start sliding and steer only slightly, which is what makes it feel like
sliding rather than crouch-walking.

---

## 6. Wall-running vs wall-climbing

Two components with an almost identical shape — worth comparing side by side:

| | `WallRunComponent` | `WallClimbComponent` |
| --- | --- | --- |
| Rays | `WallRayLeft` / `WallRayRight` (±0.8 sideways) | `WallRayForward` (1.5 up, 1.2 forward) |
| Starts when | airborne, moving ≥ `MinWallRunSpeed`, not rising fast | holding `move_forward` and facing the wall (`dot ≥ 0.5`) |
| Movement | along the wall at held momentum | straight up, fading with fatigue |
| Ends after | `WallRunTime` = 1.4 s | `ClimbTime` = 1.6 s |
| Camera | rolls ±`WallRunTiltDegrees` | none |

Both use the same three tricks:

- **Stick force.** `- wallNormal * WallStickForce * delta` pulls you gently into the wall so a bumpy
  surface does not peel you off.
- **Fatigue.** Gravity ramps up over the duration (`Lerp(g, g*4, timer/time)`), so you sink slowly
  then fall — the timer expiring never feels abrupt.
- **`_blockedNormal` + `ReattachCooldown`.** After jumping off or exhausting a wall, that exact
  surface normal is remembered and refused until you touch a *different* wall or land. Without it
  you could climb one wall forever by re-grabbing it.

---

## 7. Tuning cheat-sheet

Everything is an `[Export]`, so it is all live in the Inspector on the player scene.

| Want to change | Component | Property |
| --- | --- | --- |
| Walk / sprint speed | Ground | `WalkSpeed`, `SprintSpeed` |
| How sticky the ground feels | Ground | `GroundFriction`, `GroundDeceleration` |
| Air-strafe strength | Air | `AirCap` (small!), `AirAcceleration` (large) |
| Jump height | `FpsController` | `JumpVelocity` |
| Hold-space-to-hop | `FpsController` | `AutoBunnyHop` |
| Slide length / speed | Slide | `SlideTimerMax`, `MinSlideStartSpeed`, `MaxSlideSpeed` |
| Slide on hills | Slide | `SlideSlopeAccel`, `UphillPenalty` |
| Crouch height | Crouch | `CrouchDepth`, `StandCapsuleHeight` |
| Wall-run duration | WallRun | `WallRunTime`, `WallRunGravity` |
| Wall-run camera tilt | WallRun | `WallRunTiltDegrees` |
| Which walls are runnable | WallRun | `WallCollisionMask` (see §9) |
| Look sensitivity | Camera | `LookSensitivity`, `ControllerLookSensitivity` |
| FOV | Camera | `FieldOfView` |
| Headbob | Camera | `HeadbobMoveAmount`, `HeadbobFrequency` |

---

## 8. Scene wiring

Components are connected by **exported NodePaths, assigned in the Inspector** — not by
`GetNode("../../Something")` scattered through the code. `FPSController.tscn` sets:

```
FpsController.CameraComp   → MovementComponents/CameraController
FpsController.GroundComp   → MovementComponents/GroundMovementComponent
...                                       (and so on for all nine)
FpsController.BodyCollision → CollisionShape3D
FpsController.HeadNode      → Head
WallRunComponent.WallRayLeft/Right → WallRayLeft / WallRayRight
WallClimbComponent.WallRayForward  → WallRayForward
CrouchComponent.CeilingCheck       → CeilingCheck
```

Most components also have a `?? GetNodeOrNull("%Something")` fallback in `_Ready`, so a forgotten
assignment degrades instead of crashing. The `%` prefix needs **Access as Unique Name** ticked on
the target node (`Head`, `Camera3D`, `WorldModel` have it).

`_Ready` also pushes the player's own mesh onto visual layer 2 and off layer 1, while the camera's
`cull_mask` excludes layer 2 — that is the standard first-person trick so your body casts shadows
and is visible to others without appearing in your own view.

**Input actions used:** `move_forward`, `move_backwards`, `move_left`, `move_right`, `jump`,
`sprint`, `crouch_toggle`, `slide`, `noclip`, `restart`, `look_*`. All rebindable through the
settings menu.

---

## 9. Known rough edges

Documented because they are real and someone will hit them, not as a to-do list.

**`WallRunComponent.WallCollisionMask` defaults to `2` — layer 2, which nothing uses.**
`_Ready()` overwrites both wall rays' masks with this value, so whatever you set on the RayCast3D
nodes in the editor is discarded. Meanwhile the comment above it says *"Layer 10 bitvalue is 512"*,
`project.godot` names layer 10 `RunnableWall`, and the walls in `area_3d.tscn` are on
`collision_layer = 513` (layers 1 + 10). Mask `2` matches none of those. If wall-running is not
attaching, set this to layer 10 (and/or layer 1) in the Inspector first.

**`WallClimbComponent`'s return value is discarded.** In `FpsController._PhysicsProcess`:

```csharp
bool wallClimbing = WallClimbComp != null && WallClimbComp.TryWallClimb(this, (float)delta);
bool wallRunning  = WallRunComp  != null && WallRunComp.TryWallRun(this, (float)delta);
if (!wallRunning)
    AirComp?.UpdateAirPhysics(this, (float)delta);
```

`wallClimbing` is assigned and never read, so full air gravity is applied *on top of* wall-climbing
unless a wall-run is also active. Every other layer in the chain honours its `Try…` result.

**The crouch toggle is tracked twice.** `GroundMovementComponent._isCrouchToggled` and
`CrouchComponent._isCrouchToggled` are separate fields, both flipped by `crouch_toggle`. But
`CrouchComponent.UpdateCrouch` only runs while the state machine is already in `Crouch`, so the two
copies can drift apart. `GroundMovementComponent` is the one that actually decides the state;
`CrouchComponent`'s own input reading is effectively redundant.

**`FpsController.AutoNoclip` is dead.** It is never read — `NoclipComponent.AutoNoclip` is the real
flag. Two exports with the same name in the same Inspector, only one of which does anything.

**`NoclipComponent` looks up `%CollisionShape3D`, which does not exist.** The node is named
`CollisionShape3D` but does not have *Access as Unique Name* ticked, so that lookup returns null and
the shape is never disabled. Noclip still works, because wiping `CollisionLayer`/`CollisionMask` to
`0` is enough on its own.

**Two files have no namespace.** `SlideMovementComponent.cs` and `RestartLevelComponent.cs` sit in
the global namespace while the other eight are `Parkour.Movement`.

**Indentation is mixed.** Tabs in most files, 4 spaces in `CameraController.cs`,
`CrouchComponent.cs` and `GroundMovementComponent.cs` — and `UpdateStateTransitions` (line 49) has
its braces at the wrong indent level entirely. Tabs are the majority convention; match that in new
files.
