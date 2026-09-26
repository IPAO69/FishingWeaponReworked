using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using FishyFishy.UI;

namespace FishyFishy.Content.Items
{
    public class ObsidianSwordfishPlayer : ModPlayer
    {
        public int DashCooldown;

        private static Texture2D meterFrame;
        private static Texture2D fillBar;
        private static Texture2D fullBar;
        private static readonly Rectangle FillSource = new Rectangle(36, 30, 9, 34);

        public override void Load()
        {
            meterFrame = ModContent.Request<Texture2D>("FishyFishy/Content/UI/Asset/ObsidianFishMeter", AssetRequestMode.ImmediateLoad).Value;
            fillBar = ModContent.Request<Texture2D>("FishyFishy/Content/UI/Asset/BarObsidianFish", AssetRequestMode.ImmediateLoad).Value;
            fullBar = ModContent.Request<Texture2D>("FishyFishy/Content/UI/Asset/FullBarObsidianFish", AssetRequestMode.ImmediateLoad).Value;
        }

        public override void Unload()
        {
            meterFrame = null;
            fillBar = null;
            fullBar = null;
        }

        public override void ResetEffects()
        {
            if (DashCooldown > 0)
            {
                DashCooldown--;
                if (DashCooldown == 0)
                    SoundEngine.PlaySound(SoundID.MaxMana with { Volume = 1.6f, Pitch = 0.55f }, Player.Center);
            }
        }

        public override void PostUpdate()
        {
            if (Player.HeldItem.type == ItemID.ObsidianSwordfish || DashCooldown > 0)
                WeaponCooldownUI.SetMeter("ObsidianSwordfish", meterFrame, fillBar, fullBar, FillSource, DashCooldown, 60 * 20);
            else
                WeaponCooldownUI.ClearCooldown("ObsidianSwordfish");
        }
    }
}
