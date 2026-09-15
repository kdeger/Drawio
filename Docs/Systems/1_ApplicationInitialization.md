# Application Initialization

## Purpose

Draw.io needs one controlled startup path before the main menu. I would add a small Boot/Loading scene and let it coordinate local data, authentication and the online work that matters at startup.

I would keep the initializer contract simple: a service can be initialized asynchronously and should not start the same initialization twice if multiple callers request it.

## Flow

```text
Boot/Loading
    ↓
Load local defaults + player data
    ↓
Create/save a new player if needed
    ↓
Restore / initialize authentication
    ↓
Run required online work when available
    ├─ remote configuration
    └─ server player data
    ↓
Resolve player + EffectiveConfig
    ↓
Open main menu
```

Local data is the critical path. Network calls have their own timeout/error result and should not block startup indefinitely.

If the game starts offline, it continues with valid local data. When connectivity comes back, unfinished online work can run again. A profile or gameplay config recovered during a match is staged and applied after the match or before the next one, rather than changing live gameplay.

The loading screen can show the current stage, but I would not pretend initialization has an exact percentage unless the work is actually measurable that way.

## Result

The menu is opened from one known application state instead of depending on scene Awake / Start order. Online failures degrade to local data instead of becoming startup blockers.
