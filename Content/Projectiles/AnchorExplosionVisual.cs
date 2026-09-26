using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishyFishy.Content.Projectiles
{
    public class AnchorExplosionVisual : ModProjectile
    {
        private const float MaxRadius = 10f * 16f;
        private const int Lifetime = 20;

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WoodenArrowFriendly;

        public override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = 1f - ((float)Projectile.timeLeft / Lifetime);
            float alpha = 1f - progress;

            Texture2D glow = TextureAssets.Extra[ExtrasID.ThePerfectGlow].Value;
            Texture2D ring = ModContent.Request<Texture2D>("FishyFishy/Content/Noise/ExplosiveRing").Value;

            float scale = 0.15f + progress * 1.8f;

            Color color = Color.Lerp(new Color(100, 95, 110), new Color(50, 48, 65), progress) * alpha;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition, null, color * 0.6f, 0f, glow.Size() * 0.5f, scale * 0.6f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ring, Projectile.Center - Main.screenPosition, null, color, 0f, ring.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}
