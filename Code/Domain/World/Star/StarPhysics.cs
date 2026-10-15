using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>恒星与行星物理计算（纯函数，无状态依赖）</summary>
    internal static class StarPhysics
    {
        /// <summary>获取恒星基础温度（K）</summary>
        public static float GetStarBaseTemp(StarType t)
        {
            return t switch
            {
                StarType.RedDwarf => 3000f,
                StarType.YellowDwarf => 5778f,
                StarType.BlueGiant => 30000f,
                StarType.RedGiant => 3500f,
                StarType.WhiteDwarf => 10000f,
                StarType.NeutronStar => 1000000f,
                StarType.BlackHole => 0f,
                _ => 5000f
            };
        }

        /// <summary>计算行星表面温度（反平方定律：温度∝1/√距离）</summary>
        public static float CalculatePlanetTemp(StarType starType, float orbitRadius)
        {
            float baseTemp = GetStarBaseTemp(starType);
            float temp = baseTemp / Mathf.Sqrt(orbitRadius / 50f) * 0.3f;
            return Mathf.Round(temp * 10f) / 10f;
        }

        /// <summary>根据恒星光度计算行星温度（更准确的反平方定律）</summary>
        public static float CalculatePlanetTempFromLuminosity(float luminosity, float orbitRadius)
        {
            float au = orbitRadius / 50f;
            float temp = 278f * Mathf.Pow(luminosity / (au * au), 0.25f);
            return Mathf.Round(temp * 10f) / 10f;
        }

        /// <summary>获取恒星质量（太阳质量=1）</summary>
        public static float GetStarMass(StarType type)
        {
            switch (type)
            {
                case StarType.RedDwarf: return 0.3f;
                case StarType.YellowDwarf: return 1.0f;
                case StarType.BlueGiant: return 15f;
                case StarType.RedGiant: return 1.2f;
                case StarType.WhiteDwarf: return 0.6f;
                case StarType.NeutronStar: return 1.4f;
                case StarType.BlackHole: return 10f;
                default: return 1f;
            }
        }

        /// <summary>获取恒星半径（太阳半径=1）</summary>
        public static float GetStarRadius(StarType type)
        {
            switch (type)
            {
                case StarType.RedDwarf: return 0.4f;
                case StarType.YellowDwarf: return 1.0f;
                case StarType.BlueGiant: return 10f;
                case StarType.RedGiant: return 50f;
                case StarType.WhiteDwarf: return 0.01f;
                case StarType.NeutronStar: return 0.00003f;
                case StarType.BlackHole: return 0.0001f;
                default: return 1f;
            }
        }

        /// <summary>获取恒星光度（太阳光度=1）</summary>
        public static float GetStarLuminosity(StarType type)
        {
            switch (type)
            {
                case StarType.RedDwarf: return 0.02f;
                case StarType.YellowDwarf: return 1.0f;
                case StarType.BlueGiant: return 10000f;
                case StarType.RedGiant: return 100f;
                case StarType.WhiteDwarf: return 0.05f;
                case StarType.NeutronStar: return 0.1f;
                case StarType.BlackHole: return 0f;
                default: return 1f;
            }
        }

        /// <summary>计算行星物理参数（质量、半径、重力、密度）</summary>
        public static void CalculatePlanetPhysics(PlanetType type, System.Random rng,
            out float mass, out float radius, out float gravity, out float density)
        {
            switch (type)
            {
                case PlanetType.Gas:
                    mass = 50f + (float)rng.NextDouble() * 300f;
                    radius = 4f + (float)rng.NextDouble() * 7f;
                    density = 0.7f + (float)rng.NextDouble() * 0.8f;
                    break;
                case PlanetType.Ice:
                    mass = 10f + (float)rng.NextDouble() * 50f;
                    radius = 2f + (float)rng.NextDouble() * 3f;
                    density = 1.2f + (float)rng.NextDouble() * 0.8f;
                    break;
                case PlanetType.Lava:
                    mass = 0.5f + (float)rng.NextDouble() * 3f;
                    radius = 0.7f + (float)rng.NextDouble() * 1.5f;
                    density = 5f + (float)rng.NextDouble() * 3f;
                    break;
                case PlanetType.Ocean:
                    mass = 0.8f + (float)rng.NextDouble() * 5f;
                    radius = 0.9f + (float)rng.NextDouble() * 2f;
                    density = 2f + (float)rng.NextDouble() * 2f;
                    break;
                case PlanetType.Forest:
                    mass = 0.5f + (float)rng.NextDouble() * 3f;
                    radius = 0.8f + (float)rng.NextDouble() * 1.5f;
                    density = 4f + (float)rng.NextDouble() * 2f;
                    break;
                default:
                    mass = 0.3f + (float)rng.NextDouble() * 2f;
                    radius = 0.6f + (float)rng.NextDouble() * 1.2f;
                    density = 3.5f + (float)rng.NextDouble() * 2.5f;
                    break;
            }
            gravity = mass / (radius * radius);
        }
    }
}
