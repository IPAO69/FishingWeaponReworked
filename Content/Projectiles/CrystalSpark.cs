using System;
using System.Collections.Generic;
using FishyFishy.Content.Items;
using FishyFishy.Helpers;
using FishyFishy.Primitive;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static FishyFishy.Helpers.FishyUtils;

namespace FishyFishy.Content.Projectiles
{
    public class CrystalSpark : ModProjectile
    {
        private const int CacheSize = 10;
        private const float HomingRadius = 420f;
        private const float HomingSpeed = 9f;
        private const int HomingDelayTicks = 12;

        private int homingTimer;
        private List<Vector2> cache;
        private PrimitiveTrail trail;

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CrystalShard;

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.scale = 0.7f;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.tileCollide = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();

            int tier = CrystalSerpentReworked.GetTier(Main.player[Projectile.owner]);

            if (tier >= 2)
                HomingAI();

            if (Main.dedServ)
                return;

            Visuals();
        }

        private void HomingAI()
        {
            if (++homingTimer < HomingDelayTicks)
                return;

            NPC target = CrystalSerpentShard.FindNearestTarget(Projectile.Center, HomingRadius);
            if (target == null)
                return;

            Vector2 desired = Projectile.DirectionTo(target.Center) * HomingSpeed;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.08f);

            if (Projectile.velocity.LengthSquared() < 1f)
                Projectile.velocity = desired;
        }

        private void Visuals()
        {
            if (Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.PinkTorch,
                    Main.rand.NextVector2Circular(0.3f, 0.3f),
                    0,
                    default,
                    Main.rand.NextFloat(0.6f, 1f));
                d.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, 0.35f, 0.18f, 0.45f);

            Vector2 position = Projectile.Center + Projectile.velocity;

            cache ??= new List<Vector2>();
            cache.Add(position);
            while (cache.Count > CacheSize)
                cache.RemoveAt(0);

            trail ??= new PrimitiveTrail(CacheSize, TrailWidthFunction, ColorFunction, new RoundedTip(8));
            trail.SetPositionsSmart(cache, position, RigidPointRetreivalFunction);
            trail.NextPosition = position + Projectile.velocity;
        }

        private static float TrailWidthFunction(float progress) => 5f * MathF.Pow(progress, 0.3f);

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
