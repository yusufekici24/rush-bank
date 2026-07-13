# RushBank

RushBank, Unity 6 LTS ile geliştirilen 3D mobil banka simülasyonu oyunudur. Oyuncu banka görevlisi rolündedir; müşteriler sırayla şubeye gelir, farklı işlem istekleri oluşturur ve oyuncu bu işlemleri hızlı tamamlayarak puan, combo ve multiplier kazanır.

Proje PR akışıyla geliştiriliyor. `main` branch'ine direkt push yapılmaz; yapılan işler feature branch üzerinde hazırlanır, GitHub'a push edilir ve Yusuf'un onayından sonra merge edilir.

## Proje Durumu

Mevcut repo bir Unity proje iskeleti içerir:

- Unity sürümü: `6000.0.23f1`
- Temel klasör yapısı hazır: `Assets`, `Packages`, `ProjectSettings`
- Core scriptleri hazır:
  - `Bootstrap`: oyunun başlangıç akışını kurar.
  - `GameManager`: oyun durumunu sahneler arasında taşır.
  - `SceneLoader`: sahne geçişlerini tek noktadan yönetir.
- Gameplay temeli eklenmeye başladı:
  - Müşteri isteği modeli
  - Müşteri tipi modeli
  - Müşteri kuyruk yöneticisi
  - Gişe servis kontrolcüsü
  - Skor, combo ve multiplier yöneticisi
- UI temeli eklenmeye başladı:
  - Ana menü UXML/USS
  - Ayarlar paneli
  - Oyun içi HUD
  - Ses ve titreşim ayarlarını kaydeden controller
- Henüz oynanabilir sahneler, UI ekranları ve 3D banka ortamı eklenmedi.

## Oyun Hedefi

İlk ürün hedefi, 3D bir banka gişe simülasyonu temeli oluşturmaktır:

- Giriş ekranı
- Gişe odaklı banka içi 3D ortam
- Zaman içinde sıraya giren müşteriler
- Farklı müşteri tipleri ve işlem istekleri
- İşlem süresine bağlı skor sistemi
- Kalan süreye göre combo ve multiplier puanı
- Mobil odaklı kamera ve etkileşim sistemi

Gerçek banka isimleri, marka kullanımı ve görsel benzerlikler ileride ayrıca değerlendirilecektir. Gerekirse kurgusal banka isimleriyle ilerlenir.

## Temel Oyun Döngüsü

1. Oyun turu başlar.
2. Müşteri şubeye girer ve sıraya eklenir.
3. Sıradaki müşteri gişeye gelir.
4. Müşteri tipi ve işlem isteği gösterilir.
5. Oyuncu işlemi hedef süre içinde tamamlamaya çalışır.
6. İşlem bitince skor hesaplanır.
7. Hedef süreden kalan zaman varsa combo ilerler ve multiplier artar.
8. Yeni müşteri çağrılır.

## Klasör Yapısı

```text
Assets/
  Art/            Görsel varlıklar
  Audio/          Ses ve müzik dosyaları
  Prefabs/        Tekrar kullanılabilir Unity prefab'ları
  Scenes/         Unity sahneleri
  Scripts/
    Core/         Oyun başlangıcı, durum yönetimi, sahne yükleme
    Gameplay/     Müşteri, gişe, senaryo, skor ve etkileşim sistemleri
    UI/           Arayüz scriptleri
  UI/
    UXML/         UI Toolkit ekran yapıları
    Styles/       Ortak UI teması
Packages/         Unity paket bağımlılıkları
ProjectSettings/  Unity proje ayarları
docs/             Ürün brief'i, backlog ve karar notları
```

## UI Akışı

İlk UI katmanı Unity UI Toolkit ile hazırlanmıştır:

- `MainMenu.uxml`: giriş, senaryo ve ayarlar ekranı
- `GameHud.uxml`: skor, combo, multiplier, aktif müşteri ve işlem paneli
- `RushBankTheme.uss`: ortak modern görsel stil
- `MainMenuUIController`: menü butonları ve ayarlar paneli
- `GameHudUIController`: oyun içi sayaçlar ve işlem butonları
- `AppSettings`: ses ve titreşim tercihlerini `PlayerPrefs` ile saklar

Unity sahnesinde kullanmak için ilgili GameObject'e `UIDocument` eklenir, UXML asset'i atanır ve uygun controller component'i bağlanır.

## Core Akışı

Başlangıç akışı şu şekilde planlanmıştır:

1. `Boot` sahnesi açılır.
2. `Bootstrap`, hedef FPS değerini ayarlar.
3. `GameManager` yoksa oluşturulur ve sahneler arasında kalıcı yapılır.
4. Oyun durumu `Login` olarak ayarlanır.
5. `SceneLoader`, `Login` sahnesini yükler.

`SceneId` enum sırası Unity Build Settings sahne sırasıyla aynı tutulmalıdır:

```text
Boot = 0
Login = 1
MainMenu = 2
Game = 3
```

## Kurulum

1. Unity Hub kur.
2. Unity 6 LTS `6000.0.23f1` veya daha yeni bir `6000.0.x` sürümü kur.
3. Repoyu klonla:

   ```bash
   git clone https://github.com/yusufekici24/rush-bank.git
   ```

4. Projeyi Unity Hub üzerinden aç:

   ```text
   Add > rush-bank klasörünü seç
   ```

5. VS Code kullanılıyorsa önerilen eklentiler:

   - C# Dev Kit
   - Unity
   - GitHub Pull Requests

6. Unity içinde script editor ayarı:

   ```text
   Edit > Preferences > External Tools > External Script Editor > Visual Studio Code
   ```

## Unity Ayarları

Merge sorunlarını azaltmak için Unity içinde şu ayarlar korunmalıdır:

```text
Edit > Project Settings > Editor > Asset Serialization > Mode > Force Text
Edit > Project Settings > Editor > Version Control > Mode > Visible Meta Files
```

`.meta` dosyaları commit edilmelidir. `Library/`, `Temp/`, `Logs/` gibi Unity tarafından üretilen klasörler commit edilmez.

## Geliştirme Akışı

Her iş `main` branch'inden yeni branch açılarak yapılır:

```bash
git checkout main
git pull origin main
git checkout -b feature/ozellik-adi
```

İş tamamlanınca commit ve push yapılır:

```bash
git status
git add .
git commit -m "Kısa ve açıklayıcı commit mesajı"
git push -u origin feature/ozellik-adi
```

Ardından GitHub üzerinden PR açılır. PR Yusuf tarafından incelenir ve onaylandıktan sonra merge edilir.

## Branch İsimlendirme

- `feature/...`: yeni özellik
- `fix/...`: hata düzeltme
- `art/...`: görsel veya ses varlıkları
- `docs/...`: dokümantasyon değişiklikleri

## Geliştirme Notları

- `main` branch'ine direkt push yapılmaz.
- Aynı `.unity` sahnesinde aynı anda çalışmamaya dikkat edilir.
- Sahne ve prefab değişiklikleri mümkün olduğunca küçük PR'lara bölünür.
- Büyük binary dosyalar için gerekirse Git LFS değerlendirilir.
- İlk oynanabilir hedef: giriş ekranı, temel sahne geçişi, gişe ekranı, sırayla gelen müşteriler ve skor/combo prototipi.
