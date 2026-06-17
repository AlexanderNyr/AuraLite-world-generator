namespace AuraLiteWorldGenerator.Editor.Core
{
    public struct ErosionSettings
    {
        public float Strength;
        public int Iterations;
        public int Seed;
    }

    public interface ITerrainEroder
    {
        void Erode(float[,] heightmap, ErosionSettings settings);
    }
}
