# Multiplayer — state, decisions, and next steps

Working notes for the networking side of the project. Last updated 2026-07-30.

---

## Architecture decisions

**Listen server + client authority.** The host is a normal player that also acts as
the network coordinator. Each player node's authority belongs to *its own peer*, not
to the server (`FpsController._EnterTree` reads the peer ID out of the node name).
The server currently only spawns/despawns players and relays packets — it does not
validate movement.

This is deliberate. A movement-heavy parkour game feels best with zero input latency,
and client authority gives that for free. The cost is that a client can teleport.
Acceptable for co-op and racing friends; would need revisiting for public leaderboards
(validate *run times* server-side rather than every movement frame).

**Two independent axes**, easy to conflate:

|                       | Client authority        | Server authority             |
| --------------------- | ----------------------- | ---------------------------- |
| **Listen server**     | ← current               | host arbitrates and plays    |
| **Dedicated server**  | cheap VPS, still cheatable | full authoritative        |

Moving *down* a row is a deployment change and is cheap. Moving *right* a column is
an architecture rewrite — the server would have to re-simulate wall-run, slide, and
climb from raw inputs, and clients would need prediction + reconciliation.

**Transport plan: ENet now, Steam next, VPS only if needed.** All transport setup is
confined to `HighLevelNetworkHandler` so the peer can be swapped without touching the
spawner, synchronizers, or player code. **Keep the ENet path forever** as the local
dev transport — you can't easily run two Steam accounts on one machine, which would
otherwise break the "Run Multiple Instances" workflow.

---

## What has been done

### Autoload network handler

`HighLevelNetworkHandler` is registered as an autoload (`project.godot` →
`[autoload]`) so it lives on `/root` and survives scene changes.

Previously it was created at runtime as a child of the `HighLevelUI` Control node,
which meant `ReloadCurrentScene()` freed it and dropped every connection. A
connection's lifetime is the *session*, not the scene.

Exposes:

- `static Instance` — set in `_EnterTree` so it is available before any scene node's
  `_Ready`. Compile-checked alternative to a hardcoded `/root/...` node path.
- `IsNetworkActive` — checks our own `peer` field, **not**
  `Multiplayer.MultiplayerPeer`. Godot installs an `OfflineMultiplayerPeer` by
  default that reports itself as connected with unique ID 1.
- `ServerStarted` event — fired after the peer is assigned in `StartServer()`.

### Host player spawn + despawn

`HighLevelMultiplayerSpawner` now handles the full player lifecycle.

`Multiplayer.PeerConnected` never fires for the host, so previously the server had no
player of its own. Symptom: Godot auto-promotes the only `Camera3D` in a viewport to
current, so the **server window rendered through the client's camera** and appeared to
follow the client around.

The host spawn hangs off the handler's `ServerStarted` event rather than a
`Multiplayer.IsServer()` check in `_Ready`, because at scene-load time the
`OfflineMultiplayerPeer` makes `IsServer()` return true before any role is chosen.

`PeerDisconnected` → `QueueFree()` on the server, which propagates removal to all
clients automatically. Previously disconnected players stayed as frozen capsules.

Subscriptions are released in `_ExitTree`. The handler outlives every scene, so a
C# `event` subscription left behind would hold a delegate pointing at a freed Godot
object and throw `ObjectDisposedException` on the next fire.

### Restart reworked into respawn

`RestartLevelComponent` previously called `GetTree().ReloadCurrentScene()`. That is
incoherent in multiplayer — one player cannot rebuild the shared world, and it
discards everyone's synced state.

Now `ResetRun()` zeroes velocity and restores the transform captured at `_Ready`.
Gated on `IsMultiplayerAuthority()` because every peer holds a copy of every player
node and each copy receives input events.

Emits a `RunReset` signal. **A run timer should subscribe to this**, not to the
`restart` action directly, so future reset sources (out-of-bounds, reset volumes,
checkpoints) restart the clock for free. With the old scene reload, timer reset was
an implicit side effect of the scene being destroyed; it now has to be explicit.
Per-player timers belong on the player scene, not the level.

### Menu suppression after scene reload

`HighLevelUi._Ready` hides itself when `IsNetworkActive`, so a scene reload does not
re-prompt for a role that was already chosen.

---

## Known gaps — next up

Ordered smallest first. Steps 1c and 1d were the immediate next items.

1. **1c — Spawn points.** Every player spawns at the origin, stacked inside each
   other. Needs spawn point nodes and round-robin assignment in the spawner.
2. **1d — Replicate rotation.** `FPSController.tscn` replicates `position` only, so
   remote players slide around permanently facing the same direction. Needs body yaw;
   head pitch optional. Edit the `SceneReplicationConfig`.
3. **Interpolation.** Remote transforms are applied by snapping. Invisible at 0 ms,
   visibly choppy over real latency. Needs a buffer-and-lerp component on non-authority
   copies.
4. **Connection failure handling.** `ConnectionFailed`, `ServerDisconnected`, and
   `ConnectedToServer` are still unhandled. On localhost connects always succeed
   instantly; over the internet these fire constantly and the UI currently just hangs
   hidden.
5. **Address/port entry + CLI args.** `IP_ADDRESS`/`PORT` are hardcoded constants.
   Needs a UI field plus `--server` / `--connect=<ip>` for headless runs.
6. **ENet compression.** One line, must match on both ends:
   `peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder)`.
7. **Camera pitch on respawn.** `ResetRun()` restores body yaw but not camera pitch.
8. **Scene reload still destroys player nodes.** The connection survives now, but the
   players do not, and only newly-connecting peers get spawned. Rare since R no longer
   reloads, but re-spawning everyone on scene load is unhandled.

---

## Roadmap

### Phase A — playable with a friend (mostly done)

1. ~~Fix the bugs localhost hides~~ — in progress, see gaps above
2. Verify locally: **Debug → Run Multiple Instances → 2**
3. Verify under latency: [clumsy](https://jagt.github.io/clumsy/), filter
   `udp and outbound and port 42069`, 80 ms lag + 2% loss
4. Play with a real person: **Tailscale** (5 min, no router config, works behind
   CGNAT) or port-forward UDP 42069

### Phase B — Steam

5. Steam client running; `steam_appid.txt` containing `480` (Spacewar, the shared
   test app) next to the Godot editor exe and next to exported builds. Own AppID
   costs $100 Steam Direct, only needed to ship.
6. **Download the prebuilt GodotSteam GDExtension** — do *not* compile. The
   `godotsteam.com/howto/*` pages are for the module build, which needs SCons plus
   MSVC (~6 GB) plus the Steamworks SDK, and takes most of a day to produce a binary
   that is already published. Prebuilt zip drops into the project root; works on
   Godot 4.4+. Sources: [Codeberg releases](https://codeberg.org/godotsteam/godotsteam/releases),
   [Asset Library](https://godotengine.org/asset-library/asset/2445).
   The retired standalone `multiplayerpeer` repo is now folded into main GodotSteam,
   so one zip provides both the Steam API and `SteamMultiplayerPeer`.
7. **Spike before building on it.** Init Steam, print own SteamID, instantiate
   `SteamMultiplayerPeer` via `ClassDB.Instantiate`, cast to `MultiplayerPeer`. The
   C# story is the rough edge here — most bindings and tutorials are GDScript-first.
   Fallbacks if it fights: [craethke/steam-multiplayer-peer-csharp](https://github.com/craethke/steam-multiplayer-peer-csharp)
   (pure C#, but channels unimplemented and depends on GodotSteam C# bindings that
   lag 4.11+), or Steamworks.NET / Facepunch with a hand-written peer.
8. Lobby create/join/invite, host SteamID → `CreateClient`. Slots into the existing
   handler seam.

### Phase C — only if needed

- Dedicated server on a VPS. Godot has a dedicated-server export mode + `--headless`;
  ~$5/mo. Fixes host advantage and host migration. Does **not** require going
  server-authoritative.
- Server-authoritative movement with prediction and reconciliation. Only for
  competitive leaderboards. [netfox](https://github.com/foxssake/netfox) is the
  standard Godot 4 answer (rollback, tick interpolation, Noray relay), but it is
  GDScript, so driving it from C# means some `Call()` interop.

---

## Environment notes

- Godot 4.7, .NET / C#, Jolt Physics
- Python 3.14.4 and pip present; **SCons and a C++ compiler are not installed** —
  irrelevant unless the prebuilt GDExtension route fails
- Build check: `dotnet build testing.sln`

## Reference

Godot's docs cover Steam not at all — everything there is third-party.

- [High-level multiplayer](https://docs.godotengine.org/en/stable/tutorials/networking/high_level_multiplayer.html)
  (has C# tabs). Relevant sections: Managing connections, Hosting considerations,
  Remote procedure calls, Secure multiplayer design.
- [Exporting for dedicated servers](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_dedicated_servers.html)
- No tutorial exists for `MultiplayerSpawner` / `MultiplayerSynchronizer` — class
  reference only:
  [Synchronizer](https://docs.godotengine.org/en/stable/classes/class_multiplayersynchronizer.html),
  [Spawner](https://docs.godotengine.org/en/stable/classes/class_multiplayerspawner.html),
  [SceneMultiplayer](https://docs.godotengine.org/en/stable/classes/class_scenemultiplayer.html)
- [GodotSteam lobbies](https://godotsteam.com/tutorials/lobbies/) — read this before
  the MultiplayerPeer page; getting a host SteamID is the genuinely new part.
