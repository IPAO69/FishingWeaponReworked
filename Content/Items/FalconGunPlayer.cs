using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using FishyFishy.Content.Projectiles;

namespace FishyFishy.Content.Items
{
    public class OffhandGunPlayer : ModPlayer
    {
        public int gunDrawTimer = 0;
        public float gunAngle = 0f;
        public bool isReloading = false;
        public int recoilKickTimer = 0;
        public int pendingShotDelay = 0;
        public int pendingShotDamage = 0;
        public bool gunFrozen = false;

        public int pendingShotDelayMax = 1;

        public override void PostUpdate() {
            if (gunDrawTimer > 0) {
                gunDrawTimer--;
                if (gunDrawTimer == 0)
                    gunFrozen = false;
            }

            if (recoilKickTimer > 0)
                recoilKickTimer--;

            if (pendingShotDelay > 0) {
                Vector2 aimDirection = (Main.MouseWorld - Player.MountedCenter).SafeNormalize(Vector2.UnitX * Player.direction);
                gunAngle = aimDirection.ToRotation();

                pendingShotDelay--;

                if (pendingShotDelay == 0 && Main.myPlayer == Player.whoAmI) {
                    FireShot();
                }
            }

            if (gunDrawTimer > 0 || isReloading || gunFrozen) {
                Player.direction = Math.Cos(gunAngle) >= 0 ? 1 : -1;
            }
        }

        private void FireShot() {
            isReloading = false;
            gunFrozen = true;
            recoilKickTimer = 10;

            SoundEngine.PlaySound(SoundID.Item36, Player.Center);

            Vector2 aimDirection = gunAngle.ToRotationVector2();

            Main.instance.LoadProjectile(ProjectileID.DD2ExplosiveTrapT1Explosion);
            Texture2D explosionTexture = TextureAssets.Projectile[ProjectileID.DD2ExplosiveTrapT1Explosion].Value;
            float halfLength = explosionTexture.Height * 0.1f;

            Vector2 muzzleAnchor = Player.MountedCenter + aimDirection * 20f;
            Vector2 spawnPosition = muzzleAnchor + aimDirection * halfLength + new Vector2(-1f, -70f);

            int proj = Projectile.NewProjectile(
                Player.GetSource_ItemUse(Player.HeldItem),
                spawnPosition,
                Vector2.Zero,
                ProjectileID.DD2ExplosiveTrapT2Explosion,
                pendingShotDamage,
                1f,
                Player.whoAmI
            );

            Main.projectile[proj].rotation = gunAngle + MathHelper.PiOver2;
            Main.projectile[proj].gfxOffY -= 40f;

            FalconGunShotTracker.MarkFalconShot(proj);

            float recoilStrength = 6.5f;
            Player.velocity -= aimDirection * recoilStrength;
        }
    }
}
