# Network Synchronization and Decoy Server

## Purpose

All server save access should go through one client boundary so gameplay and UI do not care which backend is connected.

For the case, I would use a deterministic decoy rather than build a real backend. It only needs to reproduce the situations the client must handle.

## Decoy behavior

The decoy can return:

```text
NoProfile
ProfileAvailable
TemporaryFailure
Timeout
```

For ProfileAvailable, the response includes the profile revision/update information and the full profile when needed.

A separate lightweight metadata call can be useful if profiles become large, but I would not add an extra round trip unless the backend/profile size actually justifies it.

## Resolution rules

- Server unchanged + local clean → keep local data.
- Server unchanged + local dirty → keep local changes and queue/upload later.
- Server newer + local clean → validate and apply server data.
- Server newer + local dirty → treat it as a conflict; do not pick a winner from timestamp alone.
- No server profile → continue locally.
- Failure/timeout → continue with valid local data and retry later.

Server revision, save schema version and timestamps solve different problems and should stay separate.

## Offline behavior

Offline play continues from the local profile. When the connection returns, synchronization can run again in the background. Any downloaded profile that would affect gameplay is staged until a safe boundary such as returning to the menu.

## Result

The case can demonstrate success, no-data, conflict and failure paths without coupling the game to a specific backend implementation.
