using FishyFishy.Primitive;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishyFishy.Content.Projectiles
{
    public class AnchorSwingProjectile : BaseSwingProjectile
    {
        private List<Vector2> cacheEdge;
        private List<Vector2> cacheHilt;
        private PrimitiveSliceTrail trail;

        private static readonly Dictionary<(int, int), List<Vector2>> _lastCacheEdge = new();
        private static readonly Dictionary<(int, int), List<Vector2>> _lastCacheHilt = new();
        private static readonly Dictionary<(int, int), int> _lastPrimitiveTick = new();

        private const float AnchorScale = 2f;

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WoodenArrowFriendly;

        protected override Texture2D GetDrawTexture() => TextureAssets.Item[ItemID.Anchor].Value;

        // Increased SwingArc for a wider, more curved sweep path
        protected override float SwingArc => 4.5f; 
        protected override int SwingSnapPasses => 3;
        protected override int BaseRecoveryTime => 15;
        protected override float RecoveryLerpSpeed => 0.25f;
        
        // Extended collision width to match the enlarged arc and tip position
        protected override float CollisionWidth => 300f; 
        protected override bool AlternateSwingDirection => true;
        protected override int ComboLength => 2;
        protected override bool ShrinkOnRelease => false;
        protected override bool KeepBladeInFront => false;

        protected override int TrailLength => 18;
        protected override float TrailMaxAlpha => 0.6f;
        protected override Color TrailInnerColor => new Color(80, 80, 95);
        protected override Color TrailOuterColor => new Color(40, 40, 55);

        protected override SoundStyle SwingSound => new SoundStyle("FishyFishy/Content/Sound/AnchorSwing");

        private static readonly SoundStyle HitSound = new SoundStyle("FishyFishy/Content/Sound/AnchorHit");

        private float bladeLength;
        private float drawOpacity;
        private const float TrailReachExtra = 53f;

        protected override int TrailStalenessThreshold => 20;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(HitSound, Projectile.Center);
        }

        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);
            Texture2D tex = GetDrawTexture();
            bladeLength = new Vector2(tex.Width, tex.Height).Length() * AnchorScale * Projectile.scale;

            var key = (Owner.whoAmI, Type);
            bool trailIsFresh = _lastPrimitiveTick.TryGetValue(key, out int lastTick)
                && (Main.GameUpdateCount - lastTick) <= TrailStalenessThreshold;

            cacheEdge = new List<Vector2>();
            cacheHilt = new List<Vector2>();

            if (trailIsFresh) {
                if (_lastCacheEdge.TryGetValue(key, out var savedEdge))
                    cacheEdge.AddRange(savedEdge);
                if (_lastCacheHilt.TryGetValue(key, out var savedHilt))
                    cacheHilt.AddRange(savedHilt);
            }

            if (cacheEdge.Count == 0) {
                Vector2 pivot = Owner.MountedCenter;
                Vector2 tip = pivot + Projectile.rotation.ToRotationVector2() * bladeLength;
                cacheEdge.Add(tip);
                cacheHilt.Add(pivot);
            }
        }

        protected override void DrawLightTrail()
        {
        }

        private Color SliceColorFunction(float progress)
        {
            return Color.Lerp(new Color(100, 95, 110), new Color(50, 48, 65), progress) with { A = 0 } * 0.8f * drawOpacity;
        }

        private void ManagePrimitiveStuff()
        {
            Vector2 pivot = Owner.MountedCenter;
            float trailLen = bladeLength + TrailReachExtra;

            // Build Catmull-Rom smoothed positions from the base class's _trailRotations
            int count = _trailRotations.Count;
            if (count < 2)
                return;

            // Reverse to chronological order (oldest first) and unwrap angles
            float[] ang = new float[count];
            for (int i = 0; i < count; i++)
                ang[i] = _trailRotations[count - 1 - i];
            for (int i = 1; i < count; i++) {
                while (ang[i] - ang[i - 1] > MathHelper.Pi)
                    ang[i] -= MathHelper.TwoPi;
                while (ang[i] - ang[i - 1] < -MathHelper.Pi)
                    ang[i] += MathHelper.TwoPi;
            }

            // Subdivide with Catmull-Rom spline
            const int subDiv = 4;
            List<float> smooth = new List<float>(count * subDiv);
            for (int i = 0; i < count - 1; i++) {
                float p0 = ang[Math.Max(i - 1, 0)];
                float p1 = ang[i];
                float p2 = ang[i + 1];
                float p3 = ang[Math.Min(i + 2, count - 1)];
                for (int s = 0; s < subDiv; s++) {
                    float t = s / (float)subDiv;
                    float t2 = t * t;
                    float t3 = t2 * t;
                    smooth.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }
            smooth.Add(ang[count - 1]);

            // Convert smoothed angles to edge/hilt positions
            cacheHilt.Clear();
            cacheEdge.Clear();

            for (int i = 0; i < smooth.Count; i++) {
                Vector2 dir = smooth[i].ToRotationVector2();
                cacheHilt.Add(pivot);
                cacheEdge.Add(pivot + dir * trailLen);
            }

            trail ??= new PrimitiveSliceTrail(10, SliceColorFunction);

            bool isDownward = !AlternateSwingDirection || (comboStep % 2 == 0);
            bool isLeft = Owner.direction < 0;

            if (isDownward ^ isLeft)
                trail.SetPositions(cacheHilt, cacheEdge);
            else
                trail.SetPositions(cacheEdge, cacheHilt);

            var key = (Owner.whoAmI, Type);
            _lastCacheEdge[key] = new List<Vector2>(cacheEdge);
            _lastCacheHilt[key] = new List<Vector2>(cacheHilt);
            _lastPrimitiveTick[key] = (int)Main.GameUpdateCount;
        }

        public override void AI()
        {
            base.AI();

            if (Main.dedServ)
                return;

            ManagePrimitiveStuff();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            float progress = 1f - ((float)(Projectile.timeLeft - recoveryTime) / swingDuration);
            drawOpacity = MathHelper.Clamp(progress, 0f, 1f);

            if (trail != null && cacheEdge != null && cacheEdge.Count >= 2)
            {
                Effect effect = FishyFishy.SlicePrimitive;

                bool isDownward = !AlternateSwingDirection || (comboStep % 2 == 0);
                bool isLeft = Owner.direction < 0;
                bool flipY = isDownward ^ isLeft;

                effect.Parameters["flipY"]?.SetValue(flipY ? 1f : 0f);

                effect.Parameters["edgeSize"]?.SetValue(0.07f);
                effect.Parameters["edgeSizePower"]?.SetValue(2f);
                effect.Parameters["edgeTransitionSize"]?.SetValue(0.03f);
                effect.Parameters["edgeTransitionOpacity"]?.SetValue(0.2f);
                effect.Parameters["edgeColorMultiplier"]?.SetValue(new Vector4(1.5f, 1.5f, 2f, 1.8f));
                effect.Parameters["edgeColorAdd"]?.SetValue(new Vector4(1f, 1f, 1.2f, 1.5f));
                
                // Adjusted powers to accentuate the visual curvature density
                effect.Parameters["horizontalPower"]?.SetValue(2.2f);
                effect.Parameters["verticalPower"]?.SetValue(1.8f);

                RasterizerState originalRasterizer = Main.graphics.GraphicsDevice.RasterizerState;
                Main.graphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;

                trail.Render(effect, -Main.screenPosition);

                Main.graphics.GraphicsDevice.RasterizerState = originalRasterizer;
            }

            Texture2D texture = GetDrawTexture();
            if (texture == null)
                texture = TextureAssets.Item[Owner.HeldItem.type].Value;

            Vector2 anchorWorldPos = Owner.MountedCenter + Projectile.rotation.ToRotationVector2() * bladeLength;

            // Draw chain from hand to anchor tip
            Texture2D chainTexture = TextureAssets.Item[ItemID.Chain].Value;
            Vector2 chainStart = Owner.MountedCenter;
            Vector2 chainEnd = anchorWorldPos;
            Vector2 chainDelta = chainEnd - chainStart;
            float chainLen = chainDelta.Length();
            float chainAngle = chainDelta.ToRotation();
            int segmentCount = (int)(chainLen / (chainTexture.Height * 0.8f));

            for (int i = 0; i < segmentCount; i++) {
                float t = segmentCount > 1 ? (float)i / (segmentCount - 1) : 0f;
                Vector2 segPos = Vector2.Lerp(chainStart, chainEnd, t) - Main.screenPosition;

                float segRotation = chainAngle + MathHelper.PiOver2;
                Vector2 segOrigin = new Vector2(chainTexture.Width * 0.5f, 0f);

                Main.spriteBatch.Draw(chainTexture, segPos, null, lightColor * 0.9f,
                    segRotation, segOrigin, Projectile.scale, SpriteEffects.None, 0f);
            }

            Vector2 origin = texture.Size() * 0.5f;

            SpriteEffects effects = SpriteEffects.None;

            Vector2 dirToPlayer = (Owner.MountedCenter - anchorWorldPos).SafeNormalize(Vector2.UnitX);

            float drawRot = dirToPlayer.ToRotation() + MathHelper.ToRadians(180f);
            Vector2 drawPos = anchorWorldPos - Main.screenPosition;

            Main.spriteBatch.Draw(texture, drawPos, null, lightColor,
                drawRot, origin, Projectile.scale * AnchorScale, effects, 0);

            Texture2D glow = TextureAssets.Extra[ExtrasID.ThePerfectGlow].Value;
            float pulse = 0.5f + 0.3f * (float)Math.Sin(Main.timeForVisualEffects * 0.08f);
            Color glowColor = new Color(100, 95, 120) * 0.15f * pulse;
            Main.spriteBatch.Draw(glow, drawPos, null, glowColor,
                drawRot, origin, Projectile.scale * AnchorScale * 1.3f, effects, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();

            return false;
        }

        protected override void PostDraw(Texture2D texture, Vector2 drawPos, float drawRot, Vector2 origin, float scale, SpriteEffects effects, Color lightColor)
        {
        }
    }
}