using System;

namespace MapMaker.Shared.Utils
{
    /// <summary>
    /// Deterministic RNG streams derived from a single root seed.
    /// Each module must use its dedicated stream to avoid cross-module drift.
    /// </summary>
    public sealed class SeedContext
    {
        public int RootSeed { get; }

        // Named module streams (expand as new modules are added)
        public Random ElevationRng { get; }
        public Random LatitudeRng { get; }
        public Random CoastRng { get; }
        public Random MountainsRng { get; }
        public Random HydrologyRng { get; }
        public Random MoistureRng { get; }
        public Random BiomesRng { get; }

        public SeedContext(int rootSeed)
        {
            RootSeed = rootSeed;

            // SplitMix-style scrambling to get distinct seeds.
            // This is deterministic and fast; does not need to be configurable.
            int s1 = Scramble(rootSeed, unchecked((int)0x9E3779B9));
            int s2 = Scramble(rootSeed, unchecked((int)0xBB67AE85));
            int s3 = Scramble(rootSeed, unchecked((int)0x3C6EF372));
            int s4 = Scramble(rootSeed, unchecked((int)0xA54FF53A));
            int s5 = Scramble(rootSeed, unchecked((int)0x510E527F));
            int s6 = Scramble(rootSeed, unchecked((int)0x1F83D9AB));
            int s7 = Scramble(rootSeed, unchecked((int)0x5BE0CD19));

            ElevationRng = new Random(s1);
            LatitudeRng = new Random(s2);
            CoastRng = new Random(s3);
            MountainsRng = new Random(s4);
            HydrologyRng = new Random(s5);
            MoistureRng = new Random(s6);
            BiomesRng = new Random(s7);
        }

        private static int Scramble(int seed, int salt)
        {
            unchecked
            {
                uint z = (uint)seed + (uint)salt;
                z ^= z >> 16;
                z *= 0x7feb352du;
                z ^= z >> 15;
                z *= 0x846ca68bu;
                z ^= z >> 16;
                return (int)z;
            }
        }
    }
}
