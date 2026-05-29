# Cours 3 - Radar 2D et choix de vaisseau

Bienvenue dans le cours 3 !

Aujourd'hui, on va ajouter deux vraies fonctions de jeu :

- choisir son vaisseau avant de se connecter
- voir les autres joueurs sur un radar 2D

Tu ne vas pas tout refaire. La plupart des helpers Unity sont deja prets. Ton objectif est de completer seulement les fonctions importantes, celles qui changent vraiment ce que le joueur voit a l'ecran.

A la fin du cours, ton jeu devra pouvoir :

- envoyer au serveur le vaisseau choisi
- changer le modele du vaisseau local
- afficher le bon vaisseau chez les autres joueurs
- afficher les autres joueurs sur le radar

## Objectifs

Tu vas comprendre et coder :

- comment envoyer une nouvelle info dans le message `join`
- comment changer une selection dans un menu Unity
- comment charger un prefab de vaisseau et remplacer l'ancien modele
- comment convertir une position 3D du monde en position 2D sur un radar

## Petit rappel reseau

Dans ce cours, le message `join` envoie maintenant :

```json
{
  "type": "join",
  "name": "Alice",
  "shipId": "spaceship3"
}
```

Et le serveur est suppose renvoyer `shipId` dans `world.players`.

Exemple :

```json
{
  "type": "world",
  "players": [
    {
      "id": "player_1234",
      "name": "Alice",
      "shipId": "spaceship3"
    }
  ]
}
```

Le plus important dans ce cours :

- envoyer le bon `shipId`
- utiliser ce `shipId` pour afficher le bon modele
- convertir les positions des autres joueurs pour le radar

## La scene Unity deja preparee

Tu ne dois pas creer toute l'interface a la main.

La scene contient deja :

- un panneau de connexion
- un joueur local
- un HUD
- un radar deja cree par code
- une librairie de vaisseaux dans `Assets/Resources/Ships`

Les prefabs des vaisseaux viennent du dossier `FBX`.

Checkpoint :

1. Ouvre `SpaceArena`.
2. Appuie sur Play.
3. Verifie que l'interface de connexion apparait.
4. Verifie que le jeu compile avant de commencer.

## Exercice 1 - Envoyer le vaisseau choisi

Fichier :

```text
Assets/Scripts/NetworkClient.cs
```

Tu vas completer :

- `SendJoin(string playerName, string shipId)`

### Objectif

Quand le joueur clique sur Connect, Unity doit envoyer :

- `type = "join"`
- `name = playerName`
- `shipId = shipId`

Si le nom est vide, tu peux utiliser `"Player"`.

Si le `shipId` est vide, tu peux prendre le vaisseau par defaut de `ShipLibrary`.

### Etapes

1. Creer un `JoinMessage`.
2. Mettre `type = "join"`.
3. Nettoyer le nom du joueur.
4. Nettoyer le `shipId`.
5. Convertir le message en JSON.
6. Envoyer le JSON avec `SendJson`.

Indice :

```csharp
JoinMessage message = new JoinMessage();
message.type = "join";
```

Checkpoint :

- le projet compile
- au moment de la connexion, le client peut envoyer un `shipId`

## Exercice 2 - Changer de vaisseau dans le menu

Fichier :

```text
Assets/Scripts/GameUI.cs
```

Tu vas completer :

- `CycleShipSelection(int direction)`

### Objectif

Quand le joueur clique sur `<` ou `>`, il faut :

- changer l'index du vaisseau
- stocker le nouveau `pendingShipId`
- mettre a jour l'aperçu du vaisseau local
- mettre a jour le texte du menu

### Etapes

1. Recuperer le nombre de vaisseaux avec `ShipLibrary.GetShipCount()`.
2. Quitter si aucun vaisseau n'est disponible.
3. Recuperer l'index du vaisseau actuel.
4. Avancer ou reculer avec `direction`.
5. Boucler proprement entre premier et dernier vaisseau.
6. Mettre a jour `pendingShipId`.
7. Appeler `ApplyPendingShipSelection()`.
8. Appeler `UpdateShipSelectorText()`.

Indice :

```csharp
shipIndex = (shipIndex + direction + shipCount) % shipCount;
```

Checkpoint :

- les boutons `<` et `>` changent bien le nom du vaisseau
- le joueur local voit tout de suite l'aperçu changer

## Exercice 3 - Appliquer le bon modele de vaisseau

Fichiers :

```text
Assets/Scripts/SpaceshipController.cs
Assets/Scripts/RemotePlayerManager.cs
Assets/Scripts/RemotePlayer.cs
```

Tu vas completer :

- `SetSelectedShip(string shipId)`
- `ApplyShipVisual(RemotePlayer remotePlayer, string shipId)`
- `UpdateNameLabelText()`

### Partie A - Vaisseau local

Dans `SetSelectedShip`, le but est de :

1. normaliser le `shipId`
2. ignorer les ids vides
3. eviter de recréer le meme modele pour rien
4. recuperer le bon prefab avec `ShipLibrary.GetShipPrefab`
5. supprimer l'ancien `ShipModel`
6. instancier le nouveau prefab
7. appliquer `localPosition`, `localRotation` et `localScale`
8. mettre a jour les renderers du vaisseau

### Partie B - Vaisseaux distants

Dans `ApplyShipVisual`, le but est de :

1. verifier que `remotePlayer` existe
2. chercher le prefab correspondant au `shipId`
3. si le prefab existe, appeler `remotePlayer.SetShipVisual(...)`
4. sinon, utiliser `remoteShipPrefab`
5. si rien n'existe, utiliser `remotePlayer.EnsureFallbackVisual()`

### Partie C - Nom au-dessus du joueur

Quand un joueur distant apparait, son pseudo doit etre visible au-dessus de son vaisseau.

Dans `UpdateNameLabelText()`, le but est de :

1. verifier que `nameLabelText` existe
2. afficher `"Player"` si le nom est vide
3. sinon afficher `playerName`

Indice :

```csharp
string.IsNullOrWhiteSpace(playerName)
```

Checkpoint :

- ton vaisseau local change quand tu changes la selection
- un autre joueur peut afficher un autre modele de vaisseau
- les autres joueurs ont un nom au-dessus du vaisseau

## Exercice 4 - Afficher les autres joueurs sur le radar

Fichier :

```text
Assets/Scripts/GameUI.cs
```

Tu vas completer :

- `WorldToRadarPosition(Vector3 worldPosition)`
- `UpdateRadarPlayerBlips()`

### Partie A - Convertir une position monde en position radar

Le radar est centre sur le joueur local.

Pour convertir une position :

1. recuperer la position du joueur local
2. calculer le vecteur entre la cible et le joueur
3. ignorer la hauteur `y`
4. projeter ce vecteur sur `right` et `forward`
5. construire une position 2D
6. reduire la distance avec `RadarWorldRange`
7. limiter la position au bord du radar avec `RadarRadius`

Indice :

```csharp
float radarX = Vector3.Dot(delta, flatRight);
float radarY = Vector3.Dot(delta, flatForward);
```

### Partie B - Mettre a jour les points des autres joueurs

Dans `UpdateRadarPlayerBlips`, il faut :

1. verifier que `latestWorldPlayers` existe
2. vider ou nettoyer les points qui ne servent plus
3. ignorer le joueur local
4. creer un blip pour chaque autre joueur
5. calculer sa position avec `WorldToRadarPosition`
6. mettre a jour sa couleur selon `isAlive`
7. supprimer les blips des joueurs disparus

Checkpoint :

- le radar affiche les autres joueurs
- les points bougent quand les joueurs bougent
- les joueurs morts peuvent avoir une couleur differente

## Test final

A la fin, tu dois pouvoir :

- lancer Unity
- te connecter a `ws://localhost:8765`
- choisir un vaisseau avant de cliquer sur Connect
- voir le modele du vaisseau local changer
- envoyer `shipId` au serveur
- voir les autres joueurs sur le radar
- voir le bon vaisseau chez un autre joueur si le serveur renvoie bien `shipId`
- voir le nom des autres joueurs au-dessus de leur vaisseau

Si tout ca marche, ton cours 3 est reussi.
