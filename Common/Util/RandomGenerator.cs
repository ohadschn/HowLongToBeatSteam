using System;

namespace Common.Util
{
    public class RandomGenerator
    {        
        private static readonly Random s_global = new Random();

        [ThreadStatic]
        private static Random s_local;

        private static Random GetLocalRandom()
        {
            var inst = s_local;
                
            if (inst == null)
            {
                int seed;
                lock (s_global)
                {
                    seed = s_global.Next();
                }
                s_local = inst = new Random(seed);
            }

            return inst;
        }

        public static int Next(int min, int max)
        {
            return GetLocalRandom().Next(min, max);
        }
    }
}
