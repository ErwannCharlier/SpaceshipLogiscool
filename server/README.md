# Flying Nugget Python Server

This is a small Python WebSocket server for the Unity spaceship shooter client.

## Install

From `C:\dev\FlyingNugget\server`:

```powershell
py -m venv .venv
.\.venv\Scripts\Activate.ps1
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
```

## Run

```powershell
.\.venv\Scripts\python.exe server.py
```

The default address is:

```text
ws://localhost:8765
```

You can change it with environment variables:

```powershell
$env:HOST = "0.0.0.0"
$env:PORT = "8765"
.\.venv\Scripts\python.exe server.py
```

Use `0.0.0.0` when another computer on the same network must connect to the server.

## What It Does

- Accepts Unity WebSocket clients.
- Handles `join`, `state`, and `shoot` messages.
- Sends `welcome`, `world`, `shoot_event`, `disconnect`, and `hit` messages.
- Keeps a simple health and score value for each player.
- Uses a simple ray check for shots.

This is intentionally simple for students. It is not a secure production server.

## Deploy on a VPS

See `DEPLOY_VPS.md` for Docker, Nginx, and Cloudflare setup using:

```text
wss://unity.erwann.xyz
```
