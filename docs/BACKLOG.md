# RushBank Backlog

Bu dosya, Unity prototipi için bilinen isterleri, eksikleri ve önerilen geliştirme sırasını takip eder.

## Netleşen İsterler

- Oyun 3D olacak.
- Mobil odaklı geliştirilecek.
- Banka simülasyonu olacak.
- Türkiye'deki banka deneyimlerinden esinlenecek.
- Oyuncu banka görevlisi olacak.
- Müşteriler zaman içinde sırayla içeri girecek.
- Her müşteri tipinin farklı işlem isteği olacak.
- İşlem bitirme süresi müşteri isteğine göre değişecek.
- Oyuncu işlemleri tamamladıkça puan kazanacak.
- Kalan süreye göre combo bonusu ve multiplier kazanılacak.
- Repo PR akışıyla geliştirilecek; Yusuf onaylamadan merge yapılmayacak.
- İlk akışta giriş ekranı ve içeri girdikten sonra senaryo sistemi olacak.

## Eksikler

- Unity sahneleri henüz yok.
- Build Settings sahne listesi Unity içinde ayarlanmadı.
- Login UI yok.
- MainMenu UI yok.
- Banka içi 3D ortam yok.
- Mobil kontrol sistemi yok.
- Kamera sistemi yok.
- Etkileşim sistemi sahneye bağlanmadı.
- Senaryo verileri yok.
- Müşteri tipi verileri yok.
- İşlem isteği verileri yok.
- Müşteri kuyruk sistemi sahneye bağlanmadı.
- Skor, combo ve multiplier UI'ı yok.
- Modern ama basit UI Toolkit temeli oluşturuldu; sahnelere bağlanması gerekiyor.
- İlk oynanabilir gişe görevi tanımlanmadı.
- Görsel stil ve kurgusal banka isimleri netleşmedi.
- Android build/test ayarı yapılmadı.

## Önerilen Geliştirme Sırası

1. Sahne iskeleti
   - `Boot`
   - `Login`
   - `MainMenu`
   - `Game`

2. UI prototipi
   - Ana menü UXML/USS hazırlandı
   - Ayarlar paneli hazırlandı
   - Ses ve titreşim toggle'ları hazırlandı
   - Oyun içi HUD hazırlandı
   - Sahneye UIDocument olarak bağlanacak

3. 3D prototip sahnesi
   - Basit zemin ve duvarlar
   - Gişe
   - Sıra makinesi
   - Bekleme alanı

4. Oyuncu sistemi
   - Mobil joystick veya basit touch kontrol
   - Kamera takibi
   - Etkileşim mesafesi

5. Senaryo sistemi
   - Görev adımı modeli
   - Aktif görev takibi
   - Etkileşimle görev tamamlama
   - Görev tamamlandı UI

6. Müşteri ve gişe sistemi
   - Müşteri tipi modeli
   - İşlem isteği modeli
   - Müşteri kuyruk yöneticisi
   - Gişe servis kontrolcüsü

7. Skor sistemi
   - Taban puan
   - Kalan süre bonusu
   - Combo sayacı
   - Multiplier hesaplama

8. İlk oynanabilir senaryo
   - Müşteri sıraya girer
   - Gişeye çağrılır
   - Oyuncu işlemi tamamlar
   - Puan ve combo gösterilir
   - Sıradaki müşteri gelir

## Karar Bekleyen Konular

- Oyuncunun gişe ekranı nasıl görünecek?
- İşlem tamamlama mini-game mi olacak, yoksa hızlı doğru seçim akışı mı olacak?
- Kamera sabit gişe kamerası mı, yoksa hafif serbest kamera mı olacak?
- Gerçek banka isimleri yerine kurgusal isimler mi kullanılacak?
- İlk senaryo para yatırma mı, para çekme mi, hesap açma mı olacak?
- Görsel stil gerçekçi mi, düşük poligon/stylized mı olacak?
