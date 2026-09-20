using UnityEngine;

namespace SandwichGame
{
    public static class FontUtil
    {
        static Font cached;

        public static Font Thai()
        {
            if (cached != null) return cached;

            var names = new[]
            {
                "Leelawadee UI",
                "Leelawadee",
                "Tahoma",
                "Segoe UI",
                "Cordia New",
                "Angsana New",
                "Arial Unicode MS",
                "Arial"
            };

            cached = Font.CreateDynamicFontFromOSFont(names, 32);
            if (cached != null) return cached;

            cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return cached;
        }
    }
}
