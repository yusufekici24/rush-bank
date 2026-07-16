using UnityEngine;

namespace RushBank.Art
{
    public enum ChubbyCharacterStyle
    {
        Customer,
        Teller,
        Guard,
        Police,
        Thief,
        TeaLady,
        Coworker,
        Assistant,
        Vip,
        Manager,
        Cat
    }

    public static class ChubbyCharacterDresser
    {
        private static readonly Color[] HairColors =
        {
            new Color(0.16f, 0.12f, 0.10f),
            new Color(0.35f, 0.23f, 0.13f),
            new Color(0.55f, 0.35f, 0.17f),
            new Color(0.82f, 0.66f, 0.32f),
            new Color(0.42f, 0.20f, 0.14f),
            new Color(0.62f, 0.62f, 0.64f)
        };

        public static void Dress(GameObject characterRoot, ChubbyCharacterStyle style)
        {
            if (characterRoot == null || characterRoot.GetComponent<ChubbyVisualTag>() != null)
            {
                return;
            }

            characterRoot.AddComponent<ChubbyVisualTag>();

            var visualObject = new GameObject("Chubby Visual");
            visualObject.transform.SetParent(characterRoot.transform, false);
            visualObject.AddComponent<ChubbyWobble>();
            var visual = visualObject.transform;

            var outfit = ReadOutfitColor(characterRoot);
            var seed = Mathf.Abs(characterRoot.GetInstanceID());

            if (style == ChubbyCharacterStyle.Cat)
            {
                BuildCatFace(visual, outfit);
                return;
            }

            var eyeDepth = style == ChubbyCharacterStyle.Thief ? 0.47f : 0.42f;
            BuildFace(visual, eyeDepth, style != ChubbyCharacterStyle.Thief);
            BuildArms(visual, outfit);
            BuildFeet(visual);

            switch (style)
            {
                case ChubbyCharacterStyle.Customer:
                    BuildCustomerHair(visual, seed);
                    break;
                case ChubbyCharacterStyle.Teller:
                    BuildTellerOutfit(visual);
                    break;
                case ChubbyCharacterStyle.Guard:
                    BuildCapAndBadge(visual, RushBankArtLibrary.Navy, RushBankArtLibrary.Gold);
                    BuildBelt(visual);
                    break;
                case ChubbyCharacterStyle.Police:
                    BuildCapAndBadge(visual, new Color(0.10f, 0.16f, 0.34f), Color.white);
                    BuildBelt(visual);
                    break;
                case ChubbyCharacterStyle.Thief:
                    BuildThiefLook(visual);
                    break;
                case ChubbyCharacterStyle.TeaLady:
                    BuildTeaLadyLook(visual);
                    break;
                case ChubbyCharacterStyle.Coworker:
                    BuildGlasses(visual);
                    BuildTie(visual, new Color(0.72f, 0.22f, 0.20f));
                    BuildCustomerHair(visual, seed);
                    break;
                case ChubbyCharacterStyle.Assistant:
                    BuildHeadphones(visual);
                    break;
                case ChubbyCharacterStyle.Vip:
                    BuildVipLook(visual);
                    break;
                case ChubbyCharacterStyle.Manager:
                    BuildGlasses(visual);
                    BuildTie(visual, new Color(0.55f, 0.14f, 0.16f));
                    BuildSideHair(visual, HairColors[5]);
                    break;
            }
        }

        private static Color ReadOutfitColor(GameObject characterRoot)
        {
            var renderer = characterRoot.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Color"))
            {
                return renderer.sharedMaterial.color;
            }

            return RushBankArtLibrary.Cream;
        }

        private static void BuildFace(Transform visual, float eyeDepth, bool withBlush)
        {
            var white = RushBankArtLibrary.Flat(RushBankArtLibrary.EyeWhite);
            var pupil = RushBankArtLibrary.Flat(RushBankArtLibrary.EyePupil);

            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Eye L", new Vector3(-0.17f, 0.34f, eyeDepth), new Vector3(0.17f, 0.19f, 0.1f), white);
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Eye R", new Vector3(0.17f, 0.34f, eyeDepth), new Vector3(0.17f, 0.19f, 0.1f), white);
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Pupil L", new Vector3(-0.17f, 0.33f, eyeDepth + 0.05f), new Vector3(0.08f, 0.1f, 0.06f), pupil);
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Pupil R", new Vector3(0.17f, 0.33f, eyeDepth + 0.05f), new Vector3(0.08f, 0.1f, 0.06f), pupil);

            if (withBlush)
            {
                var blush = RushBankArtLibrary.Flat(RushBankArtLibrary.Blush);
                RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Blush L", new Vector3(-0.31f, 0.16f, 0.36f), new Vector3(0.11f, 0.07f, 0.05f), blush);
                RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Blush R", new Vector3(0.31f, 0.16f, 0.36f), new Vector3(0.11f, 0.07f, 0.05f), blush);
            }
        }

        private static void BuildArms(Transform visual, Color outfit)
        {
            var arm = RushBankArtLibrary.Flat(RushBankArtLibrary.Darken(outfit, 0.25f));
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Arm L", new Vector3(-0.52f, -0.1f, 0.12f), new Vector3(0.22f, 0.34f, 0.22f), arm, new Vector3(0f, 0f, 18f));
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Arm R", new Vector3(0.52f, -0.1f, 0.12f), new Vector3(0.22f, 0.34f, 0.22f), arm, new Vector3(0f, 0f, -18f));
        }

        private static void BuildFeet(Transform visual)
        {
            var feet = RushBankArtLibrary.Flat(RushBankArtLibrary.FeetDark);
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Foot L", new Vector3(-0.2f, -0.96f, 0.14f), new Vector3(0.24f, 0.12f, 0.34f), feet);
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Foot R", new Vector3(0.2f, -0.96f, 0.14f), new Vector3(0.24f, 0.12f, 0.34f), feet);
        }

        private static void BuildCustomerHair(Transform visual, int seed)
        {
            var hairColor = HairColors[(seed / 7) % HairColors.Length];
            var hair = RushBankArtLibrary.Flat(hairColor);
            var variant = seed % 4;

            switch (variant)
            {
                case 0:
                    RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Hair Flat", new Vector3(0f, 0.88f, -0.04f), new Vector3(0.72f, 0.3f, 0.72f), hair);
                    break;
                case 1:
                    RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Hair Base", new Vector3(0f, 0.86f, -0.08f), new Vector3(0.68f, 0.28f, 0.66f), hair);
                    RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Hair Bun", new Vector3(0f, 1.06f, -0.2f), new Vector3(0.3f, 0.28f, 0.3f), hair);
                    break;
                case 2:
                    RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Hair Side", new Vector3(-0.1f, 0.9f, -0.02f), new Vector3(0.66f, 0.26f, 0.68f), hair, new Vector3(0f, 0f, -9f));
                    break;
                default:
                    BuildGlasses(visual);
                    break;
            }
        }

        private static void BuildSideHair(Transform visual, Color hairColor)
        {
            var hair = RushBankArtLibrary.Flat(hairColor);
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Hair Side L", new Vector3(-0.4f, 0.55f, -0.1f), new Vector3(0.2f, 0.3f, 0.4f), hair);
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Hair Side R", new Vector3(0.4f, 0.55f, -0.1f), new Vector3(0.2f, 0.3f, 0.4f), hair);
        }

        private static void BuildGlasses(Transform visual)
        {
            var frame = RushBankArtLibrary.Flat(RushBankArtLibrary.SlateDark);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, visual, "Glasses L", new Vector3(-0.17f, 0.34f, 0.47f), new Vector3(0.2f, 0.17f, 0.03f), frame);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, visual, "Glasses R", new Vector3(0.17f, 0.34f, 0.47f), new Vector3(0.2f, 0.17f, 0.03f), frame);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, visual, "Glasses Bridge", new Vector3(0f, 0.36f, 0.47f), new Vector3(0.14f, 0.03f, 0.03f), frame);
        }

        private static void BuildTie(Transform visual, Color tieColor)
        {
            var tie = RushBankArtLibrary.Flat(tieColor);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, visual, "Tie Knot", new Vector3(0f, 0.12f, 0.46f), new Vector3(0.1f, 0.08f, 0.03f), tie);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, visual, "Tie", new Vector3(0f, -0.08f, 0.47f), new Vector3(0.09f, 0.3f, 0.03f), tie);
        }

        private static void BuildBelt(Transform visual)
        {
            var belt = RushBankArtLibrary.Flat(RushBankArtLibrary.SlateDark);
            var buckle = RushBankArtLibrary.Flat(RushBankArtLibrary.Gold);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, visual, "Belt", new Vector3(0f, -0.3f, 0f), new Vector3(1.03f, 0.05f, 1.03f), belt);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, visual, "Belt Buckle", new Vector3(0f, -0.3f, 0.5f), new Vector3(0.12f, 0.08f, 0.05f), buckle);
        }

        private static void BuildCapAndBadge(Transform visual, Color capColor, Color badgeColor)
        {
            var cap = RushBankArtLibrary.Flat(capColor);
            var badge = RushBankArtLibrary.Flat(badgeColor);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, visual, "Cap", new Vector3(0f, 0.92f, 0f), new Vector3(0.68f, 0.09f, 0.68f), cap);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, visual, "Cap Top", new Vector3(0f, 1f, -0.03f), new Vector3(0.52f, 0.07f, 0.52f), cap);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, visual, "Cap Brim", new Vector3(0f, 0.88f, 0.4f), new Vector3(0.46f, 0.05f, 0.26f), cap);
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Badge", new Vector3(-0.24f, 0.05f, 0.44f), new Vector3(0.1f, 0.12f, 0.05f), badge);
        }

        private static void BuildTellerOutfit(Transform visual)
        {
            var vest = RushBankArtLibrary.Flat(RushBankArtLibrary.BankGreenDark);
            var accent = RushBankArtLibrary.Flat(RushBankArtLibrary.Gold);

            RushBankArtLibrary.Shape(PrimitiveType.Cube, visual, "Vest", new Vector3(0f, -0.05f, 0.4f), new Vector3(0.56f, 0.56f, 0.12f), vest);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, visual, "Name Tag", new Vector3(-0.19f, 0.1f, 0.48f), new Vector3(0.14f, 0.07f, 0.02f), accent);
            BuildTie(visual, RushBankArtLibrary.Gold);

            var cap = RushBankArtLibrary.Flat(RushBankArtLibrary.BankGreen);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, visual, "Teller Cap", new Vector3(0f, 0.94f, 0f), new Vector3(0.62f, 0.08f, 0.62f), cap);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, visual, "Teller Cap Brim", new Vector3(0f, 0.9f, 0.38f), new Vector3(0.42f, 0.05f, 0.24f), cap);
        }

        private static void BuildThiefLook(Transform visual)
        {
            var beanie = RushBankArtLibrary.Flat(new Color(0.12f, 0.12f, 0.16f));
            var mask = RushBankArtLibrary.Flat(new Color(0.18f, 0.18f, 0.23f));
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Beanie", new Vector3(0f, 0.9f, -0.02f), new Vector3(0.7f, 0.34f, 0.7f), beanie);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, visual, "Eye Mask", new Vector3(0f, 0.34f, 0.43f), new Vector3(0.62f, 0.2f, 0.05f), mask);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, visual, "Loot Strap", new Vector3(0f, 0.05f, 0f), new Vector3(0.1f, 0.02f, 1.04f), mask, new Vector3(0f, 0f, 40f));
        }

        private static void BuildTeaLadyLook(Transform visual)
        {
            var scarf = RushBankArtLibrary.Flat(new Color(0.75f, 0.27f, 0.25f));
            var apron = RushBankArtLibrary.Flat(RushBankArtLibrary.Cream);
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Headscarf", new Vector3(0f, 0.86f, -0.06f), new Vector3(0.74f, 0.36f, 0.72f), scarf);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, visual, "Apron", new Vector3(0f, -0.22f, 0.42f), new Vector3(0.52f, 0.62f, 0.06f), apron);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, visual, "Apron Strap", new Vector3(0f, 0.18f, 0.44f), new Vector3(0.3f, 0.16f, 0.05f), apron);
        }

        private static void BuildHeadphones(Transform visual)
        {
            var band = RushBankArtLibrary.Flat(RushBankArtLibrary.SlateDark);
            var pad = RushBankArtLibrary.Flat(RushBankArtLibrary.BankGreen);
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Headphone Band", new Vector3(0f, 0.9f, 0f), new Vector3(0.66f, 0.2f, 0.6f), band);
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Headphone L", new Vector3(-0.42f, 0.4f, 0f), new Vector3(0.16f, 0.22f, 0.22f), pad);
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Headphone R", new Vector3(0.42f, 0.4f, 0f), new Vector3(0.16f, 0.22f, 0.22f), pad);
        }

        private static void BuildVipLook(Transform visual)
        {
            var hat = RushBankArtLibrary.Flat(RushBankArtLibrary.SlateDark);
            var gold = RushBankArtLibrary.Flat(RushBankArtLibrary.Gold);
            var briefcase = RushBankArtLibrary.Flat(RushBankArtLibrary.WoodDark);

            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, visual, "Top Hat Brim", new Vector3(0f, 0.9f, 0f), new Vector3(0.7f, 0.04f, 0.7f), hat);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, visual, "Top Hat", new Vector3(0f, 1.12f, 0f), new Vector3(0.44f, 0.22f, 0.44f), hat);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, visual, "Top Hat Band", new Vector3(0f, 0.98f, 0f), new Vector3(0.46f, 0.04f, 0.46f), gold);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, visual, "Briefcase", new Vector3(0.62f, -0.42f, 0.16f), new Vector3(0.3f, 0.24f, 0.1f), briefcase);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, visual, "Briefcase Clasp", new Vector3(0.62f, -0.36f, 0.22f), new Vector3(0.08f, 0.04f, 0.03f), gold);
        }

        private static void BuildCatFace(Transform visual, Color furColor)
        {
            var fur = RushBankArtLibrary.Flat(RushBankArtLibrary.Darken(furColor, 0.15f));
            var inner = RushBankArtLibrary.Flat(RushBankArtLibrary.Blush);
            var white = RushBankArtLibrary.Flat(RushBankArtLibrary.EyeWhite);
            var pupil = RushBankArtLibrary.Flat(RushBankArtLibrary.EyePupil);

            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Ear L", new Vector3(-0.26f, 1f, 0f), new Vector3(0.2f, 0.3f, 0.08f), fur, new Vector3(0f, 0f, 18f));
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Ear R", new Vector3(0.26f, 1f, 0f), new Vector3(0.2f, 0.3f, 0.08f), fur, new Vector3(0f, 0f, -18f));
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Eye L", new Vector3(-0.16f, 0.38f, 0.42f), new Vector3(0.14f, 0.16f, 0.08f), white);
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Eye R", new Vector3(0.16f, 0.38f, 0.42f), new Vector3(0.14f, 0.16f, 0.08f), white);
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Pupil L", new Vector3(-0.16f, 0.37f, 0.47f), new Vector3(0.06f, 0.1f, 0.05f), pupil);
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Pupil R", new Vector3(0.16f, 0.37f, 0.47f), new Vector3(0.06f, 0.1f, 0.05f), pupil);
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Muzzle", new Vector3(0f, 0.2f, 0.44f), new Vector3(0.24f, 0.16f, 0.12f), white);
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Nose", new Vector3(0f, 0.26f, 0.5f), new Vector3(0.07f, 0.05f, 0.05f), inner);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, visual, "Tail", new Vector3(0f, -0.55f, -0.52f), new Vector3(0.08f, 0.3f, 0.08f), fur, new Vector3(-40f, 0f, 0f));
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, visual, "Tail Tip", new Vector3(0f, -0.28f, -0.72f), new Vector3(0.12f, 0.12f, 0.12f), fur);
        }
    }
}
