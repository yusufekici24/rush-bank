using System.Collections;
using UnityEngine;

namespace RushBank.Art
{
    public class ChubbyVisualSkinner : MonoBehaviour
    {
        [SerializeField] private float sweepIntervalSeconds = 0.5f;

        private IEnumerator Start()
        {
            DecorateStations();

            var wait = new WaitForSeconds(sweepIntervalSeconds);
            while (enabled)
            {
                Sweep();
                yield return wait;
            }
        }

        private static void Sweep()
        {
            var meshFilters = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
            for (var i = 0; i < meshFilters.Length; i++)
            {
                var meshFilter = meshFilters[i];
                if (meshFilter == null || meshFilter.sharedMesh == null || meshFilter.sharedMesh.name != "Capsule")
                {
                    continue;
                }

                var go = meshFilter.gameObject;
                if (go.GetComponent<ChubbyVisualTag>() != null)
                {
                    continue;
                }

                var lowerName = go.name.ToLowerInvariant();
                if (lowerName.Contains("heist") || CountChildMeshFilters(go.transform) >= 3)
                {
                    go.AddComponent<ChubbyVisualTag>();
                    continue;
                }

                if (TryResolveStyle(lowerName, out var style))
                {
                    ChubbyCharacterDresser.Dress(go, style);
                }
                else
                {
                    go.AddComponent<ChubbyVisualTag>();
                }
            }
        }

        private static int CountChildMeshFilters(Transform root)
        {
            var count = 0;
            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            for (var i = 0; i < filters.Length; i++)
            {
                if (filters[i].transform != root)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool TryResolveStyle(string lowerName, out ChubbyCharacterStyle style)
        {
            if (lowerName.Contains("tea lady") || lowerName.Contains("tealady"))
            {
                style = ChubbyCharacterStyle.TeaLady;
                return true;
            }

            if (lowerName.Contains("thief"))
            {
                style = ChubbyCharacterStyle.Thief;
                return true;
            }

            if (lowerName.Contains("police"))
            {
                style = ChubbyCharacterStyle.Police;
                return true;
            }

            if (lowerName.Contains("guard"))
            {
                style = ChubbyCharacterStyle.Guard;
                return true;
            }

            if (lowerName.Contains("coworker"))
            {
                style = ChubbyCharacterStyle.Coworker;
                return true;
            }

            if (lowerName.Contains("assistant"))
            {
                style = ChubbyCharacterStyle.Assistant;
                return true;
            }

            if (lowerName.Contains("vip"))
            {
                style = ChubbyCharacterStyle.Vip;
                return true;
            }

            if (lowerName.Contains("manager"))
            {
                style = ChubbyCharacterStyle.Manager;
                return true;
            }

            if (lowerName.Contains("cat"))
            {
                style = ChubbyCharacterStyle.Cat;
                return true;
            }

            if (lowerName.Contains("player"))
            {
                style = ChubbyCharacterStyle.Teller;
                return true;
            }

            if (lowerName.Contains("customer"))
            {
                style = ChubbyCharacterStyle.Customer;
                return true;
            }

            style = ChubbyCharacterStyle.Customer;
            return false;
        }

        private static void DecorateStations()
        {
            DecorateStationsWithTag("DocumentDesk", DecorateDocumentDesk);
            DecorateStationsWithTag("ManagerDesk", DecorateManagerDesk);
            DecorateStationsWithTag("ArchiveDesk", DecorateArchiveDesk);
            DecorateStationsWithTag("ExpertiseStation", DecorateExpertiseStation);
            DecorateStationsWithTag("PassbookPrinter", DecoratePassbookPrinter);
            DecorateStationsWithTag("TeasideTable", DecorateTeasideTable);
            DecorateStationsWithTag("SnackDrawer", DecorateSnackDrawer);
            DecorateStationsWithTag("CashRegister", DecorateCashStation);
            DecorateStationsWithTag("Counter", DecorateDeliveryDesk);
        }

        private static void DecorateStationsWithTag(string stationTag, System.Action<Transform> decorate)
        {
            GameObject[] stations;
            try
            {
                stations = GameObject.FindGameObjectsWithTag(stationTag);
            }
            catch (UnityException)
            {
                return;
            }

            for (var i = 0; i < stations.Length; i++)
            {
                var station = stations[i];
                if (station.GetComponent<ChubbyVisualTag>() != null)
                {
                    continue;
                }

                station.AddComponent<ChubbyVisualTag>();
                decorate(CreatePropContainer(station));
            }
        }

        private static Transform CreatePropContainer(GameObject station)
        {
            var container = new GameObject("Station Props");
            container.transform.SetParent(station.transform, false);
            container.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            var stationScale = station.transform.localScale;
            container.transform.localScale = new Vector3(
                stationScale.x > 0.001f ? 1f / stationScale.x : 1f,
                stationScale.y > 0.001f ? 1f / stationScale.y : 1f,
                stationScale.z > 0.001f ? 1f / stationScale.z : 1f);
            return container.transform;
        }

        private static void DecorateDocumentDesk(Transform props)
        {
            var paper = RushBankArtLibrary.Flat(RushBankArtLibrary.Cream);
            var stampMaterial = RushBankArtLibrary.Flat(new Color(0.6f, 0.16f, 0.18f));
            RushBankArtLibrary.Shape(PrimitiveType.Cube, props, "Paper Stack", new Vector3(-0.3f, 0.03f, 0f), new Vector3(0.34f, 0.05f, 0.26f), paper, new Vector3(0f, 8f, 0f));
            RushBankArtLibrary.Shape(PrimitiveType.Cube, props, "Paper Sheet", new Vector3(-0.28f, 0.07f, 0.02f), new Vector3(0.3f, 0.015f, 0.24f), paper, new Vector3(0f, -6f, 0f));
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, props, "Stamp", new Vector3(0.3f, 0.08f, -0.05f), new Vector3(0.09f, 0.07f, 0.09f), stampMaterial);
        }

        private static void DecorateManagerDesk(Transform props)
        {
            var dark = RushBankArtLibrary.Flat(RushBankArtLibrary.SlateDark);
            var screen = RushBankArtLibrary.Emissive(new Color(0.2f, 0.45f, 0.5f), new Color(0.3f, 0.8f, 0.85f), 0.7f);
            var gold = RushBankArtLibrary.Flat(RushBankArtLibrary.Gold);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, props, "Monitor Stand", new Vector3(0.35f, 0.08f, -0.15f), new Vector3(0.08f, 0.16f, 0.08f), dark);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, props, "Monitor", new Vector3(0.35f, 0.26f, -0.15f), new Vector3(0.42f, 0.28f, 0.04f), dark, new Vector3(0f, 20f, 0f));
            RushBankArtLibrary.Shape(PrimitiveType.Cube, props, "Monitor Screen", new Vector3(0.34f, 0.26f, -0.13f), new Vector3(0.36f, 0.22f, 0.045f), screen, new Vector3(0f, 20f, 0f));
            RushBankArtLibrary.Shape(PrimitiveType.Cube, props, "Name Plate", new Vector3(-0.35f, 0.05f, 0.2f), new Vector3(0.3f, 0.07f, 0.03f), gold, new Vector3(-18f, 0f, 0f));
        }

        private static void DecorateArchiveDesk(Transform props)
        {
            var fileA = RushBankArtLibrary.Flat(RushBankArtLibrary.WoodDark);
            var fileB = RushBankArtLibrary.Flat(RushBankArtLibrary.BankGreenDark);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, props, "Box File A", new Vector3(-0.25f, 0.16f, -0.1f), new Vector3(0.12f, 0.32f, 0.26f), fileA);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, props, "Box File B", new Vector3(-0.1f, 0.16f, -0.1f), new Vector3(0.12f, 0.32f, 0.26f), fileB, new Vector3(0f, 0f, -6f));
        }

        private static void DecorateExpertiseStation(Transform props)
        {
            var brass = RushBankArtLibrary.Flat(RushBankArtLibrary.Gold);
            var dark = RushBankArtLibrary.Flat(RushBankArtLibrary.SlateDark);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, props, "Scale Post", new Vector3(0f, 0.16f, 0f), new Vector3(0.05f, 0.16f, 0.05f), dark);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, props, "Scale Beam", new Vector3(0f, 0.32f, 0f), new Vector3(0.5f, 0.03f, 0.03f), brass);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, props, "Scale Pan L", new Vector3(-0.24f, 0.2f, 0f), new Vector3(0.16f, 0.015f, 0.16f), brass);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, props, "Scale Pan R", new Vector3(0.24f, 0.24f, 0f), new Vector3(0.16f, 0.015f, 0.16f), brass);
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, props, "Gold Nugget", new Vector3(-0.24f, 0.25f, 0f), new Vector3(0.08f, 0.06f, 0.08f), brass);
        }

        private static void DecoratePassbookPrinter(Transform props)
        {
            var dark = RushBankArtLibrary.Flat(RushBankArtLibrary.SlateDark);
            var paper = RushBankArtLibrary.Flat(RushBankArtLibrary.Cream);
            var light = RushBankArtLibrary.Emissive(RushBankArtLibrary.BankGreen, RushBankArtLibrary.BankGreen, 1.2f);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, props, "Printer Slot", new Vector3(0f, 0.06f, 0.1f), new Vector3(0.5f, 0.12f, 0.3f), dark);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, props, "Printer Paper", new Vector3(0f, 0.14f, 0.16f), new Vector3(0.3f, 0.015f, 0.22f), paper, new Vector3(-12f, 0f, 0f));
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, props, "Printer Light", new Vector3(0.18f, 0.13f, 0.02f), new Vector3(0.05f, 0.05f, 0.05f), light);
        }

        private static void DecorateTeasideTable(Transform props)
        {
            var pot = RushBankArtLibrary.Flat(new Color(0.75f, 0.27f, 0.25f));
            var lid = RushBankArtLibrary.Flat(RushBankArtLibrary.Gold);
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, props, "Teapot", new Vector3(-0.2f, 0.14f, 0f), new Vector3(0.26f, 0.22f, 0.26f), pot);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, props, "Teapot Spout", new Vector3(-0.04f, 0.16f, 0f), new Vector3(0.05f, 0.09f, 0.05f), pot, new Vector3(0f, 0f, -55f));
            RushBankArtLibrary.Shape(PrimitiveType.Sphere, props, "Teapot Lid", new Vector3(-0.2f, 0.27f, 0f), new Vector3(0.07f, 0.06f, 0.07f), lid);
        }

        private static void DecorateSnackDrawer(Transform props)
        {
            var handle = RushBankArtLibrary.Flat(RushBankArtLibrary.Gold);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, props, "Drawer Handle Top", new Vector3(0f, -0.15f, 0.52f), new Vector3(0.3f, 0.04f, 0.05f), handle);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, props, "Drawer Handle Bottom", new Vector3(0f, -0.55f, 0.52f), new Vector3(0.3f, 0.04f, 0.05f), handle);
        }

        private static void DecorateCashStation(Transform props)
        {
            var gold = RushBankArtLibrary.Flat(RushBankArtLibrary.Gold);
            var cash = RushBankArtLibrary.Flat(RushBankArtLibrary.BankGreen);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, props, "Coin Stack A", new Vector3(-0.2f, 0.06f, 0.08f), new Vector3(0.1f, 0.05f, 0.1f), gold);
            RushBankArtLibrary.Shape(PrimitiveType.Cylinder, props, "Coin Stack B", new Vector3(-0.08f, 0.04f, -0.06f), new Vector3(0.1f, 0.03f, 0.1f), gold);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, props, "Cash Brick", new Vector3(0.18f, 0.05f, 0f), new Vector3(0.26f, 0.08f, 0.16f), cash, new Vector3(0f, 14f, 0f));
        }

        private static void DecorateDeliveryDesk(Transform props)
        {
            var parcel = RushBankArtLibrary.Flat(RushBankArtLibrary.WoodWarm);
            var strap = RushBankArtLibrary.Flat(RushBankArtLibrary.Gold);
            RushBankArtLibrary.Shape(PrimitiveType.Cube, props, "Parcel", new Vector3(0.2f, 0.12f, -0.15f), new Vector3(0.3f, 0.22f, 0.26f), parcel, new Vector3(0f, 12f, 0f));
            RushBankArtLibrary.Shape(PrimitiveType.Cube, props, "Parcel Strap", new Vector3(0.2f, 0.12f, -0.15f), new Vector3(0.08f, 0.23f, 0.27f), strap, new Vector3(0f, 12f, 0f));
        }
    }
}
