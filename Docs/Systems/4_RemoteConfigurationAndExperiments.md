# Remote Configuration

## Purpose

ScriptableObjects stay as the shipped/default configuration because they work offline and remain easy to edit in Unity. Remote configuration only overrides those defaults through one provider-independent boundary.

## Runtime flow

```text
ScriptableObject defaults
        ↓
Last valid cached values
        ↓
Fetched remote values
        ↓
Validate
        ↓
EffectiveConfig
```

EffectiveConfig is the result of that chain: the values the game actually uses right now. Gameplay and UI only read from it and do not call the remote provider directly.

Audience or A/B assignment is also a provider concern. The client only receives the effective value for that player, so the gameplay path does not need experiment-specific branches.

## Draw.io examples

Good first values to move behind config are match duration, revive duration, player count, power-up timing, XP rewards and difficulty values.

Take the revive timer. Today it is a field on the RVEndView prefab, set to 10 seconds. It would move into a ScriptableObject as the default. An experiment group gets 7 from remote config, the value is checked against an allowed range, and EffectiveConfig returns 7 for those players. Everyone else gets 10, and so does a player who has not received a valid value yet, for example on a first launch without a connection. RVEndView reads the timer the same way in every case, so there is no experiment check inside the view.

Remote config can also choose stable IDs for layouts, prefabs or visual variants that are already included in the build. It cannot create a new hierarchy, behavior or art asset that was never shipped.

A match takes the configuration it needs at match start. If gameplay config changes while a match is running, I would stage it for the next match rather than mutate the current rules.

## Result

Designers keep a simple local workflow while the live team can tune gameplay and shipped UI variants without adding provider/cohort checks across the codebase.
