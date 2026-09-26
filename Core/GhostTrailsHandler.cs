using System.Collections.Generic;
using FishyFishy.Primitive;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace FishyFishy.Core
{
	// Made after seeing spirit's interesting way to deal with lingering trails
	public class GhostTrailsHandler : ModSystem
	{
		internal static List<GhostTrail> trails;

		public override void Load()
		{
			trails = new List<GhostTrail>();
		}

		public override void Unload()
		{
			foreach (GhostTrail trail in trails)
				trail.trail.Dispose();
			trails = null;
		}

		public override void PostUpdateEverything()
		{
			if (!Main.dedServ)
				UpdateTrails();
		}

		/// <summary>
		/// Spawns the trail instance provided into the world.
		/// </summary>
		public static void LogNewTrail(GhostTrail trail)
		{
			//Don't spawn trails if on the server side either, or if the list is somehow null
			if (Main.dedServ || trails == null)
				return;

			trails.Add(trail);
		}

		public static void UpdateTrails()
		{
			for (int i = 0; i < trails.Count; i++)
			{
				GhostTrail trail = trails[i];

				trail.Decay();
				if (trail.Dead)
				{
					trails.RemoveAt(i);
					i--;
				}
			}
		}

		public override void PostDrawTiles()
		{
			if (Main.dedServ || trails == null || trails.Count == 0)
				return;

			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
			foreach (GhostTrail trail in trails)
				trail.Draw();
			Main.spriteBatch.End();
		}
	}
}
