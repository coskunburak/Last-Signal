using System;

namespace LastSignal.Loot
{
    // Version 1: FNV-1a over UTF-16 code units, then SplitMix64. Explicit unchecked arithmetic.
    public struct LootRandom
    {
        ulong state;
        public LootRandom(int sessionSeed, string pointId)
        {
            unchecked
            {
                ulong hash = 14695981039346656037UL;
                foreach (char c in pointId ?? "") { hash ^= c; hash *= 1099511628211UL; }
                state = hash ^ (uint)sessionSeed;
            }
        }
        public ulong Next()
        {
            unchecked
            {
                ulong z = (state += 0x9E3779B97F4A7C15UL);
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
        public ulong Below(ulong bound)
        {
            if (bound == 0) throw new ArgumentOutOfRangeException(nameof(bound));
            ulong threshold = unchecked(0UL - bound) % bound;
            ulong value;
            do { value = Next(); } while (value < threshold);
            return value % bound;
        }
    }
}
