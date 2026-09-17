# Player Data and Persistence

## Purpose

Gameplay and UI should use one in-memory player-data owner instead of reading/writing storage directly. Loading, saving, migration and server synchronization stay outside the model.

## Data shape

```text
PlayerSaveData
├── SchemaVersion
├── LocalPlayerId
├── Revision
├── Progression
└── Customization

PlayerSettingsData
├── SchemaVersion
└── Vibration
```

The exact fields can evolve, but the important distinction is that SchemaVersion belongs to the save format while the server revision belongs to synchronization. Settings are a second record with its own schema version, for the reason below.

Content selections such as skins should use stable IDs instead of Resources.LoadAll() array positions.

## Loading and migration

On startup:

1. Load and deserialize the local profile.
2. Apply schema migration if needed.
3. If no profile exists, create defaults and save them before continuing.
4. Import the current PlayerPrefs values once when migrating the prototype.
5. If a local file is invalid, try the last known good backup or a valid server copy before falling back to defaults.

I would only mark the old PlayerPrefs migration as complete after the new profile has been written successfully.

## Saving

Multiple changes close together can be combined into one write. Important lifecycle points such as pause/quit can explicitly flush pending work.

For the file itself I would use a safe replacement path:

```text
serialize snapshot
    ↓
write temporary file
    ↓
replace primary save after successful write
    ↓
keep a backup for recovery
```

The save file is encrypted. That is only light local protection; progression or economy that matters to the business still needs server-side validation in production.

Encryption was not part of the brief, and I would not normally add it to a prototype. It is a corner of the client most gameplay work never goes near, so it is extra effort for something the player never sees. It cost me very little to put in place here, so I kept it, but what matters is that the boundary exists at all, not the cipher behind it.

The key is a constant in the build here, which is fine for the case but not for a live game, since it ships with the client. In production it would be derived per device, and anything worth cheating for would be owned by the server rather than the save file.

## Applying a new profile

A profile that arrives from the server replaces progression and customization in place, so every accessor returns the new values immediately. Settings are deliberately left alone. Vibration and the like belong to the device the player is holding, not to the account, and signing in on a second device should not carry the first device's preferences over. They have their own file next to the profile for the same reason, with their own schema version, so a profile arriving from the server can replace progression and customization without ever touching them.

Screens are a separate question: one that is already drawn keeps whatever it read when it opened.

Startup is not affected, because the profile is settled before the menu is built. Signing in or out is, so the game reloads its scene at that point. The scene reload is the same one the game performs after a match, and it costs nothing here because the account services live outside the scene and are not initialized twice.

## Result

Settings, progression and customization have one source of truth, and gameplay code does not need to know how the profile is stored or synchronized.
