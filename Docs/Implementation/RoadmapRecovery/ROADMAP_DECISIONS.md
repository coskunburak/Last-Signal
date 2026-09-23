# Recovery decisions

Source priority follows management/02_Kapsam_Kararlar_ve_Degisiklik.md and the current request. Technical assumptions below are reversible; they are not new permanent product decisions.

| Canonical requirement | Current implementation / conflict | Basis | Status / next action |
|---|---|---|---|
| S005 pistol | Production rifle and actual S007 ammo | Current user explicitly preserves rifle | SUPERSEDED weapon choice; retain ammo/noise requirements |
| Staged magazine reload | Round-pool single commit | LS-DOC-10 explicitly permits slice round pool; verified actual S007 contract | SUPERSEDED implementation detail; preserve magazine + carried reserve on save |
| S006 searchable containers | Visible world loot points | Actual S006 report explicitly defers searchable containers | DEFERRED_BY_ACCEPTED_DECISION for this implementation; persist generated opportunities |
| S007 cells / S008 time | Actual S007 ammo / S008 shelter | Current user: implementation order differs | STILL_REQUIRED canonical P02; no renaming/reimplementation |
| S010 shelter | Actual S008 same-scene shelter/stash/preparation | Verified final S008 evidence | PARTIAL early completion; crafting/time/upgrades still missing |
| S002 twelve items | Six useful resource definitions | Actual S005 seed scope; user says do not add filler | OPEN product catalog breadth; no silent removal of canonical requirement |
| S002 mass/capacity | Fixed slots, weight deferred in actual S005 | Preserve verified container contract; PLAN-ADR-006 still baseline | PARTIAL; reconcile after persistence, do not declare weight cancelled |
| S003 player melee | Rifle player attack, zombie melee | No explicit permanent player-melee cancellation found | OPEN product scope; canonical crowbar remains MISSING |
| S004 death bag | Death/menu/new-session only | No verified death-recovery policy | OPEN; do not invent permanent death penalty |
| External pilot | No qualifying evidence | Automated API routes are not human playtests | BLOCKED_EXTERNAL; prepare route later, never fabricate completion |
