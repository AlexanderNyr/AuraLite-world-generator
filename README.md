# AuraLite World Generator (Unity 6 URP)

AuraLite is a procedural rural world generator for Unity 6 (URP 17). It dynamically generates massive tile-based landscapes, complete with roads, villages, rivers, forests, and fields.

## Features

- **Procedural Landscape**: Tiled FBM terrain with optional Hydraulic Erosion.
- **Village Generation**: 20 distinct `BuildingKind` variations dynamically generated from geometry.
- **Road & River Networks**: A* organic roads and flow-accumulation rivers.
- **Biomes**: Mixed procedural biomes affecting splatmaps.
- **Water Shader**: URP-compliant water with depth fade, foam, and dynamic waves.
- **High Performance**: `MeshCombiner` and HLOD grouping, Burst-compiled terrain noise, and asynchronous generator pipeline.
- **Exporting**: Export procedural scenes to OBJ and PNG heightmaps.

## Architecture

The project is split into Runtime, Editor, and Shared assemblies. You can use the `WorldGeneratorAPI` component to trigger world generation at runtime, while the `AuraLiteWorldGeneratorWindow` provides an extensive UI for editor generation. See `ARCHITECTURE.md` for more details.
