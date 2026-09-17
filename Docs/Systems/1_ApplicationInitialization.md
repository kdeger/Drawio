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

## What runs in the case

The Boot scene loads first and shows a splash while one coordinator runs the sequence: load the local profile or create a new player, migrate the old `PlayerPrefs` values once, initialize authentication, then ask the server whether a profile exists. Each step reports its state to the splash, and a step that fails or times out leaves the game on local data instead of stopping the sequence. The whole sequence has its own upper bound, so a slow network delays the menu by a known amount and never blocks it.

The coordinator is the same whether the Boot scene starts it or the game scene is opened directly in the editor, and it runs once per session. The game reloads its scene after every match; the services are already there, so that reload costs nothing.

## Why the scene exists

The order alone is not the point. Local data loads synchronously, so nothing is ever missing and the menu could open immediately. The problem is what the server answers afterwards: a returning player would see the local level first and watch it change a second later. The Boot scene exists so the first screen the player reads is already the right one.

## Result

The menu is opened from one known application state instead of depending on scene Awake / Start order. Online failures degrade to local data instead of becoming startup blockers.
