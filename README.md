# RushBank

RushBank, Unity 6 ile gelistirilen mobil odakli 3D banka subesi simulasyon oyunudur. Oyuncu banka gorevlisi olarak musteri sirasini, sureyi, kasa akisini, uzman masalarini, gunluk hedefleri ve sube icindeki beklenmedik krizleri ayni anda yonetir.

Proje PR akisi ile gelistirilir. `main` branch'ine dogrudan push yapilmaz. Degisiklikler feature branch uzerinde hazirlanir, GitHub'a push edilir ve Yusuf'un onayindan sonra merge edilir.

## Mevcut Durum

- Unity surumu: `6000.0.23f1`
- Aktif branch: `feature/playable-prototype-setup`
- Temel Unity klasorleri hazir: `Assets`, `Packages`, `ProjectSettings`
- Prototype setup araci hazir: `RushBank > Setup Prototype Scenes`
- Core gameplay, kriz, meta-game, uzman masa ve gunluk gorev sistemleri script seviyesinde hazir
- Bu ortamda Unity Editor acilamadigi icin compile/play mode dogrulamasi Unity Hub uzerinden yapilmalidir

## Oyun Ozeti

Oyuncu, tontis ve kaotik bir banka subesinde musteri taleplerini tamamlayarak sure, skor, combo ve altin kazanir. Hizli islemler oyuncuya nefes aldirir; uzun islemler, krizler, destek sistemleri ve gunluk gorevler mikro-yonetim kararlarini derinlestirir.

Ana hedefler:

- Mobilde okunabilir, hizli ve sevimli 3D sube deneyimi
- Kisa sureli quick-win islemler ile rahatlama ani yaratmak
- Uzun ve riskli cok istasyonlu islemler ile karar baskisi kurmak
- Sabir, sure, kasa, altin, musteri sirasini ve uzman masalarini ayni anda yonetmek
- Seviye oncesi booster, sube secimi, gunluk gorev ve achievement sistemleri ile tekrar oynanabilirligi artirmak

## Ana Oyun Dongusu

1. Musteri subeye girer ve siraya katilir.
2. Oyuncu siradaki musteriyi gise alanina cagirir.
3. Musterinin istegi ikonla ve gorev tipiyle gosterilir.
4. Oyuncu gerekli istasyonlara, uzman masalarina veya mini-game arayuzlerine gider.
5. Basarili teslimde sure, skor, combo ve altin kazanilir.
6. Gunluk gorev veya achievement ilerlemesi varsa ek odul/boost tetiklenir.
7. Musteri ayrilir, sira kayar ve yeni musteri cagrilir.
8. Sure biterse oyun sonu tetiklenir.

## Onboarding ve Sube Akisi

### Egitim Subesi

`TutorialManager`, ilk oyuncu deneyimi icin "Soft Opening" egitim subesini yonetir.

- `MoveToCounter`: oyuncu glowing ring / pointer ile giseye yurur.
- `SimpleTransaction`: tek musteri ile Passbook Update / Hesap Cuzdani benzeri basit islem ogretilir.
- `TwoStepTransaction`: elektrik faturasi alinir, `BarcodeScanner` ile okutulur ve musteriye teslim edilir.
- `Completed`: mudur tebrik mesaji gosterilir, `TutorialCompleted` ve `Branch_Tasra_Unlocked` kaydedilir.

Egitimde `TimeManager` geri sayimi durur ve musteri sabri donuktur.

### Sube Secimi

`LevelDifficultyManager` ve `GameSettingsManager`, sube secimini ve zorluk ayarlarini yonetir.

| Sube | Zorluk | Sabir carpani | Musteri araligi | Hirsiz/Raid sansi | Hedef altin |
| --- | --- | ---: | ---: | ---: | ---: |
| Tasra Subesi | Easy | 1.0x | 15s | 0.02 | 120 |
| Sehir Subesi | Medium | 1.5x | 8s | 0.10 | 240 |
| Metropol Subesi | Hard | 2.2x | 4s | 0.25 | 420 |

Secilen sube `PlayerPrefs` ile saklanir. Oyun sahnesi yuklenince spawn araligi, sabir baskisi, hirsiz/raid sansi ve hedef altin otomatik olceklenir.

## Core Sistemler

### Core

- `Bootstrap`: hedef FPS ve baslangic akisini kurar.
- `GameManager`: oyun durumunu, secili level bilgisini ve pending booster durumlarini tutar.
- `GameSettingsManager`: secili sube ayarlarini ve tutorial unlock bilgisini saklar.
- `SceneLoader`: sahne gecislerini merkezi yapar.
- `TimeManager`: geri sayim, sure ekleme/cikarma, game over ve time freeze destegi verir.
- `ScoreManager`: skor, combo, multiplier, run gold ve global gold multiplier akisini yonetir.

### Oyuncu ve Kontroller

- `MobilePlayerController`: joystick input, Grab/Deposit/Action akisi ve HoldPoint tasima.
- `ChubbyTopDownInputController`: New Input System ile Rigidbody tabanli top-down hareket.
- `ChubbyRigidbodyCharacterController`: fizik tabanli, hafif kaygan ve tontis hareket hissi.
- `ScreenJoystick`: mobil sanal joystick.
- `PlayerInteraction`: nesne alma, elde tutma ve firlatma.
- `DeliveryPoint`: dogru objeyi teslim etme, sure odulu ve feedback.

### Musteri ve Sira

- `QueueManager`: musteri kuyrugu, bekleme alanlari, gise cagrisi, redirect ve incident akislari.
- `QueueCustomer`: musteri kimligi, yas/cinsiyet, request icon ve sabir bari.
- `CustomerPatience`: Calm, Grumpy, Raging durumlari ve yas bazli sabir carpani.
- `QuestSpawner`: seviye/gun bazli gorev havuzu, weighted spawn ve dynamic pacing.
- `QuestPoolDirector`: gorev agirliklari, kritik sure quick-win agirligi ve spawn interval boostlari.
- `CustomerQueueDirector`: eski/prototype musteri akisi icin destekleyici sistem.

## Islem Sistemleri

### Hizli Islemler

- `FastTrackActionSystem`: Passbook Printing ve Card Activation gibi kisa isler.
- `UtilityBillSystem`: elektrik, su ve telefon fatura odeme akisi.
- `CardBlockMiniGame`: 3 renkli kart blokesi mini-game'i.
- `MobileActivationMiniGame`: SMS aktivasyonu, 4 haneli kod girisi ve Digital Boost.
- `WireTransferMiniGame`: alfanumerik transfer kodu, sanal klavye ve Perfect Transfer gold boost.

### Orta ve Uzun Islemler

- `BankingActionSystem`: Withdraw, Deposit ve CurrencyExchange.
- `CashDeliverySystem`: kasa kapasitesi, merkezden nakit talebi, zirhli arac ve Super Cash Bag.
- `DocumentProcessWorkflow`: kredi/kart basvurusu, imza, mudur onayi ve teslim.
- `GoldExchangeWorkflow`: altin ekspertizi, deger makbuzu ve teslim.
- `VIPEscortSystem`: VIP musteriyi mudur odasina goturen ucgen rota, relief ve praise boost.
- `VIPCustomer`: VIP gorsel/tempo farklari ve hizli sabir baskisi.

### Yonlendirme ve Uzman Masalari

- `AccountOpeningSystem`: hesap acilis musterisine 0.5 sn stamp atar, Relationship Manager Desk'e yonlendirir ve 2 charge Quick Boost verir.
- `InsuranceReferralSystem`: sigorta musterisini Insurance Specialist Desk'e yonlendirir ve 12 sn Teamwork Speed Boost verir.
- `CreditApplicationSystem`: konut/tasit/ihtiyac kredisi sorgular, 80% onay / 20% red sonucu uretir, Credit Specialist Desk'e yonlendirir ve Credit Boost verir.
- `RedAlertRedirectionSystem`: sabri cok dusuk BarutCustomer'i onceliklendirir, acil sevk eder, +200 Gold ve VIP Relief verir.
- `StationeryDeliverySystem`: Relationship, Insurance ve Credit masalarina A4/Pen/Stapler destegi tasitir; sonraki yonlendirmelere masa bazli efficiency charge verir.

### Veri Modeli

- `BankTransaction`: islem adi, sure odulu, request icon, gerekli item prefab'i ve zorluk/akis verisi.
- `CustomerRequestDefinition`: musteri istegi tanimi.
- `CustomerDefinition`: musteri profili ve olasi istekler.

## Kriz, Destek ve Sube Ici Eventler

- `ThiefEventSystem`: hirsiz musteri, Call Police butonu ve time freeze.
- `HeistRaidSystem`: nakit teslim donusunde nadir stealth soygun baskini.
- `CounterIncidentManager`: aktif gise sabri bitince meltdown, -100 Gold, security escort ve Panic Attack debuff.
- `SecurityGuardAI`: kizgin musteri veya dolandirici gibi hedefleri disari goturen guvenlik akisi.
- `ScammerDetectionSystem`: sahte evrak/foto/tarih kontrolu, decline/security/approve sonuclari.
- `PhoneInterruptionSystem`: 30-45 saniyelik telefon refleks eventi ve sure carpani.
- `StaffInterruptionSystem`: is arkadasi kesintisi ve ArchiveDesk evrak teslimi.
- `ManagerITSupportEvent`: mudurun bilgisayar arizasi, mini-game tamiri, +150 Gold ve Manager Grace Boost.
- `ManagerSatisfactionSystem`: mudur memnuniyet bari, Staff Feast, sabir freeze, hiz ve islem boostlari.
- `BankCatChaosSystem`: Pati kedisi, kuyruk sakinlestirme, panik, guvenlik kovalamacasi.
- `TeaLadyBoostSystem`: cay/kahve power-up, KafeinMode, Drink/Serve secimi ve Tea Hospitality.
- `TeaLadySystem`: cayci teyze teslimati icin alternatif/legacy destek.
- `LazyAssistantAI`: ikinci gise icin yavas ama otomatik yardimci.
- `AssistantManager`: kuyruk grumpiness seviyesine gore dolan SummonBar.
- `SecuritySystem`: en dusuk sabirli musteriyi sakinlestirme veya disari cikarama.
- `CharityDonationSystem`: hayvan, doga, cocuk ve saglik bagisi ayristirma; Karma Boost.

## Gunluk Gorevler ve Achievement

`QuestAndAchievementManager`, oyuncunun zaten yaptigi aksiyonlari gunluk hedeflere ve uzun vadeli basarimlara baglar.

Gunluk gorev ornekleri:

| Gorev | Hedef | Odul | Ek etki |
| --- | ---: | ---: | --- |
| Gune Tontis Basla | 3 musteri memnun yolla | +50 Gold | Kisa sureli sabir rahatlamasi |
| Sicak Ikram | 1 kez cay dagit | +75 Gold | Musteri sakinlesme etkisi |
| Mudurun Gozdesi | 1 IT tamiri | +100 Gold | Gecici oyuncu hizi |
| Sifreyi Coz | 2 SMS/transfer sifresi | +80 Gold | Spawn araligi rahatlatma |
| Dogru Adres | 2 kredi sevki | +120 Gold | Gecici islem hizi |
| Pati Sevgisi | 1 kez kedi icin guvenlik cagir | +60 Gold | Kisa sureli sabir rahatlamasi |

Achievement ornekleri:

- `Dedektif Biyigi`: 10 dolandirici yakala.
- `Yildirim Parmaklar`: 15 perfect typing islemi yap.
- `Kriz Savar`: 10 BarutCustomer'i patlamadan sevk et.
- `Lahmacun Selalesi`: 5 kez Staff Feast tetikle.
- `Kirtasiye Kuryesi`: 15 kirtasiye teslimi yap.
- `Hayirsever Sube`: 20 bagis islemini dogru tamamla.
- `Yilin Memuru`: 100 musteriyi basariyla servis et.

Tamamlanan gorev ve basarimlar `PlayerPrefs` ile saklanir, altin odulu verir, sag ustten kayan bildirim banner'i oynatir ve gecici/kalici passive boost uygulayabilir.

## Meta-Game ve Booster Ekonomisi

`PreGameShopManager`, oyuncunun run oncesi booster alisverisini yonetir.

- `PlayerGold`: islemlerden kazanilan soft currency.
- `Booster_TimeSlow_Qty`: sure akisini 0.85x yapan booster.
- `Booster_Speed_Qty`: oyuncu hizini 1.2x yapan booster.
- `Booster_Patience_Qty`: global sabir dususunu 0.75x yapan booster.

`PreLevelPopupController`, seviye baslamadan once booster satis popup'ini acar.

- Hizli satin al ve kusan akisi
- En fazla 2 aktif booster secimi
- Anti-Grumpiness kartinda `BestSellerRibbon`
- `BuyBundleButton` ile 3'lu "Tontis Paket"
- Bundle satin alinca konfeti ve cash register sesi

## UI

UI Toolkit ve Unity UI birlikte kullaniliyor.

- `MainMenu.uxml`: giris, senaryo ve ayarlar ekranlari.
- `GameHud.uxml`: skor, combo, multiplier, aktif musteri ve islem paneli.
- `RushBankTheme.uss`: ortak gorsel tema.
- `MainMenuUIController`: menu, ayarlar ve level start akisi.
- `GameHudUIController`: oyun ici sayaclar ve butonlar.
- `UIManager`: Call Customer cooldown, request icon, time slider ve +s sure feedback'i.
- `AppSettings`: ses ve titresim ayarlarini `PlayerPrefs` ile saklar.
- `BestSellerRibbonFloat`: populer booster kurdelesi icin bobbing animasyonu.

## Gorsel Stil

Hedef stil: sicak, renkli, low-poly / stylized, "chubby toon" banka subesi.

- Tontis karakterler
- Yuvarlatilmis mobilyalar
- Pastel ve sicak renkler
- URP mobil hedefli toon shader
- `docs/CHUBBY_TOON_SHADER.md` icinde Chubby Toon shader rehberi
- `Assets/Shaders/` altinda shader prototipleri

## Prototype Setup

Unity Editor icinde su arac calistirilir:

```text
RushBank > Setup Prototype Scenes
```

Bu arac sahneleri, ornek verileri ve prototip objeleri olusturur/gunceller:

- `Assets/Scenes/Boot.unity`
- `Assets/Scenes/Login.unity`
- `Assets/Scenes/MainMenu.unity`
- `Assets/Scenes/Game.unity`
- Build Settings sahne sirasi
- Ana menu ve HUD UI
- Basit 3D banka ortami
- Player controller ve joystick altyapisi
- Queue, Time, Score, Quest, Banking ve Cash sistemleri
- FastTrack, UtilityBill, VIP, Phone, TeaLady, Heist, Assistant ve Quest/Achievement prototipleri
- Pre-level booster popup ve bundle satis UI

Kurulumdan sonra `Boot` sahnesi acilip Play'e basilabilir. Hizli kontrol icin dogrudan `Game` sahnesi de acilabilir.

## Klasor Yapisi

```text
Assets/
  Editor/        Prototype setup editor araci
  Prefabs/       Tekrar kullanilabilir prefab'lar
  Scenes/        Unity sahneleri
  Scripts/
    Core/        Baslangic, sahne, sure ve genel ayarlar
    Gameplay/    Musteri, islem, kriz, ekonomi ve level sistemleri
    UI/          Menu, HUD, joystick ve popup scriptleri
  Shaders/       URP toon shader prototipleri
  UI/
    Styles/      UI tema dosyalari
    UXML/        UI Toolkit ekranlari
Packages/        Unity paket bagimliliklari
ProjectSettings/ Unity proje ayarlari
docs/            Product brief, backlog ve teknik notlar
```

## Kurulum

1. Unity Hub kur.
2. Unity 6 `6000.0.23f1` veya uyumlu `6000.0.x` surumu kur.
3. Repoyu klonla:

   ```bash
   git clone https://github.com/yusufekici24/rush-bank.git
   ```

4. Unity Hub uzerinden repo klasorunu ac.
5. Unity Editor'de `RushBank > Setup Prototype Scenes` aracini calistir.
6. `Boot` sahnesinden Play'e bas.

## Unity Ayarlari

Merge sorunlarini azaltmak icin:

```text
Edit > Project Settings > Editor > Asset Serialization > Mode > Force Text
Edit > Project Settings > Editor > Version Control > Mode > Visible Meta Files
```

`.meta` dosyalari commit edilmelidir. `Library/`, `Temp/`, `Logs/`, `obj/`, `Build/` gibi uretilen klasorler commit edilmez.

## Gelistirme Akisi

Her is feature branch uzerinde yapilir:

```bash
git checkout main
git pull origin main
git checkout -b feature/ozellik-adi
```

Is tamamlaninca:

```bash
git status
git add .
git commit -m "Kisa ve aciklayici commit mesaji"
git push -u origin feature/ozellik-adi
```

Ardindan GitHub uzerinden PR acilir. PR Yusuf tarafindan incelenir ve onaylandiktan sonra merge edilir.

## PR Ozeti

Bu branch, RushBank'in script seviyesindeki playable prototype temelini genisletir:

- Musteri sira, sabir, spawn ve zorluk akisi
- Hizli, orta ve cok istasyonlu bankacilik islemleri
- Uzman masasi yonlendirme sistemleri
- Kasa ve nakit teslim dongusu
- Kriz ve destek eventleri
- Pre-run booster ekonomisi
- Gunluk gorev ve achievement sistemi
- UI/HUD/menu/popup temelleri
- Chubby toon gorsel stil dokumantasyonu

## Dogrulama Notu

Bu ortamda Unity Editor calistirilmadigi icin scriptlerin Unity compile/play dogrulamasi yapilmamis olabilir. Kod degisikliklerinden sonra Unity Editor'de:

- Console compile hatalari
- `RushBank > Setup Prototype Scenes`
- `Boot` sahnesi Play testi
- Android build ayarlari

kontrol edilmelidir.
