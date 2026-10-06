# VEH-001 resident route decision

Observed S013 scene: resident ground spans x [-400,0], z [-200,200], 400 x 400 m. Four physical boundary walls enclose it. POIs and the shelter are resident. Initial pickup parking is (-340,0.2,-125), yaw90, with an open 6 x 6 m clearance query. Original (-345,0.2,-125) placed a Pine collider in the passenger exit; solver correctly rejected it.

WorldCellManager.TryEnter/ReturnToResident move the player through explicit ready-gated portal transitions. CellPortal supplies proximity prefetch and explicit interaction. WorldCellContent installs its own collision/navigation. These are not seamless vehicle streaming.

Decision: first playable route remains resident-only. Existing Player action map is disabled while occupied, so portal interaction is not available to a driver. A parked pickup remains resident when the player travels on foot. No WorldCellManager topology rewrite is authorized by this implementation decision. Seamless driving remains an explicit future architecture gate.

Resident pressure follow-up: VehicleWorld provides explicit resident bounds. WorldPressureNoiseAdapter forwards canonical events in those bounds to the existing WorldPopulationManager resident pressure state. This state uses the same ordered noise receipts, decay and PopulationSnapshot, as an optional additive residentPressure section. It does not create resident migration groups or new materialized AI. Existing local listeners handle immediate hearing. Extended occupancy test proved a resident receipt and pressure snapshot before the latest production-scene authoring change. Full regression remains pending.
