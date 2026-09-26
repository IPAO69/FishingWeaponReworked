using FishyFishy.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;

namespace FishyFishy.Content.Items
{
    public class AnchorReworked : GlobalItem
    {
        private static readonly Dictionary<int, int> SwingCounters = new();
        private static readonly Dictionary<int, int> LastUseTick = new();
        private const int ComboResetTicks = 150;

        public override bool AppliesToEntity(Item item, bool lateInstantiation)
        {
            return item.type == ItemID.Anchor;
        }

        public override void SetDefaults(Item item)
        {
            item.damage = 100;
            item.knockBack = 8f;
            item.useAnimation = 100;
            item.useTime = 100;
            item.crit += 5;
            item.useStyle = ItemUseStyleID.Shoot;
            item.noMelee = true;
            item.noUseGraphic = true;
            item.channel = true;
            item.autoReuse = false;
            item.shoot = ModContent.ProjectileType<AnchorSwingProjectile>();
            item.shootSpeed = 1f;
        }

        public override bool CanShoot(Item item, Player player)
        {
            int thrownType = ModContent.ProjectileType<AnchorThrownProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == thrownType && p.owner == player.whoAmI)
                    return false;
            }
            return true;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int whoAmI = player.whoAmI;

            if (!SwingCounters.ContainsKey(whoAmI))
                SwingCounters[whoAmI] = 0;

            if (LastUseTick.TryGetValue(whoAmI, out int lastTick) && (Main.GameUpdateCount - lastTick) > ComboResetTicks)
                SwingCounters[whoAmI] = 0;

            LastUseTick[whoAmI] = (int)Main.GameUpdateCount;
            int combo = SwingCounters[whoAmI] % 3;

            if (combo < 2)
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
            }
            else
            {
                bool exists = false;
                int thrownType = ModContent.ProjectileType<AnchorThrownProjectile>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == thrownType && p.owner == player.whoAmI)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    Vector2 dir = (Main.MouseWorld - player.Center).SafeNormalize(Vector2.UnitX * player.direction);
                    Projectile.NewProjectile(source, player.Center, dir * 22f, thrownType, damage * 5, knockback, player.whoAmI);
                    player.velocity -= dir * 8f;
                }
            }

            SwingCounters[whoAmI]++;
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "AnchorReworkNote", "3-hit combo: Swing, Swing, then Throw"));
            tooltips.Add(new TooltipLine(Mod, "AnchorReworkNote", "   Throw launches the anchor"));
            tooltips.Add(new TooltipLine(Mod, "AnchorReworkNote", "   Direct hit deals 5x damage return deals normal damage"));
            tooltips.Add(new TooltipLine(Mod, "AnchorReworkNote", "   Direct hit triggers an AOE explosion"));
            tooltips.Add(new TooltipLine(Mod, "AnchorReworkNote", "   Auto-returns when off-screen"));
        }
    }
}
