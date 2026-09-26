using FishyFishy.Content.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishyFishy.Content.Projectiles
{
	public class FalconBladeSwingProjectile : BaseSwingProjectile
	{
		private static readonly SoundStyle FalconBladeReloadSound = new SoundStyle("FishyFishy/Content/Sound/FalconBladeReloadSound");

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WoodenArrowFriendly;

		protected override Texture2D GetDrawTexture() => TextureAssets.Item[ItemID.FalconBlade].Value;

		protected override Color TrailInnerColor => new Color(120, 190, 255);
		protected override Color TrailOuterColor => new Color(50, 110, 210);

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			OffhandGunPlayer gunPlayer = Owner.GetModPlayer<OffhandGunPlayer>();

			if (gunPlayer.pendingShotDelay > 0 || gunPlayer.gunDrawTimer > 0)
				return;

			Vector2 aimDirection = (Main.MouseWorld - Owner.MountedCenter).SafeNormalize(Vector2.UnitX * Owner.direction);

			gunPlayer.gunDrawTimer = 45;
			gunPlayer.gunAngle = aimDirection.ToRotation();
			gunPlayer.isReloading = true;

			if (Main.myPlayer == Owner.whoAmI) {
				SoundEngine.PlaySound(FalconBladeReloadSound, Owner.Center);
				
				gunPlayer.pendingShotDelay = 30;
				gunPlayer.pendingShotDelayMax = 30;
				gunPlayer.pendingShotDamage = damageDone * 2;
			}
		}
	}
}
