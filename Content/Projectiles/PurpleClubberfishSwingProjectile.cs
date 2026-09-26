using FishyFishy.Content.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;

namespace FishyFishy.Content.Projectiles
{
	public class PurpleClubberfishSwingProjectile : BaseSwingProjectile
	{
		private bool hasCharged = true;
		private int chargeTimer = 0;
		private const int MaxChargeTicks = 120;
		private const int MinChargeTicks = 45;
		private bool maxChargeSoundPlayed;
		private float chargeLiftOffset;
		private static readonly SoundStyle PurpleClubSound = new("FishyFishy/Content/Sound/PurpleClubSound") { Volume = 1.5f };

		protected override float SwingStartAngleOffset => -chargeLiftOffset * direction;

		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WoodenArrowFriendly;

		protected override Texture2D GetDrawTexture() => TextureAssets.Item[ItemID.PurpleClubberfish].Value;

		protected override Color TrailInnerColor => new Color(200, 120, 255);
		protected override Color TrailOuterColor => new Color(150, 50, 240);

		public override void OnSpawn(IEntitySource source) {
			base.OnSpawn(source);
			hasCharged = true;
			chargeTimer = 0;
			maxChargeSoundPlayed = false;
			chargeLiftOffset = 0f;
			Projectile.friendly = false;
			Projectile.timeLeft = 10000;
		}

		public override void AI() {
			if (!Owner.active || Owner.dead || Owner.noItems || Owner.CCed) {
				Projectile.Kill();
				return;
			}

			int newDir = Main.MouseWorld.X > Owner.MountedCenter.X ? 1 : -1;
			if (newDir != Owner.direction) {
				Owner.direction = newDir;
				Projectile.spriteDirection = newDir;
				direction = newDir;
			}

			if (hasCharged) {
				Owner.itemAnimation = 2;
				Owner.itemTime = 2;

				chargeTimer = System.Math.Min(chargeTimer + 1, MaxChargeTicks);

				if (!maxChargeSoundPlayed && chargeTimer >= MaxChargeTicks) {
					SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = 0.5f });
					maxChargeSoundPlayed = true;
				}

				float mouseAngle = Owner.AngleTo(Main.MouseWorld);
				chargeLiftOffset = MathHelper.Lerp(0f, 0.6f, chargeTimer / (float)MaxChargeTicks);
				Projectile.rotation = mouseAngle - (SwingArc * 0.5f + chargeLiftOffset) * direction;

				UpdateHeldArm();

				if (!Owner.controlUseItem) {
					if (chargeTimer < MinChargeTicks) {
						Projectile.Kill();
						return;
					}
					SetSwingBaseAngle(Owner.AngleTo(Main.MouseWorld));
					hasCharged = false;
					Projectile.friendly = true;
					float attackSpeed = Owner.GetTotalAttackSpeed(Projectile.DamageType);
					recoveryTime = System.Math.Max(1, (int)(BaseRecoveryTime / attackSpeed));
					Projectile.timeLeft = swingDuration + recoveryTime;
				}
				return;
			}

			base.AI();
		}

		protected override void PostDraw(Texture2D texture, Vector2 drawPos, float drawRot, Vector2 origin, float scale, SpriteEffects effects, Color lightColor) {
			if (chargeTimer >= MaxChargeTicks) {
					float pulse = 0.5f + 0.3f * (float)System.Math.Sin(Main.timeForVisualEffects * 0.12f);
				Color glowColor = new Color(180, 60, 255) * pulse;

				Main.spriteBatch.Draw(texture, drawPos, null, glowColor,
					drawRot, origin, scale, effects, 0);
			}
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
			base.ModifyHitNPC(target, ref modifiers);

			if (chargeTimer >= MaxChargeTicks) {
				modifiers.HideCombatText();
				modifiers.FinalDamage *= 4f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			if (chargeTimer >= MaxChargeTicks) {
				SoundEngine.PlaySound(PurpleClubSound, target.Center);
				int healAmount = (int)(damageDone * 0.05f);
				Owner.Heal(healAmount);
				CombatText.NewText(target.Hitbox, Color.Yellow, $"{hit.Damage}!", true);
			}
		}
	}
}
