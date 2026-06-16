# AuraLite Rural World Generator

A procedural generator for large rural scenes in Unity (URP/Built-in). It creates a village with streets, roads, fields, a distant forest, a lake, a river, and a full LOD/HLOD system.

## What It Does

- Generates a world from 20 to 150 km^2.
- Creates a tiled Terrain grid (1024x1024 m or 2048x2048 m tiles).
- Places houses, barns, mills, forges, taverns, stables, and more.
- Builds roads, bridges, fences, and roadside props.
- Adds fields, wheat, stone piles, and hay bales.
- Generates a distant forest with LOD and distance-based streaming.
- Sets up lighting, camera, reflection probe, fog, global Volume (Bloom, Tonemapping, Color Adjustments), and clouds.

## Requirements

- Unity 2021.3 or newer (Unity 2022.2+ recommended for full URP feature support).
- Universal Render Pipeline (URP) is recommended. The tool falls back to `Standard`/`Diffuse` shaders if URP is not available.
- `UnityEditor` namespace -- the tool works only inside the Unity Editor.

## Quick Start (How to Run)

1. **Clone or download** this repository.
2. **Copy** the folder `Assets/AuraLiteWorldGenerator` into your Unity project under `Assets/`.
3. **Open Unity** and wait for the scripts to compile.
4. Open the generator window:
   ```
   Tools -> Procedural Scenes -> Build AAA Rural World (URP)
   ```
5. **Adjust settings** in the window (seed, map area, village size, quality, etc.).
6. Click **Build AAA Rural World**.
7. The generated scene is saved to:
   ```
   Assets/GeneratedVillageScene/<SceneName>.unity
   ```
   (default output folder; changeable via `Output Root`).

> **Note:** Large worlds (100+ km^2) can take several minutes to generate. Keep the Editor window open until the progress bar finishes.

## Installation

The same steps as Quick Start. There is no package manager setup required; the tool is a drop-in set of Editor + Runtime scripts.


## Project Structure

```
Assets/AuraLiteWorldGenerator/
|-- Editor/
|   |-- AuraLiteWorldGeneratorWindow.cs      # Main editor window and orchestrator
|   |-- GenerationSettings.cs          # Generator settings with validation
|   |-- WorldLayout.cs                 # Data classes: houses, roads, layout, build context
|   |-- WorldLayoutGenerator.cs        # Road, river, village, and house layout generation
|   |-- TerrainGenerator.cs            # Terrain, heightmaps, splatmaps, detail layers
|   |-- BuildingFactory.cs             # House specification factory
|   |-- BuildingBuilder.cs             # Building construction with props and LODs
|   |-- RoadGenerator.cs               # Roads, bridge, street fences
|   |-- WaterGenerator.cs              # Lake, river, water vegetation
|   |-- VillagePropsGenerator.cs       # Village greenery, street props, lake shore
|   |-- FieldPropsGenerator.cs         # Hedgerows, crop rows, stones, hay bales
|   |-- ForestGenerator.cs             # Distant forest
|   |-- LightingAndEnvironment.cs      # Lighting, camera, fog, Volume, clouds
|   |-- OptimizationSystem.cs          # HLOD proxies and distance streaming chunks
|   |-- BuildContextFactory.cs         # Asset folder and resource preparation
|   |-- AssetFactory.cs                # Materials, textures, meshes, prefabs creation
|   |-- MeshFactory.cs                 # Procedural meshes (roof, grass, cloud, cone)
|   |-- GeometryHelpers.cs             # Math, noise, paths, masks
|   |-- GameObjectBuilder.cs           # Primitive creation helpers
|   |-- LODHelpers.cs                  # LODGroup utilities
|   |-- VegetationBuilder.cs           # Trees, shrubs, reeds
|-- Runtime/
    |-- CameraFacingBillboard.cs       # Billboard component for HLOD cards
    |-- DistanceChunkActivator.cs      # Runtime distance-based chunk streaming
    |-- WorldGeneratorConstants.cs     # Shared constants and enums
```

## Generator Settings

| Setting | Description |
|--------|-------------|
| `Scene Name` | Name of the saved scene asset |
| `Seed` | Seed for deterministic generation |
| `Create New Scene` | Generate into a new empty scene |
| `Save Scene Asset` | Save the generated scene to disk |
| `Map Area (km^2)` | Total world area (20-150) |
| `Terrain Height Range (m)` | Height range of the terrain |
| `Village Length (m)` | Length of the village area |
| `Main Street Width (m)` | Width of the main village street |
| `Village Lane Width (m)` | Width of smaller lanes |
| `House Density` | Density of placed houses |
| `Wheat Field Ratio` | Fraction of farmland used as wheat fields |
| `Quality Boost` | Multiplier for detail, LOD, and chunk count |
| `Village Style` | European or Russian architectural style |
| `Fog Start / End (km)` | Linear fog distances |
| `Output Root` | Folder where generated assets are saved |

## Important Notes

- All generated assets are created under `Assets/GeneratedVillageScene` (or the folder specified in `Output Root`).
- The URP Volume features require the `Universal RP` package to be installed.
- If URP is not found, the generator falls back to `Standard`/`Diffuse` shaders.
- Runtime components (`CameraFacingBillboard`, `DistanceChunkActivator`) are placed in the `Runtime` folder so they work in builds.

## License

MIT License -- free to use and modify.

## Author

Original: [AlexanderNyr/AuraLite-world-generator](https://github.com/AlexanderNyr/AuraLite-world-generator)  
Improved by community contributions.
