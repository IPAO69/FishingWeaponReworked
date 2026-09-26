using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace FishyFishy.Content.Projectiles
{
	/// <summary>
	/// Draws a solid glowing wedge that sweeps along a rotation history, built from a raw
	/// triangle-strip ribbon of <see cref="VertexPositionColorTexture"/> vertices. Shared
	/// by any <see cref="BaseSwingProjectile"/> that wants a "light" style trail instead
	/// of afterimages.
	///
	/// Everything GPU-related lives here as static state (effect, scratch buffer) so every
	/// caller reuses the same resources instead of each projectile owning its own copy.
	///
	/// The vertex format always carries texture coordinates and the effect always samples
	/// them, exactly like Calamity's PrimitiveRenderer. An untextured
	/// <c>VertexPositionColor</c> draw can therefore never run before the player renderer's
	/// shader, so the FNA3D "invalid subscript 'm_t0'" crash that path could trigger is
	/// structurally impossible.
	///
	/// No custom .fx is used on purpose: tModLoader 1.4.4 does not compile shader source
	/// when building a mod (ContentConverters only converts .png files, and FxcReader only
	/// wraps pre-compiled effect bytes), and no effect compiler ships with tML. A built-in
	/// <see cref="BasicEffect"/> in VertexColor+Texture mode samples the 1x1 magic pixel
	/// through TEXCOORD0, giving the same result with zero custom assets.
	/// </summary>
	public static class SwordTrailRenderer
	{
		// Built-in effect configured for vertex colors + a sampled texture, matched 1:1 to
		// the VertexPositionColorTexture vertex format - POSITION0 + COLOR0 + TEXCOORD0.
		private static BasicEffect _effect;

		// Scratch buffer reused across every call instead of allocating a fresh array each draw.
		// Grows (rarely) if a caller ever needs more room than it currently has; never shrinks.
		private static VertexPositionColorTexture[] _vertexScratch = new VertexPositionColorTexture[64];

		// Scratch buffers for the spline-smoothed rotation history (angle unwrap + output).
		private static float[] _angleScratch = new float[64];
		private static readonly List<float> _smoothScratch = new List<float>(256);

		/// <summary>
		/// Draws the trail. Must be called from a PreDraw (or similar) while Main.spriteBatch
		/// is currently active - this flushes it, draws the raw primitive, then resumes it.
		/// </summary>
		/// <param name="pivot">World-space point the blade swings around (usually Owner.MountedCenter).</param>
		/// <param name="bladeLength">Distance from the pivot to the blade tip (the sprite's diagonal
		/// extent, scaled); the ribbon spans from just past the handle to this tip.</param>
		/// <param name="rotationHistory">Recent blade rotations, newest first (index 0 = current frame).</param>
		/// <param name="maxAlpha">Brightness of the trail at the current blade position.</param>
		/// <param name="innerColor">Color near the current blade position (keep vivid, not white,
		/// so the additive trail reads as this color).</param>
		/// <param name="outerColor">Color toward the fading tail.</param>
		/// <param name="startDistance">Distance from the pivot where the ribbon begins. Pass the
		/// player's hand distance so the sweeping trail never covers the arm holding the sword.</param>
		public static void Draw(Vector2 pivot, float bladeLength, List<float> rotationHistory,
			float maxAlpha, Color innerColor, Color outerColor, float startDistance = 0f) {
			int count = rotationHistory.Count;
			if (count < 2)
				return;

			GraphicsDevice device = Main.instance.GraphicsDevice;

			if (_effect == null) {
				_effect = new BasicEffect(device) {
					VertexColorEnabled = true,
					TextureEnabled = true
				};
			}

			// Full world->clip transform for the raw draw: translate world space into screen
			// space, then map the viewport to clip space (Y-down). Feeding only the GameView
			// matrix would skip the projection step and put the ribbon in the wrong space.
			Matrix view = Matrix.CreateTranslation(-Main.screenPosition.X, -Main.screenPosition.Y, 0f);
			Matrix projection = Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, -1000, 1000);
			_effect.View = view;
			_effect.Projection = projection;
			_effect.Texture = TextureAssets.MagicPixel.Value;

			// --- Smooth the rotation history with a Catmull-Rom spline ---
			// The raw history is a handful of straight chord segments between per-frame
			// rotations, and the eased swing makes the mid-swing frames move fast, so the
			// ribbon has visible polygon corners. Subdividing each segment and running a
			// spline through the angles (unwrapped so it never snaps across +-PI) turns the
			// wedge into a smooth circular arc.
			float[] ang = _angleScratch;
			if (ang.Length < count)
				ang = _angleScratch = new float[count];
			for (int i = 0; i < count; i++)
				ang[i] = rotationHistory[count - 1 - i];
			for (int i = 1; i < count; i++) {
				while (ang[i] - ang[i - 1] > MathHelper.Pi)
					ang[i] -= MathHelper.TwoPi;
				while (ang[i] - ang[i - 1] < -MathHelper.Pi)
					ang[i] += MathHelper.TwoPi;
			}

			const int subDiv = 8;
			List<float> smooth = _smoothScratch;
			smooth.Clear();
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

			int requiredVertices = smooth.Count * 2;
			if (_vertexScratch.Length < requiredVertices)
				_vertexScratch = new VertexPositionColorTexture[requiredVertices];

			for (int i = 0; i < smooth.Count; i++) {
				// smooth[0] is the oldest sample (tail); walk forward so progress goes
				// from 0 (oldest, tail) to 1 (newest, current blade position).
				float rotation = smooth[i];
				float progress = i / (float)(smooth.Count - 1);

				Vector2 dir = rotation.ToRotationVector2();
				float alpha = (float)Math.Pow(progress, 1.5) * maxAlpha;

				// Cross-section spans the blade from past the hand (so the sweep never washes out
				// the player's arm) to the tip, covering the outer portion of the sprite.
				float startDist = Math.Min(startDistance, bladeLength);
				float handleGap = Math.Max(startDist, bladeLength * 0.05f);
				Vector2 inner = pivot + dir * handleGap;
				Vector2 outer = pivot + dir * bladeLength;
				Color color = Color.Lerp(outerColor, innerColor, progress) * alpha;

				// White pixel is sampled, so the UV only needs to exist for the shader to
				// work - the shape and fade are fully encoded in geometry and vertex color.
				// The inner (arm-side) vertex is transparent so the ribbon fades in toward
				// the blade edge and never washes out the player's arm.
				_vertexScratch[i * 2] = new VertexPositionColorTexture(new Vector3(inner, 0f), new Color(color.R, color.G, color.B, 0), new Vector2(progress, 0f));
				_vertexScratch[i * 2 + 1] = new VertexPositionColorTexture(new Vector3(outer, 0f), color, new Vector2(progress, 1f));
			}

			// Raw GraphicsDevice draws can't happen mid-batch, so flush the current sprite
			// batch, draw the primitive, then resume spriteBatch drawing exactly as it was.
			Main.spriteBatch.End();

			// Save exactly what we're about to change so it can be put back afterward -
			// leaving any of this dirty (especially RasterizerState) corrupts unrelated
			// draws later in the frame or on later frames.
			BlendState oldBlendState = device.BlendState;
			RasterizerState oldRasterizerState = device.RasterizerState;
			DepthStencilState oldDepthStencilState = device.DepthStencilState;

			device.BlendState = BlendState.Additive;
			device.RasterizerState = RasterizerState.CullNone;
			device.DepthStencilState = DepthStencilState.None;

			foreach (EffectPass pass in _effect.CurrentTechnique.Passes) {
				pass.Apply();
				device.DrawUserPrimitives(PrimitiveType.TriangleStrip, _vertexScratch, 0, requiredVertices - 2);
			}

			// DrawUserPrimitives leaves no vertex/index buffer bound, but explicitly clearing
			// them prevents FNA3D's internal binding cache from carrying stale bindings into
			// the next draw call that uses a different vertex format (e.g. the player renderer).
			device.SetVertexBuffer(null);
			device.Indices = null;

			device.BlendState = oldBlendState;
			device.RasterizerState = oldRasterizerState;
			device.DepthStencilState = oldDepthStencilState;

			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
				DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

			// The player renderer's raw body buffer resumes drawing right after this primitive
			// (held-item sprite, front accessories, buff overlays) and those draws run under
			// whatever effect is currently applied to the device. Leaving BasicEffect active
			// here re-transforms those already screen-space vertices by BasicEffect's own
			// view/projection and pushes them off-screen - which hid the composite front arm
			// (drawn after the held projectile) while the classic skin arm (drawn before it)
			// stayed visible. Re-apply the standard pixel shader AFTER Begin (Begin does not
			// change the applied effect in deferred mode, but this guards against versions
			// that do) so those draws are unaffected.
			Main.pixelShader.CurrentTechnique.Passes[0].Apply();
		}
	}
}
