using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace FishyFishy.Content
{
    public class AnchorShakePlayer : ModPlayer
    {
        public int shakeTimer;
        public float shakeIntensity;

        public override void ModifyScreenPosition()
        {
            if (shakeTimer > 0)
            {
                float progress = shakeTimer / 10f;
                float shake = shakeIntensity * progress;
                Main.screenPosition += Main.rand.NextVector2Circular(shake, shake);
                shakeTimer--;
            }
        }
    }
}
