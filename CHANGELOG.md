# Changelog

## [1.0.2] - 2026-06-17

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
