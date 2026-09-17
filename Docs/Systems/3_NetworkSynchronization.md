# Network Synchronization and Decoy Server

## Purpose

All server save access should go through one client boundary so gameplay and UI do not care which backend is connected.

For the case, I would use a deterministic decoy rather than build a real backend. It only needs to reproduce the situations the client must handle.

## Decoy behavior

The decoy can return:

```text
NoData
HasData
Failure
Timeout
```

The first call always asks for the revision. `NoData` answers that there is no profile, `HasData` answers with one once the player is signed in to an account, and the full profile is a second call that only happens when that revision is ahead of the local one.

The existence check is that same metadata call. It costs no extra round trip, because the client has to ask whether server data exists in any case, and it keeps the payload off the wire in the common launch where the server is not ahead.

The same idea does not pay off on the local file. The game needs the profile's contents on every launch anyway, so there is no moment where knowing the revision without reading the file would save any work.

Either way the revision stays a field inside the record and never becomes part of its name. One record per player, updated in place, on the server and on the device. A name that carries the revision would mean a new file on every save and an old one to clean up, and the local writes would lose the write-to-a-temporary-file-first step that keeps an interrupted save from destroying the previous one. Keeping history is then a deliberate decision with a bound, not a side effect of how records are named.

## Resolution rules

- Server unchanged + local clean → keep local data.
- Server unchanged + local dirty → keep local changes and queue/upload later.
- Server newer + local clean → validate and apply server data.
- Server newer + local dirty → treat it as a conflict; do not pick a winner from timestamp alone.
- No server profile → continue locally.
- Failure/timeout → continue with valid local data and retry later.

Server revision, save schema version and timestamps solve different problems and should stay separate.

## Why a revision and not a timestamp

The decision is made on a revision that the server assigns and the client only reports back. A device clock is not evidence: players move it forward to skip timers, and two honest devices still disagree by minutes, so the copy with the later timestamp is not reliably the copy with more progress. Making a timestamp trustworthy means having the server stamp it, and at that point a counter is simpler and exact.

The revision also carries something a timestamp cannot. An upload says which revision it was based on, so a server that has since moved on can reject the write instead of losing whatever arrived in between. That is what turns "both sides changed" from a guess into a detectable case. Timestamps stay useful for telling the player when the profile last synced.

## Offline behavior

Offline play continues from the local profile. When the connection returns, synchronization can run again in the background. Any downloaded profile that would affect gameplay is staged until a safe boundary such as returning to the menu.

## Result

The case demonstrates the success, no-data and failure paths against the decoy without coupling the game to a specific backend implementation. The conflict path is described here as the production rule.
