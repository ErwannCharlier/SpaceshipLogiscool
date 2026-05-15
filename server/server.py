import asyncio
import json
import math
import os
import uuid
from dataclasses import dataclass

import websockets


HOST = os.environ.get("HOST", "localhost")
PORT = int(os.environ.get("PORT", "8765"))

MIN_ALTITUDE = 1.0
MAX_ALTITUDE = 20.0
MAX_NAME_LENGTH = 16

SHOT_RANGE = 80.0
HIT_RADIUS = 1.5
SHOT_DAMAGE = 25
WORLD_MESSAGES_PER_SECOND = 10


@dataclass
class Player:
    id: str
    name: str
    websocket: object
    x: float = 0.0
    y: float = 5.0
    z: float = 0.0
    yaw: float = 0.0
    pitch: float = 0.0
    roll: float = 0.0
    health: int = 100
    score: int = 0


players = {}


def clamp(value, minimum, maximum):
    return max(minimum, min(maximum, value))


def clean_name(name):
    if not isinstance(name, str):
        return "Player"

    name = name.strip()

    if name == "":
        return "Player"

    return name[:MAX_NAME_LENGTH]


def number_from_message(message, key, default=0.0):
    try:
        return float(message.get(key, default))
    except (TypeError, ValueError):
        return default


def make_world_message():
    player_list = []

    for player in list(players.values()):
        player_list.append(
            {
                "id": player.id,
                "name": player.name,
                "x": player.x,
                "y": player.y,
                "z": player.z,
                "yaw": player.yaw,
                "pitch": player.pitch,
                "roll": player.roll,
                "health": player.health,
                "score": player.score,
            }
        )

    return {"type": "world", "players": player_list}


async def send_json(websocket, message):
    await websocket.send(json.dumps(message))


async def broadcast(message):
    if len(players) == 0:
        return

    text = json.dumps(message)
    websockets_to_remove = []

    for player in list(players.values()):
        try:
            await player.websocket.send(text)
        except websockets.exceptions.ConnectionClosed:
            websockets_to_remove.append(player.websocket)

    for websocket in websockets_to_remove:
        await remove_player(websocket)


async def broadcast_world_loop():
    delay = 1.0 / WORLD_MESSAGES_PER_SECOND

    while True:
        await asyncio.sleep(delay)

        if len(players) > 0:
            await broadcast(make_world_message())


async def handle_client(websocket):
    print("Client connected")

    try:
        async for raw_message in websocket:
            await handle_message(websocket, raw_message)
    except websockets.exceptions.ConnectionClosed:
        pass
    finally:
        await remove_player(websocket)


async def handle_message(websocket, raw_message):
    try:
        message = json.loads(raw_message)
    except json.JSONDecodeError:
        print("Invalid JSON ignored:", raw_message)
        return

    message_type = message.get("type")

    if message_type == "join":
        await handle_join(websocket, message)
    elif message_type == "state":
        handle_state(websocket, message)
    elif message_type == "shoot":
        await handle_shoot(websocket, message)


async def handle_join(websocket, message):
    if websocket in players:
        players[websocket].name = clean_name(message.get("name"))
        return

    player = Player(
        id="player_" + uuid.uuid4().hex[:8],
        name=clean_name(message.get("name")),
        websocket=websocket,
    )

    players[websocket] = player

    await send_json(websocket, {"type": "welcome", "id": player.id})
    await broadcast(make_world_message())

    print(player.name, "joined as", player.id)


def handle_state(websocket, message):
    player = players.get(websocket)

    if player is None:
        return

    player.x = number_from_message(message, "x")
    player.y = clamp(number_from_message(message, "y", 5.0), MIN_ALTITUDE, MAX_ALTITUDE)
    player.z = number_from_message(message, "z")
    player.yaw = number_from_message(message, "yaw")
    player.pitch = number_from_message(message, "pitch")
    player.roll = number_from_message(message, "roll")


async def handle_shoot(websocket, message):
    shooter = players.get(websocket)

    if shooter is None:
        return

    shot_position = (
        number_from_message(message, "x", shooter.x),
        clamp(number_from_message(message, "y", shooter.y), MIN_ALTITUDE, MAX_ALTITUDE),
        number_from_message(message, "z", shooter.z),
    )

    shot_direction = normalize(
        (
            number_from_message(message, "dx", 0.0),
            number_from_message(message, "dy", 0.0),
            number_from_message(message, "dz", 1.0),
        )
    )

    await broadcast(
        {
            "type": "shoot_event",
            "id": shooter.id,
            "x": shot_position[0],
            "y": shot_position[1],
            "z": shot_position[2],
            "dx": shot_direction[0],
            "dy": shot_direction[1],
            "dz": shot_direction[2],
        }
    )

    target = find_hit_player(shooter, shot_position, shot_direction)

    if target is None:
        return

    target.health = max(0, target.health - SHOT_DAMAGE)

    if target.health == 0:
        shooter.score += 1
        target.health = 100

    await broadcast({"type": "hit", "targetId": target.id, "health": target.health})
    await broadcast(make_world_message())


def normalize(vector):
    x, y, z = vector
    length = math.sqrt(x * x + y * y + z * z)

    if length <= 0.0001:
        return (0.0, 0.0, 1.0)

    return (x / length, y / length, z / length)


def find_hit_player(shooter, shot_position, shot_direction):
    closest_target = None
    closest_distance_along_ray = SHOT_RANGE

    for target in players.values():
        if target.id == shooter.id:
            continue

        distance_to_ray, distance_along_ray = ray_distance_to_player(
            shot_position,
            shot_direction,
            (target.x, target.y, target.z),
        )

        if distance_to_ray <= HIT_RADIUS and 0.0 <= distance_along_ray <= closest_distance_along_ray:
            closest_target = target
            closest_distance_along_ray = distance_along_ray

    return closest_target


def ray_distance_to_player(ray_start, ray_direction, player_position):
    to_player = (
        player_position[0] - ray_start[0],
        player_position[1] - ray_start[1],
        player_position[2] - ray_start[2],
    )

    distance_along_ray = dot(to_player, ray_direction)

    closest_point = (
        ray_start[0] + ray_direction[0] * distance_along_ray,
        ray_start[1] + ray_direction[1] * distance_along_ray,
        ray_start[2] + ray_direction[2] * distance_along_ray,
    )

    distance_to_ray = distance(player_position, closest_point)
    return distance_to_ray, distance_along_ray


def dot(a, b):
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def distance(a, b):
    dx = a[0] - b[0]
    dy = a[1] - b[1]
    dz = a[2] - b[2]
    return math.sqrt(dx * dx + dy * dy + dz * dz)


async def remove_player(websocket):
    player = players.pop(websocket, None)

    if player is None:
        return

    print(player.name, "disconnected")
    await broadcast({"type": "disconnect", "id": player.id})
    await broadcast(make_world_message())


async def main():
    print(f"Starting server on ws://{HOST}:{PORT}")

    asyncio.create_task(broadcast_world_loop())

    async with websockets.serve(handle_client, HOST, PORT):
        print("Server ready. Press Ctrl+C to stop.")
        await asyncio.Future()


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nServer stopped")
