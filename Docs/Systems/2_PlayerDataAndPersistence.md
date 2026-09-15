# Player Data and Persistence

## Purpose

Gameplay and UI should use one in-memory player-data owner instead of reading/writing storage directly. Loading, saving, migration and server synchronization stay outside the model.

## Data shape

```text
PlayerSaveData
├── SchemaVersion
├── LocalPlayerId
├── Profile
├── SyncMetadata
├── Settings
├── Progression
└── Customization
```

The exact fields can evolve, but the important distinction is that SchemaVersion belongs to the save format while server revision/timestamps belong to synchronization.

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

## Result

Settings, progression and customization have one source of truth, and gameplay code does not need to know how the profile is stored or synchronized.
