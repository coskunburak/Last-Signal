# R05 Foundation Debt Register

| ID | Area | Observed Evidence | Risk | Affected Systems | Required Action | Status | Verification |
|---|---|---|---|---|---|---|---|
| R05-01 | Architecture | `WorldPopulationManager` retained obsolete `ReportShot` legacy API | Low | World Population | Remove `ReportShot` | HARDEN NOW | Code removed and `WorldPopulationPlayTests.cs` updated to use `ReportNoise` |
| R05-02 | Persistence | Both `weapon` (firearm state) and `combat` (slot/stamina) fields exist in `SaveGame` | None | Save/Load | None (False Positive) | NOT AN ISSUE | They store mutually exclusive necessary state. |
| R05-03 | Tests | `SaveValidationTests` throws NullReferenceException on `lastProcessed` check due to missing `worldTime` mock in fixture | Low | Tests | Initialize `worldTime` | HARDEN NOW | EditMode tests updated and passing |
