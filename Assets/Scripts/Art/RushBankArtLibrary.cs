using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RushBank.Art
{
    public static class RushBankArtLibrary
    {
        public static readonly Color BankGreen = new Color(0.09f, 0.63f, 0.35f);
        public static readonly Color BankGreenDark = new Color(0.05f, 0.42f, 0.24f);
        public static readonly Color Gold = new Color(0.95f, 0.79f, 0.30f);
        public static readonly Color Cream = new Color(0.97f, 0.95f, 0.92f);
        public static readonly Color FloorLight = new Color(0.93f, 0.89f, 0.82f);
        public static readonly Color FloorDark = new Color(0.86f, 0.81f, 0.71f);
        public static readonly Color WallUpper = new Color(0.94f, 0.92f, 0.86f);
        public static readonly Color WallLower = new Color(0.83f, 0.79f, 0.70f);
        public static readonly Color CounterDark = new Color(0.18f, 0.23f, 0.24f);
        public static readonly Color WoodWarm = new Color(0.73f, 0.54f, 0.35f);
        public static readonly Color WoodDark = new Color(0.54f, 0.39f, 0.25f);
        public static readonly Color RopeRed = new Color(0.55f, 0.18f, 0.22f);
        public static readonly Color SlateDark = new Color(0.14f, 0.15f, 0.18f);
        public static readonly Color Navy = new Color(0.13f, 0.20f, 0.29f);
        public static readonly Color LeafGreen = new Color(0.24f, 0.55f, 0.28f);
        public static readonly Color LeafGreenLight = new Color(0.36f, 0.68f, 0.34f);
        public static readonly Color PotTerracotta = new Color(0.71f, 0.35f, 0.23f);
        public static readonly Color WarmLight = new Color(1f, 0.91f, 0.77f);
        public static readonly Color SkyGlass = new Color(0.75f, 0.89f, 1f);
        public static readonly Color EyeWhite = new Color(0.98f, 0.98f, 0.98f);
        public static readonly Color EyePupil = new Color(0.15f, 0.15f, 0.18f);
        public static readonly Color Blush = new Color(0.95f, 0.65f, 0.62f);
        public static readonly Color FeetDark = new Color(0.23f, 0.23f, 0.27f);

        private static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();
        private static Material floorMaterial;
        private static Material wallMaterial;
        private static Material rugMaterial;
        private static Font labelFont;

        public static Material Flat(Color color)
        {
            var key = "flat_" + ColorKey(color);
            if (MaterialCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var material = CreateBaseMaterial(color);
            MaterialCache[key] = material;
            return material;
        }

        public static Material Emissive(Color color, Color emission, float intensity)
        {
            var key = "emissive_" + ColorKey(color) + "_" + ColorKey(emission) + "_" + intensity.ToString("F2");
            if (MaterialCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var material = CreateBaseMaterial(color);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission * intensity);
            MaterialCache[key] = material;
            return material;
        }

        public static Material Glass(Color tint, float alpha)
        {
            var key = "glass_" + ColorKey(tint) + "_" + alpha.ToString("F2");
            if (MaterialCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var material = CreateBaseMaterial(new Color(tint.r, tint.g, tint.b, alpha));
            material.SetFloat("_Mode", 3f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetFloat("_Glossiness", 0.75f);
            material.renderQueue = (int)RenderQueue.Transparent;
            MaterialCache[key] = material;
            return material;
        }

        public static Material Floor()
        {
            if (floorMaterial == null)
            {
                floorMaterial = CreateBaseMaterial(Color.white);
                floorMaterial.mainTexture = BuildCheckerTexture(FloorLight, FloorDark);
                floorMaterial.mainTextureScale = new Vector2(6f, 5f);
            }

            return floorMaterial;
        }

        public static Material Wall()
        {
            if (wallMaterial == null)
            {
                wallMaterial = CreateBaseMaterial(Color.white);
                wallMaterial.mainTexture = BuildWainscotTexture(WallUpper, WallLower);
            }

            return wallMaterial;
        }

        public static Material Rug()
        {
            if (rugMaterial == null)
            {
                rugMaterial = CreateBaseMaterial(Color.white);
                rugMaterial.mainTexture = BuildRugTexture(BankGreenDark, Gold);
            }

            return rugMaterial;
        }

        public static GameObject Shape(
            PrimitiveType type,
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            return Shape(type, parent, name, localPosition, localScale, material, Vector3.zero);
        }

        public static GameObject Shape(
            PrimitiveType type,
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            Vector3 localEuler)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = localScale;
            StripCollider(go);

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            return go;
        }

        public static TextMesh Label(
            Transform parent,
            string text,
            Vector3 localPosition,
            float characterSize,
            Color color,
            Vector3 localEuler)
        {
            var go = new GameObject("Label " + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(localEuler);

            var textMesh = go.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = characterSize;
            textMesh.fontSize = 48;
            textMesh.color = color;

            var font = ResolveFont();
            if (font != null)
            {
                textMesh.font = font;
                var renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = font.material;
                }
            }

            return textMesh;
        }

        private static Font ResolveFont()
        {
            if (labelFont != null)
            {
                return labelFont;
            }

            try
            {
                labelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch (System.Exception)
            {
                labelFont = null;
            }

            if (labelFont == null)
            {
                try
                {
                    labelFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                catch (System.Exception)
                {
                    labelFont = null;
                }
            }

            return labelFont;
        }

        public static Color Darken(Color color, float amount)
        {
            return Color.Lerp(color, Color.black, amount);
        }

        public static Color Lighten(Color color, float amount)
        {
            return Color.Lerp(color, Color.white, amount);
        }

        private static Material CreateBaseMaterial(Color color)
        {
            var material = new Material(ResolveShader());
            material.color = color;
            material.SetFloat("_Glossiness", 0.12f);
            material.SetFloat("_Metallic", 0f);
            return material;
        }

        private static Shader ResolveShader()
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            return shader;
        }

        private static void StripCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider == null)
            {
                return;
            }

            collider.enabled = false;
            if (Application.isPlaying)
            {
                Object.Destroy(collider);
            }
            else
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static Texture2D BuildCheckerTexture(Color light, Color dark)
        {
            const int size = 128;
            const int cell = size / 2;
            var texture = NewTexture(size, "RushBank Floor Checker");

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var isLight = (x / cell + y / cell) % 2 == 0;
                    var color = isLight ? light : dark;
                    var onGrout = x % cell < 2 || y % cell < 2;
                    if (onGrout)
                    {
                        color = Color.Lerp(color, dark, 0.45f);
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D BuildWainscotTexture(Color upper, Color lower)
        {
            const int size = 64;
            var texture = NewTexture(size, "RushBank Wall Wainscot");
            var bandTop = (int)(size * 0.34f);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    Color color;
                    if (y < 3)
                    {
                        color = Darken(lower, 0.35f);
                    }
                    else if (y < bandTop)
                    {
                        color = lower;
                    }
                    else if (y < bandTop + 2)
                    {
                        color = Darken(lower, 0.25f);
                    }
                    else
                    {
                        color = upper;
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D BuildRugTexture(Color body, Color border)
        {
            const int size = 64;
            const int frame = 5;
            var texture = NewTexture(size, "RushBank Rug");

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var onFrame = x < frame || y < frame || x >= size - frame || y >= size - frame;
                    var onInnerLine = !onFrame
                        && (x == frame + 2 || y == frame + 2 || x == size - frame - 3 || y == size - frame - 3);
                    texture.SetPixel(x, y, onFrame || onInnerLine ? border : body);
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D NewTexture(int size, string name)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            return texture;
        }

        private static string ColorKey(Color color)
        {
            return ((Color32)color).r + "_" + ((Color32)color).g + "_" + ((Color32)color).b + "_" + ((Color32)color).a;
        }
    }
}
