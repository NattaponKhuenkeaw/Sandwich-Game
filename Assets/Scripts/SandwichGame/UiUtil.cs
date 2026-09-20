using UnityEngine;

namespace SandwichGame
{
    public static class UiUtil
    {
        static Sprite white;

        public static Sprite White()
        {
            if (white != null && white.texture != null) return white;

#if UNITY_EDITOR
            var editorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SandwichGame/Resources/UIWhite.png");
            if (editorSprite != null)
            {
                white = editorSprite;
                return white;
            }
#endif

            var sprites = Resources.LoadAll<Sprite>("UIWhite");
            if (sprites != null && sprites.Length > 0 && sprites[0] != null)
            {
                white = sprites[0];
                return white;
            }

            var tex = Resources.Load<Texture2D>("UIWhite");
            if (tex != null)
            {
                white = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 4f);
                white.name = "UIWhite";
                return white;
            }

            var generated = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            generated.name = "UIWhiteGenerated";
            generated.filterMode = FilterMode.Point;
            generated.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            generated.SetPixels(pixels);
            generated.Apply();
            white = Sprite.Create(generated, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
            white.name = "UIWhite";
            return white;
        }
    }
}
