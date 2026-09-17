# Draw.io — Technical Director Case Study

## Approach

I would not rewrite Draw.io. The gameplay loop already works and most of the problems I found are the kind of shortcuts I would expect in a prototype.

The document first looks at where the project is today, then where I would take it and in which order. The target structure is based on systems I have already built and used in live games: startup, player data, accounts, server synchronization, remote configuration, popups and gameplay flow.

My goal would be to keep the gameplay intact while putting a clearer application structure around it. I would introduce those boundaries gradually so the game stays playable while the project moves toward a live product.

Most sections link to a short note with more detail. This document stays on the main problems, priorities and direction.

## Current state of the project

The existing ScriptableObject setup for brushes, skins, terrains, colors and power-ups is useful and I would keep it.

The main issues I would address are:

- The Game scene is both the application entry point and the gameplay scene, so startup depends heavily on Awake / Start order. An older two-scene bootstrap seems to have been removed at some point: Preload and ForcePreload are still in the project, but nothing uses them.
- Player settings and progression are spread across PlayerPrefs, and ownership is duplicated: StatsManager and RankingManager each define their own level and XP accessors over the same keys. RankingManager is still on the Managers prefab, although nothing calls it anymore.
- Nothing identifies the player. There is a nickname in PlayerPrefs and no id that a server or an account could be attached to, so progression exists only on the device that produced it.
- GameManager currently owns too many unrelated responsibilities: match flow, spawning, content loading, power-ups, results and some presentation decisions.
- Some UI views directly drive gameplay managers, other views or scene reloads.
- Content loaded through Resources.LoadAll() is effectively identified by array position in some places, which is fragile once content changes. For example, the selected skin is saved as an index into the list GameManager loads with Resources.LoadAll, so changing the content order also changes the player's selected skin. All of this content is also loaded at startup and stays in memory for the whole session.
- A number of tunable values and cohort branches are hardcoded, and nothing owns those values at runtime. The cohort names are defined in Constants and gameplay checks them directly, so changing a cohort needs a new build.
- There are no assembly-level module boundaries yet; gameplay, UI and manager code all compile into the same assembly.

These are normal prototype decisions. I would fix them incrementally rather than replace the architecture all at once.

## Priority overview

| Priority | Problem | What I would change |
| ---: | --- | --- |
| 1 | Startup has no controlled entry point | Add a small Boot/Loading scene and one initialization flow |
| 2 | Player state is spread across PlayerPrefs | Move it into one versioned serializable profile with a single owner |
| 3 | The player has no identity a server could recognize | Give every install a local id and put account sign-in behind one provider interface |
| 4 | There is no clean server/save boundary | Add a decoy server interface and explicit local/server resolution rules |
| 5 | Core managers mix too many responsibilities | Split responsibilities gradually while keeping compatibility with the current code |
| 6 | Gameplay and UI tuning is partly hardcoded | Keep ScriptableObject defaults and let remote values override them through one EffectiveConfig |
| 7 | UI/gameplay flow is tightly coupled | Separate popup lifetime, presentation sequences and gameplay flow ticks |
| 8 | Module boundaries are implicit | Add startup/service diagnostics and draw assembly boundaries once ownership is stable |

## 1. Application startup

I would add a small Boot/Loading scene before the current game scene. Its only job is to create the required services, load the local player and configuration, perform the online checks that matter for startup, then open the menu.

The basic flow is:

```text
Boot/Loading
    ↓
Load local defaults and player data
    ↓
Create and save a new player if needed
    ↓
Restore / initialize authentication
    ↓
Run required online checks when available
    ├─ remote configuration
    └─ server player data
    ↓
Resolve active player data + EffectiveConfig
    ↓
Open main menu
```

Authentication sits between local player data and server synchronization, so it runs inside this flow instead of as a login screen in front of the game. It is described in section 3.

A network failure should not keep the loading screen open forever. Local data is the fallback; remote calls have their own timeout/error result and unfinished work can retry later. If a newer profile or gameplay configuration arrives during a match, I would stage it and apply it at a safe boundary instead of changing active gameplay.

The scene is not decoration. Without it the menu draws on the first frame from local data, and a server profile that arrives a second later changes the level and the name in front of the player. With it the menu is only shown once the profile is settled, and what the player sees first is already correct.

The same initialization runs whether the Boot scene starts it or the game scene is opened directly, which keeps the editor workflow intact. The step is skipped when the services already exist, so the scene reload the game performs after every match does not re-run it.

More detail: [Application Initialization](Systems/1_ApplicationInitialization.md).

## 2. Player data and persistence

Gameplay and UI should work with one in-memory player model and should not care whether it came from a local file, migrated PlayerPrefs, or the server.

The profile would contain versioning/sync information, progression and customization data. Settings sit in their own file next to it, because they belong to the device rather than to the account. Runtime Unity objects are not serialized.

Existing PlayerPrefs values can be imported once into the new format. For local storage I would keep the save path simple and safe: serialize a fixed snapshot, write it to a temporary file, and replace the primary save only after the write succeeds. A backup/recovery path is useful for interrupted writes.

The save file is encrypted, but I would treat that only as light local protection, not as a trust or anti-cheat boundary.

Having one model also answers what happens after a server profile is applied. Gameplay and UI read the profile through the same accessors they used for `PlayerPrefs`, so the values are correct as soon as the profile is replaced and nothing has to be invalidated. At startup the profile is settled before the menu is built, so there is nothing to refresh. Signing in or out later replaces it while the menu is already on screen, and the game reloads its scene for that, the same way it already does after a match. One rule covers every screen instead of a list of views to refresh by hand.

More detail: [Player Data and Persistence](Systems/2_PlayerDataAndPersistence.md).

## 3. Identity and accounts

The first launch creates a local player id and saves the profile under it. A player who never signs in still has a stable identity and normal progression.

Signing in is a separate step, and I would keep two cases apart because they affect the player's data differently:

- **Link:** the current player connects an account. Progress stays the same and can now be restored on another device.
- **Login:** the account already has a profile. There is now a local and a server profile, and the sync rules in section 4 decide which one is used.

The difficult case is an account that already belongs to another profile. In production I would not choose automatically; the player picks between the current progress and the account's progress, and the other one is kept as a backup. For the case I kept the simple rule: the local profile is backed up, the account's profile is applied when it is newer, otherwise the local one is kept and uploaded.

For the case I implemented email sign-in through Firebase, because it can be tested in the Editor without a device or a store account. Apple and Google come next through the same interface, and they are the ones I would ship first on mobile since they are native on each platform. Gameplay and save code only see a player id, never the provider. If a token cannot be refreshed, or Firebase is not reachable at all, the game continues with the local identity instead of blocking the player.

## 4. Server data and synchronization

Authentication answers who the player is. This section answers what that player owns, and the two are deliberately separate. The provider stores an identity and nothing else: an id, the credential attached to it, and when it was last used. The profile lives behind the server interface instead, stored under that same id, so the game can change provider without moving its data and can change backend without touching sign-in.

The case doesn't need a real backend. I would put the client behind a small server interface and provide a deterministic decoy that can reproduce the cases I need to test: no server profile, same profile, newer profile, timeout and temporary failure. In production the same interface points at the backend's own storage, one record per player.

The decoy itself is simple. What matters are the decision rules:

- same revision + clean local data → keep local
- same revision + local changes → keep local and mark/upload later
- newer server revision + clean local data → download and apply
- both sides changed → do not silently overwrite either side
- no server profile → continue locally
- timeout/failure → continue with valid local data and retry later

The case implements the simple half of this against the decoy: no server profile or an older one means the local profile is uploaded, a newer one is applied after a local backup, and a failure or timeout keeps the local data. The dirty flag and the conflict branch are the production step.

The revision call is also the existence check, so asking for it first and downloading only when the server is ahead costs no extra round trip.

More detail: [Network Synchronization and Decoy Server](Systems/3_NetworkSynchronization.md).

## 5. Responsibility boundaries

I would keep the current GameManager as a compatibility layer at first and move responsibilities out one at a time. The target ownership is roughly:

- Match Flow: phases, transitions and completion
- Match Setup: arena/player creation
- Drawing Gameplay: drawing rules
- Progression: XP, level and unlock changes
- Match Statistics: score/rank/history
- Difficulty Configuration: AI tuning
- Content Catalog: brushes, skins, terrains and power-ups by stable ID
- Power-up Scheduling: spawn timing and requests
- Presentation: reacts to state, but does not own gameplay decisions

The important rule is one owner for each persistent value and one direction of dependency. I do not think this project needs a large DI framework; a small bootstrap, explicit interfaces and events are enough.

On the content side I would move from Resources to Addressables over time. Today GameManager loads every brush and skin in Awake and keeps them, including every brush prefab, in memory for the whole session. With Addressables content is loaded when it is needed and released when it is not. The trade-off is that loading becomes asynchronous and every load has to be released by whoever made it, so loads need clear owners and should happen at known points such as the loading screen or match setup.

More detail: [Content and Responsibility Boundaries](Systems/6_ContentAndResponsibilities.md).

## 6. Remote configuration and fast iteration

ScriptableObjects remain the shipped defaults. EffectiveConfig is simply the set of values the game actually uses: each value starts from its ScriptableObject default and is replaced by a cached or remote value only when that value is valid. Gameplay and UI read from EffectiveConfig and do not need to know where a value came from.

For example, the revive timer is currently a field on RVEndView set to 10 seconds. It would become a ScriptableObject default of 10. If an experiment group receives 7 from remote config and 7 is inside the allowed range, EffectiveConfig returns 7 for those players and 10 for everyone else, including a player who has not received a valid value yet, for example on a first launch without a connection.

```text
ScriptableObject defaults
        ↓
Cached / remote overrides
        ↓
Validation
        ↓
EffectiveConfig
```

Gameplay does not call Firebase (or another provider) directly. The provider also owns audience and A/B assignment; the client simply receives the effective value for that player and follows the same code path.

Good first candidates in Draw.io are match duration, revive duration, player count, power-up timing, XP rewards and difficulty values. Remote config can also select a layout/prefab/visual variant that is already shipped in the build. Truly new assets would need a separate content-delivery path later.

More detail: [Remote Configuration](Systems/4_RemoteConfigurationAndExperiments.md).

## 7. Popup, meta and gameplay flow

I would separate three things that are currently easy to mix together:

- Popup management owns popup lifetime, stack/background and transitions.
- Presentation sequences decide what should be shown and in what order.
- Gameplay flow ticks perform the state-changing steps of a match result/revive path.

The popup manager should not know whether the player is eligible for a daily reward. The daily reward feature decides that; a sequence element only connects it to presentation. The same idea applies to quests, challenges and future LiveOps screens.

For example, a win flow can remain easy to read:

```text
Win
 ↓
Lock input
 ↓
Finalize result + progression
 ↓
Save
 ↓
Show result presentation
 ↓
Return to menu
 ↓
Run eligible meta sequence
```

A flow tick here means one discrete action, not Unity's per-frame `Update`. The linked note walks through the basic cases: popups opened on top of each other, a sequence waiting for each step, and ticks that either hold the flow or run in the background.

More detail: [Popup, Sequence and Gameplay Flow](Systems/5_PopupAndMetaFlow.md).

## Team scalability

As the team grows, I would keep ownership roughly split like this:

| Area | Ownership |
| --- | --- |
| Application/Core | Bootstrap, time, player data, config, connection state |
| Gameplay | Match flow, arena, drawing, players, power-ups |
| Meta | Progression, daily rewards, quests, challenges |
| Presentation | Screens, popups, transitions, sequences |
| Content | Stable IDs, catalogs, ScriptableObject defaults |
| Infrastructure | Storage and external-service adapters |

This gives developers clearer boundaries without forcing the prototype into a large framework too early. Once these boundaries settle, assembly definitions can be added so the dependency direction is checked by the compiler and not only in code review.

## Proposed stack

- Existing Unity/C# gameplay and uGUI for the baseline
- ScriptableObjects for local defaults and content authoring
- Versioned serializable player data with safe, encrypted local saves
- Async initialization with an explicit timeout on every step, on plain `Task` here and on UniTask in a production project
- Provider-independent adapters for authentication, remote config, server save and diagnostics
- Deterministic decoy server for the assignment
- Firebase Remote Config or equivalent behind the config interface
- Addressables for content instead of Resources, starting with local groups; remote delivery only when LiveOps needs new assets without a client release
- Automated build checks for both platforms
- Analytics/crash reporting added behind adapters for production

## Implementation order

I would build it in this order. Each step only depends on the ones before it, and the game stays playable after each one.

1. Add the Boot/Loading scene and initialization coordinator.
2. Introduce the serializable player profile and migrate the current `PlayerPrefs` values.
3. Add encrypted local persistence with backup/recovery.
4. Add authentication/account linking behind a provider-independent interface.
5. Add the deterministic decoy server and local/server selection.
6. Resolve ScriptableObject defaults through one EffectiveConfig snapshot.
7. Adapt one existing result or revive path to the popup/sequence/gameplay-flow structure.
8. Add reviewer-selectable decoy scenarios so each server case can be reproduced on demand.
9. Record build results, known limitations and actual time spent.

Steps 1 to 5 cover the startup, save, account and server flow the case asks for, and step 8 came with them because the decoy is only useful if its cases can be switched on demand. None of them require rewriting gameplay.

## Production roadmap

1. **Stabilize the core**
   Finish save migration, startup diagnostics and configuration validation.

2. **Separate feature ownership**
   Continue moving responsibilities out of the large managers, remove duplicated progression/settings ownership and move content from Resources to Addressables.

3. **Establish production operations**
   Add environments, automated builds, analytics conventions, crash reporting and service diagnostics.

4. **Harden online progression**
   Replace the decoy with real services, add proper conflict/concurrency handling and server-side validation where needed.

5. **Add meta systems on the same flow**
   Daily rewards, quests and challenges should reuse the same player-data and presentation boundaries rather than creating independent chains.

6. **Expand live content delivery when needed**
   Move only content that needs to change without a client release to a remote delivery path.

7. **Operate the live game**
   Profile on target devices, test save/schema migrations across released versions and keep server contracts backward-compatible.

## Changes to the provided project

The account slice is new code under `Assets/Scripts/Account`, so the game itself only changed where it had to read the new profile. Three scripts:

- `StatsManager` now reads and writes the player profile instead of `PlayerPrefs`. Every method kept its name and signature, so the forty or so call sites around the game were left alone.
- `SettingsPanel` takes vibration from the settings data and opens the account screen from its new button.
- `PreEndView` had an editor shortcut on the `1` key that ended the round from anywhere. It now only fires during gameplay, because typing an email address into the account screen was triggering it.

The scene and prefab changes are the account button in the settings panel with its neighbors moved down, the three settings animation clips re-keyed because the panel is taller and no longer hid fully at the old position, the bootstrap component on the `Managers` prefab, the account screen on its own overlay canvas, and `Boot` added to the build scenes.

Opening the project in 2019.4.41f2 rather than the 2019.4.20f1 it was saved with also made Unity re-serialize the project settings and bump the embedded packages. None of that is my work, so it sits in its own commit.

## Running it

Open `Assets/Scenes/Boot.unity` and press play. The splash reports each step, then the menu opens. The account screen is behind the settings button in the top left.

Everything that needs a switch is on `Assets/ScriptableObjects/SO_AccountSettings`:

- `Scenario` picks what the decoy server answers: `NoData`, `HasData`, `Failure` or `Timeout`.
- `UseMockAuthenticator` runs the whole flow without Firebase, against the account in `MockExistingEmail` and `MockExistingPassword`. The same provider takes over on its own when Firebase is missing, misconfigured or offline, so the game is never blocked by it.
- Right-click the asset and choose `Reset Local Data` to test a fresh install.

The console prefixes each area: `Boot`, `Auth`, `Sync` and `Decoy`.

## Validation

The main scenarios I would cover are:

| Scenario | Expected result |
| --- | --- |
| Fresh install | A default player is created and saved before the menu |
| Slow or unreachable server at startup | The splash ends on its own timeout and the menu opens with local data |
| Returning player | Settings/progression/customization are restored |
| Existing PlayerPrefs | Data is migrated once into the new profile |
| Damaged save file | The last backup is restored, and only a missing backup falls back to a new player |
| Sign out after signing in | The account's progression leaves the device and the pre-link backup comes back |
| Guest player, never signs in | Everything works and progression is kept under the local id |
| Credential already owns another profile | The local profile is backed up, then the account's profile is applied when it is newer |
| Sign in or out from the menu | The profile is saved and the scene is reloaded, so every screen shows the new player |
| Offline or remote timeout | Valid local data is used and the game remains playable |
| Newer server profile | Progression and customization are applied before the menu is built; device settings are left alone |
| Local + server both changed | Neither side is silently overwritten |
| Invalid remote config | Last valid value or local default stays active |
| Win/loss/revive flow | Flow completes or cancels without leaving gameplay/UI blocked |
| Clean build | Enabled scenes compile and produce a repeatable test build |

## Notes

A few requirements were open to interpretation, so these are the calls I made.

I read "online support" as accounts, cloud save, remote configuration and experiments, analytics and server time, not real-time multiplayer. The opponents in the prototype are simulated locally, so netcode would be a separate project rather than a scaling step. I also kept the Unity version as provided. An engine upgrade would mix a migration into the findings, so I would treat it as its own task.

The async code uses plain `Task`. In a production project I would use UniTask instead: it allocates nothing per await, it can await Unity's own operations such as a scene load or a frame directly, and it ties cancellation to the object's lifetime so a pending call stops when the object is destroyed. `Task` is perfectly fine at this scale, and I preferred not to pull in another dependency for a case that already adds the Firebase SDK.

I first wired the initialization into the existing scene and only added the Boot scene afterwards. The order was already correct that way, but the menu was drawn before the server answered, so a returning player saw the old level for a moment and then watched it change. A startup sequence that is correct and still shows the wrong thing first is not finished, which is the argument for the extra scene.

Time spent: about 22 hours.

## Scope and limitations

The server used for the assignment is simulated. Local encryption only discourages casual editing; it is not a server trust boundary. The key is a constant in the build, so anyone who unpacks the client can find it. Keeping it out of the client means deriving it per device, or keeping the values worth protecting on the server instead, and neither is worth doing while the server is a decoy. Remote configuration can select or tune content already shipped in the client, but new assets still require a client update until remote content delivery is introduced.

I verified everything in the editor. An Android build on this engine version comes out 32 bit with the default scripting backend, so it does not install on a 64 bit device; that belongs with the engine upgrade rather than with this work.

## Conclusion

The goal is not to turn the prototype into a large framework. The main change is clearer ownership: startup, player data, online services, configuration, gameplay and presentation stop depending on each other implicitly.

That lets the existing game keep working while making the parts that matter for a live product easier to change, configure and operate.
