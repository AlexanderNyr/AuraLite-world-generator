using AuraLiteWorldGenerator.Runtime;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Factory for creating house specifications (kind, size, flags) used during layout generation.
    /// </summary>
    public static class BuildingFactory
    {
        public static HouseSpec CreateHouseSpec(Vector3 pos, float yaw, BuildingKind kind)
        {
            HouseSpec spec = new HouseSpec
            {
                position = pos,
                yaw = yaw,
                kind = kind,
                fenced = Random.value > 0.34f,
                garden = Random.value > 0.30f,
                annex = Random.value > 0.52f
            };

            ApplyKindDimensions(kind, spec);
            return spec;
        }

        private static void ApplyKindDimensions(BuildingKind kind, HouseSpec spec)
        {
            switch (kind)
            {
                case BuildingKind.Cottage:
                    spec.footprint = new Vector2(Random.Range(7.2f, 8.9f), Random.Range(6.8f, 8.5f));
                    spec.height = Random.Range(4.3f, 5.5f);
                    break;
                case BuildingKind.Farmhouse:
                    spec.footprint = new Vector2(Random.Range(8.4f, 10.6f), Random.Range(7.6f, 9.8f));
                    spec.height = Random.Range(4.8f, 6.1f);
                    break;
                case BuildingKind.Barn:
                    spec.footprint = new Vector2(Random.Range(9.5f, 13.5f), Random.Range(8.6f, 11.4f));
                    spec.height = Random.Range(4.2f, 5.3f);
                    spec.fenced = false;
                    spec.garden = false;
                    break;
                case BuildingKind.LongHouse:
                    spec.footprint = new Vector2(Random.Range(11.5f, 15.5f), Random.Range(8.2f, 10.4f));
                    spec.height = Random.Range(5.3f, 6.6f);
                    break;
                case BuildingKind.Workshop:
                    spec.footprint = new Vector2(Random.Range(8.8f, 10.8f), Random.Range(7.4f, 9.2f));
                    spec.height = Random.Range(4.7f, 5.8f);
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
