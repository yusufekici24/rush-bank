# RushBank Product Brief

RushBank, mobil odaklı 3D banka simülasyonu oyunudur. Oyuncu banka görevlisi rolündedir; müşteriler zaman içinde sırayla şubeye gelir, farklı bankacılık istekleriyle gişeye yönelir ve oyuncu bu işlemleri doğru ve hızlı tamamlayarak puan kazanır.

## Ürün Hedefi

İlk hedef, küçük ama oynanabilir bir prototip çıkarmaktır:

- Oyuncu giriş ekranından oyuna başlar.
- Banka içi 3D gişe sahnesi yüklenir.
- Müşteriler belirli aralıklarla sıraya girer.
- Her müşteri tipinin farklı işlem isteği ve işlem süresi olur.
- Oyuncu işlemleri tamamladıkça puan kazanır.
- İşlem hedef süresinden erken tamamlanırsa kalan süreye göre bonus ve combo multiplier kazanılır.

## MVP Kapsamı

İlk oynanabilir sürüm için önerilen kapsam:

1. Login sahnesi
   - Başla butonu
   - Basit profil/oyuncu adı alanı
   - Ayarlar butonu için yer ayrılması

2. Ana menü
   - Oyuna başla
   - Senaryo seçimi için temel liste
   - Çıkış veya geri dönüş akışı

3. Banka içi 3D prototip
   - Basit banka salonu
   - Oyuncunun çalıştığı gişe
   - Müşteri giriş noktası ve sıra alanı
   - Bekleme alanı temsil objeleri
   - Gişe odaklı kamera
   - Etkileşim raycast sistemi

4. İlk senaryo
   - İlk müşteri gişeye gelir
   - Müşterinin işlem isteği gösterilir
   - Oyuncu işlemi tamamlar
   - Puan, kalan süre bonusu ve combo multiplier hesaplanır
   - Sıradaki müşteri çağrılır

## Temel Oyun Döngüsü

1. Oyun turu başlar.
2. Müşteri şubeye girer ve sıraya eklenir.
3. Sıradaki müşteri gişeye gelir.
4. Müşteri tipi ve işlem isteği gösterilir.
5. Oyuncu işlemi hedef süre içinde tamamlamaya çalışır.
6. İşlem bitince skor hesaplanır.
7. Hedef süreden kalan zaman varsa combo ilerler ve multiplier artar.
8. Yeni müşteri çağrılır.

## Müşteri ve İşlem Tipleri

İlk prototip için önerilen işlem tipleri:

- Para yatırma
- Para çekme
- Hesap açma
- Kart başvurusu
- Kredi başvurusu
- Fatura ödeme
- Bilgi güncelleme

Her işlem tipi farklı hedef süre, taban puan ve zorluk değerine sahip olabilir.

## Teknik Yön

- Ana teknoloji: Unity 6 LTS
- Hedef platform: Android öncelikli mobil
- Dil: C#
- UI: İlk aşamada Unity UI veya UI Toolkit
- Sahne akışı: `Boot -> Login -> MainMenu -> Game`
- Geliştirme akışı: Feature branch + Pull Request

## Marka ve İçerik Notu

Türkiye'deki bankacılık deneyimlerinden esinlenilecek, ancak gerçek banka adları, logoları ve birebir marka benzerlikleri kullanılmadan önce hukuki ve ürün kararı verilecektir. İlk prototipte kurgusal banka isimleriyle ilerlemek daha güvenlidir.
