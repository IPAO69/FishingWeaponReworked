using FishyFishy.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;

namespace FishyFishy.Content.Items
{
	public class PurpledClubberfishReworked : GlobalItem
	{
		public override bool AppliesToEntity(Item item, bool lateInstantiation) {
			return item.type == ItemID.PurpleClubberfish;
		}

		public override void SetDefaults(Item item) {
			item.damage = 70;
			item.knockBack = 7f;
			item.crit += 10;
			item.useAnimation = 40;
			item.useTime = 40;
			item.useStyle = ItemUseStyleID.Shoot;
			item.noMelee = true;
			item.noUseGraphic = true;
			item.channel = true;
			item.shoot = ModContent.ProjectileType<PurpleClubberfishSwingProjectile>();
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
			tooltips.Add(new TooltipLine(Mod, "ClubberfishCharge", "Hold to charge up to 2s"));
			tooltips.Add(new TooltipLine(Mod, "ClubberfishCharge", $"   At full charge deals {item.damage * 4} damage ({item.damage} x 4 = {item.damage * 4}) and heals 5% of damage dealt"));
		}
	}
}
