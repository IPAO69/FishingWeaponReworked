using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishyFishy.UI
{
    public class VanillaCrateGlobalItem : GlobalItem
    {
        private const float NearbyRange = 300f;

        public override void OnConsumeItem(Item item, Player player)
        {
            if (player.whoAmI != Main.myPlayer)
                return;

            if (!IsVanillaCrate(item))
                return;

            var results = new List<CrateOpenUI.RevealEntry>();
            CollectInventoryChanges(player, results);
            CollectWorldDrops(player, results);

            if (results.Count > 0)
                CrateOpenUI.ShowReveal(results);
        }

        private static void CollectInventoryChanges(Player player, List<CrateOpenUI.RevealEntry> results)
        {
            int[] preTypes = CrateWatcherPlayer.LastFrameInvTypes;
            int[] preStacks = CrateWatcherPlayer.LastFrameInvStacks;

            for (int i = 0; i < player.inventory.Length; i++)
            {
                Item now = player.inventory[i];
                if (now.IsAir)
                    continue;

                int prevType = i < preTypes.Length ? preTypes[i] : 0;
                int prevStack = i < preStacks.Length ? preStacks[i] : 0;

                if (now.type != prevType || now.stack > prevStack)
                    results.Add(new CrateOpenUI.RevealEntry(now.Name, ItemRarity.GetColor(now.rare), now.type));
            }
        }

        private static void CollectWorldDrops(Player player, List<CrateOpenUI.RevealEntry> results)
        {
            bool[] preActive = CrateWatcherPlayer.LastFrameWorldActive;
            int[] preType = CrateWatcherPlayer.LastFrameWorldType;

            for (int i = 0; i < Main.item.Length; i++)
            {
                Item worldItem = Main.item[i];
                if (!worldItem.active)
                    continue;

                bool existedLastFrame = i < preActive.Length && preActive[i] && preType[i] == worldItem.type;
                if (existedLastFrame)
                    continue;

                bool isMine = worldItem.playerIndexTheItemIsReservedFor == player.whoAmI
                    || Vector2.Distance(worldItem.position, player.position) < NearbyRange;

                if (isMine)
                    results.Add(new CrateOpenUI.RevealEntry(worldItem.Name, ItemRarity.GetColor(worldItem.rare), worldItem.type));
            }
        }

        private static bool IsVanillaCrate(Item item)
        {
            return item.type > ItemID.None
                && item.type < ItemID.Count
                && item.Name.EndsWith("Crate");
        }
    }
}