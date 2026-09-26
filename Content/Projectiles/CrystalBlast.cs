using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishyFishy.Content.Projectiles
{
    public class CrystalBlast : ModProjectile
    {
        private const int Lifetime = 12;

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WoodenArrowFriendly;

        public override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float fade = (float)Projectile.timeLeft / Lifetime;
            Vector2 pos = Projectile.Center - Main.screenPosition;

            Texture2D explosive = ModContent.Request<Texture2D>("FishyFishy/Content/Noise/Explosive").Value;
            Texture2D explosiveRing = ModContent.Request<Texture2D>("FishyFishy/Content/Noise/ExplosiveRing").Value;
            float explosionScale = (0.3f + (1f - fade) * 0.35f) * 0.2f * 2.5f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(explosive, pos, null, new Color(255, 200, 245) * fade, 0f, explosive.Size() * 0.5f, explosionScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(explosiveRing, pos, null, new Color(255, 140, 235) * fade, 0f, explosiveRing.Size() * 0.5f, explosionScale * 0.5f, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}
