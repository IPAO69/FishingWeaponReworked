using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace FishyFishy
{
	// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
	public class FishyFishy : Mod
	{
		internal static Effect? StreakyTrailEffect;

		internal static Effect? SlicePrimitive;
		public enum MessageType : byte
        {
            SendCustomUseStylePlayerDirection,
			SendFalconReworkedUS,
            // add more packet types here as you create them
        }

		public override void Load()
		{
			StreakyTrailEffect = ModContent.Request<Effect>(
				"FishyFishy/Content/Effects/PrimitiveStreakyTrail",
				AssetRequestMode.ImmediateLoad
			).Value;

			SlicePrimitive = ModContent.Request<Effect>(
				"FishyFishy/Content/Effects/SlicePrimitive",
				AssetRequestMode.ImmediateLoad
			).Value;
		}

		public override void Unload()
		{
			Content.Projectiles.SwordFishProjectile.ClearStuckFish();
			StreakyTrailEffect = null;
			SlicePrimitive = null;
		}
	}

	public static class BossHelpers
	{
		public static bool IsAnyBossAlive() {
			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];
				if (npc.active && npc.boss) {
					return true;
				}
			}
			return false;
		}
	}
}
