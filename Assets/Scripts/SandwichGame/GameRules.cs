using UnityEngine;

namespace SandwichGame
{
    public static class GameRules
    {
        public static int RandomOrderSize()
        {
            return Random.Range(1, GameConstants.MaxHamburgersPerOrder + 1);
        }

        public static bool MeetsTarget(int score, int target)
        {
            return score >= target;
        }

        public static int NextTarget(int currentTarget)
        {
            return currentTarget + GameConstants.TargetIncrease;
        }

        public static bool IsMeat(ItemKind kind)
        {
            return kind == ItemKind.RawMeat || kind == ItemKind.CookedMeat || kind == ItemKind.BurnedMeat;
        }

        public static bool CanPlaceOnGrill(ItemKind kind)
        {
            return kind == ItemKind.RawMeat || kind == ItemKind.CookedMeat;
        }

        public static bool CanServe(ItemKind kind)
        {
            return kind == ItemKind.Sandwich;
        }

        public static bool CanCombineOnPlate(ItemKind onPlate, ItemKind incoming)
        {
            return (onPlate == ItemKind.Bun && incoming == ItemKind.CookedMeat)
                   || (onPlate == ItemKind.CookedMeat && incoming == ItemKind.Bun);
        }

        public static Color ColorFor(ItemKind kind)
        {
            switch (kind)
            {
                case ItemKind.RawMeat: return new Color(0.91f, 0.42f, 0.48f);
                case ItemKind.CookedMeat: return new Color(0.42f, 0.22f, 0.10f);
                case ItemKind.BurnedMeat: return new Color(0.08f, 0.07f, 0.06f);
                case ItemKind.Bun: return new Color(0.90f, 0.72f, 0.42f);
                case ItemKind.Sandwich: return new Color(0.86f, 0.62f, 0.32f);
                default: return Color.white;
            }
        }

        public static string ThaiName(ItemKind kind)
        {
            switch (kind)
            {
                case ItemKind.RawMeat: return "เนื้อดิบ";
                case ItemKind.CookedMeat: return "เนื้อสุก";
                case ItemKind.BurnedMeat: return "เนื้อไหม้";
                case ItemKind.Bun: return "ขนมปัง";
                case ItemKind.Sandwich: return "แฮมเบอร์เกอร์";
                default: return kind.ToString();
            }
        }
    }
}
