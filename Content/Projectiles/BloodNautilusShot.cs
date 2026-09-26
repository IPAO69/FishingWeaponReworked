using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishyFishy.Content.Projectiles
{
    public class BloodNautilusShot : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 4;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.timeLeft = 300;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 2;
        }

        public override void OnSpawn(IEntitySource source)
        {
            SoundEngine.PlaySound(SoundID.Item42, Projectile.Center);
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            Projectile.alpha = 0;

            for (int i = 0; i < 3; i++)
            {
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Blood, Projectile.velocity.X * 0.3f, Projectile.velocity.Y * 0.3f, 100, default, 1.5f);
                d.noGravity = true;
                d.velocity *= 0.5f;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HideCombatText();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(SoundID.Item171, Projectile.Center);
            target.AddBuff(BuffID.Ichor, 300);
            CombatText.NewText(target.Hitbox, Color.Yellow, $"{hit.Damage}!", true);
            for (int i = 0; i < 12; i++)
            {
                Dust d = Dust.NewDustDirect(target.position, target.width, target.height, DustID.Blood, 0f, 0f, 100, default, 2f);
                d.noGravity = true;
                d.velocity = new Vector2(Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-6f, 6f));
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item171, Projectile.Center);
            for (int i = 0; i < 20; i++)
            {
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Blood, 0f, 0f, 100, default, 2.5f);
                d.noGravity = true;
                d.velocity = new Vector2(Main.rand.NextFloat(-8f, 8f), Main.rand.NextFloat(-8f, 8f));
            }
        }
    }
}
