# AuraLite World Generator Architecture

## Assembly Definitions (asmdef)

The project is split into three main assemblies to support compilation for both the Unity Editor and the Player (Runtime):

1. **AuraLiteWorldGenerator.Runtime**  
   Contains runtime types like `WorldGeneratorConstants`, `SeededRandom`, and the `WorldGeneratorAPI` component. This assembly does not depend on `UnityEditor`.
   
2. **AuraLiteWorldGenerator.Shared**  
   Contains data types and settings shared between the runtime and editor generation systems (e.g., `GenerationSettings`, `WorldLayout`). Depends on Runtime.

3. **AuraLiteWorldGenerator.Editor**  
   Contains all procedural generation logic, layout builders, mesh combiners, editor windows, and tools. Depends on Shared and Runtime. This assembly only compiles in the Unity Editor (`#if UNITY_EDITOR` is essentially applied at the assembly level).

## World Generation Pipeline

AuraLite uses an async/await-based module pipeline. The sequence is defined in `WorldGenerationPipeline`, which processes objects implementing `IWorldGeneratorModule` in order of their `Order` property.

1. **AssetPreparationModule**: Initializes assets and loads data.
2. **LayoutGenerationModule**: Computes mathematically the placements (villages, roads, hydrology) without instantiating objects.
3. **ScenePreparationModule**: Sets up the target roots (Environment, Roads, Village, Fields, Water, ForestFar).
4. **TerrainGenerationModule**: Creates the chunked mesh terrain, paints vertex colors based on height/noise, and populates grass/wheat details.
5. **HydrologyModule**: Computes rivers using flow accumulation and creates water meshes.
6. **RoadNetworkModule**: Uses A* pathfinding (OrganicRoadStrategy) to lay down road meshes and splines.
7. **SettlementModule**: Places all 20+ types of buildings based on the layout, along with specific props per building kind.
8. **VegetationModule**: Plants forests and trees based on noise masks.
9. **PropsModule**: Adds fences, stones, lakeside props, and generic field props to their respective roots.
10. **OptimizationModule**: Performs HLOD setup and mesh combining for batching to keep draw calls low.
11. **LightingModule**: Adds the final volumetric clouds, lighting rig, and sets up cameras.

## Dependency Injection and Extensibility

The generator uses a simple Service Container (`ServiceContainer`) injected into the `GenerationContext`. This allows core algorithms to be swapped via interfaces:
- `IAssetProvider`: How the generator fetches models and materials (default: `AssetRegistryAssetProvider`).
- `ITerrainEroder`: The erosion simulation step (default: `HydraulicErosion`).
- `IRoadNetworkStrategy`: How roads are pathed (default: `OrganicRoadStrategy`).
- `IBiomeProvider`: Determining biome data for given points (default: `DefaultBiomeProvider`).
- `IBuildingProvider`: Generating the actual house geometry based on kind.

You can register your own implementations by creating a class implementing `IGeneratorPlugin` and replacing the registered service in `RegisterServices`.
