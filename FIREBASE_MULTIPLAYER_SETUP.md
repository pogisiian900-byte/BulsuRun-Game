# Firebase Multiplayer Setup Notes

## What this project is using right now

- Unity version: `6000.3.8f1`
- Company name: `IanHarold`
- Product name: `BulsuRun`
- Current multiplayer stack: `Photon PUN`
- Photon Realtime App ID: `c538b43d-d851-4328-8418-668fe8532a47`
- Photon dev region: `asia`
- Boot scene flow: `Bootstrap -> Start Screen -> Multiplayer -> Lobby -> Worlds`
- Firebase config files currently present: none

Important: Firebase is not a real-time action multiplayer engine by itself. In this project, the clean setup is:

- `Photon` for live gameplay networking
- `Firebase Auth` for player identity
- `Firestore` for cloud saves, profiles, lobby metadata, and progression
- `Cloud Functions` only if you want server-side validation or Photon custom auth

## Exact app data you can use in Firebase Console

### Android app

Use this package name when you create the Android app in Firebase:

- Android package name: `com.IanHarold.BulsuRun`

Related project values:

- Version name: `1.0`
- Version code: `1`
- Minimum SDK: `25`
- ABIs: `arm64-v8a, armeabi-v7a`

Important mismatch to fix before downloading `google-services.json`:

- Unity `PlayerSettings` still shows only `Standalone: com.DefaultCompany.2D-URP`
- Android resolver metadata already uses `com.IanHarold.BulsuRun`

Before you register Firebase, set the Android application identifier in Unity to `com.IanHarold.BulsuRun` so your Firebase app and build output match.

### iOS app

No iOS bundle identifier is configured in this repo yet. If you want iOS Firebase too, you still need to choose and set one in Unity first.

## Multiplayer shape already implied by the project

- Players enter a `player name` and a `room name`
- Player names are stored locally and capped at `20` characters
- Rooms are created and joined by string room name
- Host loads the `Lobby` scene after joining
- Host starts the run and loads `Worlds`
- The world player list UI currently has `2` player slots
- Multiplayer player spawning currently distinguishes `Player Boy` and `Player Girl`

Important limitation:

- The room UI is built for `2` players
- The current Photon room creation code does not set `MaxPlayers`, so room size is effectively uncapped unless you change it

If you want the Firebase room schema to match the actual game, use:

- `maxPlayers = 2`

## Data already in the game that maps well to Firebase

### Player profile / cloud save

These values already exist locally and are good candidates for Firestore:

- `displayName`
- `highestLevel`
- `highestWorld`
- `coins`
- `currentWeaponId`
- `inventorySlots`
- `skillIds`

Current local data sources:

- Display name: `PlayerPrefs`
- Highest level/world: `PlayerPrefs`
- Run and inventory state: in-memory `GameData`

### Suggested Firestore collections

#### `players/{uid}`

Use fields like:

- `displayName`
- `createdAt`
- `lastSeenAt`
- `highestLevel`
- `highestWorld`
- `coins`
- `currentWeaponId`
- `inventorySlots`
- `skillIds`

Example shape:

```json
{
  "displayName": "Ian",
  "highestLevel": 7,
  "highestWorld": 2,
  "coins": 140,
  "currentWeaponId": "laser_blade",
  "inventorySlots": [
    { "weaponId": "laser_blade", "itemId": "", "amount": 1 },
    { "weaponId": "", "itemId": "medkit", "amount": 2 }
  ],
  "skillIds": ["dash_boost", "shield_pulse"]
}
```

#### `rooms/{roomId}`

Use Firestore only for room metadata, not frame-by-frame gameplay.

Suggested fields:

- `roomName`
- `hostUid`
- `hostDisplayName`
- `photonRoomName`
- `status`
- `maxPlayers`
- `playerCount`
- `createdAt`

Example shape:

```json
{
  "roomName": "RoomA",
  "hostUid": "firebase-user-id",
  "hostDisplayName": "Ian",
  "photonRoomName": "RoomA",
  "status": "waiting",
  "maxPlayers": 2,
  "playerCount": 1
}
```

#### `rooms/{roomId}/members/{uid}`

Suggested fields:

- `displayName`
- `isHost`
- `joinedAt`
- `ready`

## Firebase products to enable

For this project, the practical minimum is:

- `Authentication`
- `Cloud Firestore`

Recommended auth modes:

- `Anonymous` for fastest first pass
- `Google` later if you want real accounts

Optional:

- `Cloud Functions` if you want trusted room creation, anti-cheat checks, or Photon custom auth
- `Cloud Storage` only if you later store avatars or user-generated assets
- `Realtime Database` only if you specifically want Firebase presence features; Firestore is enough for the current structure

## Data you still need to provide manually

These values are not in the repo, so Firebase Console still needs them from you:

- Firebase `project ID`
- Firebase `project name`
- Android signing `SHA-1`
- Android signing `SHA-256`
- iOS bundle ID if you want iOS support

Note: the SHA keys depend on the keystore you will sign Android builds with. They cannot be reliably inferred from this Unity repo alone.

## What to do next

1. In Unity Player Settings, set the Android package name to `com.IanHarold.BulsuRun`.
2. Create a Firebase project.
3. Add an Android app in Firebase using `com.IanHarold.BulsuRun`.
4. Enable `Authentication` and `Cloud Firestore`.
5. Download `google-services.json` and place it in `Assets/`.
6. Install the Firebase Unity SDK packages you actually need:
   - `FirebaseApp`
   - `FirebaseAuth`
   - `FirebaseFirestore`
   - `FirebaseFunctions` only if needed
7. Keep Photon for the live multiplayer session.
8. Save `displayName`, progression, and inventory to Firestore under `players/{uid}`.
9. Store room metadata in Firestore only if you want cloud room listings/history outside Photon.

## Source references

- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/ProjectSettings.asset`
- `ProjectSettings/AndroidResolverDependencies.xml`
- `ProjectSettings/EditorBuildSettings.asset`
- `Assets/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset`
- `Assets/Scripts/Player/Multiplayer/CreateAndJoin.cs`
- `Assets/Scripts/Player/Multiplayer/PlayerNameStore.cs`
- `Assets/Scripts/Player/Multiplayer/WorldPlayerListUI.cs`
- `Assets/Scripts/Player/PlayerSpawner.cs`
- `Assets/Scripts/Player/GameData.cs`
- `Assets/Scripts/Progress/GameProgress.cs`
