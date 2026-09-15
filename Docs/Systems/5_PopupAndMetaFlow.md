# Popup, Sequence and Gameplay Flow

## Purpose

I would keep three responsibilities separate:

- Popup system: creates/closes views and owns the modal stack/background.
- Presentation sequence: decides what should be shown and in what order.
- Gameplay flow: runs the state-changing ticks for match setup/results/revive.

They can work together without owning each other's rules.

## Popup management

A popup definition points to its prefab and optional open/close animation. The popup manager keeps one stack of open popups and a shared background.

The basic cases look like this:

```text
Open(Settings)           Settings is shown
Open(Shop)               Settings is hidden, Shop is shown on top
OpenAdditive(Confirm)    Confirm is shown, Shop stays visible behind it
Close(Confirm)           Shop is the top popup again
Close(Shop)              Settings is shown again
Open(Settings)           ignored, Settings is already in the stack
Close(Settings)          stack is empty, background closes, "all closed" is raised
```

Only the top popup can be closed, and closing it brings back the one below. The "all closed" event is what the menu or a waiting sequence listens to.

The popup manager should not decide whether a daily reward is available or whether a quest should be shown before an offer. That belongs to the feature or sequence that requested the popup.

## Presentation sequences

A sequence is an ordered list of small elements. Each element either has something to show or completes immediately.

```text
Start "MainMenu"
  1. Daily reward      eligible      → open popup, wait until it is closed
  2. Quest completed   not eligible  → nothing to show, completes immediately
  3. Offer             eligible      → open popup, wait until it is closed
Completed "MainMenu"   → normal menu navigation is unlocked
```

The rules are simple:

- Elements run one at a time, in list order. The next element does not start until the current one has completed.
- A sequence can be frozen and continued, for example when the player opens the shop halfway through. While it is frozen the next element does not start; it continues from there once it is resumed.
- Cancelling it, for example when a match starts, stops it before the next element, and the current element is expected to stop its own work too. The sequence then reports that it was cancelled instead of completed, so whoever started it always knows it has ended.

The feature owns eligibility and repeated-display state. The sequence only owns presentation order.

## Gameplay-flow ticks

A tick is one discrete action in the flow, not Unity's per-frame Update. Each tick also says whether the flow should wait for it.

```text
Win ticks
  Lock input
  Finalize score/rank
  Apply progression
  Save                  does not wait, runs in the background
  Win animation         waits until it ends
  Result popup          waits until it is closed
  Return to menu
  Post-match sequence   the main menu sequence above takes over
```

- If a tick waits, the next tick does not start until it has finished. If it does not wait, the next one starts right away.
- Ticks are grouped by moment: match start, main loop, win, lose and revive. The order inside a group comes from the prefab, so it can be changed without touching the manager.
- Win and lose do not overlap. Starting the win flow cancels a pending lose flow, and each of them runs only once even if the event fires twice.
- When the match is left or restarted, the running flow is cancelled, so an old tick cannot act on a new match.

The result popup presents already-calculated data; it does not calculate rewards or change progression.

For revive, the popup reports only the player's decision. Gameplay flow decides whether to restore the match or finalize the loss.

## Result

This gives daily rewards, quests, challenges and future LiveOps screens one consistent way to join the game without creating direct screen-to-screen chains or putting feature rules into the popup manager.
