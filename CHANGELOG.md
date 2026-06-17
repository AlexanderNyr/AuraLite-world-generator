# Changelog

## [1.1.0] - 2026-06-17

### Added
- Completed implementation for all 20 `BuildingKind` variations (Inn, Windmill, Watermill, School, Warehouse, Greenhouse, Watchtower, etc.).
- Fully functional `HydraulicErosion` droplet-based terrain erosion.
- Implemented `OrganicRoadStrategy` using A* pathfinding.
- Implemented `RiverNetworkGenerator` using flow accumulation.
- Real OBJ/PNG export in `WorldExporter`.
- Extracted `AuraLiteWorldGenerator.Runtime` and `Shared` asmdefs for proper player build support.
- Fully wired biome providers and DI services.
- Proper URP Water shader with depth color, foam, and Fresnel.
- `FBMJob` with Burst support for terrain generation.
- Expanded NUnit test coverage (Geometry, Layout, Erosion, Roads, Pipeline).

### Fixed
- Fixed compilation errors in `AuraLiteWorldGeneratorWindow` (missing pipeline initialization).
- Fixed `SetLODs` case sensitivity error.
- Fixed static state leakage in `MeshCombiner` by scoping filter destruction per execution.
- Refactored `EditorCoroutineRunner` to support multiple instances safely.
- Fixed hierarchy aliasing bugs (`WaterRoot`, `FieldsRoot`).
- Updated progress reporters to correctly nest.

### Added
- Complete architecture refactor (Pipeline, DI, Plugins).
- Biome system with 5 built-in types.
- Hydraulic and Thermal erosion for terrain.
- Procedural PBR material generation.
- Expanded building library (21 kinds).
- Unity Jobs & Burst support for performance.
- Unit and Integration tests.
- FBX export support.
- Runtime API for in-game generation.

### Changed
- Migrated from monolithic static methods to modular instance-based pipeline.
- Replaced primitive-based buildings with extruded geometry.
- Upgraded water rendering to URP HLSL with Gerstner waves.

### Fixed
- Fixed building overlap issues using spatial caching.
- Optimized draw calls via texture atlasing and mesh combining.
