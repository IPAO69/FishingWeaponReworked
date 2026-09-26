using System.Collections.Generic;
using FishyFishy.Content.Projectiles;
using FishyFishy.Core;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishyFishy.Content.Items
{
    public class CrystalSerpentReworked : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstantiation)
        {
            return item.type == ItemID.CrystalSerpent;
        }
        public override void SetDefaults(Item item)
        {
            item.damage = 35;
            item.useTime = 40;
            item.useAnimation = 40;
        }


        public override void ModifyManaCost(Item item, Player player, ref float reduce, ref float mult)
        {
            if (item.mana <= 0)
                return;

            mult *= GetManaCost(player) / (float)item.mana;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile shard = Projectile.NewProjectileDirect(source, position, velocity, ModContent.ProjectileType<CrystalSerpentShard>(), damage, knockback, player.whoAmI);

            if (GetTier(player) >= 3)
                shard.scale *= 1.5f;

            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "CrystalSerpentNote1", "Shards pierce up to three enemies, scattering damaging sparks with every hit"));
            tooltips.Add(new TooltipLine(Mod, "CrystalSerpentNote2", $"Grows stronger with maximum mana:"));
            tooltips.Add(new TooltipLine(Mod, "CrystalSerpentNote3", $"{Config.Tier1MaxMana}+ max mana — shards pierce three enemies and burst into sparks"));
            tooltips.Add(new TooltipLine(Mod, "CrystalSerpentNote4", $"{Config.Tier2MaxMana}+ max mana — the explosion sparks home in on enemies"));
            tooltips.Add(new TooltipLine(Mod, "CrystalSerpentNote5", $"{Config.Tier3MaxMana}+ max mana — fires massive shards"));
        }

        internal static FishyFishyConfig Config => ModContent.GetInstance<FishyFishyConfig>();

        internal static int GetTier(Player player)
        {
            int maxMana = player.statManaMax2;

            if (maxMana >= Config.Tier3MaxMana)
                return 3;

            if (maxMana >= Config.Tier2MaxMana)
                return 2;

            if (maxMana >= Config.Tier1MaxMana)
                return 1;

            return 0;
        }

        private static int GetManaCost(Player player) => System.Math.Max(1, (int)(player.statManaMax2 * Config.ManaCostPercent));
    }
}
