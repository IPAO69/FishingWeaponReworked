using System.Collections.Generic;
using FishyFishy.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishyFishy.Content.Items
{
    public class BladeTongueRework : GlobalItem
    {
        public override bool InstancePerEntity => true;

        private int attackCounter = 0;

        public override bool AppliesToEntity(Item entity, bool lateInstantiation) {
            return entity.type == ItemID.Bladetongue;     
        }

        public override void SetDefaults(Item item) {
            item.damage = 80;
            item.useStyle = ItemUseStyleID.Shoot;
            item.noMelee = true;
            item.noUseGraphic = true;
            item.channel = true;
            item.useTime = 35;
            item.useAnimation = 35;
            item.shoot = ModContent.ProjectileType<BladeTongueProjectile>();
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            bool tooEarly = false;

            for (int i = 0; i < Main.maxProjectiles; i++) {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == type && p.owner == player.whoAmI && p.timeLeft > 12) {
                    tooEarly = true;
                    break;
                }
            }

            if (tooEarly) {
                return false;
            }

            if (!player.controlUseItem) {
                attackCounter = 0;
            }

            int currentUseTime = 40;
            if (attackCounter >= 2 && attackCounter < 4) {
                currentUseTime = 20;
            }

            attackCounter = (attackCounter + 1) % 4;

            float attackSpeed = player.GetTotalAttackSpeed(DamageClass.Melee);
            int adjustedTime = (int)System.Math.Max(1, currentUseTime / attackSpeed);

            player.itemAnimation = adjustedTime;
            player.itemAnimationMax = adjustedTime;
            player.itemTime = adjustedTime;
            player.itemTimeMax = adjustedTime;


            item.useTime = currentUseTime;
            item.useAnimation = currentUseTime;

            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "BladeTongueNote" , "Charge: 3000 Damages - Get buffs for 15 seconds"));
            tooltips.Add(new TooltipLine(Mod, "BladeTongueNote" , "   Slash deal one additional hit"));
            tooltips.Add(new TooltipLine(Mod, "BladeTongueNote" , "   Give player 15 defense and 15% attack speed"));
            tooltips.Add(new TooltipLine(Mod, "BladeTongueNote" , "   Every 3rd swing fires 2 homing ichor bolts, 4th fires 3"));
            tooltips.Add(new TooltipLine(Mod, "BladeTongueNote" , "   Ichor bolts deal 75% of your melee damage"));
            tooltips.Add(new TooltipLine(Mod, "BladeTongueNote" , "   Charge is gone if you swap item"));
        }
    }
}