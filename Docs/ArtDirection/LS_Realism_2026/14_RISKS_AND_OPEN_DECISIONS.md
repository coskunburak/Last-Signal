# Risk register / open decisions

| ID | Risk | Likelihood / impact | Mitigation | Owner / trigger |
|---|---|---|---|---|
| R01 | Mixed painted/photographic art assets | High / High | one reference art board, neutral-light swatch, reject inconsistency | art owner before pilot |
| R02 | Free store tree unusable for cutting | High / High | test separated trunk, topology, wedge/cap immediately | asset engineer before import |
| R03 | Realistic alpha cards tank GPU | High / High | LOD+impostor, overdraw/shadow profiles | tech art per pack |
| R04 | HDRP rebuild exceeds budget | Medium / High | isolated A/B, URP baseline and owner ADR gate | tech lead before conversion |
| R05 | Existing custom URP weather/wind shader unsupported | High / High if HDRP | material parity checklist + no main migration | tech art at RFC |
| R06 | Scene/TerrainData hash mismatch | High / Critical | staged copies, source checks, don't bypass guard | implementer at promotion |
| R07 | Old save restores player inside generated geometry | Medium / High | old-save test in forest and vehicle/POI | QA at placement changes |
| R08 | License unclear for specific download | Medium / Critical release | store receipt/source EULA, quarantine asset | asset owner before use |
| R09 | Zombie/character art remains cartoonish | High / Medium | separate whole-game parity pass, don't break rigs | art owner after M2 |
| R10 | Boundary fence visibly boxes forest | High / Medium | landscape transition and narrative boundary redesign | level designer M2 |
| R11 | New weather plugin duplicates WorldClock | Medium / High | prohibit competing simulation/audio authority | code review |
| R12 | Wind and cut animation geometry mismatch | Medium / High | consistent shader shadow/depth and physics authority | animation/physics QA |
| R13 | Aggressive density blocks nav, loot and vehicle | High / High | exclusion masks, manual route tests, NavMesh rebake | level designer M2 |
| R14 | Time estimates too optimistic | High / Medium | one expensive sample before committing to full assets | producer after M1 |
| R15 | Some GitHub docs have stale historical branch/status | High / Medium | source verification, dated overrides, local discovery | doc owner at integration |
| R16 | Unrealistic assumption 'all free assets are sufficient' | High / Medium | allow own Blender variants and deferral, no unverified pack claims | art lead after pilot |

## OPEN decisions (no silent assumptions)

- Exact visual target screenshots/mood reference set owner accepts (currently description and supplied Scene View, not a locked pack).
- Biome: temperate fir/pine/birch, seasonality, location cues and exact road/building regional language.
- Minimum Windows GPU/VRAM/CPU, release quality preset, target resolution and acceptable average scene density.
- Whether all foliage is harvestable or only designated persistent tree subset; maintain current domain owners.
- Which current ThirdParty packs are in local editor beyond GitHub-indexed inventory.
- Whether downloaded files permit modding and commercial embed; entitlement proof acquisition needed.
- Whether URP provides accepted visual depth after new asset placement; HDRP selection deferred to benchmark.
- Final scene boundary approach without breaking map design.

**Decision rule:** Any change to scope, core gameplay, persistent IDs or render pipeline needs a separate dated ADR with rollback and owner approval.
