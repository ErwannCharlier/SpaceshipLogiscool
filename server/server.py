import asyncio
import json
import math
import os
import time
import uuid
from dataclasses import dataclass
from datetime import datetime

import websockets


HOST = os.environ.get("HOST", "localhost")
PORT = int(os.environ.get("PORT", "8765"))

MIN_ALTITUDE = 1.0
MAX_ALTITUDE = 20.0
MAX_NAME_LENGTH = 16

SPAWN_POSITION = (0.0, 5.0, 0.0)
MAX_HEALTH = 100
MAX_ENERGY = 100.0
RESPAWN_DELAY = 3.0
ENERGY_COST_PER_UNIT = 0.2
ENERGY_RECHARGE_PER_SECOND = 30.0

SHOT_RANGE = 80.0
HIT_RADIUS = 1.5
SHOT_DAMAGE = 25
WORLD_MESSAGES_PER_SECOND = 10

STATION_POSITION = (20.0, 3.0, 20.0)
STATION_SIZE = 6.0


@dataclass
class Player:
    id: str
    name: str
    websocket: object
    x: float = SPAWN_POSITION[0]
    y: float = SPAWN_POSITION[1]
    z: float = SPAWN_POSITION[2]
    yaw: float = 0.0
    pitch: float = 0.0
    roll: float = 0.0
    health: int = MAX_HEALTH
    energy: float = MAX_ENERGY
    score: int = 0
    is_alive: bool = True
    respawn_end_time: float = 0.0


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


def make_station_message():
    return {
        "x": STATION_POSITION[0],
        "y": STATION_POSITION[1],
        "z": STATION_POSITION[2],
        "size": STATION_SIZE,
    }


def make_world_message(now=None):
    if now is None:
        now = time.monotonic()

    player_list = []

    for player in list(players.values()):
        respawn_seconds = 0.0

        if not player.is_alive:
            respawn_seconds = max(0.0, player.respawn_end_time - now)

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
                "energy": player.energy,
                "score": player.score,
                "isAlive": player.is_alive,
                "respawnSeconds": respawn_seconds,
            }
        )

    return {"type": "world", "players": player_list, "station": make_station_message()}


def reset_player_at_spawn(player):
    player.x = SPAWN_POSITION[0]
    player.y = SPAWN_POSITION[1]
    player.z = SPAWN_POSITION[2]
    player.yaw = 0.0
    player.pitch = 0.0
    player.roll = 0.0


def kill_player(player, reason, now=None):
    if player is None or not player.is_alive:
        return

    if now is None:
        now = time.monotonic()

    if reason == "energy":
        player.energy = 0.0
    else:
        player.health = 0

    player.is_alive = False
    player.respawn_end_time = now + RESPAWN_DELAY


def try_respawn_player(player, now):
    if player.is_alive or now < player.respawn_end_time:
        return False

    reset_player_at_spawn(player)
    player.health = MAX_HEALTH
    player.energy = MAX_ENERGY
    player.is_alive = True
    player.respawn_end_time = 0.0
    return True


def drain_energy_from_movement(player, old_position, new_position):
    if player is None or not player.is_alive:
        return

    move_distance = distance(old_position, new_position)

    if move_distance <= 0.0001:
        return

    player.energy = max(0.0, player.energy - move_distance * ENERGY_COST_PER_UNIT)

    if player.energy <= 0.0:
        kill_player(player, "energy")


def is_player_inside_station(player):
    half_size = STATION_SIZE * 0.5

    return (
        abs(player.x - STATION_POSITION[0]) <= half_size
        and abs(player.y - STATION_POSITION[1]) <= half_size
        and abs(player.z - STATION_POSITION[2]) <= half_size
    )


def recharge_player(player, delta_time):
    if player is None or not player.is_alive:
        return

    player.energy = min(MAX_ENERGY, player.energy + ENERGY_RECHARGE_PER_SECOND * delta_time)


def update_players(delta_time, now):
    for player in list(players.values()):
        if player.is_alive:
            if is_player_inside_station(player):
                recharge_player(player, delta_time)
        else:
            try_respawn_player(player, now)


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
    last_time = time.monotonic()

    while True:
        await asyncio.sleep(delay)

        if len(players) == 0:
            last_time = time.monotonic()
            continue

        now = time.monotonic()
        delta_time = now - last_time
        last_time = now

        update_players(delta_time, now)
        await broadcast(make_world_message(now))


async def handle_client(websocket):
    print(f"{datetime.now():%I:%M:%S %p} Client connected")

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
        print("f{datetime.now():%I:%M:%S %p} Invalid JSON ignored:", raw_message)
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

    reset_player_at_spawn(player)
    players[websocket] = player

    await send_json(websocket, {"type": "welcome", "id": player.id})
    await broadcast(make_world_message())

    print(player.name, "{datetime.now():%I:%M:%S %p} joined as", player.id)


def handle_state(websocket, message):
    player = players.get(websocket)

    if player is None or not player.is_alive:
        return

    old_position = (player.x, player.y, player.z)

    player.x = number_from_message(message, "x")
    player.y = clamp(number_from_message(message, "y", SPAWN_POSITION[1]), MIN_ALTITUDE, MAX_ALTITUDE)
    player.z = number_from_message(message, "z")
    player.yaw = number_from_message(message, "yaw")
    player.pitch = number_from_message(message, "pitch")
    player.roll = number_from_message(message, "roll")

    new_position = (player.x, player.y, player.z)
    drain_energy_from_movement(player, old_position, new_position)


async def handle_shoot(websocket, message):
    shooter = players.get(websocket)

    if shooter is None or not shooter.is_alive:
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
        kill_player(target, "health")

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
        if target.id == shooter.id or not target.is_alive:
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

    print(player.name, f"{datetime.now():%I:%M:%S %p} disconnected")
    await broadcast({"type": "disconnect", "id": player.id})
    await broadcast(make_world_message())


async def main():
    print(f"Starting server on ws://{HOST}:{PORT}")

    asyncio.create_task(broadcast_world_loop())

    async with websockets.serve(handle_client, HOST, PORT):
        print(f"{datetime.now():%I:%M:%S %p} Server ready. Press Ctrl+C to stop.")
        await asyncio.Future()


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\n {datetime.now():%I:%M:%S %p} Server stopped")
