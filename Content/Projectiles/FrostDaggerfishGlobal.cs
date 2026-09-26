using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using FishyFishy.Helpers;
using FishyFishy.Primitive;
using static FishyFishy.Helpers.FishyUtils;

namespace FishyFishy.Content.Projectiles
{
    public class FrostDaggerfishGlobal : GlobalProjectile
    {
        private const int CacheSize = 15;

        private bool isDaggerfish;
        private List<Vector2> cache;
        private PrimitiveTrail trail;

        public override bool InstancePerEntity => true;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            isDaggerfish = projectile.type == ProjectileID.FrostDaggerfish;
        }

        public override void AI(Projectile projectile)
        {
            if (!isDaggerfish)
                return;

            if (Main.dedServ)
                return;

            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(
                    projectile.Center,
                    DustID.Ice,
                    -projectile.velocity * 0.05f + Main.rand.NextVector2Circular(0.3f, 0.3f),
                    0,
                    new Color(120, 180, 255),
                    Main.rand.NextFloat(0.6f, 1f));
                d.noGravity = true;
            }

            Lighting.AddLight(projectile.Center, 0.2f, 0.4f, 0.8f);

            Vector2 position = projectile.Center + projectile.velocity;

            cache ??= new List<Vector2>();
            cache.Add(position);
            while (cache.Count > CacheSize)
                cache.RemoveAt(0);

            trail ??= new PrimitiveTrail(CacheSize, WidthFunction, ColorFunction);
            trail.SetPositionsSmart(cache, position, RigidPointRetreivalFunction);
            trail.NextPosition = position;
        }

        private static float WidthFunction(float progress) => 6f * MathF.Pow(progress, 0.3f);

        private static Color ColorFunction(float progress)
        {
            Color color = Color.Lerp(new Color(80, 160, 255), new Color(200, 230, 255), progress);
            color.A = 0;
            return color * MathF.Pow(progress, 0.6f);
        }

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            if (!isDaggerfish || trail == null)
                return true;

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
            return true;
        }
    }
}
