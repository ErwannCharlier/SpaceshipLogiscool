# Flying Nugget Unity Client

This Unity project contains a simple 3D spaceship shooter client for a separate Python WebSocket server. The Unity side only sends and receives small JSON messages.

## Open the Project

1. Open Unity Hub.
2. Choose **Add project from disk**.
3. Select this folder: `C:\dev\FlyingNugget`.
4. Open it with Unity `2023.2.19f1` or a compatible Unity 2023 version.

## Scene Setup

Create a scene named `SpaceArena` or use the current sample scene while testing.

Add these GameObjects:

1. `NetworkClient`
   - Create an empty GameObject named `NetworkClient`.
   - Add `Assets/Scripts/NetworkClient.cs`.
   - The default `serverUrl` is `ws://localhost:8765`.
   - Later, you can change it to something like `wss://my-domain.com`.

2. `RemotePlayerManager`
   - Create an empty GameObject named `RemotePlayerManager`.
   - Add `Assets/Scripts/RemotePlayerManager.cs`.
   - Drag the `NetworkClient` GameObject into its `networkClient` field.
   - Drag a spaceship asset into `remoteShipPrefab`, for example one of the FBX ships in `Assets/Prefab/Spaceship Pack - Jan 2018/FBX/`.

3. Local spaceship
   - Add a spaceship model to the scene. The FBX files in `Assets/Prefab/Spaceship Pack - Jan 2018/FBX/` are good placeholders.
   - Add `Assets/Scripts/SpaceshipController.cs`.
   - Drag the `NetworkClient` GameObject into its `networkClient` field.
   - Optionally create an empty child named `ShootPoint` in front of the ship and drag it into the `shootPoint` field.

4. Laser visual
   - You can leave `laserPrefab` empty to use the simple yellow beam made by code.
   - Or create a small cube/particle/missile prefab and add `Assets/Scripts/LaserVisual.cs` to it.
   - The Kenney shooting sprites and effects are in `Assets/Prefab/kenney_space-shooter-extension/PNG/Sprites/`.

5. Camera and light
   - Add a Directional Light.
   - Put the Main Camera behind and above the local ship.
   - A simple starting camera position is `(0, 8, -14)` looking at the ship.

6. UI Canvas
   - Create a Canvas with:
     - Player name `InputField`
     - Server URL `InputField`
     - Connect `Button`
     - Disconnect `Button`
     - Status `Text`
     - Optional Health and Score `Text`
   - Add `Assets/Scripts/GameUI.cs` to the Canvas.
   - Drag each UI element into the matching field in the Inspector.
   - Optional: put the name, URL, connect, and disconnect controls inside one panel, then drag that panel into `connectionPanel`. It will hide after connection.

7. Camera follow
   - Keep `Main Camera` outside `LocalPlayer` in the Hierarchy.
   - Add `Assets/Scripts/CameraFollow.cs` to `Main Camera`.
   - Drag `LocalPlayer` into the camera's `target` field.
   - A good starting setup is `distance = 12`, `height = 4`, and `lookHeight = 1.5`.
   - The mouse is handled by `SpaceshipController`, not by the camera.

## Play and Connect

1. Start the Python WebSocket server.
2. In Unity, press Play.
3. Enter a player name.
4. Keep the server URL as `ws://localhost:8765` for local testing.
5. Click Connect.

If the Python server is on another machine, replace `localhost` with that machine's IP address.

## Controls

- `W`, `Z`, or Up Arrow: move forward
- `S` or Down Arrow: move backward
- Mouse left/right: turn left/right
- Mouse up/down: pitch the ship up/down
- `A` or `Q`: roll left
- `D`: roll right
- Left Arrow / Right Arrow: optional keyboard turn
- Space: move up
- Left Control or Left Shift: move down
- Left mouse click or `F`: shoot

The ship's altitude is clamped between `minAltitude` and `maxAltitude` on the `SpaceshipController`.

## JSON Messages

Client sends:

```json
{ "type": "join", "name": "PlayerName" }
```

```json
{ "type": "state", "x": 0, "y": 5, "z": 0, "yaw": 90, "pitch": 0, "roll": 0 }
```

```json
{ "type": "shoot", "x": 0, "y": 5, "z": 0, "dx": 0, "dy": 0, "dz": 1 }
```

Server sends:

```json
{ "type": "welcome", "id": "player123" }
```

```json
{
  "type": "world",
  "players": [
    { "id": "player123", "name": "Alice", "x": 0, "y": 5, "z": 0, "yaw": 90, "pitch": 0, "roll": 0, "health": 100, "score": 0 }
  ]
}
```

```json
{ "type": "shoot_event", "id": "player123", "x": 0, "y": 5, "z": 0, "dx": 0, "dy": 0, "dz": 1 }
```

```json
{ "type": "disconnect", "id": "player123" }
```

```json
{ "type": "hit", "targetId": "player456", "health": 75 }
```

## Safe Student Modifications

Students can safely experiment with:

- Movement values on `SpaceshipController`: speed, turn speed, altitude, cooldown.
- The local and remote ship models.
- Laser prefab color, size, lifetime, and speed.
- UI labels and layout.
- How often the client sends state messages.

The Python server should stay responsible for checking hits, health, score, and the final shared game state.
