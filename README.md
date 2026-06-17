# AuraLite World Generator v2.0 (Unity 6 URP)

A professional-grade procedural world generation platform for Unity 6, designed for high-performance rural environments.

## Features

### Phase 1: Architecture
- **Pipeline-Based Generation:** Decoupled modules for assets, terrain, hydrology, and infrastructure.
- **Dependency Injection:** Lightweight service container for modularity and testability.
- **Plugin System:** Extend building types, biomes, and strategies without modifying the core.
- **Structured Logging:** Detailed generation reports with timing and validation.

### Phase 2: Core Generation
- **Multi-Biome System:** Blended biomes based on procedural moisture and temperature.
- **Hydraulic Erosion:** Realistic landscape weathering (Hydraulic & Thermal).
- **Advanced Hydrology:** Flow-accumulation based river networks and basin-filling lakes.
- **Organic Road Networks:** A* pathfinding and L-System based rural roads.

### Phase 3: Visual Quality
- **Procedural PBR:** Automatic generation of Lit materials with Normal and Mask maps.
- **Enhanced Geometry:** Detailed architectural meshes (frames, eaves, chimneys) instead of primitives.
- **AAA Water Shader:** Gerstner waves, foam, and flow-map support.
- **Volumetrics:** Integrated volumetric fog and improved cloud impostors.

### Phase 4: Content & Variety
- **21+ Building Types:** Including Taverns, Windmills, Watchtowers, and more.
- **Aging System:** Procedural ruins and weathered material variations.
- **Populated World:** NPC placement, animal pens, and interactive village props.

### Phase 5: Performance
- **Unity Jobs & Burst:** Multi-threaded FBM and erosion algorithms.
- **Texture Atlasing:** Massive reduction in draw calls via automatic material batching.
- **Smart Caching:** Persistent asset caching to speed up subsequent generations.

### Phase 6: Professional Tooling
- **FBX Export:** Export combined meshes for external processing.
- **Runtime API:** Generate worlds in Play Mode or via scripts.
- **Scene Validator:** Automatic detection of overlapping geometry or floating objects.
- **Unit Tested:** High coverage for core mathematical and random utilities.

## Installation
1. Clone the repository into your `Assets/` folder.
2. Ensure you are using **Unity 6 (6000.0+)** and **URP 17**.
3. (Optional) Install `Unity.Burst` and `Unity.Mathematics` for maximum performance.

## Usage
- **Editor:** Go to `Tools -> Procedural Scenes -> Build AAA Rural World (URP)`.
- **Scripting:** 
  ```csharp
  var api = gameObject.AddComponent<WorldGeneratorAPI>();
  await api.GenerateAsync(seed, settings);
  ```

## License
Apache 2.0
