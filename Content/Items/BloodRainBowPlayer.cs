using FishyFishy.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishyFishy.Content.Items
{
    public class BloodRainBowPlayer : ModPlayer
    {
        public float chargeDamage = 0f;
        public const float MaxCharge = 100f;
        private bool playedMaxSound = false;
        public int windUpTimer = 0;

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Player.HeldItem.type == ItemID.BloodRainBow && (!BossHelpers.IsAnyBossAlive() || target.boss))
                chargeDamage = System.Math.Min(chargeDamage + damageDone * 0.1f, MaxCharge);
        }

        public override void PostUpdate()
        {
            if (Player.HeldItem.type != ItemID.BloodRainBow)
            {
                chargeDamage = 0f;
                playedMaxSound = false;
                windUpTimer = 0;
            }

            if (chargeDamage >= MaxCharge && !playedMaxSound && Main.myPlayer == Player.whoAmI)
            {
                playedMaxSound = true;
                SoundEngine.PlaySound(SoundID.MaxMana, Player.Center);
            }

            if (Main.myPlayer == Player.whoAmI && Main.mouseRight && Player.HeldItem.type == ItemID.BloodRainBow && chargeDamage >= MaxCharge && windUpTimer == 0)
            {
                chargeDamage = 0f;
                playedMaxSound = false;
                windUpTimer = 120;
                SoundEngine.PlaySound(SoundID.Item170, Player.Center);
            }

            if (windUpTimer > 0)
            {
                Vector2 bowPos = Player.MountedCenter;
                Vector2 aimDir = Vector2.Zero;
                if (Main.myPlayer == Player.whoAmI)
                {
                    Player.direction = Main.MouseWorld.X > Player.Center.X ? 1 : -1;
                    float aim = (Main.MouseWorld - Player.MountedCenter).ToRotation();
                    Player.itemRotation = Player.direction == 1 ? aim : aim - MathHelper.Pi;

                    aimDir = new Vector2(System.MathF.Cos(aim), System.MathF.Sin(aim));
                    bowPos = Player.MountedCenter + new Vector2(Player.direction * 4 - 2, Player.gravDir * -10 + 5f) + aimDir * 20f;

                    for (int j = 0; j < 4; j++)
                    {
                        float a = MathHelper.TwoPi / 4f * j;
                        Vector2 dDir = new Vector2(System.MathF.Cos(a), System.MathF.Sin(a)) * Main.rand.NextFloat(0.5f, 1.5f);
                        Dust d = Dust.NewDustDirect(bowPos - new Vector2(2), 4, 4, DustID.RainbowTorch, dDir.X, dDir.Y, 0, Color.Red, 0.6f);
                        d.noGravity = true;
                    }

                    for (int j = 0; j < 6; j++)
                    {
                        Vector2 pos = bowPos + aimDir * (j * 4f + Main.rand.NextFloat(-0.5f, 0.5f));
                        Dust d = Dust.NewDustDirect(pos - new Vector2(2), 4, 4, DustID.RainbowTorch, 0f, 0f, 0, Color.Red, 0.6f);
                        d.noGravity = true;
                        d.velocity *= 0.1f;
                    }
                }

                Lighting.AddLight(bowPos, 0.8f, 0f, 0f);

                windUpTimer--;
                Player.itemAnimation = System.Math.Max(Player.itemAnimation, windUpTimer + 1);
                Player.itemTime = System.Math.Max(Player.itemTime, windUpTimer + 1);

                if (windUpTimer == 0 && Main.myPlayer == Player.whoAmI)
                {
                    Vector2 pos = Player.Center + aimDir * 40f;
                    int dmg = Player.HeldItem.damage * 4;
                    Projectile.NewProjectileDirect(Player.GetSource_ItemUse(Player.HeldItem), pos, aimDir * Player.HeldItem.shootSpeed, ModContent.ProjectileType<BloodNautilusShot>(), dmg, 3f, Player.whoAmI);

                    Vector2 dustCenter = pos + aimDir * 20f;
                    for (int i = 0; i < 30; i++)
                    {
                        float angle = MathHelper.TwoPi / 30f * i;
                        Vector2 dir = new Vector2(System.MathF.Cos(angle), System.MathF.Sin(angle)) * Main.rand.NextFloat(4f, 10f);
                        Dust d = Dust.NewDustDirect(dustCenter - new Vector2(4), 8, 8, DustID.Blood, dir.X, dir.Y, 100, default, 2f);
                        d.noGravity = true;
                    }
                }
            }

        }
    }
}
