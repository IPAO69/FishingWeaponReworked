using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishyFishy.Content.Items
{
    public class ToxikarpReworked : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstantiation)
        {
            return item.type == ItemID.Toxikarp;
        }

        public override void SetDefaults(Item item)
        {
            item.damage = 30;
            item.useAmmo = AmmoID.None;
            item.autoReuse = true;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            ToxikarpPlayer toxikarp = ToxikarpPlayer.For(player);

            if (toxikarp.IsWindingUp)
                return false;

            if (toxikarp.ShouldFireShotgun())
            {
                toxikarp.BeginWindup(damage, knockback);
                return false;
            }

            return true;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "ToxikarpNote", "Fires a rapid stream of toxic bubbles"));
            tooltips.Add(new TooltipLine(Mod, "ToxikarpNote", "Every 30th shot charges a powerful shotgun blast"));
            tooltips.Add(new TooltipLine(Mod, "ToxikarpNote", "   Uses your equipped bullets for the musket volley"));
            tooltips.Add(new TooltipLine(Mod, "ToxikarpNote", "   Center bubble deals heavy damage with a trail"));
        }
    }
}
