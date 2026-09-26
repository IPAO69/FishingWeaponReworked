using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishyFishy.Content.Items
{
	public class PurpleClubberfishPlayer : ModPlayer
	{
		public int chargeTimer = 0;
		public const int MaxCharge = 120; // 3 seconds at 60 FPS

		public override void PostUpdate() {
			if (Player.HeldItem.type == ItemID.PurpleClubberfish && Player.itemAnimation > 0)
				chargeTimer = System.Math.Min(chargeTimer + 1, MaxCharge);
			else
				chargeTimer = 0;
		}
	}
}
