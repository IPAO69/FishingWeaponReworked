using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace FishyFishy.UI
{
    [Autoload(Side = ModSide.Client)]
    public sealed class CrateOpenUI : ModSystem
    {
        public readonly struct RevealEntry
        {
            public readonly string Name;
            public readonly Color Color;
            public readonly int ItemType;

            public RevealEntry(string name, Color color, int itemType)
            {
                Name = name;
                Color = color;
                ItemType = itemType;
            }
        }

        private enum State
        {
            Idle,
            Result
        }

        private const int DefaultLingerTicks = 150;
        private const float SpinDurationTicks = 45f;
        private const float SpinCount = 3f;
        private const float SlotScale = 2.2f;
        private const float SlotGap = 24f;

        private static State state = State.Idle;
        private static int resultLinger;
        private static int totalLingerTicks;
        private static List<RevealEntry> currentReveal = new();
        private static bool wasSkipped;

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int idx = layers.FindIndex(layer => layer.Name == "Vanilla: Entity Health Bars");
            if (idx == -1)
                idx = layers.Count - 1;

            layers.Insert(idx, new LegacyGameInterfaceLayer("FishyFishy: Crate Open UI", DrawLayer, InterfaceScaleType.UI));
        }

        public override void Unload()
        {
            state = State.Idle;
            currentReveal = new List<RevealEntry>();
        }

        public static void ShowReveal(IEnumerable<RevealEntry> items, int lingerTicks = DefaultLingerTicks)
        {
            currentReveal = new List<RevealEntry>(items);
            if (currentReveal.Count == 0)
                return;

            wasSkipped = false;
            totalLingerTicks = lingerTicks;
            resultLinger = lingerTicks;
            state = State.Result;
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        public static void ShowReveal(string name, Color color, int itemType, int lingerTicks = DefaultLingerTicks)
            => ShowReveal(new[] { new RevealEntry(name, color, itemType) }, lingerTicks);

        public static void ShowSkipped(int lingerTicks = DefaultLingerTicks)
        {
            currentReveal.Clear();
            wasSkipped = true;
            totalLingerTicks = lingerTicks;
            resultLinger = lingerTicks;
            state = State.Result;
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        private static bool DrawLayer()
        {
            if (state != State.Result)
                return true;

            resultLinger--;
            if (resultLinger <= 0)
            {
                state = State.Idle;
                return true;
            }

            float elapsed = totalLingerTicks - resultLinger;

            SpriteBatch sb = Main.spriteBatch;
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black * 0.8f);

            if (wasSkipped)
            {
                DrawCenteredText(sb, "YOU SKIPPED!", 0f, Color.Red);
                return true;
            }

            DrawCenteredText(sb, "GOT:", -140f, Color.White, 1.3f);

            Texture2D slotBg = TextureAssets.InventoryBack.Value;
            Vector2 slotSize = slotBg.Size() * SlotScale;

            int count = currentReveal.Count;
            float totalWidth = count * slotSize.X + Math.Max(0, count - 1) * SlotGap;
            float startX = Main.screenWidth * 0.5f - totalWidth * 0.5f + slotSize.X * 0.5f;
            float centerY = Main.screenHeight * 0.5f;

            for (int i = 0; i < count; i++)
            {
                RevealEntry entry = currentReveal[i];
                Vector2 center = new Vector2(startX + i * (slotSize.X + SlotGap), centerY);

                DrawItemBox(sb, entry, center, slotBg, slotSize, elapsed);
                DrawCenteredTextAt(sb, entry.Name, new Vector2(center.X, center.Y + slotSize.Y * 0.5f + 22f), entry.Color, 1f);
            }

            return true;
        }

        private static void DrawItemBox(SpriteBatch sb, RevealEntry entry, Vector2 center, Texture2D slotBg, Vector2 slotSize, float elapsed)
        {
            Rectangle slotRect = new Rectangle(
                (int)(center.X - slotSize.X * 0.5f),
                (int)(center.Y - slotSize.Y * 0.5f),
                (int)slotSize.X,
                (int)slotSize.Y);
            sb.Draw(slotBg, slotRect, Color.White);

            Texture2D itemTex = TextureAssets.Item[entry.ItemType].Value;
            Rectangle frame = itemTex.Bounds;

            float spinProgress = MathHelper.Clamp(elapsed / SpinDurationTicks, 0f, 1f);
            float eased = 1f - (float)Math.Pow(1f - spinProgress, 3);
            float rotation = eased * MathHelper.TwoPi * SpinCount;

            float postSpin = Math.Max(0f, elapsed - SpinDurationTicks);
            float bounce = 1f + 0.25f * Math.Max(0f, 1f - postSpin / 12f);

            float targetSize = slotSize.X * 0.5f;
            float baseScale = targetSize / Math.Max(frame.Width, frame.Height);
            float finalScale = baseScale * bounce;

            Vector2 origin = new Vector2(frame.Width * 0.5f, frame.Height * 0.5f);
            sb.Draw(itemTex, center, frame, Color.White, rotation, origin, finalScale, SpriteEffects.None, 0f);
        }

        private static void DrawCenteredText(SpriteBatch sb, string text, float yOffset, Color color, float scale = 1.5f)
            => DrawCenteredTextAt(sb, text, new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f + yOffset), color, scale);

        private static void DrawCenteredTextAt(SpriteBatch sb, string text, Vector2 center, Color color, float scale)
        {
            Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * scale;
            Vector2 pos = center - size * 0.5f;
            Utils.DrawBorderString(sb, text, pos, color, scale);
        }
    }
}