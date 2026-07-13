# RushBank

RushBank, Unity 6 LTS ile geliştirilen 3D mobil banka simülasyonu oyunudur. Amaç, Türkiye'deki banka deneyimlerinden esinlenen, senaryo tabanlı ve oynanabilir bir banka ortamı kurmaktır.

Proje PR akışıyla geliştiriliyor. `main` branch'ine direkt push yapılmaz; yapılan işler feature branch üzerinde hazırlanır, GitHub'a push edilir ve Yusuf'un onayından sonra merge edilir.

## Proje Durumu

Mevcut repo bir Unity proje iskeleti içerir:

- Unity sürümü: `6000.0.23f1`
- Temel klasör yapısı hazır: `Assets`, `Packages`, `ProjectSettings`
- Core scriptleri hazır:
  - `Bootstrap`: oyunun başlangıç akışını kurar.
  - `GameManager`: oyun durumunu sahneler arasında taşır.
  - `SceneLoader`: sahne geçişlerini tek noktadan yönetir.
- Henüz oynanabilir sahneler, UI ekranları, 3D banka ortamı ve senaryolar eklenmedi.

## Oyun Hedefi

İlk ürün hedefi, 3D bir banka simülasyonu temeli oluşturmaktır:

- Giriş ekranı
- Oyuncu profili veya oturum başlangıcı
- Banka içi 3D ortam
- Senaryo tabanlı görevler
- Bankacılık işlemlerini simüle eden kullanıcı akışları
- Mobil odaklı kamera ve kontrol sistemi

Gerçek banka isimleri, marka kullanımı ve görsel benzerlikler ileride ayrıca değerlendirilecektir. Gerekirse kurgusal banka isimleriyle ilerlenir.

## Klasör Yapısı

```text
Assets/
  Art/            Görsel varlıklar
  Audio/          Ses ve müzik dosyaları
  Prefabs/        Tekrar kullanılabilir Unity prefab'ları
  Scenes/         Unity sahneleri
  Scripts/
    Core/         Oyun başlangıcı, durum yönetimi, sahne yükleme
    UI/           Arayüz scriptleri
Packages/         Unity paket bağımlılıkları
ProjectSettings/  Unity proje ayarları
```

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
- İlk oynanabilir hedef: giriş ekranı, temel sahne geçişi ve banka içi 3D prototip akışı.
