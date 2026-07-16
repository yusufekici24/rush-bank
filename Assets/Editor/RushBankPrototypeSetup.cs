using RushBank.Core;
using RushBank.Gameplay;
using RushBank.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace RushBank.EditorTools
{
    public static class RushBankPrototypeSetup
    {
        private const string ScenesFolder = "Assets/Scenes";
        private const string DataFolder = "Assets/Data";
        private const string RequestsFolder = "Assets/Data/Requests";
        private const string CustomersFolder = "Assets/Data/Customers";
        private const string TransactionsFolder = "Assets/Data/Transactions";
        private const string PrefabsFolder = "Assets/Prefabs";
        private const string TransactionPrefabsFolder = "Assets/Prefabs/Transactions";
        private const string PanelSettingsPath = "Assets/UI/Settings/RushBankPanelSettings.asset";
        private const string MainMenuUxmlPath = "Assets/UI/UXML/MainMenu.uxml";
        private const string GameHudUxmlPath = "Assets/UI/UXML/GameHud.uxml";
        private const string ThemeUssPath = "Assets/UI/Styles/RushBankTheme.uss";

        [MenuItem("RushBank/Setup Prototype Scenes")]
        public static void SetupPrototypeScenes()
        {
            if (!EditorUtility.DisplayDialog(
                    "RushBank Prototype Setup",
                    "Prototype sahneleri ve örnek veriler oluşturulacak. Aynı isimli sahneler varsa güncellenir.",
                    "Kur",
                    "Vazgeç"))
            {
                return;
            }

            EnsureFolders();
            EnsureTag("Interactable");
            EnsureTag("CashRegister");
            EnsureTag("Counter");
            EnsureTag("DocumentDesk");
            EnsureTag("ManagerDesk");
            EnsureTag("ExpertiseStation");
            EnsureTag("PassbookPrinter");
            EnsureTag("BarcodeScanner");
            EnsureTag("ArchiveDesk");
            EnsureTag("TeasideTable");
            EnsureTag("SnackDrawer");

            var panelSettings = EnsurePanelSettings();
            var depositRequest = EnsureRequest(
                "Deposit",
                "Para Yatırma",
                "Hesabıma para yatırmak istiyorum.",
                CustomerRequestType.Deposit,
                18f,
                100,
                6,
                1);
            var withdrawRequest = EnsureRequest(
                "Withdraw",
                "Para Çekme",
                "Hesabımdan para çekmem gerekiyor.",
                CustomerRequestType.Withdraw,
                20f,
                120,
                6,
                2);
            var cardRequest = EnsureRequest(
                "CardApplication",
                "Kart Başvurusu",
                "Yeni banka kartı başvurusu yapmak istiyorum.",
                CustomerRequestType.CardApplication,
                28f,
                180,
                8,
                3);

            var passbookPrefab = EnsureTransactionItemPrefab(
                "Passbook",
                new Vector3(0.72f, 0.08f, 0.42f),
                new Color(0.27f, 0.58f, 0.95f),
                "passbook");
            var utilityBillPrefab = EnsureTransactionItemPrefab(
                "Utility Bill",
                new Vector3(0.62f, 0.04f, 0.36f),
                new Color(0.46f, 0.86f, 0.62f),
                "utility_bill");
            var blockedCardPrefab = EnsureTransactionItemPrefab(
                "Blocked Bank Card",
                new Vector3(0.52f, 0.04f, 0.32f),
                new Color(0.94f, 0.62f, 0.36f),
                "blocked_bank_card");
            var cashBundlePrefab = EnsureTransactionItemPrefab(
                "Cash Bundle",
                new Vector3(0.5f, 0.16f, 0.34f),
                new Color(0.18f, 0.68f, 0.32f),
                "cash_bundle");
            var foreignCurrencyBagPrefab = EnsureTransactionItemPrefab(
                "Foreign Currency Bag",
                new Vector3(0.48f, 0.48f, 0.48f),
                new Color(0.32f, 0.72f, 0.95f),
                "foreign_currency_bag");
            var creditFormPrefab = EnsureTransactionItemPrefab(
                "Credit Application Form",
                new Vector3(0.72f, 0.04f, 0.5f),
                new Color(0.94f, 0.92f, 0.76f),
                "credit_application_form");
            var goldReceiptPrefab = EnsureTransactionItemPrefab(
                "Gold Value Receipt",
                new Vector3(0.62f, 0.04f, 0.36f),
                new Color(1f, 0.78f, 0.22f),
                "gold_value_receipt");
            var vipKeyPrefab = EnsureTransactionItemPrefab(
                "VIP Safe Key",
                new Vector3(0.18f, 0.08f, 0.48f),
                new Color(0.72f, 0.52f, 0.95f),
                "vip_safe_key");

            EnsureBankTransaction(
                "PassbookPrinting",
                "Hesap Cüzdanı",
                4f,
                null,
                passbookPrefab,
                "Cüzdan Yazıcısı (Gişe içi)",
                BankTransactionDifficulty.VeryEasy,
                BankTransactionFlow.FastTrack,
                0.8f,
                0f,
                0f,
                0f,
                false,
                false,
                false);
            EnsureBankTransaction(
                "BillPayment",
                "Fatura Ödeme",
                4f,
                null,
                utilityBillPrefab,
                "Barkod Okuyucu (Gişe içi)",
                BankTransactionDifficulty.VeryEasy,
                BankTransactionFlow.FastTrack,
                0.8f,
                0f,
                0f,
                0f,
                false,
                false,
                false,
                "Oyuncuya nefes aldıran hızlı gişe içi quick win.",
                "Müşteri elektrik veya su faturasıyla gelir; oyuncu faturayı barkod okuyucuya okutur, nakdi alır ve çekmeceye bırakır.",
                new[]
                {
                    "Müşteriden elektrik veya su faturasını al.",
                    "Faturayı bankodaki Barkod Okuyucuya tut.",
                    "Bip sesinden sonra müşteriden nakit parayı al.",
                    "Nakit parayı gişe altındaki çekmeceye bırak.",
                    "İşlemi tamamla ve +4 saniye kazan."
                },
                "barcode_beep");
            EnsureBankTransaction(
                "BlockRemoval",
                "Kart Şifre Blokesi Kaldırma",
                5f,
                null,
                blockedCardPrefab,
                "Mini Terminal (Gişe içi - Mini Game)",
                BankTransactionDifficulty.Easy,
                BankTransactionFlow.MiniGame,
                1.2f,
                0f,
                0f,
                0f,
                false,
                false,
                false,
                "Dokunmatik ekranda hızlı yapılacak mini etkileşim.",
                "Müşteri bloke olmuş kartını verir; oyuncu kartı mini terminale takar ve 3 haneli renk sırasını doğru girer.",
                new[]
                {
                    "Müşteriden bloke olmuş kartı al.",
                    "Kartı gişedeki mini terminale tak.",
                    "Ekranda çıkan 3 haneli renk sırasını izle.",
                    "Kırmızı/Mavi gibi renk butonlarına doğru sırayla bas.",
                    "Kartı müşteriye geri ver ve +5 saniye kazan."
                },
                "terminal_color_sequence");
            EnsureBankTransaction(
                "CashWithdrawDeposit",
                "Para Çekme/Yatırma",
                7f,
                null,
                cashBundlePrefab,
                "Gişe <-> Ana Kasa (Vault)",
                BankTransactionDifficulty.Medium,
                BankTransactionFlow.CashVault,
                0f,
                0f,
                0f,
                0f,
                false,
                false,
                false);
            EnsureBankTransaction(
                "CurrencyExchange",
                "Döviz Bozdurma",
                7f,
                null,
                foreignCurrencyBagPrefab,
                "Gişe <-> Kur Çevirici Masası",
                BankTransactionDifficulty.Medium,
                BankTransactionFlow.CurrencyExchange,
                0f,
                0f,
                0f,
                1.5f,
                false,
                false,
                false,
                "Hızlı karar verme ve renk/sembol eşleştirme mekaniği.",
                "Müşteri dolar veya euro simgeli çuvalla gelir; oyuncu çuvalı Kur Çevirici masasına koyar ve yerel para makbuzunu teslim eder.",
                new[]
                {
                    "Müşteriden yabancı para çuvalını al.",
                    "Çuvalı gişe arkasındaki Kur Çevirici masasına koy.",
                    "Cihazın yaktığı yeşil veya mavi ışığı/simgeyi takip et.",
                    "Dönen yerel para makbuzunu al.",
                    "Makbuzu müşteriye teslim et ve +7 saniye kazan."
                },
                "currency_converter_light");
            EnsureBankTransaction(
                "GoldExchange",
                "Altın Bozdurma",
                10f,
                null,
                goldReceiptPrefab,
                "Gişe -> Ekspertiz Masası -> Gişe",
                BankTransactionDifficulty.Hard,
                BankTransactionFlow.GoldExchange,
                0f,
                0f,
                0f,
                2f,
                false,
                false,
                true);
            EnsureBankTransaction(
                "CreditApproval",
                "Kredi Onayı",
                12f,
                null,
                creditFormPrefab,
                "Gişe -> İmza -> Müdür Odası -> Gişe",
                BankTransactionDifficulty.Hard,
                BankTransactionFlow.CreditApproval,
                0f,
                1.5f,
                2f,
                0f,
                true,
                true,
                false);
            EnsureBankTransaction(
                "VipSafeRental",
                "VIP Kiralık Kasa",
                15f,
                null,
                vipKeyPrefab,
                "VIP Müşteriye Kasa Odasına Kadar Eşlik Etme",
                BankTransactionDifficulty.VeryHard,
                BankTransactionFlow.VipSafeEscort,
                0f,
                0f,
                0f,
                0f,
                false,
                false,
                false,
                "Oyuncuyu bankodan uzaklaştıran riskli ama yüksek ödüllü eşlik görevi.",
                "VIP müşteri anahtar simgesiyle gelir; oyuncu müşteriye kiralık kasa odasına kadar eşlik eder, kapıda bekler ve müşteriyi çıkışa uğurlar.",
                new[]
                {
                    "VIP müşterinin yanına git.",
                    "Müşteriye şubenin arkasındaki Kiralık Kasa Odası'na kadar eşlik et.",
                    "Kasa kapısında 2 saniye bekleyip kapıyı aç.",
                    "Müşterinin içeri girip altınını veya mücevherini almasını bekle.",
                    "VIP müşteriyi tekrar çıkışa kadar uğurla ve +15 saniye kazan."
                },
                "safe_door_open");

            var quickCustomer = EnsureCustomer(
                "QuickCustomer",
                "Hızlı Müşteri",
                "Kısa ve net işlem isteyen müşteri.",
                35f,
                new[] { depositRequest, withdrawRequest });
            var detailedCustomer = EnsureCustomer(
                "DetailedCustomer",
                "Detaycı Müşteri",
                "İşlem sırasında daha fazla açıklama bekleyen müşteri.",
                55f,
                new[] { cardRequest, depositRequest });
            var urgentCustomer = EnsureCustomer(
                "UrgentCustomer",
                "Acelesi Olan Müşteri",
                "İşlemin hızlı bitmesini bekleyen müşteri.",
                25f,
                new[] { withdrawRequest, depositRequest });

            CreateBootScene();
            CreateMenuScene("Login", MainMenuUxmlPath, panelSettings);
            CreateMenuScene("MainMenu", MainMenuUxmlPath, panelSettings);
            CreateGameScene(
                GameHudUxmlPath,
                panelSettings,
                new[] { quickCustomer, detailedCustomer, urgentCustomer, quickCustomer, urgentCustomer, detailedCustomer });
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("RushBank Prototype Setup", "Prototype sahneleri hazır.", "Tamam");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "Scenes");
            EnsureFolder("Assets", "Data");
            EnsureFolder(DataFolder, "Requests");
            EnsureFolder(DataFolder, "Customers");
            EnsureFolder(DataFolder, "Transactions");
            EnsureFolder("Assets", "Prefabs");
            EnsureFolder(PrefabsFolder, "Transactions");
            EnsureFolder("Assets/UI", "Settings");
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void EnsureTag(string tagName)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tags = tagManager.FindProperty("tags");

            for (var i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tagName)
                {
                    return;
                }
            }

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tagName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static PanelSettings EnsurePanelSettings()
        {
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings != null)
            {
                return panelSettings;
            }

            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.name = "RushBankPanelSettings";
            AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
            return panelSettings;
        }

        private static CustomerRequestDefinition EnsureRequest(
            string id,
            string displayName,
            string customerLine,
            CustomerRequestType type,
            float targetSeconds,
            int baseScore,
            int scorePerRemainingSecond,
            int difficulty)
        {
            var path = $"{RequestsFolder}/{id}.asset";
            var request = AssetDatabase.LoadAssetAtPath<CustomerRequestDefinition>(path);
            if (request == null)
            {
                request = ScriptableObject.CreateInstance<CustomerRequestDefinition>();
                AssetDatabase.CreateAsset(request, path);
            }

            var serializedObject = new SerializedObject(request);
            serializedObject.FindProperty("requestId").stringValue = id;
            serializedObject.FindProperty("displayName").stringValue = displayName;
            serializedObject.FindProperty("customerLine").stringValue = customerLine;
            serializedObject.FindProperty("requestType").enumValueIndex = (int)type;
            serializedObject.FindProperty("targetProcessingSeconds").floatValue = targetSeconds;
            serializedObject.FindProperty("baseScore").intValue = baseScore;
            serializedObject.FindProperty("scorePerRemainingSecond").intValue = scorePerRemainingSecond;
            serializedObject.FindProperty("difficulty").intValue = difficulty;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(request);
            return request;
        }

        private static BankTransaction EnsureBankTransaction(
            string id,
            string transactionName,
            float baseTimeReward,
            Sprite requestIcon,
            GameObject itemPrefabNeeded,
            string stationRoute,
            BankTransactionDifficulty difficulty,
            BankTransactionFlow flow,
            float processingSeconds,
            float customerSignatureSeconds,
            float managerApprovalSeconds,
            float expertiseEvaluationSeconds,
            bool requiresDocumentDesk,
            bool requiresManagerApproval,
            bool requiresExpertiseDesk,
            string gameplayRole = "",
            string flowSummary = "",
            string[] workflowSteps = null,
            string feedbackCue = "")
        {
            var path = $"{TransactionsFolder}/{id}.asset";
            var transaction = AssetDatabase.LoadAssetAtPath<BankTransaction>(path);
            if (transaction == null)
            {
                transaction = ScriptableObject.CreateInstance<BankTransaction>();
                AssetDatabase.CreateAsset(transaction, path);
            }

            var serializedObject = new SerializedObject(transaction);
            serializedObject.FindProperty("transactionName").stringValue = transactionName;
            serializedObject.FindProperty("baseTimeReward").floatValue = baseTimeReward;
            serializedObject.FindProperty("requestIcon").objectReferenceValue = requestIcon;
            serializedObject.FindProperty("itemPrefabNeeded").objectReferenceValue = itemPrefabNeeded;
            serializedObject.FindProperty("stationRoute").stringValue = stationRoute;
            serializedObject.FindProperty("difficulty").enumValueIndex = (int)difficulty;
            serializedObject.FindProperty("gameplayRole").stringValue = gameplayRole;
            serializedObject.FindProperty("flowSummary").stringValue = flowSummary;
            var stepsProperty = serializedObject.FindProperty("workflowSteps");
            var steps = workflowSteps ?? System.Array.Empty<string>();
            stepsProperty.arraySize = steps.Length;
            for (var i = 0; i < steps.Length; i++)
            {
                stepsProperty.GetArrayElementAtIndex(i).stringValue = steps[i];
            }

            serializedObject.FindProperty("feedbackCue").stringValue = feedbackCue;
            serializedObject.FindProperty("flow").enumValueIndex = (int)flow;
            serializedObject.FindProperty("processingSeconds").floatValue = processingSeconds;
            serializedObject.FindProperty("customerSignatureSeconds").floatValue = customerSignatureSeconds;
            serializedObject.FindProperty("managerApprovalSeconds").floatValue = managerApprovalSeconds;
            serializedObject.FindProperty("expertiseEvaluationSeconds").floatValue = expertiseEvaluationSeconds;
            serializedObject.FindProperty("requiresDocumentDesk").boolValue = requiresDocumentDesk;
            serializedObject.FindProperty("requiresManagerApproval").boolValue = requiresManagerApproval;
            serializedObject.FindProperty("requiresExpertiseDesk").boolValue = requiresExpertiseDesk;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(transaction);
            return transaction;
        }

        private static GameObject EnsureTransactionItemPrefab(
            string name,
            Vector3 scale,
            Color color,
            string itemId)
        {
            var path = $"{TransactionPrefabsFolder}/{name}.prefab";
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existingPrefab != null)
            {
                return existingPrefab;
            }

            var prefabRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefabRoot.name = name;
            prefabRoot.tag = "Interactable";
            prefabRoot.transform.localScale = scale;

            var body = prefabRoot.AddComponent<Rigidbody>();
            body.mass = 0.4f;

            var deliverable = prefabRoot.AddComponent<DeliverableItem>();
            SetString(deliverable, "itemId", itemId);
            SetColor(deliverable, "itemColor", color);

            var renderer = prefabRoot.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = color;
                renderer.sharedMaterial = material;
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
            Object.DestroyImmediate(prefabRoot);
            return prefab;
        }

        private static CustomerDefinition EnsureCustomer(
            string id,
            string displayName,
            string description,
            float patienceSeconds,
            CustomerRequestDefinition[] requests)
        {
            var path = $"{CustomersFolder}/{id}.asset";
            var customer = AssetDatabase.LoadAssetAtPath<CustomerDefinition>(path);
            if (customer == null)
            {
                customer = ScriptableObject.CreateInstance<CustomerDefinition>();
                AssetDatabase.CreateAsset(customer, path);
            }

            var serializedObject = new SerializedObject(customer);
            serializedObject.FindProperty("customerId").stringValue = id;
            serializedObject.FindProperty("displayName").stringValue = displayName;
            serializedObject.FindProperty("description").stringValue = description;
            serializedObject.FindProperty("patienceSeconds").floatValue = patienceSeconds;

            var possibleRequests = serializedObject.FindProperty("possibleRequests");
            possibleRequests.arraySize = requests.Length;
            for (var i = 0; i < requests.Length; i++)
            {
                possibleRequests.GetArrayElementAtIndex(i).objectReferenceValue = requests[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(customer);
            return customer;
        }

        private static void CreateBootScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrap = new GameObject("Bootstrap");
            bootstrap.AddComponent<Bootstrap>();

            SaveScene(scene, "Boot");
        }

        private static void CreateMenuScene(string sceneName, string uxmlPath, PanelSettings panelSettings)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(new Vector3(0f, 1f, -8f), Quaternion.Euler(5f, 0f, 0f), new Color(0.07f, 0.08f, 0.09f));
            CreateDirectionalLight();

            var uiObject = new GameObject($"{sceneName} UI");
            var document = uiObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            var controller = uiObject.AddComponent<MainMenuUIController>();
            SetReference(controller, "document", document);
            SetReference(controller, "themeStyleSheet", AssetDatabase.LoadAssetAtPath<StyleSheet>(ThemeUssPath));
            var preLevelPopup = CreatePreLevelPopupCanvas();
            SetReference(controller, "preLevelPopupController", preLevelPopup);
            var levelDifficultyManager = uiObject.AddComponent<LevelDifficultyManager>();
            SetReference(levelDifficultyManager, "preLevelPopupController", preLevelPopup);
            SetReference(controller, "levelDifficultyManager", levelDifficultyManager);

            SaveScene(scene, sceneName);
        }

        private static PreLevelPopupController CreatePreLevelPopupCanvas()
        {
            var canvasObject = new GameObject("PreLevel Booster Popup Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var shopManager = canvasObject.AddComponent<PreGameShopManager>();

            var panel = new GameObject("PreLevelBoosterPopup");
            panel.transform.SetParent(canvasObject.transform, false);
            var panelImage = panel.AddComponent<UnityEngine.UI.Image>();
            panelImage.color = new Color(0.12f, 0.15f, 0.18f, 0.94f);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(900f, 980f);

            CreatePopupText(panel.transform, "Title", "Zorlu Bir Gün Seni Bekliyor!\nHazırlıklı mısın?", new Vector2(0f, 390f), new Vector2(780f, 120f), 42, Color.white);
            CreatePopupText(panel.transform, "Gold Text", "0", new Vector2(0f, 286f), new Vector2(360f, 58f), 34, new Color(1f, 0.84f, 0.24f));

            CreateBoosterCard(panel.transform, "TimeSlow Card", "Zaman Bükücü", "Süre 0.85x", new Vector2(-290f, 45f), new Color(0.35f, 0.56f, 0.95f));
            CreateBoosterCard(panel.transform, "Speed Card", "Turbo Tabanlık", "Hız 1.2x", new Vector2(0f, 45f), new Color(0.96f, 0.58f, 0.24f));
            CreateBoosterCard(panel.transform, "Patience Card", "Papatya Çayı", "Homurdanma 0.75x", new Vector2(290f, 45f), new Color(0.42f, 0.78f, 0.46f));

            var startButtonObject = CreatePopupButton(panel.transform, "Start Level Button", "Başla", new Vector2(0f, -386f), new Vector2(420f, 92f), new Color(0.2f, 0.76f, 0.38f), 40);
            var closeButtonObject = CreatePopupButton(panel.transform, "Close Button", "X", new Vector2(382f, 426f), new Vector2(72f, 72f), new Color(0.76f, 0.22f, 0.22f), 36);

            var patienceCard = panel.transform.Find("Patience Card");
            if (patienceCard != null)
            {
                CreateBestSellerRibbon(patienceCard);
            }

            var bundleButtonObject = CreatePopupButton(panel.transform, "BuyBundleButton", "Tontis Paket: 150 yerine 120 Altin", new Vector2(0f, -282f), new Vector2(660f, 76f), new Color(1f, 0.66f, 0.18f), 28);
            var bundleConfetti = CreateBundleConfetti(panel.transform);
            var uiAudioSource = canvasObject.AddComponent<AudioSource>();
            uiAudioSource.playOnAwake = false;

            var controller = canvasObject.AddComponent<PreLevelPopupController>();
            SetReference(controller, "shopManager", shopManager);
            SetReference(controller, "popupPanel", panel);
            SetReference(controller, "popupRoot", panelRect);
            SetReference(controller, "playerGoldText", panel.transform.Find("Gold Text").GetComponent<TextMeshProUGUI>());
            SetReference(controller, "startLevelButton", startButtonObject.GetComponent<UnityEngine.UI.Button>());
            SetReference(controller, "closeButton", closeButtonObject.GetComponent<UnityEngine.UI.Button>());
            SetReference(controller, "buyBundleButton", bundleButtonObject.GetComponent<UnityEngine.UI.Button>());
            SetReference(controller, "bundleActionText", bundleButtonObject.transform.Find("Label").GetComponent<TextMeshProUGUI>());
            SetReference(controller, "bundleConfettiParticles", bundleConfetti);
            SetReference(controller, "uiAudioSource", uiAudioSource);
            SetInt(controller, "singleCost", 50);
            SetInt(controller, "bundleCost", 120);
            SetInt(controller, "selectedLevelBuildIndex", (int)SceneId.Game);

            panel.SetActive(false);
            return controller;
        }

        private static void CreateBoosterCard(Transform parent, string name, string title, string effect, Vector2 position, Color color)
        {
            var card = new GameObject(name);
            card.transform.SetParent(parent, false);
            var image = card.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            var button = card.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            var rect = card.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(250f, 440f);

            CreatePopupText(card.transform, "Title Text", title, new Vector2(0f, 148f), new Vector2(220f, 70f), 28, Color.white);
            CreatePopupText(card.transform, "Effect Text", effect, new Vector2(0f, 80f), new Vector2(220f, 54f), 23, Color.white);
            CreatePopupText(card.transform, "Owned Text", "Sahip olunan: 0", new Vector2(0f, -20f), new Vector2(220f, 44f), 21, Color.white);
            CreatePopupText(card.transform, "Cost Text", "50 Altın", new Vector2(0f, -74f), new Vector2(220f, 44f), 22, new Color(1f, 0.88f, 0.28f));
            CreatePopupText(card.transform, "Action Text", "Satın Al & Kuşan", new Vector2(0f, -142f), new Vector2(220f, 56f), 22, Color.white);

            var buyButton = CreatePopupButton(card.transform, "Buy Equip Button", "", new Vector2(0f, -142f), new Vector2(220f, 68f), new Color(0f, 0f, 0f, 0f), 1);
            var buyImage = buyButton.GetComponent<UnityEngine.UI.Image>();
            buyImage.color = new Color(0f, 0f, 0f, 0.12f);

            var check = CreatePopupText(card.transform, "Checkmark", "✓", new Vector2(86f, 176f), new Vector2(52f, 52f), 36, new Color(0.2f, 1f, 0.45f));
            check.SetActive(false);
        }

        private static void CreateBestSellerRibbon(Transform parent)
        {
            var ribbon = new GameObject("BestSellerRibbon");
            ribbon.transform.SetParent(parent, false);
            var image = ribbon.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(1f, 0.78f, 0.18f, 0.96f);
            var rect = ribbon.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-78f, 206f);
            rect.sizeDelta = new Vector2(152f, 48f);
            rect.localRotation = Quaternion.Euler(0f, 0f, 9f);
            ribbon.AddComponent<BestSellerRibbonFloat>();

            CreatePopupText(ribbon.transform, "Label", "Cok Satan!", Vector2.zero, new Vector2(144f, 44f), 21, new Color(0.28f, 0.16f, 0.02f));
        }

        private static ParticleSystem CreateBundleConfetti(Transform parent)
        {
            var confettiObject = new GameObject("Bundle Confetti");
            confettiObject.transform.SetParent(parent, false);
            var rect = confettiObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -270f);
            rect.sizeDelta = Vector2.zero;

            var particles = confettiObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.playOnAwake = false;
            main.duration = 0.8f;
            main.startLifetime = 0.75f;
            main.startSpeed = 170f;
            main.startSize = 10f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 80;

            var emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 48) });

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 18f;

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.88f, 0.24f), 0f),
                    new GradientColorKey(new Color(0.35f, 0.9f, 0.72f), 0.5f),
                    new GradientColorKey(new Color(0.95f, 0.35f, 0.72f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            return particles;
        }

        private static GameObject CreatePopupButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color color, int fontSize)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            var button = buttonObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            if (!string.IsNullOrEmpty(label))
            {
                CreatePopupText(buttonObject.transform, "Label", label, Vector2.zero, size, fontSize, Color.white);
            }

            return buttonObject;
        }

        private static GameObject CreatePopupText(Transform parent, string name, string text, Vector2 position, Vector2 size, int fontSize, Color color)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var label = textObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.enableWordWrapping = true;
            return textObject;
        }

        private static void CreateGameScene(
            string hudUxmlPath,
            PanelSettings panelSettings,
            CustomerDefinition[] customerSequence)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(new Vector3(0f, 3.2f, -6.4f), Quaternion.Euler(24f, 0f, 0f), new Color(0.07f, 0.08f, 0.09f));
            CreateDirectionalLight();

            var environmentObject = new GameObject("Prototype Bank Environment");
            var environmentBuilder = environmentObject.AddComponent<PrototypeBankEnvironmentBuilder>();
            environmentBuilder.Build();
            CreateInteractablePrototypeObjects();
            CreateDeliveryPoint();
            CreateDocumentStations();
            var archiveDeskZone = CreateArchiveDeskStation();
            var goldSparkleEffect = CreateExpertiseStation();
            CreatePassbookPrinterStation();
            var barcodeScanner = CreateBarcodeScannerStation(out var barcodeScannerLight);
            var cashVaultRestockZone = CreateCashVaultStation(out var noCashWarningIcon, out var vaultCashExplosionEffect);
            CreateSnackDrawerStation();
            var teasideTable = CreateTeasideTableStation(out var cupPlacementPoint);
            var heistEntranceTrigger = CreateHeistEntranceTrigger();
            var heistAlarmButton = CreateHeistAlarmButton();
            var player = CreatePlayerController(goldSparkleEffect);
            var playerInteraction = player.GetComponentInChildren<PlayerInteraction>();
            var bankingActionSystem = player.GetComponent<BankingActionSystem>();

            var spawnPoint = CreateMarker("Customer Spawn Point", new Vector3(-3.2f, 0.9f, 3.2f));
            var queueStartPoint = CreateMarker("Queue Start Point", new Vector3(-1.8f, 0.9f, 2.4f));
            var servicePoint = CreateMarker("Service Point", new Vector3(0f, 0.9f, -0.45f));
            var assistantCounterPoint = CreateMarker("Assistant Counter Point", new Vector3(-2.25f, 0.9f, -0.45f));
            var assistantBreakExit = CreateMarker("Assistant Break Exit", new Vector3(4.8f, 0.9f, -3.8f));
            var coworkerSpawnPoint = CreateMarker("Coworker Spawn Point", new Vector3(5.2f, 0.9f, -3.6f));
            var coworkerCounterPoint = CreateMarker("Coworker Counter Interruption Point", new Vector3(0.95f, 0.9f, -0.95f));
            var coworkerExitPoint = CreateMarker("Coworker Exit Point", new Vector3(5.2f, 0.9f, 3.8f));
            var armoredVehicleSpawnPoint = CreateMarker("Armored Cash Vehicle Spawn Point", new Vector3(-4.9f, 0.65f, 5.8f));
            var armoredVehicleParkingPoint = CreateMarker("Armored Cash Vehicle Parking Point", new Vector3(-3.25f, 0.65f, 4.35f));
            var armoredVehicleExitPoint = CreateMarker("Armored Cash Vehicle Exit Point", new Vector3(-5.6f, 0.65f, 5.8f));
            var teaLadySpawnPoint = CreateMarker("Tea Lady Spawn Point", new Vector3(5.2f, 0.9f, -3.2f));
            var teaLadyExitPoint = CreateMarker("Tea Lady Exit Point", new Vector3(5.4f, 0.9f, 3.2f));
            var heistThiefSpawnA = CreateMarker("Heist Thief Spawn A", new Vector3(-0.9f, 0.9f, 1.25f));
            var heistThiefSpawnB = CreateMarker("Heist Thief Spawn B", new Vector3(0.35f, 0.9f, 1.45f));
            var heistThiefSpawnC = CreateMarker("Heist Thief Spawn C", new Vector3(1.55f, 0.9f, 1.05f));
            var heistPoliceSpawn = CreateMarker("Heist Police Spawn Point", new Vector3(-4.9f, 0.9f, 3.85f));
            var heistPoliceExit = CreateMarker("Heist Police Exit Point", new Vector3(-5.25f, 0.9f, 4.85f));
            var heistPlayerResetPoint = CreateMarker("Heist Player Reset Point", new Vector3(-2.35f, 0.9f, 3.15f));

            var systems = new GameObject("Gameplay Systems");
            var scoreManager = systems.AddComponent<ScoreManager>();
            var queueDirector = systems.AddComponent<CustomerQueueDirector>();
            var queueManager = systems.AddComponent<QueueManager>();
            var securitySystem = systems.AddComponent<SecuritySystem>();
            var thiefEventSystem = systems.AddComponent<ThiefEventSystem>();
            var questPoolDirector = systems.AddComponent<QuestPoolDirector>();
            var questSpawner = systems.AddComponent<QuestSpawner>();
            var staffInterruptionSystem = systems.AddComponent<StaffInterruptionSystem>();
            var cashDeliverySystem = systems.AddComponent<CashDeliverySystem>();
            var phoneInterruptionSystem = systems.AddComponent<PhoneInterruptionSystem>();
            var teaLadyBoostSystem = systems.AddComponent<TeaLadyBoostSystem>();
            var heistRaidSystem = systems.AddComponent<HeistRaidSystem>();
            var preGameShopManager = systems.AddComponent<PreGameShopManager>();
            var phoneAudioSource = systems.AddComponent<AudioSource>();
            var heistAudioSource = systems.AddComponent<AudioSource>();
            var tellerService = systems.AddComponent<TellerServiceController>();
            var timeManager = systems.AddComponent<TimeManager>();
            queueDirector.enabled = false;
            questPoolDirector.enabled = false;

            SetReference(queueDirector, "spawnPoint", spawnPoint);
            SetReference(queueDirector, "queueStartPoint", queueStartPoint);
            SetReference(queueDirector, "servicePoint", servicePoint);
            SetFloat(queueDirector, "arrivalIntervalSeconds", 4f);
            SetVector3(queueDirector, "queueDirection", Vector3.back);
            SetCustomerSequence(queueDirector, customerSequence);

            SetReference(tellerService, "queueDirector", queueDirector);
            SetReference(tellerService, "scoreManager", scoreManager);

            var guard = CreateSecurityGuard();
            var guardHome = CreateMarker("Security Guard Home", new Vector3(-4.5f, 0.9f, -2.8f));
            var securityExit = CreateMarker("Security Escort Exit", new Vector3(-5.2f, 0.9f, 3.8f));
            SetReference(securitySystem, "queueManager", queueManager);
            SetReference(securitySystem, "securityGuard", guard);
            SetReference(securitySystem, "guardHomePoint", guardHome);
            SetReference(securitySystem, "escortExitPoint", securityExit);

            var thiefSpawn = CreateMarker("Thief Spawn Point", new Vector3(-3.6f, 0.9f, 3.8f));
            var policeSpawn = CreateMarker("Police Spawn Point", new Vector3(-5.2f, 0.9f, -3.8f));
            var policeExit = CreateMarker("Police Exit Point", new Vector3(-5.2f, 0.9f, 4.2f));
            SetReference(thiefEventSystem, "queueManager", queueManager);
            SetReference(thiefEventSystem, "thiefSpawnPoint", thiefSpawn);
            SetReference(thiefEventSystem, "policeSpawnPoint", policeSpawn);
            SetReference(thiefEventSystem, "policeExitPoint", policeExit);
            SetBool(thiefEventSystem, "spawnWhenTimeCritical", false);

            SetReference(questPoolDirector, "queueManager", queueManager);
            SetReference(questPoolDirector, "thiefEventSystem", thiefEventSystem);
            SetInt(questPoolDirector, "currentDay", 1);
            SetInt(questPoolDirector, "maxQueueSize", 5);
            SetFloat(questPoolDirector, "baseArrivalIntervalSeconds", 6f);
            SetFloat(questPoolDirector, "criticalTimeThresholdSeconds", 15f);
            SetFloat(questPoolDirector, "criticalQuickWinWeightMultiplier", 3f);

            SetReference(questSpawner, "queueManager", queueManager);
            SetReference(questSpawner, "thiefEventSystem", thiefEventSystem);
            SetReference(questSpawner, "spawnPoint", spawnPoint);
            SetInt(questSpawner, "currentLevel", 1);
            SetInt(questSpawner, "maxQueueCapacity", 5);
            SetFloat(questSpawner, "criticalTimeThresholdSeconds", 20f);
            SetFloat(questSpawner, "quickWinWeightMultiplier", 2.5f);

            SetReference(staffInterruptionSystem, "queueManager", queueManager);
            SetReference(staffInterruptionSystem, "playerInteraction", playerInteraction);
            SetReference(staffInterruptionSystem, "player", player);
            SetReference(staffInterruptionSystem, "coworkerSpawnPoint", coworkerSpawnPoint);
            SetReference(staffInterruptionSystem, "counterInterruptionPoint", coworkerCounterPoint);
            SetReference(staffInterruptionSystem, "coworkerExitPoint", coworkerExitPoint);
            SetReference(staffInterruptionSystem, "archiveDeskZone", archiveDeskZone);
            SetReferenceArray(
                staffInterruptionSystem,
                "pausableCounterSystems",
                new Object[]
                {
                    player.GetComponent<BankingActionSystem>(),
                    player.GetComponent<DocumentProcessWorkflow>(),
                    player.GetComponent<GoldExchangeWorkflow>(),
                    player.GetComponent<FastTrackActionSystem>(),
                    player.GetComponent<UtilityBillSystem>(),
                    player.GetComponent<CardBlockMiniGame>()
                });

            SetReference(bankingActionSystem, "cashDeliverySystem", cashDeliverySystem);
            SetReference(cashDeliverySystem, "bankingActionSystem", bankingActionSystem);
            SetReference(cashDeliverySystem, "playerInteraction", playerInteraction);
            SetReference(cashDeliverySystem, "player", player);
            SetReference(cashDeliverySystem, "vaultRestockZone", cashVaultRestockZone);
            SetReference(cashDeliverySystem, "noCashWarningIcon", noCashWarningIcon);
            SetReference(cashDeliverySystem, "vehicleSpawnPoint", armoredVehicleSpawnPoint);
            SetReference(cashDeliverySystem, "vehicleParkingPoint", armoredVehicleParkingPoint);
            SetReference(cashDeliverySystem, "vehicleExitPoint", armoredVehicleExitPoint);
            SetReference(cashDeliverySystem, "vaultCashExplosionEffect", vaultCashExplosionEffect);
            SetReference(cashDeliverySystem, "topDownController", player.GetComponent<ChubbyTopDownInputController>());
            SetReference(cashDeliverySystem, "mobilePlayerController", player.GetComponent<MobilePlayerController>());
            SetInt(cashDeliverySystem, "maxVaultCash", 5);
            SetInt(cashDeliverySystem, "currentVaultCash", 5);
            SetReferenceArray(
                cashDeliverySystem,
                "pausableCounterSystems",
                new Object[]
                {
                    player.GetComponent<BankingActionSystem>(),
                    player.GetComponent<DocumentProcessWorkflow>(),
                    player.GetComponent<GoldExchangeWorkflow>(),
                    player.GetComponent<FastTrackActionSystem>(),
                    player.GetComponent<UtilityBillSystem>(),
                    player.GetComponent<CardBlockMiniGame>()
                });

            var lazyAssistant = CreateLazyAssistantPrototype(assistantCounterPoint, assistantBreakExit, queueManager);
            SetReference(lazyAssistant, "cashDeliverySystem", cashDeliverySystem);
            CreateVIPEscortPrototype(player);

            SetReference(phoneInterruptionSystem, "audioSource", phoneAudioSource);
            SetReference(teaLadyBoostSystem, "raycastCamera", Camera.main);
            SetReference(teaLadyBoostSystem, "teaLadySpawnPoint", teaLadySpawnPoint);
            SetReference(teaLadyBoostSystem, "teaLadyExitPoint", teaLadyExitPoint);
            SetReference(teaLadyBoostSystem, "teasideTable", teasideTable);
            SetReference(teaLadyBoostSystem, "cupPlacementPoint", cupPlacementPoint);
            SetReference(teaLadyBoostSystem, "mobilePlayerController", player.GetComponent<MobilePlayerController>());
            SetReference(teaLadyBoostSystem, "topDownController", player.GetComponent<ChubbyTopDownInputController>());
            SetReference(teaLadyBoostSystem, "fastTrackActionSystem", player.GetComponent<FastTrackActionSystem>());
            SetReference(teaLadyBoostSystem, "utilityBillSystem", player.GetComponent<UtilityBillSystem>());
            SetReference(teaLadyBoostSystem, "documentProcessWorkflow", player.GetComponent<DocumentProcessWorkflow>());
            SetReference(teaLadyBoostSystem, "goldExchangeWorkflow", player.GetComponent<GoldExchangeWorkflow>());

            SetReference(preGameShopManager, "timeManager", timeManager);
            SetReference(preGameShopManager, "mobilePlayerController", player.GetComponent<MobilePlayerController>());
            SetReference(preGameShopManager, "topDownController", player.GetComponent<ChubbyTopDownInputController>());
            SetBool(preGameShopManager, "applyBoostersOnStart", true);

            var utilityBillSystem = player.GetComponent<UtilityBillSystem>();
            SetReference(utilityBillSystem, "queueManager", queueManager);
            SetReference(utilityBillSystem, "barcodeScannerRenderer", barcodeScanner);
            SetReference(utilityBillSystem, "barcodeScannerLight", barcodeScannerLight);

            SetReference(heistRaidSystem, "cashDeliverySystem", cashDeliverySystem);
            SetReference(heistRaidSystem, "playerInteraction", playerInteraction);
            SetReference(heistRaidSystem, "player", player);
            SetReference(heistRaidSystem, "playerResetPoint", heistPlayerResetPoint);
            SetReference(heistRaidSystem, "mobilePlayerController", player.GetComponent<MobilePlayerController>());
            SetReference(heistRaidSystem, "topDownController", player.GetComponent<ChubbyTopDownInputController>());
            SetReference(heistRaidSystem, "bankEntranceTrigger", heistEntranceTrigger);
            SetReference(heistRaidSystem, "alarmButton", heistAlarmButton);
            SetReference(heistRaidSystem, "policeSpawnPoint", heistPoliceSpawn);
            SetReference(heistRaidSystem, "policeExitPoint", heistPoliceExit);
            SetReference(heistRaidSystem, "audioSource", heistAudioSource);
            SetReferenceArray(
                heistRaidSystem,
                "thiefSpawnPoints",
                new Object[] { heistThiefSpawnA, heistThiefSpawnB, heistThiefSpawnC });
            SetReference(cashDeliverySystem, "heistRaidSystem", heistRaidSystem);

            var hudObject = new GameObject("Game HUD");
            var document = hudObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(hudUxmlPath);
            var hudController = hudObject.AddComponent<GameHudUIController>();
            SetReference(hudController, "document", document);
            SetReference(hudController, "themeStyleSheet", AssetDatabase.LoadAssetAtPath<StyleSheet>(ThemeUssPath));
            SetReference(hudController, "scoreManager", scoreManager);
            SetReference(hudController, "tellerService", tellerService);
            CreateRuntimeUICanvas(timeManager, queueManager, lazyAssistant, cashDeliverySystem, phoneInterruptionSystem, teaLadyBoostSystem);

            SaveScene(scene, "Game");
        }

        private static Transform CreateCamera(Vector3 position, Quaternion rotation, Color backgroundColor)
        {
            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.transform.SetPositionAndRotation(position, rotation);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
            return camera.transform;
        }

        private static Transform CreatePlayerController(ParticleSystem goldSparkleEffect)
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player Controller Prototype";
            player.transform.position = new Vector3(0f, 1f, -3.2f);

            var body = player.AddComponent<Rigidbody>();
            body.mass = 1.2f;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var playerInput = player.AddComponent<PlayerInput>();
            var controller = player.AddComponent<ChubbyTopDownInputController>();
            SetReference(controller, "playerInput", playerInput);
            SetFloat(controller, "movementSpeed", 3.2f);
            SetFloat(controller, "rotationSpeed", 8f);
            SetFloat(controller, "accelerationRate", 4.5f);
            SetFloat(controller, "decelerationRate", 2.2f);

            var holdPoint = new GameObject("Hold Point");
            holdPoint.transform.SetParent(player.transform, false);
            holdPoint.transform.localPosition = new Vector3(0f, 0.35f, 0.95f);

            var triggerObject = new GameObject("Interaction Trigger");
            triggerObject.transform.SetParent(player.transform, false);
            triggerObject.transform.localPosition = new Vector3(0f, 0f, 1.05f);
            var trigger = triggerObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1.2f, 1.2f, 1.2f);

            var interaction = triggerObject.AddComponent<PlayerInteraction>();
            SetReference(interaction, "holdPoint", holdPoint.transform);
            SetReference(interaction, "throwDirectionSource", player.transform);

            var bankingActionSystem = player.AddComponent<BankingActionSystem>();
            SetReference(bankingActionSystem, "holdPoint", holdPoint.transform);

            var documentWorkflow = player.AddComponent<DocumentProcessWorkflow>();
            SetReference(documentWorkflow, "holdPoint", holdPoint.transform);

            var goldWorkflow = player.AddComponent<GoldExchangeWorkflow>();
            SetReference(goldWorkflow, "holdPoint", holdPoint.transform);
            SetReference(goldWorkflow, "goldSparkleEffect", goldSparkleEffect);

            var fastTrackActionSystem = player.AddComponent<FastTrackActionSystem>();
            SetReference(fastTrackActionSystem, "holdPoint", holdPoint.transform);

            var utilityBillSystem = player.AddComponent<UtilityBillSystem>();
            SetReference(utilityBillSystem, "holdPoint", holdPoint.transform);
            SetReference(utilityBillSystem, "audioSource", player.AddComponent<AudioSource>());

            var cardBlockMiniGame = player.AddComponent<CardBlockMiniGame>();
            SetReference(cardBlockMiniGame, "playerRoot", player.transform);
            SetReference(cardBlockMiniGame, "playerBody", body);
            return player.transform;
        }

        private static void CreateInteractablePrototypeObjects()
        {
            CreateInteractableCube(
                "Money Bag Prototype",
                new Vector3(-1.4f, 0.35f, -2.2f),
                new Vector3(0.55f, 0.55f, 0.55f),
                new Color(0.13f, 0.55f, 0.27f));
            CreateInteractableCube(
                "File Folder Prototype",
                new Vector3(1.2f, 0.2f, -2.1f),
                new Vector3(0.75f, 0.18f, 0.5f),
                new Color(0.94f, 0.68f, 0.22f));
        }

        private static void CreateInteractableCube(string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.tag = "Interactable";
            go.transform.position = position;
            go.transform.localScale = scale;

            var body = go.AddComponent<Rigidbody>();
            body.mass = 0.6f;
            var deliverable = go.AddComponent<DeliverableItem>();
            SetString(deliverable, "itemId", name.Contains("Money") ? "money_bag" : "file_folder");
            SetColor(deliverable, "itemColor", color);

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = color;
                renderer.sharedMaterial = material;
            }
        }

        private static void CreateDeliveryPoint()
        {
            var desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            desk.name = "Delivery Desk Prototype";
            desk.tag = "Counter";
            desk.transform.position = new Vector3(2.4f, 0.45f, -0.8f);
            desk.transform.localScale = new Vector3(1.4f, 0.9f, 1.1f);

            var renderer = desk.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.25f, 0.85f, 0.48f);
                renderer.sharedMaterial = material;
            }

            var triggerObject = new GameObject("Delivery Trigger");
            triggerObject.transform.SetParent(desk.transform, false);
            triggerObject.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            var trigger = triggerObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1.6f, 1.2f, 1.3f);

            var audioSource = desk.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            var deliveryPoint = triggerObject.AddComponent<DeliveryPoint>();
            SetString(deliveryPoint, "requiredItemId", "file_folder");
            SetFloat(deliveryPoint, "deliveryRewardTime", 5f);
            SetReference(deliveryPoint, "audioSource", audioSource);
            SetReference(deliveryPoint, "bounceTarget", desk.transform);
        }

        private static void CreateDocumentStations()
        {
            CreateStationCube(
                "Document Desk Prototype",
                "DocumentDesk",
                new Vector3(-2.7f, 0.45f, -3.0f),
                new Vector3(1.5f, 0.9f, 1.0f),
                new Color(0.35f, 0.55f, 0.95f));

            CreateStationCube(
                "Manager Desk Prototype",
                "ManagerDesk",
                new Vector3(4.0f, 0.45f, 1.6f),
                new Vector3(1.7f, 0.9f, 1.1f),
                new Color(0.65f, 0.42f, 0.95f));
        }

        private static Collider CreateArchiveDeskStation()
        {
            var desk = CreateStationCube(
                "Archive Desk Prototype",
                "ArchiveDesk",
                new Vector3(4.35f, 0.45f, -2.75f),
                new Vector3(1.45f, 0.9f, 1.05f),
                new Color(0.95f, 0.56f, 0.34f));

            var triggerObject = new GameObject("Archive Desk Delivery Zone");
            triggerObject.transform.SetParent(desk.transform, false);
            triggerObject.transform.localPosition = new Vector3(0f, 0.75f, 0f);

            var trigger = triggerObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1.7f, 1.35f, 1.3f);
            return trigger;
        }

        private static ParticleSystem CreateExpertiseStation()
        {
            var station = CreateStationCube(
                "Gold Expertise Station Prototype",
                "ExpertiseStation",
                new Vector3(-4.1f, 0.45f, 1.7f),
                new Vector3(1.6f, 0.9f, 1.1f),
                new Color(1f, 0.78f, 0.22f));

            var sparkleObject = new GameObject("Gold Sparkle Effect");
            sparkleObject.transform.SetParent(station.transform, false);
            sparkleObject.transform.localPosition = Vector3.up * 1.1f;
            var particles = sparkleObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startColor = new Color(1f, 0.82f, 0.18f);
            main.startLifetime = 0.55f;
            main.startSpeed = 1.2f;
            main.maxParticles = 24;
            particles.Stop();
            return particles;
        }

        private static void CreatePassbookPrinterStation()
        {
            CreateStationCube(
                "Passbook Printer Prototype",
                "PassbookPrinter",
                new Vector3(2.7f, 0.55f, -2.8f),
                new Vector3(0.9f, 1.1f, 0.8f),
                new Color(0.32f, 0.72f, 0.95f));
        }

        private static Renderer CreateBarcodeScannerStation(out Light scannerLight)
        {
            var scanner = CreateStationCube(
                "Barcode Scanner Prototype",
                "BarcodeScanner",
                new Vector3(1.65f, 0.72f, -2.15f),
                new Vector3(0.58f, 0.34f, 0.52f),
                new Color(0.14f, 0.16f, 0.18f));

            var labelObject = new GameObject("Barcode Scanner Label");
            labelObject.transform.SetParent(scanner.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            labelObject.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            var label = labelObject.AddComponent<TextMesh>();
            label.text = "SCAN";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.16f;
            label.fontStyle = FontStyle.Bold;
            label.color = new Color(0.85f, 0.95f, 1f);

            var lightObject = new GameObject("Barcode Scanner Feedback Light");
            lightObject.transform.SetParent(scanner.transform, false);
            lightObject.transform.localPosition = Vector3.up * 0.75f;
            scannerLight = lightObject.AddComponent<Light>();
            scannerLight.type = LightType.Point;
            scannerLight.range = 2.1f;
            scannerLight.intensity = 1.25f;
            scannerLight.color = new Color(1f, 0.84f, 0.08f);
            scannerLight.enabled = false;

            return scanner.GetComponent<Renderer>();
        }

        private static Collider CreateCashVaultStation(out GameObject noCashWarningIcon, out ParticleSystem cashExplosionEffect)
        {
            var vault = CreateStationCube(
                "Cash Vault Prototype",
                "CashRegister",
                new Vector3(-4.4f, 0.55f, -1.15f),
                new Vector3(1.2f, 1.1f, 1.2f),
                new Color(0.18f, 0.62f, 0.38f));

            var triggerObject = new GameObject("Cash Vault Restock Zone");
            triggerObject.transform.SetParent(vault.transform, false);
            triggerObject.transform.localPosition = new Vector3(0f, 0.75f, 0f);

            var trigger = triggerObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1.55f, 1.35f, 1.55f);

            noCashWarningIcon = new GameObject("No Cash Warning Icon");
            noCashWarningIcon.transform.SetParent(vault.transform, false);
            noCashWarningIcon.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            noCashWarningIcon.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var warningText = noCashWarningIcon.AddComponent<TextMesh>();
            warningText.text = "NO CASH";
            warningText.anchor = TextAnchor.MiddleCenter;
            warningText.alignment = TextAlignment.Center;
            warningText.characterSize = 0.22f;
            warningText.fontStyle = FontStyle.Bold;
            warningText.color = new Color(1f, 0.2f, 0.16f);
            noCashWarningIcon.SetActive(false);

            var effectObject = new GameObject("Vault Cash Explosion Effect");
            effectObject.transform.SetParent(vault.transform, false);
            effectObject.transform.localPosition = Vector3.up * 1.1f;
            cashExplosionEffect = effectObject.AddComponent<ParticleSystem>();
            var main = cashExplosionEffect.main;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.86f, 0.2f),
                new Color(0.18f, 0.72f, 0.32f));
            main.startLifetime = 0.8f;
            main.startSpeed = 2.5f;
            main.maxParticles = 72;
            cashExplosionEffect.Stop();
            return trigger;
        }

        private static void CreateSnackDrawerStation()
        {
            var drawer = CreateStationCube(
                "Snack Drawer Prototype",
                "SnackDrawer",
                new Vector3(0.85f, 0.28f, -1.42f),
                new Vector3(0.75f, 0.32f, 0.48f),
                new Color(0.62f, 0.36f, 0.18f));

            var labelObject = new GameObject("Snack Drawer Label");
            labelObject.transform.SetParent(drawer.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            labelObject.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            var label = labelObject.AddComponent<TextMesh>();
            label.text = "SNACK";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.14f;
            label.fontStyle = FontStyle.Bold;
            label.color = new Color(1f, 0.88f, 0.46f);

            var cookiePreview = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cookiePreview.name = "Drawer Cookie Preview";
            cookiePreview.transform.SetParent(drawer.transform, false);
            cookiePreview.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            cookiePreview.transform.localScale = new Vector3(0.22f, 0.08f, 0.22f);
            if (cookiePreview.TryGetComponent<Collider>(out var colliderComponent))
            {
                Object.DestroyImmediate(colliderComponent);
            }

            if (cookiePreview.TryGetComponent<Renderer>(out var rendererComponent))
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.76f, 0.42f, 0.16f);
                rendererComponent.sharedMaterial = material;
            }
        }

        private static Transform CreateTeasideTableStation(out Transform cupPlacementPoint)
        {
            var table = CreateStationCube(
                "Teaside Table Prototype",
                "TeasideTable",
                new Vector3(3.85f, 0.38f, -1.85f),
                new Vector3(0.95f, 0.76f, 0.85f),
                new Color(0.95f, 0.72f, 0.34f));

            var placement = new GameObject("Tea Cup Placement Point");
            placement.transform.SetParent(table.transform, false);
            placement.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            cupPlacementPoint = placement.transform;
            return table.transform;
        }

        private static Collider CreateHeistEntranceTrigger()
        {
            var triggerObject = new GameObject("Heist Raid Entrance Trigger");
            triggerObject.transform.position = new Vector3(-2.85f, 0.9f, 3.35f);

            var trigger = triggerObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2.35f, 1.8f, 1.45f);
            return trigger;
        }

        private static Transform CreateHeistAlarmButton()
        {
            var button = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            button.name = "Police Alarm Button Prototype";
            button.transform.position = new Vector3(0.85f, 0.92f, -1.08f);
            button.transform.localScale = new Vector3(0.28f, 0.12f, 0.28f);

            var renderer = button.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.92f, 0.12f, 0.12f);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(0.7f, 0.02f, 0.02f));
                renderer.sharedMaterial = material;
            }

            var labelObject = new GameObject("Alarm Label");
            labelObject.transform.SetParent(button.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            labelObject.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
            var label = labelObject.AddComponent<TextMesh>();
            label.text = "POLIS";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.18f;
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;

            return button.transform;
        }

        private static GameObject CreateStationCube(string name, string tagName, Vector3 position, Vector3 scale, Color color)
        {
            var station = GameObject.CreatePrimitive(PrimitiveType.Cube);
            station.name = name;
            station.tag = tagName;
            station.transform.position = position;
            station.transform.localScale = scale;

            var renderer = station.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = color;
                renderer.sharedMaterial = material;
            }

            return station;
        }

        private static LazyAssistantAI CreateLazyAssistantPrototype(
            Transform assistantCounterPoint,
            Transform assistantBreakExit,
            QueueManager queueManager)
        {
            var assistant = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            assistant.name = "Lazy Assistant Prototype";
            assistant.transform.position = assistantBreakExit != null
                ? assistantBreakExit.position
                : new Vector3(4.8f, 0.9f, -3.8f);
            assistant.transform.localScale = new Vector3(1.05f, 1.05f, 1.05f);

            var renderer = assistant.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.78f, 0.86f, 0.98f);
                renderer.sharedMaterial = material;
            }

            var coffeeMugHoldPoint = new GameObject("Coffee Mug Hold Point");
            coffeeMugHoldPoint.transform.SetParent(assistant.transform, false);
            coffeeMugHoldPoint.transform.localPosition = new Vector3(0.28f, 0.38f, 0.42f);

            var assistantAi = assistant.AddComponent<LazyAssistantAI>();
            SetReference(assistantAi, "secondaryCounter", assistantCounterPoint);
            SetReference(assistantAi, "breakExitPoint", assistantBreakExit);
            SetReference(assistantAi, "queueManager", queueManager);
            SetReference(assistantAi, "coffeeMugHoldPoint", coffeeMugHoldPoint.transform);
            SetFloat(assistantAi, "serveSpeedMultiplier", 0.5f);
            SetInt(assistantAi, "maxTasksBeforeBreak", 2);
            SetInt(assistantAi, "currentTasksBeforeBreak", 2);
            SetFloat(assistantAi, "snackSlowingPenalty", 1.2f);
            SetInt(assistantAi, "snackExtraTasks", 1);
            SetFloat(assistantAi, "withdrawalBaseSeconds", 5.5f);
            SetFloat(assistantAi, "passbookBaseSeconds", 4.5f);
            SetFloat(assistantAi, "complexTaskBaseSeconds", 8f);
            SetFloat(assistantAi, "reducedTimeBonus", 2f);
            return assistantAi;
        }

        private static void CreateVIPEscortPrototype(Transform player)
        {
            var vipCounterZone = CreateTriggerZone(
                "VIP Waiting Queue Zone",
                new Vector3(-1.45f, 0.65f, 1.35f),
                new Vector3(1.6f, 1.3f, 1.4f),
                new Color(0.72f, 0.52f, 0.95f, 0.18f));
            var safeDepositVaultZone = CreateTriggerZone(
                "VIP Manager Room Door Zone",
                new Vector3(3.35f, 0.75f, -1.85f),
                new Vector3(1.75f, 1.5f, 1.45f),
                new Color(0.95f, 0.78f, 0.26f, 0.18f));
            var bankExitZone = CreateTriggerZone(
                "Bank Exit Zone",
                new Vector3(-3.65f, 0.75f, 3.7f),
                new Vector3(1.8f, 1.5f, 1.4f),
                new Color(0.24f, 0.8f, 0.55f, 0.18f));

            var playerCounter = CreateMarker("VIP Triangle PlayerCounter", new Vector3(0f, 0.9f, -0.45f));
            var vipWaitingSpot = CreateMarker("VIP Triangle VIPWaitingSpot", new Vector3(-1.45f, 0.9f, 1.35f));
            var managerRoomEntrance = CreateMarker("VIP Triangle ManagerRoomEntrance", new Vector3(3.35f, 0.9f, -1.85f));
            var vaultInsidePoint = CreateMarker("VIP Manager Room Meeting Point", new Vector3(3.55f, 0.9f, -2.2f));
            var vaultExitPoint = CreateMarker("VIP Manager Room Exit Point", new Vector3(2.8f, 0.9f, -1.35f));

            var managerDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            managerDoor.name = "VIP Manager Room Closed Door Cue";
            managerDoor.transform.position = new Vector3(3.35f, 0.95f, -1.15f);
            managerDoor.transform.localScale = new Vector3(1.1f, 1.25f, 0.12f);
            var doorRenderer = managerDoor.GetComponent<Renderer>();
            if (doorRenderer != null)
            {
                var doorMaterial = new Material(Shader.Find("Standard"));
                doorMaterial.color = new Color(0.62f, 0.38f, 0.18f);
                doorRenderer.sharedMaterial = doorMaterial;
            }

            managerDoor.SetActive(false);

            var praiseBubbleAnchor = CreateMarker("Managerial Praise Bubble Anchor", new Vector3(3.35f, 1.55f, -1.15f));
            var praiseAudioSource = managerDoor.AddComponent<AudioSource>();
            praiseAudioSource.playOnAwake = false;

            var praiseEffectObject = new GameObject("Player Managerial Praise Effect");
            praiseEffectObject.transform.SetParent(player, false);
            praiseEffectObject.transform.localPosition = Vector3.up * 0.22f;
            var praiseEffect = praiseEffectObject.AddComponent<ParticleSystem>();
            var praiseMain = praiseEffect.main;
            praiseMain.loop = false;
            praiseMain.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.88f, 0.24f),
                new Color(1f, 0.62f, 0.08f));
            praiseMain.startLifetime = 0.55f;
            praiseMain.startSpeed = 1.1f;
            praiseMain.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            praiseMain.maxParticles = 42;
            var praiseEmission = praiseEffect.emission;
            praiseEmission.rateOverTime = 0f;
            praiseEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });
            var praiseShape = praiseEffect.shape;
            praiseShape.shapeType = ParticleSystemShapeType.Sphere;
            praiseShape.radius = 0.45f;
            praiseEffect.Stop();

            var vip = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            vip.name = "VIP Customer Prototype";
            vip.transform.position = new Vector3(-1.45f, 0.9f, 1.35f);
            vip.transform.localScale = new Vector3(1.15f, 1.1f, 1.15f);

            var renderer = vip.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.92f, 0.56f, 0.88f);
                renderer.sharedMaterial = material;
            }

            var vipSparkleObject = new GameObject("VIP Golden Sparkle Aura");
            vipSparkleObject.transform.SetParent(vip.transform, false);
            vipSparkleObject.transform.localPosition = Vector3.up * 1.35f;
            var vipSparkle = vipSparkleObject.AddComponent<ParticleSystem>();
            var sparkleMain = vipSparkle.main;
            sparkleMain.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.82f, 0.2f),
                new Color(1f, 1f, 0.72f));
            sparkleMain.startLifetime = 0.75f;
            sparkleMain.startSpeed = 0.45f;
            sparkleMain.maxParticles = 28;
            var sparkleEmission = vipSparkle.emission;
            sparkleEmission.rateOverTime = 12f;
            var sparkleShape = vipSparkle.shape;
            sparkleShape.shapeType = ParticleSystemShapeType.Circle;
            sparkleShape.radius = 0.45f;

            var vipCustomer = vip.AddComponent<VIPCustomer>();
            SetReference(vipCustomer, "sparkleAuraObject", vipSparkleObject);
            SetReference(vipCustomer, "sparkleAuraParticles", vipSparkle);
            vipCustomer.Initialize(CustomerAgeGroup.Middle, CustomerGender.Male, CustomerRequestKind.VipSafeRental, renderer != null ? renderer.sharedMaterial : null);

            var agent = vip.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.speed = 2.3f;
            agent.angularSpeed = 540f;
            agent.acceleration = 6f;
            agent.stoppingDistance = 1.5f;

            var confettiObject = new GameObject("VIP Confetti Effect");
            confettiObject.transform.position = bankExitZone.transform.position + Vector3.up * 1.2f;
            var confetti = confettiObject.AddComponent<ParticleSystem>();
            var main = confetti.main;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.72f, 0.24f),
                new Color(0.55f, 0.8f, 1f));
            main.startLifetime = 0.85f;
            main.startSpeed = 2.2f;
            main.maxParticles = 80;
            confetti.Stop();

            var escortSystem = vip.AddComponent<VIPEscortSystem>();
            SetReference(escortSystem, "player", player);
            SetReference(escortSystem, "vipCustomer", vip.transform);
            SetReference(escortSystem, "vipAgent", agent);
            SetReference(escortSystem, "mobilePlayerController", player.GetComponent<MobilePlayerController>());
            SetReference(escortSystem, "topDownInputController", player.GetComponent<ChubbyTopDownInputController>());
            SetReference(escortSystem, "playerCounter", playerCounter);
            SetReference(escortSystem, "vipWaitingSpot", vipWaitingSpot);
            SetReference(escortSystem, "managerRoomEntrance", managerRoomEntrance);
            SetReference(escortSystem, "vipWaitingSpotZone", vipCounterZone);
            SetReference(escortSystem, "managerRoomEntranceZone", safeDepositVaultZone);
            SetReference(escortSystem, "managerRoomInsidePoint", vaultInsidePoint);
            SetReference(escortSystem, "managerDoorVisual", managerDoor);
            SetReference(escortSystem, "praiseAudioSource", praiseAudioSource);
            SetReference(escortSystem, "praiseBubbleAnchor", praiseBubbleAnchor);
            SetReference(escortSystem, "playerPraiseEffect", praiseEffect);
            SetReference(escortSystem, "vipCounterZone", vipCounterZone);
            SetReference(escortSystem, "safeDepositVaultZone", safeDepositVaultZone);
            SetReference(escortSystem, "bankExitZone", bankExitZone);
            SetReference(escortSystem, "vaultInsidePoint", vaultInsidePoint);
            SetReference(escortSystem, "vaultExitPoint", vaultExitPoint);
            SetReference(escortSystem, "completionEffect", confetti);
            SetFloat(escortSystem, "temporaryPatienceDrainMultiplier", 0.5f);
            SetFloat(escortSystem, "reliefBoostSeconds", 6f);
            SetFloat(escortSystem, "playerSpeedBoostMultiplier", 1.2f);
            SetFloat(escortSystem, "playerSpeedBoostSeconds", 5f);
        }

        private static Collider CreateTriggerZone(string name, Vector3 position, Vector3 size, Color color)
        {
            var zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zone.name = name;
            zone.transform.position = position;
            zone.transform.localScale = size;

            var collider = zone.GetComponent<BoxCollider>();
            collider.isTrigger = true;

            var renderer = zone.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = color;
                renderer.sharedMaterial = material;
            }

            return collider;
        }

        private static Transform CreateSecurityGuard()
        {
            var guard = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            guard.name = "Security Guard Prototype";
            guard.transform.position = new Vector3(-4.5f, 0.9f, -2.8f);
            guard.transform.localScale = new Vector3(0.9f, 1.15f, 0.9f);

            var renderer = guard.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.12f, 0.24f, 0.55f);
                renderer.sharedMaterial = material;
            }

            return guard.transform;
        }

        private static void CreateRuntimeUICanvas(
            TimeManager timeManager,
            QueueManager queueManager,
            LazyAssistantAI lazyAssistant,
            CashDeliverySystem cashDeliverySystem,
            PhoneInterruptionSystem phoneInterruptionSystem,
            TeaLadyBoostSystem teaLadyBoostSystem)
        {
            var canvasObject = new GameObject("Runtime UI Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var sliderObject = new GameObject("Time Remaining Slider");
            sliderObject.transform.SetParent(canvasObject.transform, false);
            var slider = sliderObject.AddComponent<UnityEngine.UI.Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            var sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 1f);
            sliderRect.anchorMax = new Vector2(0.5f, 1f);
            sliderRect.pivot = new Vector2(0.5f, 1f);
            sliderRect.anchoredPosition = new Vector2(0f, -24f);
            sliderRect.sizeDelta = new Vector2(420f, 28f);

            var timerObject = new GameObject("Timer Text");
            timerObject.transform.SetParent(canvasObject.transform, false);

            var rect = timerObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -24f);
            rect.sizeDelta = new Vector2(220f, 64f);

            var text = timerObject.AddComponent<TextMeshProUGUI>();
            text.text = "01:00";
            text.fontSize = 40f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            SetReference(timeManager, "timerText", text);

            var lockObject = new GameObject("Freeze Lock Icon");
            lockObject.transform.SetParent(canvasObject.transform, false);
            var lockText = lockObject.AddComponent<UnityEngine.UI.Text>();
            lockText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            lockText.text = "LOCK";
            lockText.alignment = TextAnchor.MiddleCenter;
            lockText.fontSize = 24;
            lockText.fontStyle = FontStyle.Bold;
            lockText.color = new Color(0.35f, 0.75f, 1f);
            lockObject.SetActive(false);
            var lockRect = lockObject.GetComponent<RectTransform>();
            lockRect.anchorMin = new Vector2(0.5f, 1f);
            lockRect.anchorMax = new Vector2(0.5f, 1f);
            lockRect.pivot = new Vector2(0.5f, 1f);
            lockRect.anchoredPosition = new Vector2(118f, -32f);
            lockRect.sizeDelta = new Vector2(90f, 32f);

            SetReference(timeManager, "freezeLockIcon", lockObject);

            var bonusObject = new GameObject("Bonus Time Text");
            bonusObject.transform.SetParent(canvasObject.transform, false);
            var bonusText = bonusObject.AddComponent<UnityEngine.UI.Text>();
            bonusText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            bonusText.alignment = TextAnchor.MiddleCenter;
            bonusText.fontSize = 44;
            bonusText.fontStyle = FontStyle.Bold;
            bonusText.color = Color.green;
            bonusText.gameObject.SetActive(false);
            var bonusRect = bonusObject.GetComponent<RectTransform>();
            bonusRect.anchorMin = new Vector2(0.5f, 1f);
            bonusRect.anchorMax = new Vector2(0.5f, 1f);
            bonusRect.pivot = new Vector2(0.5f, 1f);
            bonusRect.anchoredPosition = new Vector2(0f, -104f);
            bonusRect.sizeDelta = new Vector2(220f, 60f);

            var callButtonObject = new GameObject("Call Customer Button");
            callButtonObject.transform.SetParent(canvasObject.transform, false);
            var callImage = callButtonObject.AddComponent<UnityEngine.UI.Image>();
            callImage.color = new Color(0.18f, 0.68f, 0.4f);
            var callButton = callButtonObject.AddComponent<UnityEngine.UI.Button>();
            var callRect = callButtonObject.GetComponent<RectTransform>();
            callRect.anchorMin = new Vector2(1f, 0f);
            callRect.anchorMax = new Vector2(1f, 0f);
            callRect.pivot = new Vector2(1f, 0f);
            callRect.anchoredPosition = new Vector2(-28f, 28f);
            callRect.sizeDelta = new Vector2(280f, 84f);

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(callButtonObject.transform, false);
            var label = labelObject.AddComponent<UnityEngine.UI.Text>();
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.text = "Call Customer";
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 30;
            label.color = Color.white;
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var uiManager = canvasObject.AddComponent<UIManager>();
            SetReference(uiManager, "queueManager", queueManager);
            SetReference(uiManager, "callCustomerButton", callButton);
            SetReference(uiManager, "timeRemainingSlider", slider);
            SetReference(uiManager, "bonusTimeText", bonusText);

            CreateAssistantSummonUI(canvasObject.transform, queueManager, lazyAssistant);
            CreateCashDeliveryRequestUI(canvasObject.transform, cashDeliverySystem);
            CreatePhoneInterruptionUI(canvasObject.transform, phoneInterruptionSystem, callButton);
            CreateTeaLadyBoostUI(canvasObject.transform, teaLadyBoostSystem);
        }

        private static void CreateTeaLadyBoostUI(Transform canvasTransform, TeaLadyBoostSystem teaLadyBoostSystem)
        {
            var overlayObject = new GameObject("KafeinMode Golden Overlay");
            overlayObject.transform.SetParent(canvasTransform, false);
            var overlayImage = overlayObject.AddComponent<UnityEngine.UI.Image>();
            overlayImage.color = new Color(1f, 0.74f, 0.24f, 0.16f);
            overlayImage.raycastTarget = false;
            var overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            var sliderObject = new GameObject("KafeinMode Timer Slider");
            sliderObject.transform.SetParent(canvasTransform, false);
            var slider = sliderObject.AddComponent<UnityEngine.UI.Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.interactable = false;
            var sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 1f);
            sliderRect.anchorMax = new Vector2(0.5f, 1f);
            sliderRect.pivot = new Vector2(0.5f, 1f);
            sliderRect.anchoredPosition = new Vector2(0f, -154f);
            sliderRect.sizeDelta = new Vector2(360f, 24f);

            var fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(sliderObject.transform, false);
            var fillImage = fillObject.AddComponent<UnityEngine.UI.Image>();
            fillImage.color = new Color(1f, 0.76f, 0.22f);
            fillImage.type = UnityEngine.UI.Image.Type.Filled;
            fillImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 1f;
            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(4f, 4f);
            fillRect.offsetMax = new Vector2(-4f, -4f);
            slider.fillRect = fillRect;

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(sliderObject.transform, false);
            var label = labelObject.AddComponent<UnityEngine.UI.Text>();
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.text = "KafeinMode";
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 18;
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            overlayObject.SetActive(false);
            sliderObject.SetActive(false);

            if (teaLadyBoostSystem != null)
            {
                SetReference(teaLadyBoostSystem, "boostOverlay", overlayObject);
                SetReference(teaLadyBoostSystem, "boostRemainingSlider", slider);
            }
        }

        private static void CreatePhoneInterruptionUI(
            Transform canvasTransform,
            PhoneInterruptionSystem phoneInterruptionSystem,
            UnityEngine.UI.Button callCustomerButton)
        {
            var root = new GameObject("Phone Notification");
            root.transform.SetParent(canvasTransform, false);
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(1f, 1f);
            rootRect.anchoredPosition = new Vector2(-28f, -124f);
            rootRect.sizeDelta = new Vector2(96f, 96f);

            var background = root.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.1f, 0.16f, 0.18f, 0.86f);

            var fillObject = new GameObject("Ring Timeout Fill");
            fillObject.transform.SetParent(root.transform, false);
            var fill = fillObject.AddComponent<UnityEngine.UI.Image>();
            fill.color = new Color(0.35f, 0.9f, 0.72f, 0.72f);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            fill.fillOrigin = 2;
            fill.fillClockwise = false;
            fill.fillAmount = 1f;
            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(7f, 7f);
            fillRect.offsetMax = new Vector2(-7f, -7f);

            var buttonObject = new GameObject("Answer Phone Button");
            buttonObject.transform.SetParent(root.transform, false);
            var buttonImage = buttonObject.AddComponent<UnityEngine.UI.Image>();
            buttonImage.color = new Color(0.18f, 0.52f, 0.82f, 0.96f);
            var button = buttonObject.AddComponent<UnityEngine.UI.Button>();
            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = Vector2.zero;
            buttonRect.sizeDelta = new Vector2(66f, 66f);

            var phoneLabelObject = new GameObject("Phone Icon");
            phoneLabelObject.transform.SetParent(buttonObject.transform, false);
            var phoneLabel = phoneLabelObject.AddComponent<UnityEngine.UI.Text>();
            phoneLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            phoneLabel.text = "TEL";
            phoneLabel.alignment = TextAnchor.MiddleCenter;
            phoneLabel.fontSize = 20;
            phoneLabel.fontStyle = FontStyle.Bold;
            phoneLabel.color = Color.white;
            var phoneLabelRect = phoneLabelObject.GetComponent<RectTransform>();
            phoneLabelRect.anchorMin = Vector2.zero;
            phoneLabelRect.anchorMax = Vector2.one;
            phoneLabelRect.offsetMin = Vector2.zero;
            phoneLabelRect.offsetMax = Vector2.zero;

            var rewardObject = new GameObject("Phone Reward Text");
            rewardObject.transform.SetParent(canvasTransform, false);
            var reward = rewardObject.AddComponent<UnityEngine.UI.Text>();
            reward.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            reward.text = "+8s (Perfect!)";
            reward.alignment = TextAnchor.MiddleCenter;
            reward.fontSize = 26;
            reward.fontStyle = FontStyle.Bold;
            reward.color = new Color(0.35f, 1f, 0.62f);
            var rewardRect = rewardObject.GetComponent<RectTransform>();
            rewardRect.anchorMin = new Vector2(1f, 1f);
            rewardRect.anchorMax = new Vector2(1f, 1f);
            rewardRect.pivot = new Vector2(1f, 1f);
            rewardRect.anchoredPosition = new Vector2(-132f, -134f);
            rewardRect.sizeDelta = new Vector2(220f, 42f);

            root.SetActive(false);
            rewardObject.SetActive(false);

            if (phoneInterruptionSystem != null)
            {
                SetReference(phoneInterruptionSystem, "notificationRoot", root);
                SetReference(phoneInterruptionSystem, "answerButton", button);
                SetReference(phoneInterruptionSystem, "ringTimeoutFill", fill);
                SetReference(phoneInterruptionSystem, "shakingPhoneIcon", phoneLabelRect);
                SetReference(phoneInterruptionSystem, "rewardFloatingText", reward);
                SetReferenceArray(
                    phoneInterruptionSystem,
                    "blockedControlsDuringCall",
                    new Object[] { callCustomerButton });
            }
        }

        private static void CreateCashDeliveryRequestUI(Transform canvasTransform, CashDeliverySystem cashDeliverySystem)
        {
            var buttonObject = new GameObject("Request Cash Delivery Button");
            buttonObject.transform.SetParent(canvasTransform, false);
            var image = buttonObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.18f, 0.52f, 0.82f);
            var button = buttonObject.AddComponent<UnityEngine.UI.Button>();

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 28f);
            rect.sizeDelta = new Vector2(430f, 76f);

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(buttonObject.transform, false);
            var label = labelObject.AddComponent<UnityEngine.UI.Text>();
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.text = "Request Cash Dispatch";
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 25;
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            buttonObject.SetActive(false);

            if (cashDeliverySystem != null)
            {
                SetReference(cashDeliverySystem, "requestCashButton", button);
                SetReference(cashDeliverySystem, "requestCashButtonRoot", buttonObject);
            }
        }

        private static void CreateAssistantSummonUI(
            Transform canvasTransform,
            QueueManager queueManager,
            LazyAssistantAI lazyAssistant)
        {
            var barObject = new GameObject("SummonBar");
            barObject.transform.SetParent(canvasTransform, false);
            var barBackground = barObject.AddComponent<UnityEngine.UI.Image>();
            barBackground.color = new Color(0.08f, 0.12f, 0.16f, 0.78f);
            var summonBar = barObject.AddComponent<UnityEngine.UI.Slider>();
            summonBar.minValue = 0f;
            summonBar.maxValue = 1f;
            summonBar.value = 0f;
            summonBar.interactable = false;
            summonBar.transition = UnityEngine.UI.Selectable.Transition.None;

            var barRect = barObject.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(0f, 0f);
            barRect.pivot = new Vector2(0f, 0f);
            barRect.anchoredPosition = new Vector2(28f, 28f);
            barRect.sizeDelta = new Vector2(320f, 30f);

            var fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(barObject.transform, false);
            var fillImage = fillObject.AddComponent<UnityEngine.UI.Image>();
            fillImage.color = new Color(0.95f, 0.64f, 0.18f);
            fillImage.type = UnityEngine.UI.Image.Type.Filled;
            fillImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 0f;

            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(4f, 4f);
            fillRect.offsetMax = new Vector2(-4f, -4f);

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(barObject.transform, false);
            var label = labelObject.AddComponent<UnityEngine.UI.Text>();
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.text = "Helper";
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 18;
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var summonButtonObject = new GameObject("Summon Helper Button");
            summonButtonObject.transform.SetParent(canvasTransform, false);
            var summonButtonImage = summonButtonObject.AddComponent<UnityEngine.UI.Image>();
            summonButtonImage.color = new Color(0.92f, 0.42f, 0.22f);
            var summonButton = summonButtonObject.AddComponent<UnityEngine.UI.Button>();
            summonButton.interactable = false;

            var summonButtonRect = summonButtonObject.GetComponent<RectTransform>();
            summonButtonRect.anchorMin = new Vector2(0f, 0f);
            summonButtonRect.anchorMax = new Vector2(0f, 0f);
            summonButtonRect.pivot = new Vector2(0f, 0f);
            summonButtonRect.anchoredPosition = new Vector2(28f, 68f);
            summonButtonRect.sizeDelta = new Vector2(320f, 68f);

            var buttonLabelObject = new GameObject("Label");
            buttonLabelObject.transform.SetParent(summonButtonObject.transform, false);
            var buttonLabel = buttonLabelObject.AddComponent<UnityEngine.UI.Text>();
            buttonLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            buttonLabel.text = "Summon Helper";
            buttonLabel.alignment = TextAnchor.MiddleCenter;
            buttonLabel.fontSize = 26;
            buttonLabel.fontStyle = FontStyle.Bold;
            buttonLabel.color = Color.white;

            var buttonLabelRect = buttonLabelObject.GetComponent<RectTransform>();
            buttonLabelRect.anchorMin = Vector2.zero;
            buttonLabelRect.anchorMax = Vector2.one;
            buttonLabelRect.offsetMin = Vector2.zero;
            buttonLabelRect.offsetMax = Vector2.zero;

            var assistantManager = canvasTransform.gameObject.AddComponent<AssistantManager>();
            SetReference(assistantManager, "queueManager", queueManager);
            SetReference(assistantManager, "lazyAssistant", lazyAssistant);
            SetReference(assistantManager, "summonBar", summonBar);
            SetReference(assistantManager, "summonBarFill", fillImage);
            SetReference(assistantManager, "summonHelperButton", summonButton);
        }

        private static void CreateDirectionalLight()
        {
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        }

        private static Transform CreateMarker(string name, Vector3 position)
        {
            var marker = new GameObject(name);
            marker.transform.position = position;
            return marker.transform;
        }

        private static void SaveScene(Scene scene, string sceneName)
        {
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/{sceneName}.unity");
        }

        private static void UpdateBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{ScenesFolder}/Boot.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Login.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/MainMenu.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Game.unity", true)
            };
        }

        private static void SetReference(Object target, string fieldName, Object value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(fieldName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetReferenceArray(Object target, string fieldName, Object[] values)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(fieldName);
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetFloat(Object target, string fieldName, float value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(fieldName).floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetInt(Object target, string fieldName, int value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(fieldName).intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetBool(Object target, string fieldName, bool value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(fieldName).boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetVector3(Object target, string fieldName, Vector3 value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(fieldName).vector3Value = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetString(Object target, string fieldName, string value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(fieldName).stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetColor(Object target, string fieldName, Color value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(fieldName).colorValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetCustomerSequence(CustomerQueueDirector queueDirector, CustomerDefinition[] customerSequence)
        {
            var serializedObject = new SerializedObject(queueDirector);
            var property = serializedObject.FindProperty("customerSequence");
            property.arraySize = customerSequence.Length;
            for (var i = 0; i < customerSequence.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = customerSequence[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(queueDirector);
        }
    }
}
