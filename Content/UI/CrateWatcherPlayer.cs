using System;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace FishyFishy.UI
{
    public class CrateWatcherPlayer : ModPlayer
    {
        public static int[] LastFrameInvTypes = Array.Empty<int>();
        public static int[] LastFrameInvStacks = Array.Empty<int>();

        public static bool[] LastFrameWorldActive = Array.Empty<bool>();
        public static int[] LastFrameWorldType = Array.Empty<int>();

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            LastFrameInvTypes = Player.inventory.Select(i => i.type).ToArray();
            LastFrameInvStacks = Player.inventory.Select(i => i.stack).ToArray();

            int len = Main.item.Length;
            if (LastFrameWorldActive.Length != len)
            {
                LastFrameWorldActive = new bool[len];
                LastFrameWorldType = new int[len];
            }

            for (int i = 0; i < len; i++)
            {
                Item it = Main.item[i];
                LastFrameWorldActive[i] = it.active;
                LastFrameWorldType[i] = it.type;
            }
        }
    }
}