using AuraLiteWorldGenerator.Runtime;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Factory for creating house specifications (kind, size, flags) used during layout generation.
    /// </summary>
    public static class BuildingFactory
    {
        public static HouseSpec CreateHouseSpec(Vector3 pos, float yaw, BuildingKind kind, SeededRandom random)
        {
            if (random == null)
                throw new System.ArgumentNullException(nameof(random));

            HouseSpec spec = new HouseSpec
            {
                position = pos,
                yaw = yaw,
                kind = kind,
                fenced = random.Value > 0.34f,
                garden = random.Value > 0.30f,
                annex = random.Value > 0.52f
            };

            ApplyKindDimensions(kind, spec, random);
            return spec;
        }

        private static void ApplyKindDimensions(BuildingKind kind, HouseSpec spec, SeededRandom random)
        {
            switch (kind)
            {
                case BuildingKind.Cottage:
                    spec.footprint = new Vector2(random.Range(7.2f, 8.9f), random.Range(6.8f, 8.5f));
                    spec.height = random.Range(4.3f, 5.5f);
                    break;
                case BuildingKind.Farmhouse:
                    spec.footprint = new Vector2(random.Range(8.4f, 10.6f), random.Range(7.6f, 9.8f));
                    spec.height = random.Range(4.8f, 6.1f);
                    break;
                case BuildingKind.Barn:
                    spec.footprint = new Vector2(random.Range(9.5f, 13.5f), random.Range(8.6f, 11.4f));
                    spec.height = random.Range(4.2f, 5.3f);
                    spec.fenced = false;
                    spec.garden = false;
                    break;
                case BuildingKind.LongHouse:
                    spec.footprint = new Vector2(random.Range(11.5f, 15.5f), random.Range(8.2f, 10.4f));
                    spec.height = random.Range(5.3f, 6.6f);
                    break;
                case BuildingKind.Workshop:
                    spec.footprint = new Vector2(random.Range(8.8f, 10.8f), random.Range(7.4f, 9.2f));
                    spec.height = random.Range(4.7f, 5.8f);
                    spec.garden = false;
                    break;
                case BuildingKind.Chapel:
                    spec.footprint = new Vector2(9.5f, 16.0f);
                    spec.height = 8.4f;
                    spec.fenced = false;
                    spec.garden = false;
                    spec.annex = false;
                    break;
                case BuildingKind.Forge:
                    spec.footprint = new Vector2(10.6f, 8.8f);
                    spec.height = 5.6f;
                    spec.fenced = false;
                    spec.garden = false;
                    spec.annex = true;
                    break;
                case BuildingKind.Mill:
                    spec.footprint = new Vector2(11.8f, 11.8f);
                    spec.height = 9.2f;
                    spec.fenced = false;
                    spec.garden = false;
                    spec.annex = false;
                    break;
                case BuildingKind.Tavern:
                    spec.footprint = new Vector2(13.2f, 10.4f);
                    spec.height = 6.4f;
                    spec.fenced = false;
                    spec.garden = false;
                    spec.annex = true;
                    break;
                case BuildingKind.Stable:
                    spec.footprint = new Vector2(12.8f, 9.8f);
                    spec.height = 5.2f;
                    spec.fenced = true;
                    spec.garden = false;
                    spec.annex = false;
                    break;
                case BuildingKind.Granary:
                    spec.footprint = new Vector2(9.2f, 8.8f);
                    spec.height = 6.0f;
                    spec.fenced = false;
                    spec.garden = false;
                    spec.annex = false;
                    break;
                case BuildingKind.Boathouse:
                    spec.footprint = new Vector2(10.8f, 8.4f);
                    spec.height = 4.8f;
                    spec.fenced = false;
                    spec.garden = false;
                    spec.annex = false;
                    break;
                case BuildingKind.Inn:
                    spec.footprint = new Vector2(random.Range(12.5f, 15.0f), random.Range(10.0f, 12.0f));
                    spec.height = random.Range(6.0f, 7.5f);
                    spec.fenced = false;
                    spec.garden = false;
                    spec.annex = true;
                    break;
                case BuildingKind.Windmill:
                    spec.footprint = new Vector2(11.0f, 11.0f);
                    spec.height = random.Range(8.5f, 9.5f);
                    spec.fenced = false;
                    spec.garden = false;
                    spec.annex = false;
                    break;
                case BuildingKind.Watermill:
                    spec.footprint = new Vector2(12.0f, 9.0f);
                    spec.height = random.Range(5.5f, 6.5f);
                    spec.fenced = false;
                    spec.garden = false;
                    spec.annex = true;
                    break;
                case BuildingKind.School:
                    spec.footprint = new Vector2(14.0f, 8.5f);
                    spec.height = random.Range(5.0f, 6.0f);
                    spec.fenced = true;
                    spec.garden = false;
                    spec.annex = false;
                    break;
                case BuildingKind.Warehouse:
                    spec.footprint = new Vector2(random.Range(14.0f, 18.0f), random.Range(10.0f, 13.0f));
                    spec.height = random.Range(5.5f, 7.0f);
                    spec.fenced = false;
                    spec.garden = false;
                    spec.annex = true;
                    break;
                case BuildingKind.Greenhouse:
                    spec.footprint = new Vector2(random.Range(9.0f, 12.0f), random.Range(6.0f, 8.0f));
                    spec.height = random.Range(4.0f, 4.5f);
                    spec.fenced = true;
                    spec.garden = true;
                    spec.annex = false;
                    break;
                case BuildingKind.Watchtower:
                    spec.footprint = new Vector2(8.0f, 8.0f);
                    spec.height = random.Range(10.0f, 12.0f);
                    spec.fenced = false;
                    spec.garden = false;
                    spec.annex = false;
                    break;
                default:
                    spec.footprint = new Vector2(16f, 12f);
                    spec.height = 7.2f;
                    spec.fenced = true;
                    spec.garden = true;
                    spec.annex = true;
                    break;
            }
        }
    }
}
