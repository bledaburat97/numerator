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
        public readonly float BoardHeight, ActionWidth, ChoicesWidth, ChoicesHeight, PowerUpHeight;
        public readonly float GameAreaHeight, HistoryHeight, HistoryRowHeight, HistorySpacing;

        public FruitLabLayoutMetrics(float width, float height)
        {
            Width = Math.Max(1f, width);
            Height = Math.Max(1f, height);
            float scale = Width / 375f;
            float compact = Clamp(Height / (667f * scale), 0.72f, 1f);
            Padding = 12f * scale * compact;
            TopPadding = BottomPadding = 4f * scale * compact;
            Gap = 6f * scale * compact;
            HeaderHeight = 72f * scale;
            BoardHeight = 64f * scale * compact;
            ActionWidth = 48f * scale * compact;
            ChoicesWidth = Width - Padding * 2f - (ActionWidth + Gap) * 2f;
            ChoicesHeight = 116f * scale * compact;
            PowerUpHeight = 44f * scale * compact;
            GameAreaHeight = BoardHeight + ChoicesHeight + PowerUpHeight + Gap * 2f;
            HistoryHeight = Height - TopPadding - BottomPadding - HeaderHeight - GameAreaHeight - Gap * 2f;
            HistorySpacing = 4f * scale * compact;
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
            return Math.Max(1f, Math.Min(rowHeight - 6f, availableWidth / Math.Max(1, boardCount)));
        }

        private static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
    }
}
