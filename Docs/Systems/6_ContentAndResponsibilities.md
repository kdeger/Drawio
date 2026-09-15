# Content and Responsibility Boundaries

## Goal

I would not replace the existing gameplay architecture. The plan is to introduce clearer ownership around it and keep GameManager as a temporary compatibility layer while responsibilities move out gradually.

## Content

Draw.io already has useful ScriptableObjects for brushes, skins, terrains, colors and power-ups. I would keep those as the editor-facing source of content.

The main change is to give content stable IDs and resolve it through a small catalog instead of depending on asset names, paths or Resources.LoadAll() order.

```text
ScriptableObject content
        ↓
Catalog of stable IDs
        ↓
Loaded through Addressables when needed
        ↓
Gameplay / presentation
```

Remote config can pick an ID from the catalog, for example which skin is featured, but it does not load anything itself. New art or prefabs without a client update would need a remote content path later.

## Resources and Addressables

Today GameManager loads every brush, skin, color and power-up with Resources.LoadAll in Awake and keeps them for the whole session. Each skin points to its brush prefab, so every brush prefab and what it references is in memory from the start, whether it is used in this match or not. With four brushes that is small, but a live game keeps adding cosmetics, and everything in Resources is also always included in the build.

With Addressables the catalog holds references instead of loaded objects, and content is loaded when a screen or a match needs it. A few things need care:

- Loading is asynchronous. Code that today expects `m_Skins` to be ready immediately has to wait for the load, so loading should happen at known points such as the loading screen, before the menu is shown, or during match setup.
- Every load returns a handle, and the asset stays in memory until that handle is released. Whoever loads something is responsible for releasing it: a screen when it closes, match setup when the match ends. Releasing too early breaks whoever still uses it; never releasing is a leak. Objects instantiated through Addressables are released through Addressables as well, not only destroyed.
- An asset should not be in Resources and in an Addressable group at the same time, otherwise it ends up in the build twice.

I would start with local Addressable groups, which already gives the memory control without a server. Remote content delivery can come later on the same catalog.

## GameManager split

I would move responsibilities out roughly in this direction:

```text
GameManager compatibility layer
├── Match Flow
├── Match Setup / Player Spawning
├── Drawing Gameplay
├── Match Statistics
├── Progression
├── Content Catalog
├── Power-up Scheduling
└── Presentation Events
```

Existing code can keep calling GameManager while those calls are delegated internally. Once callers migrate, the compatibility layer can shrink or disappear.

## Ownership

| Area | Responsibility |
| --- | --- |
| Application/Core | Startup, player data, config, connection state |
| Gameplay | Match phases, drawing, arena, players, power-ups |
| Meta | Progression, daily rewards, quests, challenges |
| Presentation | Screens, popups, transitions, sequences |
| Content | Stable IDs, catalogs, ScriptableObject defaults |
| Infrastructure | Local storage and external-service adapters |

I would use small interfaces/events and explicit references rather than add a large DI framework for this project. Once this ownership settles, assembly definitions can be added so the dependency direction is checked by the compiler.

## Example: match end

1. Match Flow locks input and finalizes the match state.
2. Match Statistics finalizes score/rank.
3. Progression applies XP/unlocks once.
4. Player Data schedules the save.
5. Presentation shows the result, then the menu sequence can evaluate meta features.

No single class needs to calculate results, update XP, write a file, load a prefab and navigate UI at the same time.
