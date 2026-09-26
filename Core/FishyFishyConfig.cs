using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace FishyFishy.Core
{
    public class FishyFishyConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [DefaultValue(200)]
        [Range(0, 1000)]
        public int Tier1MaxMana;

        [DefaultValue(300)]
        [Range(0, 1000)]
        public int Tier2MaxMana;

        [DefaultValue(400)]
        [Range(0, 1000)]
        public int Tier3MaxMana;

        [DefaultValue(0.08f)]
        [Range(0.01f, 1f)]
        [Increment(0.01f)]
        public float ManaCostPercent;
    }
}
