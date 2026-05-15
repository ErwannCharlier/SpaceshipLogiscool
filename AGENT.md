# AGENTS.md

## Project context

This is a Unity 3D client for a beginner-friendly online spaceship shooter course.

The server is a separate Python WebSocket server. Do not implement Unity Netcode, Photon, Mirror, Relay, Lobby, or any complex multiplayer framework.

## Coding rules

- Keep C# scripts simple and readable.
- Prefer small classes with clear responsibilities.
- Add comments for students.
- Avoid advanced Unity patterns.
- Avoid overengineering.
- The client communicates with the Python server using JSON over WebSocket.
- The game must stay suitable for 14-year-old students learning Unity.

## Architecture

Unity client:
- Handles input.
- Displays the local player.
- Displays remote players.
- Sends movement and shoot messages to the Python server.
- Receives world state from the Python server.

Python server:
- Owns the real game state.
- Receives player states.
- Broadcasts world state.
- Handles hit validation and score.