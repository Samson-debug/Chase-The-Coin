# Chase The Coin — Multiplayer 2D Platformer

A real-time, two-player competitive coin-collection game built in **Unity** with **Photon Fusion 2** networking. Players race to collect coins on a shared 2D arena within a timed match; the player with the highest score when the timer expires wins.

---

## Networking Solution

**Photon Fusion 2** (Server/Host mode) is used as the networking framework.

- **Topology:** Host–Client. One player acts as the **Host** (server authority) while the other connects as a **Client**. Fusion's `AutoHostOrClient` mode is also supported for quick-play matchmaking.
- **Input Handling:** Player input is polled locally via Unity's **New Input System** and fed into Fusion through the `INetworkRunnerCallbacks.OnInput` callback using a custom `NetworkInputData` struct. Fusion handles input prediction and reconciliation.

---

## Architecture

The project follows a **manager-based architecture** with a centralized service locator pattern.

### Core Systems

| Component | Description |
|---|---|
| **`GlobalManagers`** | Singleton service locator (`DontDestroyOnLoad`). All managers register/unregister themselves via the `IManager` interface. Other systems look up dependencies through `GlobalManagers.Instance.GetManager<T>()`. |
| **`NetworkRunnerController`** | Manages the Photon Fusion `NetworkRunner` lifecycle — session creation, matchmaking, scene loading, shutdown, and all `INetworkRunnerCallbacks`. Persists across scenes. |
| **`PlayerSpawner`** | Scene-bound `NetworkBehaviour` in the Gameplay scene. On `Spawned()`, the host iterates over active players and spawns a player prefab at designated spawn points with correct input authority. |
| **`CoinManager`** | Authoritative coin spawning and recycling (see [Coin Synchronization](#coin-spawning--collection-synchronization) below). |
| **`ScoreManager`** | Networked score storage using `NetworkDictionary<PlayerRef, int>`. Clients request score changes via an RPC to the State Authority. |
| **`TimerManager`** | Drives match flow through a state machine: `WaitingForPlayers → Countdown → Playing → Finished`. Both `MatchState` and `TickTimer` are `[Networked]`, keeping all clients in sync. |
| **`UIManager`** | Networked UI controller that reads scores and timer state each frame and updates HUD text (scores, countdown timer). |

### Player Systems

| Component | Description |
|---|---|
| **`PlayerController2D`** | Handles movement and jumping using `Rigidbody2D` physics inside `FixedUpdateNetwork()`. Movement is disabled when the match state is not `Playing`. Supports boundary respawn on collision with "World Edge" layer. |
| **`PlayerInputPoller`** | Reads input from Unity's New Input System (`InputActionReference`) and injects it into Fusion's input pipeline. Only active on the local player (`HasInputAuthority`). |
| **`NetworkInputData`** | A lightweight `INetworkInput` struct carrying a `float` for horizontal movement and `NetworkButtons` for the jump action. |

### UI Systems

| Component | Description |
|---|---|
| **`MainMenuManager`** | Main menu flow: Create Room (Host), Join Room (Client), or Auto Join (AutoHostOrClient). Validates room code length before allowing creation/joining. |
| **`LoadingPanel`** | Matchmaking overlay with a cancel button. Automatically hides on successful connection, failure, or shutdown. |
| **`CountdownAnimator`** | Animated "3 → 2 → 1 → GO!" countdown using DOTween scale and fade sequences. Triggered when match state enters `Countdown`. |
| **`GameOverScreen`** | End-of-match results panel with DOTween slide-in animation. Displays win/lose/draw outcome and final scores. Includes a Home button that shuts down the network session and loads the Main Menu. |
| **`ScorePopup`** | World-space "+1" popup that floats upward and fades out on coin collection. Uses a static object pool for performance. |
| **`ButtonAnimation`** | Reusable hover/click scale animation component using DOTween pointer event handlers. |

### Scene Structure

| Scene | Purpose |
|---|---|
| **Main Menu** (index 0) | Lobby UI, room code entry, matchmaking initiation. |
| **Gameplay** (index 1) | The game arena. Loaded by the host via `NetworkRunner.LoadScene()` once both players have joined. |

### Dependency Flow

```
GlobalManagers (Singleton, DontDestroyOnLoad)
├── NetworkRunnerController  (persistent, registered in Awake)
│
├── ScoreManager             (NetworkBehaviour, registered on Spawned)
├── TimerManager             (NetworkBehaviour, registered on Spawned)
├── CoinManager              (NetworkBehaviour, registered on Spawned)
└── UIManager                (NetworkBehaviour, registered on Spawned)
```

Managers that are `NetworkBehaviour`s are spawned as part of the Gameplay scene. Late-binding is handled via `GlobalManagers.OnManagerRegistered` events or coroutine-based polling, ensuring systems that depend on each other can safely resolve references regardless of spawn order.

---

## Coin Spawning & Collection Synchronization

Coin spawning and collection are fully **host-authoritative** to prevent cheating and ensure consistency:

### Spawning
1. **Host-only spawning:** `CoinManager.SpawnCoin()` is guarded by `HasStateAuthority` — only the host spawns coins.
2. **Spawn point selection:** A random spawn point is chosen from a predefined array, excluding the previously used point and any point currently occupied by a player (via `Physics2D.OverlapCircle`).
3. **Object recycling:** A single coin `NetworkObject` is spawned once and reused. After collection, the coin is teleported off-screen (`y = -1000`) during the respawn delay, then teleported to the new spawn point via `NetworkTransform.Teleport()`. This avoids repeated Spawn/Despawn overhead.
4. **Timed respawn:** After collection, a `TickTimer` (networked) enforces a configurable delay before the coin reappears. The timer is evaluated in `FixedUpdateNetwork()`, which runs on the host's simulation tick.

### Collection
1. **Host-side detection:** The `Coin` script checks for player overlap in `FixedUpdateNetwork()` (host only) using `Physics2D.OverlapCircle` with a player layer mask.
2. **State flag:** A `[Networked] NetworkBool _isCollected` prevents double-collection. It is set to `true` on the host and automatically replicated to clients.
3. **Score update:** On collection, `ScoreManager.AddScore()` is called. If the caller is not the State Authority, an RPC (`Rpc_AddScore`) forwards the request to the host, which updates the `NetworkDictionary<PlayerRef, int>`. The updated scores are then automatically replicated to all clients.
4. **Visual feedback:** An `[Rpc(StateAuthority → All)]` call (`RpcShowScorePopup`) triggers a local "+1" popup animation on all clients at the coin's world position.

---

## Assumptions

- The game uses a **flat 2D arena** with predefined spawn points placed in the scene. Level design is static and not procedurally generated.
- **Room codes** must be at least 4 characters long (enforced by `MainMenuManager`). Auto Join bypasses room codes entirely by using Fusion's default session matchmaking.
- The host is assumed to remain connected for the full duration of the match. **Host migration** is not implemented.

---

## Known Issues & Limitations

- **Manager initialization order dependency:** Because networked managers (`ScoreManager`, `TimerManager`, `CoinManager`, `UIManager`) are `NetworkBehaviour`s, their `Spawned()` order is non-deterministic. Systems that depend on each other must use workarounds — `OnManagerRegistered` event subscriptions or coroutine-based polling — to safely resolve references.
- **No host migration:** If the host disconnects mid-match, the client will be disconnected and the match is lost. There is no fallback or session recovery.
- **Player left handling:** `PlayerSpawner` implements `IPlayerLeft` to despawn the leaving player's character, but the remaining player has no UI notification or automatic match-end when the opponent leaves.
- **Static spawn points:** Coin and player spawn points are fixed in the scene and not configurable at runtime.

---

## Bonus Features

- **DOTween UI Animations:**
  - Animated **countdown sequence** ("3 → 2 → 1 → GO!") with scale punch and fade effects (`CountdownAnimator`).
  - **Game-over panel** slides in from off-screen with `Ease.OutBack` and a background fade overlay (`GameOverScreen`).
  - **Score popup** (+1) floats upward and fades out at the coin's world position, using a **static object pool** for zero-allocation reuse (`ScorePopup`).
  - **UI micro-animations** with scale tweens (`ButtonAnimation`).

- **Smart Coin Spawning:**
  - Avoids spawning at the same location consecutively.
  - Avoids spawning on top of a player by checking for colliders at spawn points.
  - Single coin object is recycled (teleported) instead of spawned/despawned each time, reducing network overhead.

- **Manager Service Locator Pattern:**
  - Clean dependency injection via `GlobalManagers` with an `IManager` interface.
  - Late-binding support through `OnManagerRegistered` events, allowing NetworkBehaviour managers to resolve each other regardless of spawn order.
