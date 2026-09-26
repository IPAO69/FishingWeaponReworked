using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;

namespace FishyFishy.Content.Items
{
    public class BloodRainBowReworked : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.BloodRainBow;
        }

        public override void SetDefaults(Item entity)
        {
            entity.damage = 28;
            entity.useAnimation = 24;
            entity.useTime = 24;
            entity.crit += 10;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            return true;
        }


        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            var bp = Main.LocalPlayer.GetModPlayer<BloodRainBowPlayer>();
            int pct = (int)(bp.chargeDamage / BloodRainBowPlayer.MaxCharge * 100f);
            tooltips.Add(new TooltipLine(Mod, "BloodRainCharge", $"Charge: {pct}% — Right-click at 100%"));
            tooltips.Add(new TooltipLine(Mod, "BloodRainCharge", $"   Fire a blood shot that deals {item.damage * 4} damage ({item.damage} x 4 = {item.damage * 4})"));
            tooltips.Add(new TooltipLine(Mod, "BloodRainCharge", "   Inflict Ichor for 5 seconds"));
        }

    }
}
