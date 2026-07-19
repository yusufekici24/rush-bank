using RushBank.Art;
using UnityEngine;
using UnityEngine.Rendering;

namespace RushBank.Gameplay
{
    public class PrototypeBankEnvironmentBuilder : MonoBehaviour
    {
        private const string RootName = "Prototype Environment";

        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private Color floorColor = new Color(0.66f, 0.62f, 0.54f);
        [SerializeField] private Color wallColor = new Color(0.78f, 0.74f, 0.65f);
        [SerializeField] private Color counterColor = new Color(0.20f, 0.22f, 0.21f);
        [SerializeField] private Color accentColor = new Color(0.18f, 0.48f, 0.34f);

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
            var existing = transform.Find(RootName);
            if (existing != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(existing.gameObject);
                }
                else
                {
                    DestroyImmediate(existing.gameObject);
                }
            }

            var root = new GameObject(RootName).transform;
            root.SetParent(transform, false);

            BuildShell(root);
            BuildCounterArea(root);
            BuildQueueArea(root);
            BuildBackWallDressing(root);
            BuildDecor(root);
            BuildCeilingLights(root);
            TuneLightingAndCamera();
            EnsureSkinner();
        }

        private void BuildShell(Transform root)
        {
            CreateSolidCube(root, "Floor", new Vector3(0f, -0.05f, -4f), new Vector3(12f, 0.1f, 18f), RushBankArtLibrary.Floor());
            CreateSolidCube(root, "Back Wall", new Vector3(0f, 1.5f, 5f), new Vector3(12f, 3f, 0.2f), RushBankArtLibrary.Wall());
            CreateSolidCube(root, "Left Wall", new Vector3(-6f, 1.5f, -4f), new Vector3(0.2f, 3f, 18f), RushBankArtLibrary.Wall());
            CreateSolidCube(root, "Right Wall", new Vector3(6f, 1.5f, -4f), new Vector3(0.2f, 3f, 18f), RushBankArtLibrary.Wall());

            var baseboard = RushBankArtLibrary.Flat(RushBankArtLibrary.Darken(floorColor, 0.35f));
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Baseboard Back", new Vector3(0f, 0.09f, 4.87f), new Vector3(11.9f, 0.18f, 0.06f), baseboard);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Baseboard Left", new Vector3(-5.87f, 0.09f, -4f), new Vector3(0.06f, 0.18f, 17.85f), baseboard);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Baseboard Right", new Vector3(5.87f, 0.09f, -4f), new Vector3(0.06f, 0.18f, 17.85f), baseboard);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Front Floor Lip", new Vector3(0f, 0.025f, -12.96f), new Vector3(12f, 0.06f, 0.18f), baseboard);
        }

        private void BuildCounterArea(Transform root)
        {
            var counterRoot = new GameObject("Counter Area").transform;
            counterRoot.SetParent(root, false);
            counterRoot.localPosition = new Vector3(0f, 0f, -7.1f);
            root = counterRoot;

            CreateSolidCube(root, "Teller Counter", new Vector3(0f, 0.55f, -1.35f), new Vector3(4.8f, 1.1f, 0.8f), RushBankArtLibrary.Flat(counterColor));

            var counterTop = RushBankArtLibrary.Flat(RushBankArtLibrary.WoodDark);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Counter Top", new Vector3(0f, 1.13f, -1.35f), new Vector3(5.05f, 0.07f, 0.95f), counterTop);

            var accent = RushBankArtLibrary.Flat(accentColor);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Counter Front Stripe", new Vector3(0f, 0.72f, -0.94f), new Vector3(4.8f, 0.1f, 0.04f), accent);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Counter Accent", new Vector3(0f, 1.18f, -1.72f), new Vector3(4.9f, 0.05f, 0.08f), accent);

            var glass = RushBankArtLibrary.Glass(RushBankArtLibrary.SkyGlass, 0.28f);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Counter Glass L", new Vector3(-1.35f, 1.52f, -1.35f), new Vector3(1.5f, 0.7f, 0.03f), glass);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Counter Glass R", new Vector3(1.35f, 1.52f, -1.35f), new Vector3(1.5f, 0.7f, 0.03f), glass);

            var brass = RushBankArtLibrary.Flat(RushBankArtLibrary.Gold);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, root, "Glass Post A", new Vector3(-2.1f, 1.5f, -1.35f), new Vector3(0.05f, 0.36f, 0.05f), brass);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, root, "Glass Post B", new Vector3(-0.6f, 1.5f, -1.35f), new Vector3(0.05f, 0.36f, 0.05f), brass);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, root, "Glass Post C", new Vector3(0.6f, 1.5f, -1.35f), new Vector3(0.05f, 0.36f, 0.05f), brass);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, root, "Glass Post D", new Vector3(2.1f, 1.5f, -1.35f), new Vector3(0.05f, 0.36f, 0.05f), brass);

            var dark = RushBankArtLibrary.Flat(RushBankArtLibrary.SlateDark);
            var screen = RushBankArtLibrary.Emissive(new Color(0.2f, 0.45f, 0.5f), new Color(0.3f, 0.8f, 0.85f), 0.7f);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Teller Monitor Stand L", new Vector3(-0.9f, 1.22f, -1.55f), new Vector3(0.07f, 0.12f, 0.07f), dark);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Teller Monitor L", new Vector3(-0.9f, 1.38f, -1.55f), new Vector3(0.4f, 0.26f, 0.03f), dark, new Vector3(0f, 180f, 0f));
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Teller Screen L", new Vector3(-0.9f, 1.38f, -1.57f), new Vector3(0.34f, 0.2f, 0.035f), screen, new Vector3(0f, 180f, 0f));
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Teller Monitor Stand R", new Vector3(0.9f, 1.22f, -1.55f), new Vector3(0.07f, 0.12f, 0.07f), dark);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Teller Monitor R", new Vector3(0.9f, 1.38f, -1.55f), new Vector3(0.4f, 0.26f, 0.03f), dark, new Vector3(0f, 180f, 0f));
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Teller Screen R", new Vector3(0.9f, 1.38f, -1.57f), new Vector3(0.34f, 0.2f, 0.035f), screen, new Vector3(0f, 180f, 0f));

            var cream = RushBankArtLibrary.Flat(RushBankArtLibrary.Cream);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, root, "Pen Cup", new Vector3(2f, 1.24f, -1.5f), new Vector3(0.09f, 0.07f, 0.09f), dark);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Counter Papers", new Vector3(-2f, 1.19f, -1.5f), new Vector3(0.32f, 0.04f, 0.24f), cream, new Vector3(0f, 10f, 0f));

            var plate = RushBankArtLibrary.Flat(RushBankArtLibrary.Navy);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Teller Sign Plate", new Vector3(0f, 1.95f, -1.35f), new Vector3(1.1f, 0.32f, 0.05f), plate);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, root, "Teller Sign Rod", new Vector3(0f, 2.5f, -1.35f), new Vector3(0.03f, 0.42f, 0.03f), brass);
            RushBankArtLibrary.Label(root, "GISE 01", new Vector3(0f, 1.95f, -1.32f), 0.12f, Color.white, new Vector3(0f, 180f, 0f));
        }

        private void BuildQueueArea(Transform root)
        {
            CreateQueueRail(root, "Queue Rail Left", -2.2f);
            CreateQueueRail(root, "Queue Rail Right", 2.2f);

            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Queue Rug", new Vector3(0f, 0.012f, 1.3f), new Vector3(3.6f, 0.02f, 4.2f), RushBankArtLibrary.Rug());
        }

        private void CreateQueueRail(Transform root, string railName, float x)
        {
            var rail = CreateSolidCube(root, railName, new Vector3(x, 0.35f, 1.4f), new Vector3(0.12f, 0.7f, 3.2f), null);
            var railRenderer = rail.GetComponent<MeshRenderer>();
            if (railRenderer != null)
            {
                railRenderer.enabled = false;
            }

            var pole = RushBankArtLibrary.Flat(RushBankArtLibrary.Navy);
            var brass = RushBankArtLibrary.Flat(RushBankArtLibrary.Gold);
            var rope = RushBankArtLibrary.Flat(RushBankArtLibrary.RopeRed);

            var postPositionsZ = new[] { -0.15f, 0.9f, 1.95f, 3f };
            for (var i = 0; i < postPositionsZ.Length; i++)
            {
                var z = postPositionsZ[i];
                RushBankArtLibrary.Shape(PrimitiveType.Cylinder, root, railName + " Base " + i, new Vector3(x, 0.02f, z), new Vector3(0.16f, 0.02f, 0.16f), brass);
                RushBankArtLibrary.Shape(PrimitiveType.Cylinder, root, railName + " Post " + i, new Vector3(x, 0.4f, z), new Vector3(0.06f, 0.4f, 0.06f), pole);
                RushBankArtLibrary.Shape(PrimitiveType.Sphere, root, railName + " Post Top " + i, new Vector3(x, 0.85f, z), new Vector3(0.13f, 0.13f, 0.13f), brass);

                if (i < postPositionsZ.Length - 1)
                {
                    var midZ = (z + postPositionsZ[i + 1]) * 0.5f;
                    RushBankArtLibrary.Shape(PrimitiveType.Cylinder, root, railName + " Rope " + i, new Vector3(x, 0.66f, midZ), new Vector3(0.045f, 0.48f, 0.045f), rope, new Vector3(90f, 0f, 0f));
                }
            }
        }

        private void BuildBackWallDressing(Transform root)
        {
            var brass = RushBankArtLibrary.Flat(RushBankArtLibrary.Gold);
            var navy = RushBankArtLibrary.Flat(RushBankArtLibrary.Navy);
            var wood = RushBankArtLibrary.Flat(RushBankArtLibrary.WoodDark);
            var windowGlow = RushBankArtLibrary.Emissive(RushBankArtLibrary.SkyGlass, RushBankArtLibrary.SkyGlass, 0.85f);

            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Brand Sign Board", new Vector3(0f, 2.25f, 4.93f), new Vector3(3.6f, 0.75f, 0.08f), navy);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Brand Sign Trim Top", new Vector3(0f, 2.66f, 4.92f), new Vector3(3.7f, 0.06f, 0.1f), brass);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Brand Sign Trim Bottom", new Vector3(0f, 1.84f, 4.92f), new Vector3(3.7f, 0.06f, 0.1f), brass);
            RushBankArtLibrary.Label(root, "RUSHBANK", new Vector3(0f, 2.25f, 4.82f), 0.4f, RushBankArtLibrary.Gold, new Vector3(0f, 180f, 0f));

            CreateWindow(root, new Vector3(4f, 1.9f, 4.9f), wood, windowGlow);
            CreateWindow(root, new Vector3(-4.6f, 1.9f, 4.9f), wood, windowGlow);
            CreateEntranceDoor(root, wood, brass);
            CreateWallClock(root);

            CreatePoster(root, "Poster Kredi", new Vector3(-5.88f, 1.9f, -2f), new Vector3(0f, 90f, 0f), RushBankArtLibrary.Lighten(accentColor, 0.7f), "KREDI\n%1.99");
            CreatePoster(root, "Poster Kart", new Vector3(5.88f, 1.9f, -2f), new Vector3(0f, -90f, 0f), RushBankArtLibrary.Lighten(RushBankArtLibrary.Gold, 0.55f), "RUSH\nKART");
        }

        private static void CreateWindow(Transform root, Vector3 position, Material frame, Material glow)
        {
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Window Frame", position, new Vector3(1.5f, 1.3f, 0.1f), frame);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Window Glass", position + new Vector3(0f, 0f, -0.04f), new Vector3(1.3f, 1.1f, 0.06f), glow);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Window Bar V", position + new Vector3(0f, 0f, -0.06f), new Vector3(0.05f, 1.1f, 0.04f), frame);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Window Bar H", position + new Vector3(0f, 0f, -0.06f), new Vector3(1.3f, 0.05f, 0.04f), frame);
        }

        private void CreateEntranceDoor(Transform root, Material frame, Material brass)
        {
            var panel = RushBankArtLibrary.Flat(RushBankArtLibrary.BankGreenDark);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Door Frame", new Vector3(-2.6f, 1.1f, 4.9f), new Vector3(1.4f, 2.2f, 0.12f), frame);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Door Panel", new Vector3(-2.6f, 1.05f, 4.86f), new Vector3(1.15f, 2f, 0.08f), panel);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Door Glass", new Vector3(-2.6f, 1.45f, 4.82f), new Vector3(0.8f, 0.7f, 0.05f), RushBankArtLibrary.Glass(RushBankArtLibrary.SkyGlass, 0.4f));
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, root, "Door Handle", new Vector3(-2.18f, 1f, 4.8f), new Vector3(0.08f, 0.08f, 0.08f), brass);
            RushBankArtLibrary.Label(root, "GIRIS", new Vector3(-2.6f, 2.35f, 4.83f), 0.12f, RushBankArtLibrary.Cream, new Vector3(0f, 180f, 0f));
        }

        private static void CreateWallClock(Transform root)
        {
            var face = RushBankArtLibrary.Flat(RushBankArtLibrary.Cream);
            var rim = RushBankArtLibrary.Flat(RushBankArtLibrary.SlateDark);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, root, "Clock Rim", new Vector3(2.6f, 2.55f, 4.9f), new Vector3(0.52f, 0.02f, 0.52f), rim, new Vector3(90f, 0f, 0f));
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, root, "Clock Face", new Vector3(2.6f, 2.55f, 4.88f), new Vector3(0.46f, 0.02f, 0.46f), face, new Vector3(90f, 0f, 0f));
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Clock Hand Minute", new Vector3(2.6f, 2.62f, 4.86f), new Vector3(0.025f, 0.16f, 0.01f), rim);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Clock Hand Hour", new Vector3(2.66f, 2.58f, 4.86f), new Vector3(0.025f, 0.11f, 0.01f), rim, new Vector3(0f, 0f, -55f));
        }

        private static void CreatePoster(Transform root, string posterName, Vector3 position, Vector3 euler, Color paperColor, string text)
        {
            var rotation = Quaternion.Euler(euler);
            var frame = RushBankArtLibrary.Flat(RushBankArtLibrary.WoodDark);
            var paper = RushBankArtLibrary.Flat(paperColor);

            var holder = new GameObject(posterName);
            holder.transform.SetParent(root, false);
            holder.transform.localPosition = position;
            holder.transform.localRotation = rotation;

            RushBankArtLibrary.Shape(PrimitiveType.Cube, holder.transform, "Poster Frame", Vector3.zero, new Vector3(0.95f, 1.25f, 0.05f), frame);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, holder.transform, "Poster Paper", new Vector3(0f, 0f, -0.04f), new Vector3(0.82f, 1.12f, 0.03f), paper);
            RushBankArtLibrary.Label(holder.transform, text, new Vector3(0f, 0f, -0.07f), 0.14f, RushBankArtLibrary.Navy, new Vector3(0f, 180f, 0f));
        }

        private void BuildDecor(Transform root)
        {
            CreatePlant(root, new Vector3(5.5f, 0f, 4.5f));
            CreatePlant(root, new Vector3(5.5f, 0f, -7.4f));
            CreatePlant(root, new Vector3(-5.5f, 0f, -7.4f));

            CreateWaitingLounge(root);
            CreateSideServiceDesks(root);
            CreateAtm(root);
        }

        private static void CreateWaitingLounge(Transform root)
        {
            CreateBench(root, new Vector3(-0.7f, 0f, 0.65f), Quaternion.Euler(0f, 90f, 0f));
            CreateBench(root, new Vector3(0.7f, 0f, 0.65f), Quaternion.Euler(0f, 90f, 0f));
            CreateBench(root, new Vector3(-0.7f, 0f, 2.45f), Quaternion.Euler(0f, 90f, 0f));
            CreateBench(root, new Vector3(0.7f, 0f, 2.45f), Quaternion.Euler(0f, 90f, 0f));

            var table = RushBankArtLibrary.Flat(RushBankArtLibrary.WoodWarm);
            var leg = RushBankArtLibrary.Flat(RushBankArtLibrary.SlateDark);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, root, "Waiting Coffee Table", new Vector3(0f, 0.34f, 1.55f), new Vector3(0.65f, 0.08f, 0.95f), table);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, root, "Waiting Table Leg A", new Vector3(-0.24f, 0.17f, 1.18f), new Vector3(0.035f, 0.17f, 0.035f), leg);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, root, "Waiting Table Leg B", new Vector3(0.24f, 0.17f, 1.18f), new Vector3(0.035f, 0.17f, 0.035f), leg);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, root, "Waiting Table Leg C", new Vector3(-0.24f, 0.17f, 1.92f), new Vector3(0.035f, 0.17f, 0.035f), leg);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, root, "Waiting Table Leg D", new Vector3(0.24f, 0.17f, 1.92f), new Vector3(0.035f, 0.17f, 0.035f), leg);
        }

        private void CreateSideServiceDesks(Transform root)
        {
            CreateSideDesk(root, "Left Service Desk A", new Vector3(-5.05f, 0f, 2.35f), Quaternion.Euler(0f, 90f, 0f), "ILISKI");
            CreateSideDesk(root, "Left Service Desk B", new Vector3(-5.05f, 0f, -1.35f), Quaternion.Euler(0f, 90f, 0f), "KREDI");
            CreateSideDesk(root, "Right Service Desk A", new Vector3(5.05f, 0f, 2.35f), Quaternion.Euler(0f, -90f, 0f), "SIGORTA");
            CreateSideDesk(root, "Right Service Desk B", new Vector3(5.05f, 0f, -1.35f), Quaternion.Euler(0f, -90f, 0f), "MUDUR");
        }

        private void CreateSideDesk(Transform root, string name, Vector3 position, Quaternion rotation, string label)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(root, false);
            holder.transform.localPosition = position;
            holder.transform.localRotation = rotation;

            var wood = RushBankArtLibrary.Flat(RushBankArtLibrary.WoodDark);
            var accent = RushBankArtLibrary.Flat(RushBankArtLibrary.BankGreenDark);
            var screen = RushBankArtLibrary.Emissive(new Color(0.17f, 0.36f, 0.38f), new Color(0.28f, 0.75f, 0.78f), 0.45f);
            var chair = RushBankArtLibrary.Flat(RushBankArtLibrary.Navy);

            RushBankArtLibrary.Shape(PrimitiveType.Cube, holder.transform, "Desk", new Vector3(0f, 0.45f, 0f), new Vector3(1.45f, 0.9f, 0.78f), wood);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, holder.transform, "Desk Stripe", new Vector3(0f, 0.75f, -0.41f), new Vector3(1.35f, 0.08f, 0.04f), accent);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, holder.transform, "Monitor", new Vector3(0f, 1.08f, -0.1f), new Vector3(0.46f, 0.28f, 0.04f), screen);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, holder.transform, "Chair Back", new Vector3(0f, 0.78f, 0.82f), new Vector3(0.7f, 0.7f, 0.12f), chair);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, holder.transform, "Chair Seat", new Vector3(0f, 0.38f, 0.58f), new Vector3(0.7f, 0.12f, 0.55f), chair);
            RushBankArtLibrary.Label(holder.transform, label, new Vector3(0f, 1.05f, -0.44f), 0.08f, RushBankArtLibrary.Cream, new Vector3(65f, 0f, 0f));
        }

        private static void CreatePlant(Transform root, Vector3 position)
        {
            var holder = new GameObject("Plant");
            holder.transform.SetParent(root, false);
            holder.transform.localPosition = position;

            var pot = RushBankArtLibrary.Flat(RushBankArtLibrary.PotTerracotta);
            var potRim = RushBankArtLibrary.Flat(RushBankArtLibrary.Darken(RushBankArtLibrary.PotTerracotta, 0.25f));
            var trunk = RushBankArtLibrary.Flat(RushBankArtLibrary.WoodDark);
            var leafA = RushBankArtLibrary.Flat(RushBankArtLibrary.LeafGreen);
            var leafB = RushBankArtLibrary.Flat(RushBankArtLibrary.LeafGreenLight);

            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, holder.transform, "Pot", new Vector3(0f, 0.22f, 0f), new Vector3(0.3f, 0.22f, 0.3f), pot);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, holder.transform, "Pot Rim", new Vector3(0f, 0.44f, 0f), new Vector3(0.33f, 0.03f, 0.33f), potRim);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, holder.transform, "Trunk", new Vector3(0f, 0.6f, 0f), new Vector3(0.05f, 0.18f, 0.05f), trunk);
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, holder.transform, "Foliage A", new Vector3(0f, 1f, 0f), new Vector3(0.6f, 0.55f, 0.6f), leafA);
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, holder.transform, "Foliage B", new Vector3(0.18f, 1.2f, 0.06f), new Vector3(0.4f, 0.38f, 0.4f), leafB);
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, holder.transform, "Foliage C", new Vector3(-0.16f, 1.16f, -0.08f), new Vector3(0.42f, 0.4f, 0.42f), leafA);
        }

        private static void CreateBench(Transform root, Vector3 position)
        {
            CreateBench(root, position, Quaternion.identity);
        }

        private static void CreateBench(Transform root, Vector3 position, Quaternion rotation)
        {
            var holder = new GameObject("Waiting Bench");
            holder.transform.SetParent(root, false);
            holder.transform.localPosition = position;
            holder.transform.localRotation = rotation;

            var seat = RushBankArtLibrary.Flat(RushBankArtLibrary.WoodWarm);
            var leg = RushBankArtLibrary.Flat(RushBankArtLibrary.SlateDark);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, holder.transform, "Seat", new Vector3(0f, 0.42f, 0f), new Vector3(1.6f, 0.09f, 0.5f), seat);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, holder.transform, "Leg L", new Vector3(-0.65f, 0.2f, 0f), new Vector3(0.08f, 0.4f, 0.42f), leg);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, holder.transform, "Leg R", new Vector3(0.65f, 0.2f, 0f), new Vector3(0.08f, 0.4f, 0.42f), leg);
        }

        private void CreateAtm(Transform root)
        {
            var holder = new GameObject("ATM");
            holder.transform.SetParent(root, false);
            holder.transform.localPosition = new Vector3(-5.62f, 0f, -1.2f);
            holder.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            var body = RushBankArtLibrary.Flat(RushBankArtLibrary.SlateDark);
            var screen = RushBankArtLibrary.Emissive(new Color(0.16f, 0.5f, 0.42f), new Color(0.25f, 0.9f, 0.7f), 0.8f);
            var keypad = RushBankArtLibrary.Flat(RushBankArtLibrary.Cream);
            var accent = RushBankArtLibrary.Flat(accentColor);

            RushBankArtLibrary.Shape(PrimitiveType.Cube, holder.transform, "ATM Body", new Vector3(0f, 0.85f, 0f), new Vector3(0.7f, 1.7f, 0.5f), body);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, holder.transform, "ATM Screen", new Vector3(0f, 1.18f, 0.26f), new Vector3(0.42f, 0.32f, 0.03f), screen);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, holder.transform, "ATM Keypad", new Vector3(0f, 0.88f, 0.26f), new Vector3(0.36f, 0.2f, 0.03f), keypad);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, holder.transform, "ATM Slot", new Vector3(0f, 0.62f, 0.26f), new Vector3(0.3f, 0.04f, 0.03f), body);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, holder.transform, "ATM Cap", new Vector3(0f, 1.76f, 0f), new Vector3(0.74f, 0.12f, 0.54f), accent);
            RushBankArtLibrary.Label(holder.transform, "ATM", new Vector3(0f, 1.55f, 0.27f), 0.1f, RushBankArtLibrary.Cream, Vector3.zero);
        }

        private static void BuildCeilingLights(Transform root)
        {
            CreateCeilingLight(root, new Vector3(-2.2f, 0f, 0.6f));
            CreateCeilingLight(root, new Vector3(2.2f, 0f, 0.6f));
            CreateCeilingLight(root, new Vector3(0f, 0f, -2.6f));
        }

        private static void CreateCeilingLight(Transform root, Vector3 floorPosition)
        {
            var holder = new GameObject("Ceiling Light");
            holder.transform.SetParent(root, false);
            holder.transform.localPosition = floorPosition;

            var cord = RushBankArtLibrary.Flat(RushBankArtLibrary.SlateDark);
            var shade = RushBankArtLibrary.Flat(RushBankArtLibrary.BankGreenDark);
            var bulb = RushBankArtLibrary.Emissive(RushBankArtLibrary.WarmLight, RushBankArtLibrary.WarmLight, 1.6f);

            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, holder.transform, "Cord", new Vector3(0f, 2.82f, 0f), new Vector3(0.02f, 0.2f, 0.02f), cord);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, holder.transform, "Shade", new Vector3(0f, 2.58f, 0f), new Vector3(0.5f, 0.1f, 0.5f), shade);
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, holder.transform, "Bulb", new Vector3(0f, 2.47f, 0f), new Vector3(0.26f, 0.14f, 0.26f), bulb);

            var lightObject = new GameObject("Point Light");
            lightObject.transform.SetParent(holder.transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 2.35f, 0f);
            var pointLight = lightObject.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = RushBankArtLibrary.WarmLight;
            pointLight.intensity = 0.75f;
            pointLight.range = 6f;
            pointLight.shadows = LightShadows.None;
        }

        private void TuneLightingAndCamera()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.76f, 0.72f, 0.64f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.52f, 0.46f);
            RenderSettings.ambientGroundColor = new Color(0.28f, 0.27f, 0.25f);
            RenderSettings.fog = false;

            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (var i = 0; i < lights.Length; i++)
            {
                if (lights[i].type != LightType.Directional)
                {
                    continue;
                }

                lights[i].color = new Color(0.90f, 0.82f, 0.68f);
                lights[i].intensity = 0.72f;
                lights[i].transform.rotation = Quaternion.Euler(52f, -28f, 0f);
                lights[i].shadows = LightShadows.Soft;
                lights[i].shadowStrength = 0.55f;
            }

            var camera = Camera.main;
            if (camera != null && camera.clearFlags == CameraClearFlags.SolidColor)
            {
                camera.backgroundColor = new Color(0.12f, 0.14f, 0.14f);
            }
        }

        private void EnsureSkinner()
        {
            if (FindFirstObjectByType<ChubbyVisualSkinner>() == null)
            {
                gameObject.AddComponent<ChubbyVisualSkinner>();
            }
        }

        private static GameObject CreateSolidCube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            return go;
        }
    }
}
