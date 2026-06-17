using AuraLiteWorldGenerator.Runtime;
using UnityEngine;
using UnityEngine.Rendering;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Constructs the actual building GameObjects from HouseSpec data, including detail props, yards and LODs.
    /// </summary>
    public static class BuildingBuilder
    {
        public static void Build(BuildContext ctx, TerrainGrid grid, HouseSpec spec, Transform parent, int index, VillageStyle style, SeededRandom random)
        {
            if (random == null)
                throw new System.ArgumentNullException(nameof(random));

            GameObject root = new GameObject(spec.kind + "_" + index.ToString("00"));
            root.transform.SetParent(parent);
            Vector3 pos = spec.position;
            pos.y = GeometryHelpers.SampleTerrainHeight(grid, pos);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, spec.yaw, 0f);

            BuildingProfile profile = CreateProfile(ctx, spec, style, random);
            CreateMainStructure(ctx, root.transform, profile, random);
            AddWindows(ctx, root.transform, profile);
            AddTimberFraming(ctx, root.transform, profile);
            AddChimney(ctx, root.transform, profile, random);
            AddKindSpecificDetails(ctx, root.transform, profile, random);
            AddYardProps(ctx, root.transform, profile, random);

            if (spec.fenced)
            {
                CreateFenceLoop(ctx, root.transform, new Vector3(0f, 0f, -profile.depth * 0.06f), profile.width * 1.9f, profile.depth * 2.1f, 3.0f);
                if (spec.garden)
                    CreateGardenShrubs(ctx, root.transform, profile.width, profile.depth, random);
            }

            SetupBuildingLOD(ctx, root, profile);
            GameObjectBuilder.MarkStaticRecursive(root);
        }

        private struct BuildingProfile
        {
            public BuildingKind kind;
            public float width;
            public float depth;
            public float height;
            public float roofHeight;
            public Material wall;
            public Material roof;
            public bool russian;
            public bool annex;
        }

        private static BuildingProfile CreateProfile(BuildContext ctx, HouseSpec spec, VillageStyle style, SeededRandom random)
        {
            BuildingProfile p = new BuildingProfile
            {
                kind = spec.kind,
                width = spec.footprint.x,
                depth = spec.footprint.y,
                height = spec.height,
                roofHeight = Mathf.Lerp(1.8f, 3.2f, spec.height / 7.2f),
                russian = style == VillageStyle.Russian,
                annex = spec.annex
            };

            ApplyStyle(ctx, ref p, random);
            ApplyKindScaleAndMaterials(ctx, ref p);
            return p;
        }

        private static void ApplyStyle(BuildContext ctx, ref BuildingProfile p, SeededRandom random)
        {
            if (p.russian)
            {
                p.wall = ctx.logWallMat;
                p.roof = random.Value > 0.35f ? ctx.roofDarkMat : ctx.roofRedMat;
            }
            else
            {
                bool warmWall = p.kind == BuildingKind.Barn || p.kind == BuildingKind.Workshop || p.kind == BuildingKind.Forge || p.kind == BuildingKind.Mill;
                p.wall = warmWall ? ctx.wallWarmMat : ctx.wallCreamMat;
                p.roof = (p.kind == BuildingKind.Barn || random.Value > 0.55f) ? ctx.roofDarkMat : ctx.roofRedMat;
            }
        }

        private static void ApplyKindScaleAndMaterials(BuildContext ctx, ref BuildingProfile p)
        {
            switch (p.kind)
            {
                case BuildingKind.LongHouse:
                    p.width *= 1.18f;
                    p.roofHeight += 0.25f;
                    break;
                case BuildingKind.Manor:
                    p.width *= 1.25f;
                    p.depth *= 1.18f;
                    p.height *= 1.18f;
                    p.roofHeight += 0.45f;
                    break;
                case BuildingKind.Chapel:
                    p.width *= 0.94f;
                    p.depth *= 1.18f;
                    p.height *= 1.22f;
                    p.roof = p.russian ? ctx.copperRoofMat : ctx.roofDarkMat;
                    p.wall = ctx.wallCreamMat;
                    break;
                case BuildingKind.Forge:
                    p.width *= 1.08f;
                    p.depth *= 1.04f;
                    p.wall = p.russian ? ctx.logWallMat : ctx.wallWarmMat;
                    p.roof = ctx.roofDarkMat;
                    break;
                case BuildingKind.Mill:
                    p.width *= 1.06f;
                    p.depth *= 1.06f;
                    p.height *= 1.22f;
                    p.roof = p.russian ? ctx.roofDarkMat : ctx.roofRedMat;
                    p.wall = p.russian ? ctx.logWallMat : ctx.wallCreamMat;
                    break;
                case BuildingKind.Tavern:
                    p.width *= 1.18f;
                    p.depth *= 1.12f;
                    p.height *= 1.08f;
                    p.roofHeight += 0.35f;
                    p.wall = p.russian ? ctx.logWallMat : ctx.wallCreamMat;
                    p.roof = ctx.roofDarkMat;
                    break;
                case BuildingKind.Stable:
                    p.depth *= 1.08f;
                    p.wall = p.russian ? ctx.logWallMat : ctx.wallWarmMat;
                    p.roof = ctx.roofDarkMat;
                    break;
                case BuildingKind.Granary:
                    p.height *= 1.12f;
                    p.roof = ctx.roofDarkMat;
                    p.wall = p.russian ? ctx.logWallMat : ctx.wallWarmMat;
                    break;
                case BuildingKind.Boathouse:
                    p.depth *= 1.18f;
                    p.wall = p.russian ? ctx.logWallMat : ctx.wallWarmMat;
                    p.roof = ctx.roofDarkMat;
                    break;
                case BuildingKind.Inn:
                    p.width *= 1.2f;
                    p.depth *= 1.1f;
                    p.height *= 1.1f;
                    p.wall = p.russian ? ctx.logWallMat : ctx.wallCreamMat;
                    break;
                case BuildingKind.Windmill:
                    p.width *= 1.0f;
                    p.depth *= 1.0f;
                    p.height *= 1.4f;
                    p.wall = p.russian ? ctx.logWallMat : ctx.wallWarmMat;
                    p.roof = ctx.roofDarkMat;
                    break;
                case BuildingKind.Watermill:
                    p.wall = p.russian ? ctx.logWallMat : ctx.wallWarmMat;
                    break;
                case BuildingKind.School:
                    p.width *= 1.3f;
                    p.wall = p.russian ? ctx.logWallMat : ctx.wallCreamMat;
                    break;
                case BuildingKind.Warehouse:
                    p.width *= 1.4f;
                    p.depth *= 1.2f;
                    p.height *= 1.1f;
                    p.wall = p.russian ? ctx.logWallMat : ctx.wallWarmMat;
                    p.roof = ctx.roofDarkMat;
                    break;
                case BuildingKind.Greenhouse:
                    p.wall = ctx.glassMat ?? ctx.wallCreamMat;
                    p.roof = ctx.glassMat ?? ctx.roofDarkMat;
                    break;
                case BuildingKind.Watchtower:
                    p.width *= 0.8f;
                    p.depth *= 0.8f;
                    p.height *= 1.8f;
                    p.wall = p.russian ? ctx.logWallMat : ctx.wallWarmMat;
                    p.roof = ctx.roofDarkMat;
                    break;
            }
        }

        private static void CreateMainStructure(BuildContext ctx, Transform root, BuildingProfile p, SeededRandom random)
        {
            Material wall = p.wall ?? ctx.wallCreamMat;
            Material roof = p.roof ?? ctx.roofDarkMat;

            // Foundation/Basement to handle slopes better
            GameObjectBuilder.CreateCubeChild(root, "StoneBase", new Vector3(0f, -0.4f, 0f), new Vector3(p.width * 1.05f, 1.2f, p.depth * 1.05f), ctx.stoneMat);
            
            // Main body
            GameObjectBuilder.CreateCubeChild(root, "Body", new Vector3(0f, p.height * 0.5f, 0f), new Vector3(p.width, p.height, p.depth), wall);
            
            // Roof with better overhang
            float overH = 0.4f;
            GameObjectBuilder.CreateMeshChild(root, "Roof", ctx.roofMesh, new Vector3(0f, p.height - 0.05f, 0f), new Vector3(p.width + overH, p.roofHeight, p.depth + overH), roof);
            
            // Trim / Plinth
            GameObjectBuilder.CreateCubeChild(root, "SillBand", new Vector3(0f, 0.15f, p.depth * 0.51f), new Vector3(p.width * 1.02f, 0.25f, 0.12f), ctx.timberMat);

            if (p.annex && p.kind != BuildingKind.Barn)
            {
                float side = random.Value > 0.5f ? -1f : 1f;
                float aw = p.width * 0.45f;
                float ad = p.depth * 0.55f;
                float ah = p.height * 0.75f;
                GameObjectBuilder.CreateCubeChild(root, "Annex", new Vector3(side * (p.width * 0.52f), ah * 0.5f, -p.depth * 0.05f), new Vector3(aw, ah, ad), wall);
                GameObjectBuilder.CreateMeshChild(root, "AnnexRoof", ctx.roofMesh, new Vector3(side * (p.width * 0.52f), ah - 0.02f, -p.depth * 0.05f), new Vector3(aw + overH * 0.6f, p.roofHeight * 0.75f, ad + overH * 0.6f), roof);
            }

            float doorW = GetDoorWidth(p.kind);
            float doorH = GetDoorHeight(p.kind);
            GameObjectBuilder.CreateCubeChild(root, "Door", new Vector3(0f, doorH * 0.5f, p.depth * 0.505f), new Vector3(doorW, doorH, 0.2f), ctx.timberMat);
            GameObjectBuilder.CreateCubeChild(root, "DoorLintel", new Vector3(0f, doorH + 0.1f, p.depth * 0.52f), new Vector3(doorW + 0.3f, 0.2f, 0.2f), ctx.stoneMat);
        }

        private static float GetDoorWidth(BuildingKind kind)
        {
            switch (kind)
            {
                case BuildingKind.Barn: return 2.6f;
                case BuildingKind.Manor: return 1.55f;
                case BuildingKind.Chapel: return 1.4f;
                case BuildingKind.Tavern: return 1.55f;
                default: return 1.2f;
            }
        }

        private static float GetDoorHeight(BuildingKind kind)
        {
            switch (kind)
            {
                case BuildingKind.Barn: return 3.0f;
                case BuildingKind.Chapel: return 2.9f;
                case BuildingKind.Tavern: return 2.65f;
                default: return 2.45f;
            }
        }

        private static void AddWindows(BuildContext ctx, Transform root, BuildingProfile p)
        {
            if (p.kind == BuildingKind.Chapel)
            {
                GameObjectBuilder.CreateCubeChild(root, "ChapelWindowFront", new Vector3(0f, p.height * 0.64f, p.depth * 0.505f), new Vector3(0.85f, 1.8f, 0.08f), ctx.glassMat);
                GameObjectBuilder.CreateCubeChild(root, "ChapelWindowBack", new Vector3(0f, p.height * 0.64f, -p.depth * 0.505f), new Vector3(0.85f, 1.8f, 0.08f), ctx.glassMat);
                GameObjectBuilder.CreateCubeChild(root, "ChapelWindowSideL", new Vector3(-p.width * 0.505f, p.height * 0.58f, 0f), new Vector3(0.08f, 1.55f, 0.9f), ctx.glassMat);
                GameObjectBuilder.CreateCubeChild(root, "ChapelWindowSideR", new Vector3(p.width * 0.505f, p.height * 0.58f, 0f), new Vector3(0.08f, 1.55f, 0.9f), ctx.glassMat);
                return;
            }

            if (p.kind == BuildingKind.Barn)
            {
                GameObjectBuilder.CreateCubeChild(root, "LoftDoor", new Vector3(0f, p.height * 0.72f, p.depth * 0.505f), new Vector3(1.8f, 1.2f, 0.12f), ctx.timberMat);
                GameObjectBuilder.CreateCubeChild(root, "SideWindowL", new Vector3(-p.width * 0.505f, p.height * 0.56f, p.depth * 0.10f), new Vector3(0.1f, 0.9f, 1.0f), ctx.glassMat);
                GameObjectBuilder.CreateCubeChild(root, "SideWindowR", new Vector3(p.width * 0.505f, p.height * 0.56f, -p.depth * 0.12f), new Vector3(0.1f, 0.9f, 1.0f), ctx.glassMat);
                return;
            }

            bool tall = p.kind == BuildingKind.Manor || p.kind == BuildingKind.LongHouse || p.kind == BuildingKind.Tavern;
            int frontCount = tall ? 3 : 2;
            float y1 = p.height * 0.54f;
            float spacing = p.width / (frontCount + 1f);
            for (int i = 0; i < frontCount; i++)
            {
                float x = -p.width * 0.5f + spacing * (i + 1);
                GameObjectBuilder.CreateCubeChild(root, "WindowFront_" + i, new Vector3(x, y1, p.depth * 0.505f), new Vector3(0.95f, 1.0f, 0.08f), ctx.glassMat);
                GameObjectBuilder.CreateCubeChild(root, "WindowBack_" + i, new Vector3(x, y1, -p.depth * 0.505f), new Vector3(0.95f, 1.0f, 0.08f), ctx.glassMat);
                if (tall)
                    GameObjectBuilder.CreateCubeChild(root, "WindowTop_" + i, new Vector3(x, p.height * 0.75f, p.depth * 0.505f), new Vector3(0.84f, 0.86f, 0.08f), ctx.glassMat);
            }

            GameObjectBuilder.CreateCubeChild(root, "WindowSideL", new Vector3(-p.width * 0.505f, y1, p.depth * 0.15f), new Vector3(0.08f, 0.95f, 1.0f), ctx.glassMat);
            GameObjectBuilder.CreateCubeChild(root, "WindowSideR", new Vector3(p.width * 0.505f, y1, -p.depth * 0.15f), new Vector3(0.08f, 0.95f, 1.0f), ctx.glassMat);
        }

        private static void AddTimberFraming(BuildContext ctx, Transform root, BuildingProfile p)
        {
            if (p.kind == BuildingKind.Barn || p.kind == BuildingKind.Chapel)
                return;

            bool heavy = p.kind != BuildingKind.Cottage;
            float beam = heavy ? 0.18f : 0.14f;
            GameObjectBuilder.CreateCubeChild(root, "BeamTopFront", new Vector3(0f, p.height * 0.92f, p.depth * 0.51f), new Vector3(p.width * 1.02f, beam, 0.10f), ctx.timberMat);
            GameObjectBuilder.CreateCubeChild(root, "BeamMidFront", new Vector3(0f, p.height * 0.48f, p.depth * 0.51f), new Vector3(p.width * 1.02f, beam * 0.9f, 0.10f), ctx.timberMat);
            GameObjectBuilder.CreateCubeChild(root, "BeamVL", new Vector3(-p.width * 0.40f, p.height * 0.47f, p.depth * 0.51f), new Vector3(beam, p.height * 0.88f, 0.10f), ctx.timberMat);
            GameObjectBuilder.CreateCubeChild(root, "BeamVR", new Vector3(p.width * 0.40f, p.height * 0.47f, p.depth * 0.51f), new Vector3(beam, p.height * 0.88f, 0.10f), ctx.timberMat);
            if (heavy)
                GameObjectBuilder.CreateCubeChild(root, "BeamCenter", new Vector3(0f, p.height * 0.47f, p.depth * 0.51f), new Vector3(beam, p.height * 0.88f, 0.10f), ctx.timberMat);
        }

        private static void AddChimney(BuildContext ctx, Transform root, BuildingProfile p, SeededRandom random)
        {
            float x = random.Value > 0.5f ? p.width * 0.22f : -p.width * 0.22f;
            GameObjectBuilder.CreateCubeChild(root, "Chimney", new Vector3(x, p.height + p.roofHeight * 0.8f, 0f), new Vector3(0.72f, 2.0f, 0.72f), ctx.stoneMat);
        }

        private static void AddKindSpecificDetails(BuildContext ctx, Transform root, BuildingProfile p, SeededRandom random)
        {
            switch (p.kind)
            {
                case BuildingKind.Workshop: CreateWorkshopSign(ctx, root, p); break;
                case BuildingKind.Barn: CreateBarnProps(ctx, root, p); break;
                case BuildingKind.Chapel: CreateChapelDetails(ctx, root, p); break;
                case BuildingKind.Forge: CreateForgeDetails(ctx, root, p); break;
                case BuildingKind.Mill: CreateMillDetails(ctx, root, p); break;
                case BuildingKind.Tavern: CreateTavernDetails(ctx, root, p); break;
                case BuildingKind.Stable: CreateStableDetails(ctx, root, p); break;
                case BuildingKind.Granary: CreateGranaryDetails(ctx, root, p); break;
                case BuildingKind.Boathouse: CreateBoathouseDetails(ctx, root, p); break;
                case BuildingKind.Inn: CreateInnDetails(ctx, root, p); break;
                case BuildingKind.Windmill: CreateWindmillDetails(ctx, root, p); break;
                case BuildingKind.Watermill: CreateWatermillDetails(ctx, root, p); break;
                case BuildingKind.School: CreateSchoolDetails(ctx, root, p); break;
                case BuildingKind.Warehouse: CreateWarehouseDetails(ctx, root, p); break;
                case BuildingKind.Greenhouse: CreateGreenhouseDetails(ctx, root, p); break;
                case BuildingKind.Watchtower: CreateWatchtowerDetails(ctx, root, p); break;
            }
        }

        private static void CreateInnDetails(BuildContext ctx, Transform root, BuildingProfile p)
        {
            // Sign
            GameObject sign = new GameObject("InnSign");
            sign.transform.SetParent(root, false);
            sign.transform.localPosition = new Vector3(-p.width * 0.45f, p.height * 0.65f, p.depth * 0.55f);
            GameObjectBuilder.CreateCubeChild(sign.transform, "Arm", Vector3.zero, new Vector3(0.12f, 1.0f, 0.12f), ctx.timberMat);
            GameObjectBuilder.CreateCubeChild(sign.transform, "Board", new Vector3(0.4f, -0.1f, 0f), new Vector3(0.6f, 0.5f, 0.1f), ctx.timberMat);

            // Barrels
            GameObjectBuilder.CreateCylinderChild(root, "Barrel1", new Vector3(p.width * 0.3f, 0.4f, p.depth * 0.55f), Quaternion.Euler(0, 15, 0), new Vector3(0.6f, 0.4f, 0.6f), ctx.timberMat);
            GameObjectBuilder.CreateCylinderChild(root, "Barrel2", new Vector3(p.width * 0.45f, 0.4f, p.depth * 0.55f), Quaternion.Euler(0, -10, 0), new Vector3(0.6f, 0.4f, 0.6f), ctx.timberMat);
            
            // Canopy over entrance
            GameObjectBuilder.CreateCubeChild(root, "Canopy", new Vector3(0f, p.height * 0.5f, p.depth * 0.55f), new Vector3(p.width * 0.4f, 0.1f, 1.0f), ctx.roofDarkMat);
        }

        private static void CreateWindmillDetails(BuildContext ctx, Transform root, BuildingProfile p)
        {
            // Tower base is replaced by body, we add cone roof and sails
            if (ctx.coneMesh != null)
            {
                var r2 = root.Find("Roof");
                if (r2 != null) GameObject.DestroyImmediate(r2.gameObject);
                
                GameObject cone = new GameObject("ConeRoof");
                cone.transform.SetParent(root, false);
                cone.transform.localPosition = new Vector3(0f, p.height + 0.5f, 0f);
                cone.transform.localScale = new Vector3(p.width * 1.2f, 2.0f, p.depth * 1.2f);
                MeshFilter mf = cone.AddComponent<MeshFilter>();
                mf.sharedMesh = ctx.coneMesh;
                cone.AddComponent<MeshRenderer>().sharedMaterial = p.roof;
            }

            GameObject sailsNode = new GameObject("Sails");
            sailsNode.transform.SetParent(root, false);
            sailsNode.transform.localPosition = new Vector3(0f, p.height * 0.7f, p.depth * 0.55f);
            
            // Central hub
            GameObjectBuilder.CreateCubeChild(sailsNode.transform, "Hub", Vector3.zero, new Vector3(0.6f, 0.6f, 0.8f), ctx.timberMat);
            
            // 4 sails
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f;
                GameObject sail = new GameObject($"Sail_{i}");
                sail.transform.SetParent(sailsNode.transform, false);
                sail.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                // Arm
                GameObjectBuilder.CreateCubeChild(sail.transform, "Arm", new Vector3(0f, 2.0f, 0.2f), new Vector3(0.15f, 4.0f, 0.15f), ctx.timberMat);
                // Canvas
                GameObjectBuilder.CreateCubeChild(sail.transform, "Canvas", new Vector3(0.4f, 2.5f, 0.25f), new Vector3(0.8f, 3.0f, 0.05f), ctx.wallCreamMat);
            }
        }

        private static void CreateWatermillDetails(BuildContext ctx, Transform root, BuildingProfile p)
        {
            GameObject wheelNode = new GameObject("WaterWheel");
            wheelNode.transform.SetParent(root, false);
            // Placed to the side
            wheelNode.transform.localPosition = new Vector3(-p.width * 0.55f, 1.5f, 0f);
            
            // Hub
            GameObjectBuilder.CreateCylinderChild(wheelNode.transform, "Hub", Vector3.zero, Quaternion.Euler(0, 0, 90), new Vector3(0.8f, 1.0f, 0.8f), ctx.timberMat);
            
            // Blades
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                GameObject blade = new GameObject($"Blade_{i}");
                blade.transform.SetParent(wheelNode.transform, false);
                blade.transform.localRotation = Quaternion.Euler(angle, 0f, 0f);
                GameObjectBuilder.CreateCubeChild(blade.transform, "Board", new Vector3(0f, 0f, 1.5f), new Vector3(0.8f, 0.1f, 1.0f), ctx.timberMat);
            }
            
            // Trough
            GameObjectBuilder.CreateCubeChild(root, "Trough", new Vector3(-p.width * 0.55f, 3.0f, -p.depth * 0.6f), new Vector3(0.6f, 0.4f, p.depth), ctx.timberMat);
        }

        private static void CreateSchoolDetails(BuildContext ctx, Transform root, BuildingProfile p)
        {
            // Porch
            GameObjectBuilder.CreateCubeChild(root, "Porch", new Vector3(0f, 0.4f, p.depth * 0.6f), new Vector3(p.width * 0.6f, 0.8f, 1.2f), ctx.timberMat);
            // Bell on roof
            GameObject bellTower = new GameObject("BellTower");
            bellTower.transform.SetParent(root, false);
            bellTower.transform.localPosition = new Vector3(0f, p.height + p.roofHeight + 0.2f, p.depth * 0.4f);
            GameObjectBuilder.CreateCubeChild(bellTower.transform, "Pillar", Vector3.zero, new Vector3(0.6f, 1.0f, 0.6f), ctx.timberMat);
            GameObjectBuilder.CreateCubeChild(bellTower.transform, "Bell", new Vector3(0f, 0f, 0.4f), new Vector3(0.4f, 0.5f, 0.4f), ctx.copperRoofMat ?? ctx.timberMat);
            GameObjectBuilder.CreateCubeChild(bellTower.transform, "Roof", new Vector3(0f, 0.6f, 0f), new Vector3(0.8f, 0.2f, 0.8f), p.roof);
        }

        private static void CreateWarehouseDetails(BuildContext ctx, Transform root, BuildingProfile p)
        {
            // Large doors
            GameObjectBuilder.CreateCubeChild(root, "LargeDoor", new Vector3(0f, p.height * 0.35f, p.depth * 0.51f), new Vector3(3.0f, p.height * 0.7f, 0.2f), ctx.timberMat);
            // Side extension
            GameObjectBuilder.CreateCubeChild(root, "Extension", new Vector3(p.width * 0.55f, p.height * 0.4f, 0f), new Vector3(2.0f, p.height * 0.8f, p.depth * 0.6f), p.wall);
            GameObjectBuilder.CreateCubeChild(root, "ExtensionRoof", new Vector3(p.width * 0.55f, p.height * 0.85f, 0f), new Vector3(2.2f, 0.2f, p.depth * 0.65f), p.roof);
            
            // Crates
            GameObjectBuilder.CreateCubeChild(root, "Crate1", new Vector3(-p.width * 0.3f, 0.5f, p.depth * 0.6f), new Vector3(1.0f, 1.0f, 1.0f), ctx.timberMat);
            GameObjectBuilder.CreateCubeChild(root, "Crate2", new Vector3(-p.width * 0.3f, 1.5f, p.depth * 0.6f), new Vector3(0.8f, 0.8f, 0.8f), ctx.timberMat);
        }

        private static void CreateGreenhouseDetails(BuildContext ctx, Transform root, BuildingProfile p)
        {
            // Internal beds
            GameObjectBuilder.CreateCubeChild(root, "Bed1", new Vector3(-p.width * 0.25f, 0.3f, 0f), new Vector3(p.width * 0.3f, 0.6f, p.depth * 0.8f), ctx.dirtMat ?? ctx.timberMat);
            GameObjectBuilder.CreateCubeChild(root, "Bed2", new Vector3(p.width * 0.25f, 0.3f, 0f), new Vector3(p.width * 0.3f, 0.6f, p.depth * 0.8f), ctx.dirtMat ?? ctx.timberMat);
        }

        private static void CreateWatchtowerDetails(BuildContext ctx, Transform root, BuildingProfile p)
        {
            // Platform
            GameObjectBuilder.CreateCubeChild(root, "Platform", new Vector3(0f, p.height, 0f), new Vector3(p.width * 1.2f, 0.4f, p.depth * 1.2f), ctx.timberMat);
            
            // Battlements
            for (int x = -1; x <= 1; x += 2)
            {
                GameObjectBuilder.CreateCubeChild(root, $"BattlementX{x}", new Vector3(x * p.width * 0.55f, p.height + 0.6f, 0f), new Vector3(0.2f, 1.2f, p.depth * 1.2f), p.wall);
                GameObjectBuilder.CreateCubeChild(root, $"BattlementZ{x}", new Vector3(0f, p.height + 0.6f, x * p.depth * 0.55f), new Vector3(p.width * 1.2f, 1.2f, 0.2f), p.wall);
            }
            
            // Lantern
            GameObjectBuilder.CreateCubeChild(root, "Lantern", new Vector3(0f, p.height + 1.5f, 0f), new Vector3(0.8f, 1.5f, 0.8f), ctx.glassMat ?? ctx.wallCreamMat);
            GameObjectBuilder.CreateCubeChild(root, "LanternRoof", new Vector3(0f, p.height + 2.4f, 0f), new Vector3(1.2f, 0.3f, 1.2f), p.roof);
        }

        private static void CreateWorkshopSign(BuildContext ctx, Transform root, BuildingProfile p)
        {
            GameObject sign = new GameObject("WorkshopSign");
            sign.transform.SetParent(root, false);
            sign.transform.localPosition = new Vector3(-p.width * 0.34f, p.height * 0.74f, p.depth * 0.60f);
            GameObjectBuilder.CreateCubeChild(sign.transform, "Arm", Vector3.zero, new Vector3(0.14f, 1.1f, 0.14f), ctx.timberMat);
            GameObjectBuilder.CreateCubeChild(sign.transform, "Board", new Vector3(0.42f, -0.08f, 0f), new Vector3(0.78f, 0.48f, 0.08f), ctx.wheatBaleMat);
        }

        private static void CreateBarnProps(BuildContext ctx, Transform root, BuildingProfile p)
        {
            GameObjectBuilder.CreateCylinderChild(root, "BaleA", new Vector3(-p.width * 0.30f, 0.45f, p.depth * 0.76f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.55f, 0.32f, 0.55f), ctx.wheatBaleMat);
            GameObjectBuilder.CreateCylinderChild(root, "BaleB", new Vector3(p.width * 0.28f, 0.42f, p.depth * 0.72f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.48f, 0.28f, 0.48f), ctx.wheatBaleMat);
        }

        private static void CreateChapelDetails(BuildContext ctx, Transform root, BuildingProfile p)
        {
            GameObjectBuilder.CreateCubeChild(root, "BellTower", new Vector3(0f, p.height + 1.7f, -p.depth * 0.18f), new Vector3(1.4f, 3.0f, 1.4f), p.wall ?? ctx.wallCreamMat);
            if (p.russian)
            {
                GameObjectBuilder.CreateSphereChild(root, "OnionBase", new Vector3(0f, p.height + 3.55f, -p.depth * 0.18f), new Vector3(1.25f, 1.45f, 1.25f), p.roof ?? ctx.copperRoofMat);
                GameObjectBuilder.CreateMeshChild(root, "Spire", ctx.coneMesh, new Vector3(0f, p.height + 4.55f, -p.depth * 0.18f), new Vector3(0.75f, 1.45f, 0.75f), p.roof ?? ctx.copperRoofMat);
            }
            else
            {
                GameObjectBuilder.CreateMeshChild(root, "TowerRoof", ctx.roofMesh, new Vector3(0f, p.height + 3.1f, -p.depth * 0.18f), new Vector3(1.7f, 1.9f, 1.7f), p.roof ?? ctx.roofDarkMat);
            }
            GameObjectBuilder.CreateCubeChild(root, "CrossV", new Vector3(0f, p.height + 5.2f, -p.depth * 0.18f), new Vector3(0.10f, 0.9f, 0.10f), ctx.stoneMat);
            GameObjectBuilder.CreateCubeChild(root, "CrossH", new Vector3(0f, p.height + 5.05f, -p.depth * 0.18f), new Vector3(0.44f, 0.10f, 0.10f), ctx.stoneMat);
        }

        private static void CreateForgeDetails(BuildContext ctx, Transform root, BuildingProfile p)
        {
            GameObjectBuilder.CreateCubeChild(root, "ForgeCanopy", new Vector3(-p.width * 0.60f, p.height * 0.52f, p.depth * 0.10f), new Vector3(2.2f, 0.18f, 2.6f), ctx.timberMat);
            GameObjectBuilder.CreateCubeChild(root, "ForgePostA", new Vector3(-p.width * 0.98f, p.height * 0.26f, p.depth * 0.85f), new Vector3(0.14f, p.height * 0.52f, 0.14f), ctx.timberMat);
            GameObjectBuilder.CreateCubeChild(root, "ForgePostB", new Vector3(-p.width * 0.22f, p.height * 0.26f, p.depth * 0.85f), new Vector3(0.14f, p.height * 0.52f, 0.14f), ctx.timberMat);
            GameObjectBuilder.CreateCubeChild(root, "AnvilBase", new Vector3(-p.width * 0.60f, 0.55f, p.depth * 0.58f), new Vector3(0.7f, 1.1f, 0.7f), ctx.stoneMat);
            GameObjectBuilder.CreateCubeChild(root, "AnvilTop", new Vector3(-p.width * 0.60f, 1.16f, p.depth * 0.58f), new Vector3(0.9f, 0.20f, 0.38f), ctx.stoneMat);
            GameObjectBuilder.CreateCubeChild(root, "ForgeFire", new Vector3(-p.width * 0.22f, 0.44f, p.depth * 0.46f), new Vector3(0.75f, 0.22f, 0.75f), ctx.forgeFireMat);
            GameObjectBuilder.CreateCubeChild(root, "ForgeChimney", new Vector3(p.width * 0.28f, p.height + 1.5f, -p.depth * 0.08f), new Vector3(0.92f, 3.2f, 0.92f), ctx.stoneMat);
        }

        private static void CreateMillDetails(BuildContext ctx, Transform root, BuildingProfile p)
        {
            GameObjectBuilder.CreateCylinderChild(root, "MillTower", new Vector3(0f, p.height * 0.58f, 0f), new Vector3(p.width * 0.12f, p.height * 0.58f, p.depth * 0.12f), p.wall ?? ctx.wallCreamMat);
            GameObjectBuilder.CreateMeshChild(root, "MillTopRoof", ctx.roofMesh, new Vector3(0f, p.height * 1.18f, 0f), new Vector3(p.width * 0.62f, 2.2f, p.depth * 0.62f), p.roof ?? ctx.roofDarkMat);
            GameObject sails = new GameObject("Sails");
            sails.transform.SetParent(root, false);
            sails.transform.localPosition = new Vector3(0f, p.height * 0.82f, p.depth * 0.58f);
            for (int i = 0; i < 4; i++)
            {
                GameObject sail = new GameObject("Sail_" + i);
                sail.transform.SetParent(sails.transform, false);
                sail.transform.localRotation = Quaternion.Euler(0f, 0f, i * 90f);
                GameObjectBuilder.CreateCubeChild(sail.transform, "Beam", new Vector3(0f, 1.5f, 0f), new Vector3(0.14f, 3.0f, 0.14f), ctx.timberMat);
                GameObjectBuilder.CreateCubeChild(sail.transform, "Cloth", new Vector3(0.30f, 1.5f, 0f), new Vector3(0.62f, 2.2f, 0.06f), ctx.wheatBladeMat);
            }
        }

        private static void CreateTavernDetails(BuildContext ctx, Transform root, BuildingProfile p)
        {
            GameObjectBuilder.CreateCubeChild(root, "Awning", new Vector3(0f, p.height * 0.62f, p.depth * 0.72f), new Vector3(p.width * 0.46f, 0.16f, 1.0f), ctx.roofRedMat);
            GameObjectBuilder.CreateCubeChild(root, "SignPost", new Vector3(-p.width * 0.46f, p.height * 0.58f, p.depth * 0.66f), new Vector3(0.12f, 1.4f, 0.12f), ctx.timberMat);
            GameObjectBuilder.CreateCubeChild(root, "SignBoard", new Vector3(-p.width * 0.12f, p.height * 0.72f, p.depth * 0.67f), new Vector3(1.0f, 0.56f, 0.08f), ctx.wallCreamMat);
            GameObjectBuilder.CreateCylinderChild(root, "BarrelA", new Vector3(p.width * 0.46f, 0.44f, p.depth * 0.72f), new Vector3(0.34f, 0.44f, 0.34f), ctx.timberMat);
            GameObjectBuilder.CreateCylinderChild(root, "BarrelB", new Vector3(p.width * 0.62f, 0.42f, p.depth * 0.58f), new Vector3(0.30f, 0.42f, 0.30f), ctx.timberMat);
        }

        private static void CreateStableDetails(BuildContext ctx, Transform root, BuildingProfile p)
        {
            GameObjectBuilder.CreateCubeChild(root, "Canopy", new Vector3(0f, p.height * 0.46f, p.depth * 0.72f), new Vector3(p.width * 0.70f, 0.14f, 1.4f), ctx.timberMat);
            GameObjectBuilder.CreateCubeChild(root, "PostL", new Vector3(-p.width * 0.56f, p.height * 0.22f, p.depth * 1.10f), new Vector3(0.16f, p.height * 0.46f, 0.16f), ctx.timberMat);
            GameObjectBuilder.CreateCubeChild(root, "PostR", new Vector3(p.width * 0.56f, p.height * 0.22f, p.depth * 1.10f), new Vector3(0.16f, p.height * 0.46f, 0.16f), ctx.timberMat);
            GameObjectBuilder.CreateCylinderChild(root, "WaterTrough", new Vector3(0f, 0.34f, p.depth * 1.12f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.36f, 1.05f, 0.36f), ctx.stoneMat);
        }

        private static void CreateGranaryDetails(BuildContext ctx, Transform root, BuildingProfile p)
        {
            for (int i = -1; i <= 1; i += 2)
                GameObjectBuilder.CreateCubeChild(root, "Stilt_" + i, new Vector3(i * p.width * 0.28f, 0.55f, 0f), new Vector3(0.38f, 1.1f, 0.38f), ctx.stoneMat);
            GameObjectBuilder.CreateCubeChild(root, "Ladder", new Vector3(-p.width * 0.50f, 1.1f, p.depth * 0.58f), Quaternion.Euler(65f, 0f, 0f), new Vector3(0.14f, 2.4f, 0.70f), ctx.timberMat);
        }

        private static void CreateBoathouseDetails(BuildContext ctx, Transform root, BuildingProfile p)
        {
            GameObjectBuilder.CreateCubeChild(root, "Ramp", new Vector3(0f, -0.10f, p.depth * 0.82f), Quaternion.Euler(12f, 0f, 0f), new Vector3(p.width * 0.72f, 0.14f, 4.4f), ctx.timberMat);
            GameObjectBuilder.CreateCubeChild(root, "Boat", new Vector3(0f, 0.18f, p.depth * 1.24f), new Vector3(2.8f, 0.36f, 5.2f), ctx.timberMat);
        }

        private static void AddYardProps(BuildContext ctx, Transform root, BuildingProfile p, SeededRandom random)
        {
            if (p.kind == BuildingKind.Barn || p.kind == BuildingKind.Chapel)
                return;

            if (p.kind == BuildingKind.Farmhouse || p.kind == BuildingKind.LongHouse || p.kind == BuildingKind.Manor || p.kind == BuildingKind.Tavern)
            {
                GameObjectBuilder.CreateCubeChild(root, "WoodPile", new Vector3(-p.width * 0.42f, 0.30f, -p.depth * 0.58f), new Vector3(1.2f, 0.6f, 0.7f), ctx.timberMat);
                if (p.kind != BuildingKind.Manor)
                    GameObjectBuilder.CreateCubeChild(root, "Shed", new Vector3(p.width * 0.72f, 1.05f, -p.depth * 0.38f), new Vector3(2.1f, 2.1f, 2.0f), ctx.wallWarmMat);
                if (p.kind == BuildingKind.Manor || random.Value > 0.48f)
                {
                    GameObject tree = new GameObject("YardTree");
                    tree.transform.SetParent(root, false);
                    tree.transform.localPosition = new Vector3(-p.width * 0.55f, 0f, p.depth * 0.95f);
                    tree.transform.localScale = Vector3.one * (p.kind == BuildingKind.Manor ? 0.85f : 0.62f);
                    VegetationBuilder.BuildBroadleafTree(ctx, tree.transform);
                }
            }
            else if (p.kind == BuildingKind.Workshop || p.kind == BuildingKind.Forge)
            {
                GameObjectBuilder.CreateCubeChild(root, "Bench", new Vector3(p.width * 0.60f, 0.36f, p.depth * 0.72f), new Vector3(1.8f, 0.18f, 0.6f), ctx.timberMat);
                GameObjectBuilder.CreateCylinderChild(root, "Barrel", new Vector3(-p.width * 0.55f, 0.42f, p.depth * 0.70f), new Vector3(0.32f, 0.42f, 0.32f), ctx.timberMat);
            }
            else if (p.kind == BuildingKind.Stable)
            {
                GameObjectBuilder.CreateCylinderChild(root, "Trough", new Vector3(0f, 0.30f, p.depth * 0.94f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.24f, 1.0f, 0.24f), ctx.stoneMat);
                GameObjectBuilder.CreateCubeChild(root, "HayRack", new Vector3(-p.width * 0.52f, 0.46f, p.depth * 0.72f), new Vector3(1.3f, 0.92f, 0.5f), ctx.wheatBaleMat);
            }
            else if (p.kind == BuildingKind.Granary)
            {
                GameObjectBuilder.CreateCubeChild(root, "CrateA", new Vector3(p.width * 0.56f, 0.32f, p.depth * 0.56f), new Vector3(0.72f, 0.64f, 0.72f), ctx.timberMat);
                GameObjectBuilder.CreateCubeChild(root, "CrateB", new Vector3(p.width * 0.72f, 0.34f, p.depth * 0.78f), new Vector3(0.64f, 0.68f, 0.64f), ctx.timberMat);
            }
            else if (p.kind == BuildingKind.Boathouse)
            {
                GameObjectBuilder.CreateCubeChild(root, "PierPostA", new Vector3(-p.width * 0.28f, -0.22f, p.depth * 1.42f), new Vector3(0.18f, 1.1f, 0.18f), ctx.timberMat);
                GameObjectBuilder.CreateCubeChild(root, "PierPostB", new Vector3(p.width * 0.28f, -0.22f, p.depth * 1.42f), new Vector3(0.18f, 1.1f, 0.18f), ctx.timberMat);
            }
            else if (p.kind == BuildingKind.Cottage && random.Value > 0.45f)
            {
                GameObjectBuilder.CreateCubeChild(root, "Bench", new Vector3(-p.width * 0.44f, 0.34f, p.depth * 0.76f), new Vector3(1.1f, 0.16f, 0.38f), ctx.timberMat);
            }
        }

        private static void CreateFenceLoop(BuildContext ctx, Transform parent, Vector3 centerOffset, float width, float depth, float gateWidth)
        {
            GameObject root = new GameObject("Fence");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = centerOffset;

            float hw = width * 0.5f;
            float hd = depth * 0.5f;
            float y = 0.55f;

            CreateFenceSegment(ctx, root.transform, new Vector3(-hw, y, -hd), new Vector3(hw, y, -hd));
            CreateFenceSegment(ctx, root.transform, new Vector3(-hw, y, hd), new Vector3(-gateWidth * 0.5f, y, hd));
            CreateFenceSegment(ctx, root.transform, new Vector3(gateWidth * 0.5f, y, hd), new Vector3(hw, y, hd));
            CreateFenceSegment(ctx, root.transform, new Vector3(-hw, y, -hd), new Vector3(-hw, y, hd));
            CreateFenceSegment(ctx, root.transform, new Vector3(hw, y, -hd), new Vector3(hw, y, hd));
        }

        private static void CreateFenceSegment(BuildContext ctx, Transform parent, Vector3 a, Vector3 b)
        {
            Vector3 dir = b - a;
            float len = dir.magnitude;
            int posts = Mathf.Max(2, Mathf.RoundToInt(len / 1.8f) + 1);
            for (int i = 0; i < posts; i++)
            {
                float t = i / (float)(posts - 1);
                Vector3 p = Vector3.Lerp(a, b, t);
                GameObjectBuilder.CreateCubeChild(parent, "Post", p, new Vector3(0.14f, 1.08f, 0.14f), ctx.timberMat);
            }

            Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            Vector3 mid = (a + b) * 0.5f;
            GameObjectBuilder.CreateCubeChild(parent, "RailA", mid + Vector3.up * 0.14f, rot, new Vector3(0.10f, 0.10f, len), ctx.timberMat);
            GameObjectBuilder.CreateCubeChild(parent, "RailB", mid - Vector3.up * 0.22f, rot, new Vector3(0.10f, 0.10f, len), ctx.timberMat);
        }

        private static void CreateGardenShrubs(BuildContext ctx, Transform houseRoot, float w, float d, SeededRandom random)
        {
            GameObject shrubs = new GameObject("Garden");
            shrubs.transform.SetParent(houseRoot, false);
            int count = random.Range(4, 8);
            for (int i = 0; i < count; i++)
            {
                float x = random.Range(-w * 0.7f, w * 0.7f);
                float z = random.Range(-d * 0.9f, -d * 0.18f);
                VegetationBuilder.CreateShrubCluster(ctx, shrubs.transform, new Vector3(x, 0f, z), random.Range(0.8f, 1.3f));
            }
        }

        private static void SetupBuildingLOD(BuildContext ctx, GameObject root, BuildingProfile p)
        {
            Material wall = p.wall ?? ctx.wallCreamMat;
            Material roof = p.roof ?? ctx.roofDarkMat;

            // Create 10 LOD levels
            const int lodCount = 10;
            GameObject[] lodObjects = new GameObject[lodCount];
            LOD[] lods = new LOD[lodCount];

            // LOD0 - Full Detail (Original children)
            lodObjects[0] = new GameObject("LOD0");
            lodObjects[0].transform.SetParent(root.transform, false);
            GameObjectBuilder.MoveAllChildrenExcept(root.transform, lodObjects[0].transform, null);

            // Progressive simplification for remaining levels
            for (int i = 1; i < lodCount; i++)
            {
                lodObjects[i] = new GameObject("LOD" + i);
                lodObjects[i].transform.SetParent(root.transform, false);

                float t = i / (float)(lodCount - 1);
                
                // For intermediate LODs (1-4), we use the body and roof, but hide detail parts
                if (i < 5)
                {
                    // Copy main parts
                    GameObjectBuilder.CreateCubeChild(lodObjects[i].transform, "Body", new Vector3(0f, p.height * 0.5f, 0f), new Vector3(p.width, p.height, p.depth), wall);
                    GameObjectBuilder.CreateMeshChild(lodObjects[i].transform, "Roof", ctx.roofMesh, new Vector3(0f, p.height - 0.05f, 0f), new Vector3(p.width + 0.3f, p.roofHeight, p.depth + 0.3f), roof);
                    
                    if (i < 3) // Keep annex for early LODs
                    {
                         if (p.annex && p.kind != BuildingKind.Barn)
                         {
                             float aw = p.width * 0.45f; float ad = p.depth * 0.55f; float ah = p.height * 0.75f;
                             GameObjectBuilder.CreateCubeChild(lodObjects[i].transform, "Annex", new Vector3(p.width * 0.52f, ah * 0.5f, -p.depth * 0.05f), new Vector3(aw, ah, ad), wall);
                         }
                    }
                    if (i < 2) // Keep door and chimney for very early LODs
                    {
                        float doorW = GetDoorWidth(p.kind); float doorH = GetDoorHeight(p.kind);
                        GameObjectBuilder.CreateCubeChild(lodObjects[i].transform, "Door", new Vector3(0f, doorH * 0.5f, p.depth * 0.505f), new Vector3(doorW, doorH, 0.1f), ctx.timberMat);
                        GameObjectBuilder.CreateCubeChild(lodObjects[i].transform, "Chimney", new Vector3(p.width * 0.22f, p.height + p.roofHeight * 0.8f, 0f), new Vector3(0.6f, 1.5f, 0.6f), ctx.stoneMat);
                    }
                }
                else // For far LODs (5-9), extremely simplified boxy shapes
                {
                    float scale = Mathf.Lerp(1.0f, 0.9f, t);
                    GameObjectBuilder.CreateCubeChild(lodObjects[i].transform, "SimplBody", new Vector3(0f, p.height * 0.45f, 0f), new Vector3(p.width * scale, p.height * 0.9f, p.depth * scale), wall);
                    GameObjectBuilder.CreateCubeChild(lodObjects[i].transform, "SimplRoof", new Vector3(0f, p.height + p.roofHeight * 0.3f, 0f), new Vector3(p.width * 1.1f * scale, p.roofHeight * 0.6f, p.depth * 1.1f * scale), roof);
                }

                GameObjectBuilder.SetShadowsRecursive(lodObjects[i], i < 3 ? ShadowCastingMode.On : ShadowCastingMode.Off, i < 5);
            }

            // Define screen relative heights for 10 levels
            // 0 -> 1.0, 9 -> 0.01
            for (int i = 0; i < lodCount; i++)
            {
                float screenHeight = Mathf.Pow(0.85f, i); // Exponential decay for transitions
                if (i == lodCount - 1) screenHeight = 0.01f;
                
                lods[i] = new LOD(screenHeight, lodObjects[i].GetComponentsInChildren<Renderer>());
            }

            LODGroup group = root.AddComponent<LODGroup>();
            group.SetLODs(lods);
            group.RecalculateBounds();
        }

    }
}
