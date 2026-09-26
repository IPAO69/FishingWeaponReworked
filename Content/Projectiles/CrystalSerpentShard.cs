using System;
using System.Collections.Generic;
using FishyFishy.Content.Items;
using FishyFishy.Helpers;
using FishyFishy.Primitive;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static FishyFishy.Helpers.FishyUtils;

namespace FishyFishy.Content.Projectiles
{
    public class CrystalSerpentShard : ModProjectile
    {
        private const int CacheSize = 14;
        private const int BaseSparkCount = 3;
        private const int Tier3SparkCount = 5;
        private const float BaseBlastRadius = 90f;
        private const float Tier3BlastRadius = 130f;

        private List<Vector2> cache;
        private PrimitiveTrail trail;

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CrystalShard;

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Main.dedServ)
                return;

            Visuals();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SoundEngine.PlaySound(SoundID.Item110, Projectile.Center);
            return true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(SoundID.Item110, Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            int tier = CrystalSerpentReworked.GetTier(Main.player[Projectile.owner]);
            float blastRadius = tier >= 3 ? Tier3BlastRadius : BaseBlastRadius;

            Projectile blast = Projectile.NewProjectileDirect(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<CrystalBlast>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            blast.width = (int)(blastRadius * 2f);
            blast.height = (int)(blastRadius * 2f);
            blast.Center = Projectile.Center;

            SoundEngine.PlaySound(SoundID.Item118 with { PitchVariance = 0.4f }, Projectile.Center);

            int sparkCount = tier >= 3 ? Tier3SparkCount : BaseSparkCount;
            float sparkScaleBoost = tier >= 3 ? 1.35f : 1f;

            var source = Projectile.GetSource_Death();
            for (int i = 0; i < sparkCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(5f, 8f);
                Projectile spark = Projectile.NewProjectileDirect(source, Projectile.Center, velocity, ModContent.ProjectileType<CrystalSpark>(), Projectile.damage, Projectile.knockBack * 0.5f, Projectile.owner);
                spark.scale *= sparkScaleBoost;
            }
        }

        internal static NPC FindNearestTarget(Vector2 center, float radius)
        {
            NPC closest = null;
            float closestDistance = radius;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.lifeMax <= 5)
                    continue;

                float distance = (npc.Center - center).Length();
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = npc;
                }
            }

            return closest;
        }

        private void Visuals()
        {
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.PinkTorch,
                    -Projectile.velocity * 0.05f + Main.rand.NextVector2Circular(0.4f, 0.4f),
                    0,
                    default,
                    Main.rand.NextFloat(0.7f, 1.2f));
                d.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, 0.5f, 0.25f, 0.6f);

            Vector2 position = Projectile.Center + Projectile.velocity;

            cache ??= new List<Vector2>();
            cache.Add(position);
            while (cache.Count > CacheSize)
                cache.RemoveAt(0);

            trail ??= new PrimitiveTrail(CacheSize, TrailWidthFunction, ColorFunction, new RoundedTip(12));
            trail.SetPositionsSmart(cache, position, RigidPointRetreivalFunction);
            trail.NextPosition = position + Projectile.velocity;
        }

        private float TrailWidthFunction(float progress) => 9f * Projectile.scale * MathF.Pow(progress, 0.3f);

        private static Color ColorFunction(float progress)
        {
            Color color = MulticolorLerp(progress, new Color(255, 120, 200), new Color(180, 100, 255), new Color(255, 210, 240));
            color.A = 0;
            return color * MathF.Pow(progress, 0.6f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (trail == null)
                return false;

            Effect effect = FishyFishy.StreakyTrailEffect;
            effect.Parameters["time"].SetValue(Main.GameUpdateCount * 0.02f);
            effect.Parameters["verticalStretch"].SetValue(0.5f);
            effect.Parameters["repeats"].SetValue(2f);
            effect.Parameters["overlayScroll"].SetValue(Main.GameUpdateCount * -0.015f);
            effect.Parameters["overlayOpacity"].SetValue(0.4f);
            effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>("FishyFishy/Content/Noise/BasicTrail").Value);
            effect.Parameters["streakNoiseTexture"].SetValue(TextureAssets.MagicPixel.Value);
            effect.Parameters["streakScale"].SetValue(1f);

            trail.Render(effect, -Main.screenPosition);
            return false;
        }
    }
}
