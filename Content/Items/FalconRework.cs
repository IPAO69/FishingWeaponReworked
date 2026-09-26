using FishyFishy.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;

namespace FishyFishy.Content.Items
{
	public class FalconBladeRework : GlobalItem
	{
		public override bool AppliesToEntity(Item item, bool lateInstantiation) {
			return item.type == ItemID.FalconBlade;
		}

		public override void SetDefaults(Item item) {
			item.damage = 30;
			item.knockBack = 5f;
			item.useAnimation = 50;
			item.useTime = 50;
			item.crit += 5;
			item.useStyle = ItemUseStyleID.Shoot;
			item.noMelee = true;
			item.noUseGraphic = true;
			item.channel = true;
			item.shoot = ModContent.ProjectileType<FalconBladeSwingProjectile>();
		}

		public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			bool exists = false;
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile p = Main.projectile[i];
				if (p.active && p.type == type && p.owner == player.whoAmI) {
					exists = true;
					break;
				}
			}

			if (!exists)
				Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);

			return false;
		}

		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
			tooltips.Add(new TooltipLine(Mod, "FalconReworkNote", "After successful hit"));
			tooltips.Add(new TooltipLine(Mod, "FalconReworkNote", $"   Use the shotgun to bump deals {item.damage * 2} damage ({item.damage} x 2 = {item.damage * 2})"));
		}
	}
}
