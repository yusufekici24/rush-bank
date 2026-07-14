using UnityEngine;

namespace RushBank.Gameplay
{
    public class PrototypeBankEnvironmentBuilder : MonoBehaviour
    {
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private Color floorColor = new Color(0.78f, 0.75f, 0.68f);
        [SerializeField] private Color wallColor = new Color(0.92f, 0.9f, 0.84f);
        [SerializeField] private Color counterColor = new Color(0.18f, 0.23f, 0.24f);
        [SerializeField] private Color accentColor = new Color(0.09f, 0.63f, 0.35f);

        private void Start()
        {
            if (buildOnStart)
            {
                Build();
            }
        }

        [ContextMenu("Build Prototype Environment")]
        public void Build()
        {
            if (transform.Find("Prototype Environment") != null)
            {
                return;
            }

            var root = new GameObject("Prototype Environment");
            root.transform.SetParent(transform, false);

            CreateCube(root.transform, "Floor", new Vector3(0f, -0.05f, 0f), new Vector3(12f, 0.1f, 10f), floorColor);
            CreateCube(root.transform, "Back Wall", new Vector3(0f, 1.5f, 5f), new Vector3(12f, 3f, 0.2f), wallColor);
            CreateCube(root.transform, "Left Wall", new Vector3(-6f, 1.5f, 0f), new Vector3(0.2f, 3f, 10f), wallColor);
            CreateCube(root.transform, "Right Wall", new Vector3(6f, 1.5f, 0f), new Vector3(0.2f, 3f, 10f), wallColor);

            CreateCube(root.transform, "Teller Counter", new Vector3(0f, 0.55f, -1.35f), new Vector3(4.8f, 1.1f, 0.8f), counterColor);
            CreateCube(root.transform, "Counter Accent", new Vector3(0f, 1.14f, -1.8f), new Vector3(4.8f, 0.12f, 0.12f), accentColor);
            CreateCube(root.transform, "Queue Rail Left", new Vector3(-2.2f, 0.35f, 1.4f), new Vector3(0.12f, 0.7f, 3.2f), accentColor);
            CreateCube(root.transform, "Queue Rail Right", new Vector3(2.2f, 0.35f, 1.4f), new Vector3(0.12f, 0.7f, 3.2f), accentColor);

            CreateSign(root.transform, "RUSHBANK", new Vector3(0f, 2.25f, 4.82f), 0.45f, accentColor);
            CreateSign(root.transform, "GISE 01", new Vector3(0f, 1.45f, -1.85f), 0.28f, Color.white);
        }

        private static void CreateCube(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateMaterial(color);
            }
        }

        private static void CreateSign(Transform parent, string text, Vector3 position, float size, Color color)
        {
            var go = new GameObject(text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var textMesh = go.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = size;
            textMesh.color = color;
        }

        private static Material CreateMaterial(Color color)
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

            var material = new Material(shader);
            material.color = color;
            return material;
        }
    }
}
