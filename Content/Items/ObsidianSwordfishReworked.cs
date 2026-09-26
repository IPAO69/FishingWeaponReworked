using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using FishyFishy.Content.Projectiles;
using System.Collections.Generic;

namespace FishyFishy.Content.Items
{
    public class ObsidianSwordfishReworked : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstantiation)
        {
            return item.type == ItemID.ObsidianSwordfish;
        }

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[ItemID.ObsidianSwordfish] = true;
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
            if (item.type != ItemID.ObsidianSwordfish)
                return;

            bool usingAltFire = player.altFunctionUse == 2;

            if (usingAltFire)
            {
                item.noUseGraphic = true;
                item.noMelee = true;
                item.autoReuse = true;
                item.useTime = 30;
                item.useStyle = ItemUseStyleID.Swing;
                item.useAnimation = 30;
                item.useTurn = false;
                item.shootSpeed = 16f;
                item.UseSound = SoundID.Item1;
                item.shoot = ModContent.ProjectileType<SwordFishProjectile>();
            }
            else
            {
                item.noUseGraphic = true;
                item.noMelee = true;
                item.autoReuse = false;
                item.channel = true;
                item.useTime = 10;
                item.useAnimation = 10;
                item.useTurn = false;
                item.useStyle = ItemUseStyleID.Shoot;
                item.shootSpeed = 0f;
                item.UseSound = null;
                item.shoot = ModContent.ProjectileType<ObsidianSwordfishDashProjectile>();
            }
        }

        public override bool CanShoot(Item item, Player player)
        {
            return true;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (type == ModContent.ProjectileType<ObsidianSwordfishDashProjectile>())
            {
                bool exists = false;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == type && p.owner == player.whoAmI)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
                return false;
            }

            if (type == ModContent.ProjectileType<SwordFishProjectile>())
            {
                Vector2 coordOffset = new Vector2(0, -20f);
                Vector2 direction = Main.MouseWorld - (player.Center + coordOffset);
                if (direction == Vector2.Zero)
                    direction = Vector2.UnitX * player.direction;

                direction.Normalize();
                velocity = direction * item.shootSpeed;
                position = player.Center + coordOffset;

                Projectile p = Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, player.whoAmI);
                if (p.ModProjectile is SwordFishProjectile fish)
                {
                    fish.MaxStuckFish = 10;
                    fish.MeleeHitsToTrigger = 3;
                    fish.SpriteTexture = "Terraria/Images/Item_" + ItemID.ObsidianSwordfish;
                    fish.IsObsidian = true;
                }
                return false;
            }

            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "SwordfishNote", "Left-click: Hold to charge a dash"));
            tooltips.Add(new TooltipLine(Mod, "SwordfishNote", "   Longer charge = longer dash; hits charge stuck fish (3 hits to pop)"));
            tooltips.Add(new TooltipLine(Mod, "SwordfishNote", "   Dash has a 20 second cooldown shown on screen"));
            tooltips.Add(new TooltipLine(Mod, "SwordfishNote", "   During the cooldown, left-click charges a stab instead"));
            tooltips.Add(new TooltipLine(Mod, "SwordfishNote", "Right-click: Throw a swordfish that sticks to enemies"));
            tooltips.Add(new TooltipLine(Mod, "SwordfishNote", "   When all stuck fish are fully charged, they pop together"));
            tooltips.Add(new TooltipLine(Mod, "SwordfishNote", "   Each stuck fish adds 10% to the pop damage (up to 10 stuck)"));
            tooltips.Add(new TooltipLine(Mod, "SwordfishNote", "   Indicator glows white to red as it charges"));
            tooltips.Add(new TooltipLine(Mod, "SwordfishNote", "   A sound plays when the dash cooldown is ready"));
        }
    }
}
