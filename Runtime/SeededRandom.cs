using System;
using UnityEngine;

namespace AuraLiteWorldGenerator.Runtime
{
    /// <summary>
    /// Deterministic pseudo-random generator used by the world builder.
    /// Isolated from UnityEngine.Random so external code cannot affect the generation sequence.
    /// </summary>
    public sealed class SeededRandom
    {
        private uint _state;

        public SeededRandom(int seed)
        {
            _state = (uint)(seed == int.MinValue ? int.MaxValue : Math.Abs(seed));
            if (_state == 0)
                _state = 2463534242u;
            // Warm up the generator to avoid correlation with small seeds.
            for (int i = 0; i < 8; i++)
                NextUint();
        }

        /// <summary>Returns a float in the range [0, 1).</summary>
        public float Value => NextFloat();

        /// <summary>Returns a float in the range [min, max).</summary>
        public float Range(float min, float max)
        {
            if (min >= max)
                return min;
            return min + NextFloat() * (max - min);
        }

        /// <summary>Returns an integer in the range [min, max).</summary>
        public int Range(int min, int max)
        {
            if (min >= max)
                return min;
            return min + (int)(NextFloat() * (max - min));
        }

        /// <summary>Returns a random element from the provided array.</summary>
        public T Choice<T>(T[] items)
        {
            if (items == null || items.Length == 0)
                throw new ArgumentException("Array must contain at least one item.", nameof(items));
            return items[Range(0, items.Length)];
        }

        /// <summary>Returns true with the given probability in range [0, 1].</summary>
        public bool Chance(float probability) => NextFloat() < Mathf.Clamp01(probability);

        private float NextFloat()
        {
            return NextUint() / (float)uint.MaxValue;
        }

        private uint NextUint()
        {
            // Xorshift32 — fast, compact, and well-distributed for procedural generation.
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state;
        }
    }
}
