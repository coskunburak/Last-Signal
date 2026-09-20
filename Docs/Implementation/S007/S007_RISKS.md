# S007 Risks

- **Balance Risk**: Economy currently guarantees 5-20 rounds per Security drop, which might make ammo too scarce or too abundant depending on zombie population.
- **Missing References Risk**: While fixed, the UI and combat systems rely heavily on event subscriptions. `AcceptanceHud` prevents leaks via explicit unbinding, but future UI additions must maintain this rigor.
- **Timing Robustness**: Commit times (1.667s tactical, 1.83s empty) are strictly tied to current animations. If animations change, code timings must be manually updated since there are no AnimationEvents.
