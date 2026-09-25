namespace SandwichGame
{
    public enum ItemKind
    {
        RawMeat,
        CookedMeat,
        BurnedMeat,
        Bun,
        Sandwich
    }

    public enum StationKind
    {
        MeatPile,
        BunPile,
        Grill,
        Plate,
        Trash,
        Customer
    }

    public enum GamePhase
    {
        Title,
        Playing,
        RoundEnd,
        GameOver
    }

    public static class GameConstants
    {
        public const int StartingLives = 3;
        public const float RoundDuration = 90f;
        public const float CustomerWait = 28f;
        public const int StartingTarget = 200;
        public const int ScorePerOrder = 100;
        public const int TargetIncrease = 500;
        public const float GrillCookSeconds = 10f;
        public const float GrillBurnSeconds = 10f;
        public const int CustomerCount = 3;
        public const int GrillSlotCount = 3;
        public const int MaxHamburgersPerOrder = 2;
    }
}
