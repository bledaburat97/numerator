using System;

namespace Scripts
{
    // Canvas units, independent of the device's pixel density. Kept free of Unity APIs
    // so the complete phone/board/fruit layout matrix can be checked outside the editor.
    public readonly struct FruitLabLayoutMetrics
    {
        public const int MinimumVisibleGuesses = 6;
        public const float HistoryCardGap = 2f;
        public const float HistoryFeedbackReservation = 112f;
        public readonly float Width, Height, Padding, Gap, HeaderHeight;
        public readonly float TopPadding, BottomPadding;
        public readonly float BoardHeight, BoardFooterHeight, ActionWidth, ChoicesWidth, ChoicesHeight, PowerUpHeight;
        public readonly float GameAreaHeight, HistoryHeight, HistoryRowHeight, HistorySpacing;

        public FruitLabLayoutMetrics(float width, float height, bool mechanicCaption = false, bool boardFooter = false, bool positionIndicators = false, bool positionEditing = false)
        {
            Width = Math.Max(1f, width);
            Height = Math.Max(1f, height);
            float scale = Width / 375f;
            float compact = Clamp(Height / (667f * scale), 0.72f, 1f);
            Padding = 12f * scale * compact;
            TopPadding = BottomPadding = 4f * scale * compact;
            Gap = 6f * scale * compact;
            HeaderHeight = mechanicCaption ? 104f : 90f;
            BoardFooterHeight = boardFooter ? Math.Max(24f,24f*scale*compact) : 0f;
            BoardFooterHeight += Math.Max(18f, Math.Min(24f, 20f*scale*compact + (Height-580f*scale)*.45f));
            BoardHeight = 64f * scale * compact + BoardFooterHeight;
            ActionWidth = Math.Max(44f,48f * scale * compact);
            ChoicesWidth = Width - Padding * 2f - (ActionWidth + Gap) * 2f;
            ChoicesHeight = 146f * scale * compact;
            PowerUpHeight = Math.Max(48f, 48f * scale * compact);
            GameAreaHeight = BoardHeight + ChoicesHeight + PowerUpHeight + Gap * 2f;
            HistoryHeight = Height - TopPadding - BottomPadding - HeaderHeight - GameAreaHeight - Gap * 2f;
            HistorySpacing = (Height >= 667f*scale?4f:2f) * scale * compact;
            HistoryRowHeight = GetHistoryRowHeight(HistoryHeight, HistorySpacing, scale);
        }

        public static float GetHistoryRowHeight(float viewportHeight, float spacing, float scale = 1f)
        {
            // Six whole rows, five gaps and top/bottom padding, even on short phones.
            float available = (viewportHeight - spacing * 7f) / MinimumVisibleGuesses;
            return Math.Max(1f, Math.Min(64f * scale, available));
        }

        public static int GetFirstChoiceRowCount(int fruitCount) => (fruitCount + 1) / 2;

        public static float GetHistoryCardSize(float width, float rowHeight, int boardCount)
        {
            float availableWidth = width - HistoryFeedbackReservation - Math.Max(0, boardCount - 1) * HistoryCardGap;
            return Math.Max(1f, Math.Min(rowHeight - 4f, availableWidth / Math.Max(1, boardCount)));
        }

        private static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
    }
}
