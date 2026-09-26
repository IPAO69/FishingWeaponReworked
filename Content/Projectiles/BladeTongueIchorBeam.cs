using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using FishyFishy.Core;
using FishyFishy.Helpers;
using FishyFishy.Primitive;
using static FishyFishy.Helpers.FishyUtils;

namespace FishyFishy.Content.Projectiles
{
    public class BladeTongueIchorBeam : ModProjectile
    {
        private const int CacheSize = 20;
        private const int FadeTime = 25;

        private int Timer;
        private bool fading;
        private float fade = 1f;
        private List<Vector2>? cache;
        private PrimitiveTrail? trail;
        private PrimitiveTrail? trailBloom;

        public override string Texture => "FishyFishy/Content/Projectiles/IchorBolt";

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 19;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.timeLeft = 180;
            Projectile.scale = 2f;
        }

        public override void AI()
        {
            Timer++;

            if (Projectile.timeLeft <= 1 && !fading)
                StartFade();

            ManageTrail();

            Projectile.rotation = Projectile.velocity.ToRotation();

            NPC? target = null;
            float nearestDistSq = 600f * 600f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n.active && !n.friendly && n.CanBeChasedBy())
                {
                    float distSq = Vector2.DistanceSquared(Projectile.Center, n.Center);
                    if (distSq < nearestDistSq)
                    {
                        nearestDistSq = distSq;
                        target = n;
                    }
                }
            }

            if (fading)
            {
                Projectile.velocity *= 0.9f;

                fade -= 1f / FadeTime;
                if (fade <= 0f)
                {
                    Projectile.Kill();
                    return;
                }
            }
            else if (target != null)
            {
                Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 20f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.1f);
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero) * Math.Max(Projectile.velocity.Length(), 14f);
            }

            Lighting.AddLight(Projectile.Center, 1f, 0.8f, 0.2f);

            for (int i = 0; i < 3; i++)
            {
                Vector2 vel = -Projectile.velocity * Main.rand.NextFloat(0.1f, 0.5f) + Main.rand.NextVector2Circular(1f, 1f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, vel, 0, new Color(255, 210, 60), Main.rand.NextFloat(1f, 1.6f));
                d.noGravity = true;
            }
        }

        private void StartFade()
        {
            if (fading)
                return;

            fading = true;
            fade = 1f;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
            Projectile.timeLeft = FadeTime + 1;
            Projectile.velocity *= 0.1f;

            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            StartFade();
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

            trail ??= new PrimitiveTrail(30, WidthFunction, ColorFunction);
            trail.SetPositionsSmart(cache, position, RigidPointRetreivalFunction);
            trail.NextPosition = position;

            trailBloom ??= new PrimitiveTrail(30, BloomWidthFunction, BloomColorFunction);
            trailBloom.SetPositionsSmart(cache, position, RigidPointRetreivalFunction);
            trailBloom.NextPosition = position;
        }

        private static float WidthFunction(float progress) => 10f * MathF.Pow(progress, 0.3f);

        private Color ColorFunction(float progress)
        {
            Color color = Color.Lerp(Color.Gold, Color.Yellow, MathF.Pow(progress, 4f));
            color.A = 0;
            return color * MathF.Pow(progress, 1.2f) * fade;
        }

        private static float BloomWidthFunction(float progress) => 18f * MathF.Pow(progress, 0.3f);

        private Color BloomColorFunction(float progress)
        {
            Color color = Color.Lerp(Color.OrangeRed, Color.Orange, MathF.Pow(progress, 4f));
            color.A = 0;
            return color * MathF.Pow(progress, 1.2f) * fade * 0.6f;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ || cache == null || trail == null || cache.Count < 2)
                return;

            fade = 1f;

            GhostTrail ghost = new GhostTrail(cache, trail, 0.3f, null, delegate (Effect effect, float fading)
            {
                effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["verticalStretch"].SetValue(0.5f);
                effect.Parameters["repeats"].SetValue(4f);
                effect.Parameters["overlayScroll"].SetValue(Main.GameUpdateCount * -0.01f);
                effect.Parameters["overlayOpacity"].SetValue(0.5f);
                effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>("FishyFishy/Content/Noise/FireTrail").Value);
                effect.Parameters["streakNoiseTexture"].SetValue(TextureAssets.MagicPixel.Value);
                effect.Parameters["streakScale"].SetValue(1f);
            });

            ghost.ShrinkTrailLenght = true;
            ghost.DrawLayer = DrawhookLayer.AboveTiles;
            GhostTrailsHandler.LogNewTrail(ghost);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (trailBloom != null)
            {
                Effect effect = FishyFishy.StreakyTrailEffect;
                effect.Parameters["time"].SetValue(Main.GameUpdateCount * 0.02f);
                effect.Parameters["verticalStretch"].SetValue(0.5f);
                effect.Parameters["repeats"].SetValue(4f);
                effect.Parameters["overlayScroll"].SetValue(Main.GameUpdateCount * -0.01f);
                effect.Parameters["overlayOpacity"].SetValue(0.5f);
                effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>("FishyFishy/Content/Noise/BasicTrail").Value);
                effect.Parameters["streakNoiseTexture"].SetValue(TextureAssets.MagicPixel.Value);
                effect.Parameters["streakScale"].SetValue(1f);

                trailBloom.Render(effect, -Main.screenPosition);
            }

            if (trail != null)
            {
                Effect effect = FishyFishy.StreakyTrailEffect;
                effect.Parameters["time"].SetValue(Main.GameUpdateCount * 0.02f);
                effect.Parameters["verticalStretch"].SetValue(0.5f);
                effect.Parameters["repeats"].SetValue(4f);
                effect.Parameters["overlayScroll"].SetValue(Main.GameUpdateCount * -0.01f);
                effect.Parameters["overlayOpacity"].SetValue(0.5f);
                effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>("FishyFishy/Content/Noise/FireTrail").Value);
                effect.Parameters["streakNoiseTexture"].SetValue(TextureAssets.MagicPixel.Value);
                effect.Parameters["streakScale"].SetValue(1f);

                trail.Render(effect, -Main.screenPosition);
            }

            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;

            Texture2D glow = TextureAssets.Extra[ExtrasID.ThePerfectGlow].Value;
            float pulse = 0.7f + 0.3f * (float)Math.Sin(Timer * 0.3f);
            Main.spriteBatch.Draw(glow, pos, null, new Color(255, 210, 60) * 0.5f * pulse * fade, 0f, glow.Size() * 0.5f, 0.4f, SpriteEffects.None, 0f);

            if (fading)
            {
                Texture2D explosive = ModContent.Request<Texture2D>("FishyFishy/Content/Noise/Explosive").Value;
                Texture2D explosiveRing = ModContent.Request<Texture2D>("FishyFishy/Content/Noise/ExplosiveRing").Value;
                float explosionScale = (0.3f + (1f - fade) * 0.35f) * 0.2f;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(explosive, pos, null, new Color(255, 180, 60) * fade, 0f, explosive.Size() * 0.5f, explosionScale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(explosiveRing, pos, null, Color.Yellow * fade, 0f, explosiveRing.Size() * 0.5f, explosionScale * 0.5f, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            }

            Main.spriteBatch.Draw(tex, pos, null, Color.White * fade, Projectile.rotation + MathHelper.PiOver2, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
