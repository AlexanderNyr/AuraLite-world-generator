# AuraLite Rural World Generator (Unity 6 Edition)

A procedural generator for large rural scenes in Unity 6 (URP). It creates a village with streets, roads, fields, a distant forest, a lake, a river, and a full LOD/HLOD system. Optimized for AAA visuals using Unity 6's latest rendering features.

## What It Does

- Generates a world from 20 to 150 km^2.
- Creates a tiled Terrain grid (1024x1024 m or 2048x2048 m tiles).
- Places houses, barns, mills, forges, taverns, stables, and more.
- Builds roads, bridges, fences, and roadside props.
- Adds fields, wheat, stone piles, and hay bales.
- Generates a distant forest with LOD and distance-based streaming.
- **AAA Visuals:** Physically Based Sky, Volumetric Clouds, and Volumetric Fog.
- **Interactive Preview:** Real-time Gizmo-based preview of the layout before generation.

## New in Version 1.0.1

- **Full Unity 6 Support:** Optimized for version 6000.3.5f2 and URP 17.
- **Smart Placement:** Overhauled collision detection to prevent overlapping houses or buildings on roads.
- **Visual Polish:** Realistic non-reflective terrain and improved atmospheric lighting.
- **Interactive Layout Preview:** Visualize roads, houses, and water in the Scene View via the "Update Preview" button.

## Requirements

- **Unity 6 (6000.3.5f2)** or newer.
- **Universal Render Pipeline (URP)**.
- `UnityEditor` namespace -- the tool works inside the Unity Editor.

## Quick Start (How to Run)

1. **Clone or download** this repository.
2. **Copy** the folder `Assets/AuraLiteWorldGenerator` into your Unity project under `Assets/`.
3. **Open Unity** and wait for the scripts to compile.
4. Open the generator window:
   ```
   Tools -> Procedural Scenes -> Build AAA Rural World (URP)
   ```
5. **Adjust settings** and click **Update Preview Layout** to see the gizmos in your Scene View.
6. Click **Build AAA Rural World**.
7. The generated scene is saved to:
   ```
   Assets/GeneratedVillageScene/<SceneName>.unity
   ```

> **Note:** Large worlds (100+ km^2) can take several minutes to generate. Keep the Editor window open until the progress bar finishes.

## Project Structure

```
Assets/AuraLiteWorldGenerator/
|-- Editor/
|   |-- AuraLiteWorldGeneratorWindow.cs      # Main editor window, orchestrator & Preview logic
|   |-- WorldLayoutGenerator.cs        # OVERHAULED: Multi-stage placement logic
|   |-- TerrainGenerator.cs            # Terrain, heightmaps, splatmaps, detail layers
|   |-- AssetFactory.cs                # Unity 6 URP 17 Volume & Shader setup
|   |-- LightingAndEnvironment.cs      # Physically Based Sky & Volumetric effects
|-- Runtime/
    |-- CameraFacingBillboard.cs       # Billboard component for HLOD cards
    |-- DistanceChunkActivator.cs      # Runtime distance-based chunk streaming
```

## License

Apache 2.0

## Author

Original: [AlexanderNyr/AuraLite-world-generator](https://github.com/AlexanderNyr/AuraLite-world-generator)  
Unity 6 Update: Agent-led improvements.
