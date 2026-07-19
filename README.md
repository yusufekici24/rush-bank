# RushBank

RushBank, Unity 6 ile gelistirilen dikey ekranli mobil 3D banka subesi simulasyon oyunudur. Oyuncu banka gorevlisi olarak musteri sirasini, sureyi, kasa akisini, uzman masalarini, gunluk hedefleri ve sube icindeki beklenmedik krizleri ayni anda yonetir.

Proje PR akisi ile gelistirilir. `main` branch'ine dogrudan push yapilmaz. Degisiklikler feature branch uzerinde hazirlanir, GitHub'a push edilir ve Yusuf'un onayindan sonra merge edilir.

## Mevcut Durum

- Unity surumu: `6000.5.4f1`
- Hedef ekran: dikey mobil, `9:16`
- Render hedefi: URP paketi proje manifestine eklendi
- Input: Unity New Input System aktif
- Prototype sahneleri Unity icinden kurulabiliyor: `RushBank > Setup Prototype Scenes`
- `Game` sahnesi Play Mode'da aciliyor
- UI gorunuyor, karakter hareket ediyor, Console'da kirmizi hata kalmadi
- Dikey sahne kompozisyonu baslatildi: alt bolgede oyuncu/gise, orta bolgede bekleme alani, yanlarda uzman masalari, ustte giris ve guvenlik

## Hizli Baslangic

1. Unity Hub ile projeyi ac.
2. Unity surumunun `6000.5.4f1` oldugundan emin ol.
3. Console'da kirmizi hata olmadigini kontrol et.
4. Ust menuden `RushBank > Setup Prototype Scenes` calistir.
5. `Assets/Scenes/Game.unity` sahnesini ac.
6. Game View aspect ayarini `9:16` yap.
7. Play'e bas.

> Not: APK almak icin Unity kurulumunda `Android Build Support`, `Android SDK & NDK Tools` ve `OpenJDK` modulleri gerekir. Sahneleme ve editor testleri icin bu moduller zorunlu degildir.

## Unity Ayarlari

Editor icinde onerilen ayarlar:

```text
Edit > Project Settings > Editor > Asset Serialization > Mode > Force Text
Edit > Project Settings > Editor > Version Control > Mode > Visible Meta Files
```

Dikey mobil ayar icin:

```text
RushBank > Configure Android Portrait Settings
```

Bu arac:

- uygulama adini `RushBank` yapar
- package name'i `com.yusufekici.rushbank` yapar
- portrait orientation ayarini uygular
- landscape yonleri kapatir
- Android module yoksa kirmizi hata basmadan bilgi verir

## Prototype Sahne Yapisi

`RushBank > Setup Prototype Scenes` araci asagidaki sahneleri olusturur veya gunceller:

- `Assets/Scenes/Boot.unity`
- `Assets/Scenes/Login.unity`
- `Assets/Scenes/MainMenu.unity`
- `Assets/Scenes/Game.unity`

`Game` sahnesinde otomatik kurulan ana alanlar:

- alt bolgede oyuncu ve ana gise
- orta bolgede dikey bekleme banklari ve bekleme sehpasi
- solda iki uzman masa alani
- sagda iki uzman masa alani
- ust bolgede sube giris kapisi
- giris kapisi yaninda guvenlik gorevlisi konumu
- kasa, barkod okuyucu, yazici, dokuman masasi, mudur/uzman masalari ve prototip teslim noktalar

## Gorsel Yon

Hedef stil sicak, okunabilir ve tontis bir low-poly banka subesi gorunumudur.

- Renkler ilk parlak prototipten daha mat ve tasra subesi havasina cekildi.
- Kamera dikey mobil kadraj icin orthographic/isometric olarak ayarlandi.
- UI butonlari mobil dokunma icin buyutuldu.
- Ana oynama alani alt bolgeye alindi; orta/ust bolgede musteri akisi ve yan masa olaylari izlenebilir.
- Prosedurel gorsel sistem dis model gerektirmeden ilk sahne gorunumunu olusturur.

Ana gorsel dosyalar:

- `Assets/Scripts/Art/RushBankArtLibrary.cs`
- `Assets/Scripts/Art/ChubbyCharacterDresser.cs`
- `Assets/Scripts/Gameplay/PrototypeBankEnvironmentBuilder.cs`
- `Assets/UI/Styles/RushBankTheme.uss`

## Core Oyun Dongusu

1. Musteri subeye girer.
2. Musteri siraya veya bekleme alanina katilir.
3. Oyuncu `Call Customer` ile musteriyi giseye cagirir.
4. Musteri istegi ikon ve gorev tipiyle gosterilir.
5. Oyuncu gerekli istasyon, masa veya mini-game akisini tamamlar.
6. Basarili islem sure, skor, combo ve altin kazandirir.
7. Musteri ayrilir, sira kayar ve yeni musteri cagrilir.
8. Sure biterse oyun sonu tetiklenir.

## Ana Sistemler

### Core

- `Bootstrap`: baslangic akisi ve FPS hedefi.
- `GameManager`: oyun durumu, secili level ve pending booster bilgisi.
- `GameSettingsManager`: sube ayarlari ve tutorial unlock bilgisi.
- `SceneLoader`: sahne gecisleri.
- `TimeManager`: geri sayim, sure ekleme/cikarma, game over ve time freeze.
- `ScoreManager`: skor, combo, multiplier ve altin akisi.

### Oyuncu

- `MobilePlayerController`: joystick hareketi, action/grab/deposit ve HoldPoint.
- `ChubbyTopDownInputController`: New Input System ile Rigidbody tabanli hareket.
- `ChubbyRigidbodyCharacterController`: inertia ve tontis hareket hissi.
- `ScreenJoystick`: mobil sanal joystick.
- `PlayerInteraction`: nesne alma, tutma ve firlatma.

### Musteri ve Sira

- `QueueManager`: kuyruk, gise cagrisi, servis, redirect ve incident akislari.
- `QueueCustomer`: musteri kimligi, request icon ve sabir bilgisi.
- `CustomerPatience`: Calm, Grumpy, Raging ve yas bazli sabir carpani.
- `QuestSpawner`: weighted musteri/gorev spawn sistemi.
- `QuestPoolDirector`: gorev havuzu ve kritik sure quick-win pacing.
- `DynamicWeatherSystem`: Sunny/Rainy dongusu ve yagmurda hafif sabir baskisi.

## Islem Sistemleri

Hizli islemler:

- `FastTrackActionSystem`
- `UtilityBillSystem`
- `CardBlockMiniGame`
- `MobileActivationMiniGame`
- `WireTransferMiniGame`

Orta ve uzun islemler:

- `BankingActionSystem`
- `CashDeliverySystem`
- `DocumentProcessWorkflow`
- `GoldExchangeWorkflow`
- `VIPEscortSystem`

Yonlendirme sistemleri:

- `AccountOpeningSystem`
- `InsuranceReferralSystem`
- `CreditApplicationSystem`
- `RedAlertRedirectionSystem`
- `StationeryDeliverySystem`

Kriz ve destek sistemleri:

- `CounterIncidentManager`
- `SecurityGuardAI`
- `ScammerDetectionSystem`
- `ThiefEventSystem`
- `HeistRaidSystem`
- `PhoneInterruptionSystem`
- `TwoTierPhoneCallSystem`
- `StaffInterruptionSystem`
- `StaffRequestUrgency`
- `ManagerITSupportEvent`
- `ManagerSatisfactionSystem`
- `BankCatChaosSystem`
- `TeaLadyBoostSystem`
- `TeaLadyRefillEvent`
- `LazyAssistantAI`
- `AssistantManager`
- `WetFloorAccidentSystem`

Meta-game:

- `PreGameShopManager`
- `PreLevelPopupController`
- `QuestAndAchievementManager`
- `LevelDifficultyManager`
- `TutorialManager`

## Klasor Yapisi

```text
Assets/
  Data/          ScriptableObject prototip verileri
  Editor/        RushBank editor araclari
  Prefabs/       Prototype transaction prefab'lari
  Scenes/        Boot, Login, MainMenu, Game
  Scripts/
    Art/         Prosedurel gorsel katman
    Core/        Baslangic, sahne, sure ve genel ayarlar
    Gameplay/    Musteri, islem, kriz, ekonomi ve level sistemleri
    UI/          Menu, HUD, joystick ve popup scriptleri
  UI/
    Settings/    UI Toolkit panel ayarlari
    Styles/      USS tema dosyalari
    UXML/        UI Toolkit ekranlari
Packages/        Unity paket bagimliliklari
ProjectSettings/ Unity proje ayarlari
docs/            Urun, backlog ve PR notlari
tools/           Yerel Unity acilis yardimci scriptleri
```

## Git Akisi

Yeni isler feature branch uzerinde yapilir:

```bash
git checkout main
git pull origin main
git checkout -b agent/ozellik-adi
```

Is tamamlaninca:

```bash
git status
git add <ilgili-dosyalar>
git commit -m "Kisa ve aciklayici commit mesaji"
git push -u origin agent/ozellik-adi
```

Sonra GitHub uzerinden PR acilir.

## Dogrulama

Bu son sahneleme turunda Unity icinde:

- `RushBank > Setup Prototype Scenes` calisti
- `Game` sahnesi olustu
- Game View `9:16` olarak test edildi
- Play Mode goruntu verdi
- UI gorundu
- karakter hareket etti
- Console'da kirmizi hata kalmadi

Bir sonraki hedef, ayni portre sahne uzerinden gorsel polish ve temel musteri akisini oynanabilir MVP seviyesine cekmektir.
