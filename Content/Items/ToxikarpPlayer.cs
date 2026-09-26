using FishyFishy.Content.Projectiles;
using FishyFishy.Helpers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishyFishy.Content.Items
{
    public class ToxikarpPlayer : ModPlayer
    {
        public const int ShotsPerBurst = 30;
        public const int WindupTicks = 24;

        private int shotCount;
        private int windupTimer;
        private bool windingUp;
        private int shotgunDamage;
        private float shotgunKnockback;

        public static ToxikarpPlayer For(Player player) => player.GetModPlayer<ToxikarpPlayer>();

        public bool IsWindingUp => windingUp;

        public bool ShouldFireShotgun()
        {
            shotCount++;
            return shotCount >= ShotsPerBurst;
        }

        public void BeginWindup(int damage, float knockback)
        {
            windingUp = true;
            windupTimer = WindupTicks;
            shotgunDamage = damage;
            shotgunKnockback = knockback;
            shotCount = 0;
        }

        public override void PostUpdate()
        {
            if (!windingUp)
                return;

            if (Player.HeldItem.type != ItemID.Toxikarp)
            {
                CancelWindup();
                return;
            }

            float progress = 1f - windupTimer / (float)WindupTicks;
            float shakeIntensity = MathHelper.Lerp(1f, 6f, progress);
            Main.instance.CameraModifiers.Add(new ShakeCamera(shakeIntensity, 4, "ToxikarpWindup"));

            windupTimer--;
            if (windupTimer > 0)
                return;

            FireShotgun();
            windingUp = false;
        }

        private void FireShotgun()
        {
            Main.instance.CameraModifiers.Add(new ShakeCamera(10f, 10, "ToxikarpShotgun"));

            var source = new EntitySource_ItemUse(Player, Player.HeldItem, "ToxikarpShotgun");
            Vector2 position = Player.MountedCenter;
            Vector2 dir = (Main.MouseWorld - position).SafeNormalize(Vector2.UnitX * Player.direction);

            int bulletProj = ProjectileID.Bullet;
            for (int i = 0; i < Player.inventory.Length; i++)
            {
                Item ammo = Player.inventory[i];
                if (ammo.active && ammo.ammo == AmmoID.Bullet)
                {
                    bulletProj = ammo.shoot;
                    break;
                }
            }

            for (int i = 0; i < 15; i++)
            {
                float angle = MathHelper.Lerp(MathHelper.ToRadians(15f), MathHelper.ToRadians(-15f), i / 14f);
                Vector2 vel = dir.RotatedBy(angle) * 10f;
                Projectile.NewProjectile(source, position, vel, bulletProj, shotgunDamage , shotgunKnockback, Player.whoAmI);
            }

            int center = Projectile.NewProjectile(source, position, dir * 18f, ModContent.ProjectileType<ToxicBubbleProjectile>(), shotgunDamage * 2, shotgunKnockback, Player.whoAmI);
            if (center >= 0 && center < Main.maxProjectiles)
                Main.projectile[center].ai[0] = 1f;
        }

        private void CancelWindup()
        {
            windingUp = false;
            windupTimer = 0;
        }
    }
}
