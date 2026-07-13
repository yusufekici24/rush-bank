# RushBank

Mobil oyun projesi (Unity).

**Unity sürümü:** Unity 6 LTS (6000.0.x). Unity Hub'dan 6000.0.23f1 veya daha
yeni bir 6000.0 sürümü kurun; ilk açılışta sürüm yükseltme sorarsa onaylayın.

## Kurulum (yeni katılan geliştirici için)

1. Unity Hub ve Unity 6 LTS'i kur.
2. Projeyi klonla:

   ```bash
   git clone https://github.com/yusufekici24/rush-bank.git
   ```

3. Git kimliğini ayarla (bir kere):

   ```bash
   git config --global user.name "GITHUB_KULLANICI_ADIN"
   git config --global user.email "GITHUB_EPOSTAN"
   ```

4. Unity Hub → Add → klonladığın `rush-bank` klasörünü seç ve aç.
5. Kod düzenleme için VS Code kullanılıyorsa aşağıdaki eklentileri kur:
   - **C# Dev Kit** (Microsoft) — C# dil desteği
   - **Unity** (Microsoft) — Unity hata ayıklama ve entegrasyon
   - **GitHub Pull Requests** (GitHub) — PR'ları VS Code içinden aç/yönet
6. Unity içinde: Edit → Preferences → External Tools → External Script Editor
   olarak **Visual Studio Code**'u seç. Artık script'e çift tıklayınca VS Code açılır.

> **Not:** `main` branch'i korumalıdır — direkt push GitHub tarafından
> reddedilir. Tüm işler PR ile gelir ve 1 onay almadan merge edilemez.

## Geliştirme Akışı

İki kişilik ekip, GitHub üzerinden PR (Pull Request) akışıyla çalışır:

1. `main` branch her zaman çalışır durumda tutulur, **direkt push yapılmaz**.
2. Yeni bir iş için `main`'den branch açılır:

   ```bash
   git checkout main
   git pull origin main
   git checkout -b feature/ozellik-adi
   ```

3. İş bitince push edilip PR açılır:

   ```bash
   git push -u origin feature/ozellik-adi
   gh pr create   # veya GitHub web arayüzünden
   ```

4. PR'ı **Yusuf** inceler ve onaylar; onaysız PR merge edilmez.
5. Merge sonrası branch silinir (GitHub bunu otomatik yapar).

## Branch İsimlendirme

- `feature/...` — yeni özellik (ör. `feature/level-sistemi`)
- `fix/...` — hata düzeltme (ör. `fix/skor-hesabi`)
- `art/...` — görsel/ses varlıkları

## Unity Ayarları (İLK KURULUMDA ZORUNLU)

Unity projesini bu klasöre oluşturduktan sonra iki ayar mutlaka yapılmalı,
yoksa git birleştirmeleri (merge) bozulur:

1. **Edit → Project Settings → Editor → Asset Serialization → Mode: Force Text**
2. **Edit → Project Settings → Editor → Version Control → Mode: Visible Meta Files**

## Dikkat Edilecekler

- `Library/`, `Temp/`, `Logs/` klasörleri commit edilmez (.gitignore hallediyor).
- `.meta` dosyaları **mutlaka** commit edilir — silme/taşıma işlemlerini Unity içinden yapın.
- Aynı sahne (`.unity`) üzerinde aynı anda çalışmayın; sahne dosyaları zor birleşir.
  İş bölümünü sahne/prefab bazında ayırın.
- Büyük binary dosyalar (100 MB üstü) GitHub'a sığmaz; o noktaya gelirsek Git LFS kurarız.
