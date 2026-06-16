# Changelog

All notable changes to the AuraLite Rural World Generator project will be documented in this file.

## [1.0.1] - 2026-06-16

### Added
- **Unity 6 Support:** Full optimization and support for Unity 6 (6000.3.5f2) and URP 17.
- **Preview System:** Added "Update Preview Layout" button and Scene View Gizmos to visualize the world layout (roads, houses, water) before full generation.
- **Volumetric Clouds:** Integrated native Unity 6 Volumetric Clouds with high-quality settings (128 primary steps, shadows enabled).
- **Physically Based Sky:** Replaced procedural skybox with a physically correct atmospheric model in the Global Volume.
- **Volumetric Fog:** Native fog integration in the Volume profile for deeper environmental atmosphere.

### Changed
- **Placement Logic:** Completely overhauled house placement. Buildings now check for collisions against roads, rivers, and other houses using their actual footprint dimensions.
- **Generation Stages:** Refactored the generator to run in strict logical stages: Hydrology -> Infrastructure -> Buildings.
- **Visual Improvements:** Set terrain smoothness to 0 to remove "plastic" reflections. Adjusted Sun intensity and Reflection Probe settings for a more natural look.
- **Code Quality:** Removed legacy shader fallbacks (Standard/Diffuse) in favor of strict URP Lit requirements.

### Fixed
- Fixed compilation error CS1626 (yield return in try-catch block) in the main editor window.
- Fixed ambiguous `Debug` reference between `System.Diagnostics` and `UnityEngine`.
- Fixed missing namespace reference for `CameraFacingBillboard` in `LightingAndEnvironment.cs`.
- Fixed buildings potentially spawning on water or overlapping road geometry.
