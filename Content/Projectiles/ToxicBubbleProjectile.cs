using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using FishyFishy.Helpers;
using FishyFishy.Primitive;
using static FishyFishy.Helpers.FishyUtils;

namespace FishyFishy.Content.Projectiles
{
    public class ToxicBubbleProjectile : ModProjectile
    {
        private const int CacheSize = 15;

        private List<Vector2> cache = null;
        private PrimitiveTrail trail = null;

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Bubble;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 2;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.timeLeft = 120;
        }

        private Color DustColor => Projectile.ai[0] >= 0.5f ? new Color(100, 255, 50) : new Color(30, 255, 30);

        public override void AI()
        {
            if (Projectile.ai[0] >= 0.5f)
                ManageTrail();

            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Water,
                    -Projectile.velocity * 0.1f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                    0,
                    DustColor,
                    Main.rand.NextFloat(0.8f, 1.2f));
                d.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, 0.3f, 0.8f, 0.2f);
        }

        private void ManageTrail()
        {
            if (Main.dedServ)
                return;

            Vector2 position = Projectile.Center + Projectile.velocity;

            cache ??= new List<Vector2>();
            cache.Add(position);
            while (cache.Count > CacheSize)
                cache.RemoveAt(0);

            trail ??= new PrimitiveTrail(CacheSize, WidthFunction, ColorFunction);
            trail.SetPositionsSmart(cache, position, RigidPointRetreivalFunction);
            trail.NextPosition = position;
        }

        private static float WidthFunction(float progress) => 8f * MathF.Pow(progress, 0.4f);

        private static Color ColorFunction(float progress)
        {
            Color color = Color.Lerp(Color.LimeGreen, Color.Yellow, progress);
            color.A = 0;
            return color * MathF.Pow(progress, 0.8f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.ai[0] < 0.5f || trail == null)
                return true;

            Effect effect = FishyFishy.StreakyTrailEffect;
            effect.Parameters["time"].SetValue(Main.GameUpdateCount * 0.02f);
            effect.Parameters["verticalStretch"].SetValue(0.5f);
            effect.Parameters["repeats"].SetValue(2f);
            effect.Parameters["overlayScroll"].SetValue(Main.GameUpdateCount * -0.01f);
            effect.Parameters["overlayOpacity"].SetValue(0.5f);
            effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>("FishyFishy/Content/Noise/BasicTrail").Value);
            effect.Parameters["streakNoiseTexture"].SetValue(TextureAssets.MagicPixel.Value);
            effect.Parameters["streakScale"].SetValue(1f);

            trail.Render(effect, -Main.screenPosition);
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 8; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(2f, 2f);
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Water,
                    vel,
                    0,
                    DustColor,
                    Main.rand.NextFloat(0.8f, 1.2f));
                d.noGravity = true;
            }
        }
    }
}
