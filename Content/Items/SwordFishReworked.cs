using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using FishyFishy.Content.Projectiles;
using System.Collections.Generic;

namespace FishyFishy.Content.Items
{
    public class SwordFishReworked : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstantiation)
        {
            return item.type == ItemID.Swordfish;
        }

        public override void SetDefaults(Item item)
        {   

            item.DamageType = DamageClass.Melee;
            item.autoReuse = true;
            item.crit += 10;
        }
                public override bool AltFunctionUse(Item item, Player player)
        {
            return true;
        }
        public override void HoldItem(Item item, Player player)
        {
            if (item.type != ItemID.Swordfish)
                return;
            
            bool usingAltFire = player.altFunctionUse == 2;

            if (usingAltFire)
            {
                item.noUseGraphic = true;
                item.noMelee = true;
                item.autoReuse = true;
                item.useTime = 40;
                item.useStyle = ItemUseStyleID.Swing;
                item.useAnimation = 40;
                item.useTurn = false;
                item.shootSpeed = 12f;
                item.shoot = ModContent.ProjectileType<SwordFishProjectile>();
            }
            else
            {
                item.noUseGraphic = true;
                item.noMelee = true;
                item.autoReuse = true;
                item.useTime = 20;
                item.useAnimation = 20;
                item.useTurn = false;
                item.useStyle = ItemUseStyleID.Shoot;
                item.shootSpeed = 3.7f;
                item.shoot = ProjectileID.Swordfish;
    
            }
        }


        public override bool CanShoot(Item item, Player player)
        {
            return true;
        }
        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (type == ModContent.ProjectileType<SwordFishProjectile>())
            {

                Vector2 coordOffset = new Vector2(0, -20f);
                Vector2 direction = Main.MouseWorld - (player.Center + coordOffset);
                if (direction == Vector2.Zero)
                    direction = Vector2.UnitX * player.direction; 

                direction.Normalize();
                velocity = direction * item.shootSpeed;
                
                position = player.Center + coordOffset;
            }

            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "SwordfishNote", "Left-click: Melee swings"));
            tooltips.Add(new TooltipLine(Mod, "SwordfishNote", "   Melee hits charge each stuck fish (10 hits each to pop)"));
            tooltips.Add(new TooltipLine(Mod, "SwordfishNote", "Right-click: Throw a swordfish that sticks to enemies"));
            tooltips.Add(new TooltipLine(Mod, "SwordfishNote", "   When all stuck fish are fully charged, they pop together"));
            tooltips.Add(new TooltipLine(Mod, "SwordfishNote", "   Each stuck fish adds 10% to the pop damage"));
            tooltips.Add(new TooltipLine(Mod, "SwordfishNote", "   Indicator glows white to red as it charges"));
        }

    }
}
