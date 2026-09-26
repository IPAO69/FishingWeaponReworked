using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishyFishy.Content.Items
{
    public class FrostDaggerfishReworked : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstantiation)
        {
            return item.type == ItemID.FrostDaggerfish;
        }

        public override void SetDefaults(Item item)
        {
            item.consumable = false;
            item.DamageType = DamageClass.Ranged;
            item.damage = 32;
            item.useTime = 14;
            item.useAnimation = 14;
            item.shootSpeed = 18f;
            item.knockBack = 4f;
            item.crit += 5;
            item.UseSound = SoundID.Item1;
            item.maxStack = 1;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "FrostDaggerfishNote", "Unconsumable — does not use ammo"));
            tooltips.Add(new TooltipLine(Mod, "FrostDaggerfishNote", "Icy trail follows each daggerfish"));
        }
    }
}
