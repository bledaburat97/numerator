using System;
using Scripts;

public static class FruitLabLayoutChecks
{
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    public static void Main()
    {
        float[,] screens = {
            { 375, 500 }, { 375, 580 }, { 375, 620 }, { 375, 647 }, { 375, 667 },
            { 375, 724 }, { 375, 780 }, { 375, 812 }, { 375, 854 }, { 375, 928 },
            { 320, 568 }, { 390, 720 }, { 430, 932 }, { 768, 1024 }
        };
        int configurations = 0;
        float smallestCard = float.MaxValue;
        for (int screen = 0; screen < screens.GetLength(0); screen++)
        {
            var layout = new FruitLabLayoutMetrics(screens[screen, 0], screens[screen, 1]);
            float width = layout.Width - layout.Padding * 2f;
            // The real ScrollRect is inset by six units on each edge.
            float viewportHeight = layout.HistoryHeight - 12f;
            float viewportWidth = width - 12f;
            float padding = (float)Math.Ceiling(layout.HistorySpacing);
            float rowHeight = FruitLabLayoutMetrics.GetHistoryRowHeight(viewportHeight, padding, viewportWidth / 331f);
            float sixRows = rowHeight * 6f + layout.HistorySpacing * 5f + padding * 2f;
            Require(sixRows <= viewportHeight + 0.01f, "Six complete guesses must fit: " + screen);
            Require(viewportHeight > 0f && rowHeight >= 24f, "History remains usable: " + screen);
            float sectionTotal = layout.TopPadding + layout.BottomPadding + layout.HeaderHeight + layout.GameAreaHeight + layout.Gap * 2f + layout.HistoryHeight;
            Require(Math.Abs(sectionTotal - layout.Height) < 0.01f, "Sections must stay in the safe area");

            // Actual prefab dimensions are 70x70 (board) and 54x54 (fruit).
            float ratio = Math.Min(width / (5f * 70f + 4f * 3f), layout.BoardHeight / 70f);
            float trayRatio = Math.Min(ratio, Math.Min(layout.ChoicesWidth / (5f * 54f + 4f * 2f), layout.ChoicesHeight / (2f * 54f)));
            float boardSize = ratio * 70f, fruitSize = trayRatio * 54f;
            Require(layout.ChoicesHeight > layout.ActionWidth, "Side buttons must be taller than they are wide");
            Require(Math.Abs(layout.ChoicesWidth + 2f * (layout.ActionWidth + layout.Gap) - width) < .01f,
                "Fruit tray and side buttons share one row without overlap");
            for (int board = 2; board <= 5; board++)
            for (int choices = 5; choices <= 9; choices++)
            {
                Require(board * boardSize + (board - 1) * ratio * 3f <= width + 0.01f,
                    "Board slots must not overflow");
                int first = FruitLabLayoutMetrics.GetFirstChoiceRowCount(choices);
                int second = choices - first;
                Require(first <= 5 && second <= 5 && first + second == choices, "Every fruit has a slot");
                Require(first - second >= 0 && first - second <= 1, "Choice rows remain balanced");
                Require(first * fruitSize + (first - 1) * trayRatio * 2f <= layout.ChoicesWidth + 0.01f,
                    "First fruit row must not overflow");
                Require(fruitSize <= layout.ChoicesHeight * 0.5f, "Both choice rows fit vertically");
                Require(fruitSize <= boardSize, "Dragged fruit fits the target slot");
                float card = FruitLabLayoutMetrics.GetHistoryCardSize(viewportWidth, rowHeight, board);
                smallestCard = Math.Min(smallestCard, card);
                Require(card > 20f, "All history fruits remain distinguishable");
                Require(board * card + (board - 1) * FruitLabLayoutMetrics.HistoryCardGap + FruitLabLayoutMetrics.HistoryFeedbackReservation <= viewportWidth + 0.01f,
                    "History fruits leave room for both feedback counters");
                configurations++;
            }
        }
        Require(FruitLabLayoutMetrics.GetFirstChoiceRowCount(4) == 2, "Existing four-fruit tutorial levels stay supported");
        Console.WriteLine("PASS: " + configurations + " screen/board/fruit configurations; six visible guesses, bounds and feedback clearance.");
        Console.WriteLine("Smallest history fruit in the matrix: " + smallestCard.ToString("F1") + " canvas units.");
    }
}
