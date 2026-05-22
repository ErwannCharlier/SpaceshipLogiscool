# Cours 2 - Vie, energie, station et respawn

Bienvenue dans le cours 2 de notre projet de vaisseaux multijoueurs.

Dans le cours 1, ton jeu savait deja :

- se connecter au serveur
- envoyer la position du vaisseau
- afficher les autres joueurs
- tirer des lasers

Aujourd'hui, on ajoute du vrai gameplay :

- une barre de vie
- une barre d'energie
- une vraie explosion
- un respawn avec cooldown
- une station d'energie
- un viseur qui suit le laser

A la fin du cours, ton projet devra a nouveau etre complet.

## Objectifs

Tu vas comprendre et coder :

- comment Unity lit de nouvelles donnees du serveur
- comment un joueur passe de vivant a mort puis respawn
- comment afficher une station en code
- comment mettre a jour une vraie interface de jeu


## Les 6 fonctions a coder

Tu dois completer ces 6 fonctions :

1. `NetworkClient.HandleServerMessage(string json)`
2. `SpaceshipController.HandleLocalPlayerWorldState(NetworkPlayerInfo playerInfo)`
3. `StationVisualManager.CreateStationVisual()`
4. `GameUI.UpdateHealthBar()`
5. `GameUI.UpdateEnergyBar()`
6. `GameUI.UpdateRespawnText()`

Tout le reste est deja code pour t'aider.

## Les fonctions a lire dans le code

Ces fonctions sont deja terminees, mais elles vont beaucoup t'aider pour comprendre le projet :

- `SpaceshipController.CanControlShip()`
- `SpaceshipController.SetShipVisible(bool isVisible)`
- `SpaceshipController.PlayExplosion()`
- `RemotePlayer.ApplyWorldState(...)`
- `StationVisualManager.UpdateStationVisual(StationInfo stationInfo)`
- `GameUI.CreateHudIfNeeded()`
- `GameUI.UpdateCrosshairPosition()`

Conseil :

- commence toujours par lire les fonctions deja faites autour de ton `TODO`
- souvent, la reponse est juste a quelques lignes

## Les nouvelles regles du jeu

### La vie

Quand un joueur se fait toucher, il perd de la vie.

Quand la vie arrive a `0` :

- le vaisseau explose
- le vaisseau disparait
- le joueur ne peut plus bouger
- le joueur attend quelques secondes
- le joueur respawn ensuite avec toute sa vie

### L'energie

Le vaisseau a aussi une reserve d'energie.

Quand le joueur se deplace, le serveur retire un peu d'energie.

Quand l'energie arrive a `0` :

- le vaisseau explose aussi
- le joueur entre en cooldown
- le joueur respawn ensuite avec toute son energie

### La station

Le serveur envoie la position d'une station d'energie.

Dans Unity, on va l'afficher comme un cube bleu transparent.

Quand le joueur entre dans cette zone, son energie remonte.

## Le nouveau message `world`

Le message `world` du serveur contient maintenant plus d'informations.

Exemple :

```json
{
  "type": "world",
  "players": [
    {
      "id": "player_1234",
      "name": "Alice",
      "x": 0,
      "y": 5,
      "z": 0,
      "yaw": 90,
      "pitch": 15,
      "roll": 20,
      "health": 100,
      "energy": 100,
      "score": 0,
      "isAlive": true,
      "respawnSeconds": 0
    }
  ],
  "station": {
    "x": 20,
    "y": 3,
    "z": 20,
    "size": 6
  }
}
```

Dans ce cours, ton travail est de faire reagir Unity a ces nouvelles infos.

## Avant de coder

1. Ouvre la scene `SpaceArena`.
2. Ouvre les scripts Unity du dossier `Assets/Scripts`.
3. Cherche `TODO Cours 2`.
4. Verifie qu'il y a bien seulement 6 `TODO`.

Checkpoint :

- Le projet compile avant tes changements.
- Tu sais exactement quelles fonctions tu dois completer.

## Exercice 1 - Recevoir les nouvelles donnees

Fichier :

```text
Assets/Scripts/NetworkClient.cs
```

Fonction :

```csharp
private void HandleServerMessage(string json)
```

Objectif :

Quand le serveur envoie un message `world`, Unity doit :

1. lire `world`
2. stocker `world.station` dans `CurrentStation`
3. appeler `WorldReceived`
4. appeler `StationReceived`

Indice :

```csharp
WorldMessage world = JsonUtility.FromJson<WorldMessage>(json);
```

Va aussi regarder dans ce fichier :

- `WorldMessage`
- `StationInfo`
- `NetworkPlayerInfo`

Checkpoint :

- Le projet compile.
- La station peut maintenant etre envoyee au reste du client Unity.

## Exercice 2 - Gerer la mort et le respawn du joueur local

Fichier :

```text
Assets/Scripts/SpaceshipController.cs
```

Fonction :

```csharp
public void HandleLocalPlayerWorldState(NetworkPlayerInfo playerInfo)
```

Objectif :

1. lire `playerInfo.isAlive`
2. gerer le premier message avec `hasLocalWorldState`
3. detecter le passage vivant -> mort
4. detecter le passage mort -> vivant
5. jouer l'explosion au bon moment
6. cacher le vaisseau quand il meurt
7. reafficher le vaisseau quand il respawn
8. replacer le vaisseau avec `ApplyServerTransform(...)`
9. liberer la souris avec `UnlockCursor()` quand le joueur meurt

Lis d'abord ces fonctions juste a cote :

- `CanControlShip()`
- `SetShipVisible(bool isVisible)`
- `PlayExplosion()`

Tu peux aussi t'aider de ces variables :

- `isAlive`
- `hasLocalWorldState`

Checkpoint :

- Quand le joueur meurt, il ne peut plus bouger.
- Le vaisseau disparait.
- Quand il respawn, il revient.

## Exercice 3 - Afficher la station d'energie

Fichier :

```text
Assets/Scripts/StationVisualManager.cs
```

Fonction :

```csharp
public void CreateStationVisual()
```

Objectif :

1. creer un cube avec `GameObject.CreatePrimitive(PrimitiveType.Cube)`
2. lui donner le nom `Energy Station`
3. le mettre comme enfant de l'objet courant
4. supprimer son collider
5. recuperer son `Renderer`
6. lui appliquer un materiau bleu transparent

Tres important :

- `UpdateStationVisual(...)` est deja code
- donc ta fonction sera appelee automatiquement ensuite

Va lire juste apres :

- `UpdateStationVisual(StationInfo stationInfo)`

Checkpoint :

- Un cube bleu transparent apparait.
- Il est place a la bonne position.
- Sa taille suit `stationInfo.size`.

## Exercice 4 - Mettre a jour l'interface de jeu

Fichier :

```text
Assets/Scripts/GameUI.cs
```

Fonctions :

```csharp
public void UpdateHealthBar()
public void UpdateEnergyBar()
public void UpdateRespawnText()
```

Le HUD est deja cree pour toi.

Va lire avant :

- `CreateHudIfNeeded()`

Tu verras que les objets existent deja :

- `healthBarFill`
- `healthValueText`
- `energyBarFill`
- `energyValueText`
- `respawnText`

### Partie 1 - La barre de vie

Dans `UpdateHealthBar()` :

1. mets a jour `healthBarFill.fillAmount`
2. mets a jour le texte du style `100 / 100`

Indice :

```csharp
Mathf.Clamp01(localHealth / MaxStatValue)
```

### Partie 2 - La barre d'energie

Dans `UpdateEnergyBar()` :

1. fais la meme chose avec `localEnergy`
2. mets a jour le texte `100 / 100`

### Partie 3 - Le texte de respawn

Dans `UpdateRespawnText()` :

1. affiche le texte seulement si le joueur est mort
2. cache le texte sinon
3. affiche une phrase du style :

```text
Respawn dans: 2.4s
```

Checkpoint :

- La vie s'affiche dans une vraie barre.
- L'energie s'affiche dans une vraie barre.
- Le texte de respawn apparait seulement pendant le cooldown.

## Si tu finis en avance

Le projet contient deja d'autres morceaux interessants a lire.

Tu peux aller voir :

- `RemotePlayer.ApplyWorldState(...)` pour comprendre comment les autres joueurs meurent et respawn
- `GameUI.UpdateCrosshairPosition()` pour comprendre comment le viseur suit le laser
- `SpaceshipController.GetLaserAimPoint(float distance)` pour comprendre comment on calcule le point vise

Ces fonctions sont deja codees, donc ce n'est pas obligatoire pour finir le cours.

## Test final

A la fin du cours, ton projet doit pouvoir :

- se connecter au serveur
- recevoir `energy`, `isAlive`, `respawnSeconds` et `station`
- afficher la station d'energie
- afficher une barre de vie
- afficher une barre d'energie
- afficher le score
- afficher le cooldown de respawn
- bloquer les controles quand le joueur est mort
- jouer une explosion
- cacher le vaisseau pendant le cooldown
- reafficher le vaisseau au respawn
- afficher le viseur au bon endroit
