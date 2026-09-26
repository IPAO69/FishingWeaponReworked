using System;
using Terraria.ModLoader;

namespace FishyFishy.Helpers
{
    public class HitStopSystem : ModSystem
    {
        public static int Timer;
        private const int MaxHitStop = 14;

        public static void Request(int frames)
        {
            Timer = Math.Max(Timer, Math.Min(frames, MaxHitStop));
        }

        public static bool Active => Timer > 0;

        public override void PostUpdateEverything()
        {
            if (Timer > 0)
                Timer--;
        }
    }
}