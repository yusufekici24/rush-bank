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
- Birinci aşama MVP mekanik havuzu tasarım olarak tamamlandı: temel işlemler, fatura varyasyonları, kriz anları ve stratejik destekler netleşti.
- Sıradaki ana odak yeni mekanik eklemek değil; Unity sahnesinde daraltılmış MVP akışını çalıştırmak, test etmek ve ritim/denge ayarı yapmak.
- Game feel hedefi netleşti: başarılı oyuncu VIP, KafeinMode, telefon, fatura ve tembel yardımcı kararlarını senkronize ederek oyunun temposunu kendi lehine hızlandırabilmeli.
- Meta-game hedefi eklendi: oyuncu `PlayerGold` ile seviye öncesi tek kullanımlık booster satın alıp run başlamadan en fazla 2 güçlendirici seçebilmeli.

## Eksikler

- Unity sahneleri henüz yok.
- Build Settings sahne listesi Unity içinde ayarlanmadı.
- Login UI yok.
- MainMenu UI yok.
- Banka içi 3D ortam yok.
- Mobil kontrol sistemi yok.
- New Input System destekli Rigidbody top-down chubby karakter controller eklendi; mobil joystick UI bağlantısı kaldı.
- MobilePlayerController eklendi; Virtual Joystick, Grab/Deposit action ve HoldPoint item taşıma akışı hazır.
- PlayerInteraction eklendi; trigger ile interactable nesne algılama, tutma ve fırlatma hazır.
- DeliveryPoint eklendi; doğru item ID/renk kontrolü, teslim, +süre ödülü ve görsel feedback hazır.
- QueueManager eklendi; müşteri kuyruğu, bekleme alanı, gişeye çağırma, request icon ve patience bar temeli hazır.
- BankingActionSystem eklendi; Withdraw, Deposit ve CurrencyExchange akışları için vault/counter görev sistemi hazır.
- DocumentProcessWorkflow eklendi; kredi/kart başvuru formu, müşteri imzası, manager onayı ve teslim akışı hazır.
- GoldExchangeWorkflow eklendi; Gold Bag/Gold Bar, ExpertiseStation değerlendirme, Value Receipt ve sparkle ödül akışı hazır.
- FastTrackActionSystem eklendi; Passbook Printing ve Card Activation gibi hızlı/no-approval görevler hazır.
- UtilityBillSystem eklendi; Elektrik/Su/Telefon fatura tipleri, müşteri üstü renkli ikon bubble, BarcodeScanner renk/ses feedback'i, 0.5 saniye scan ve +4 saniye teslim ödülü hazır.
- Gelişmiş Fatura Yönetimi planlandı; late game'de sarı/mavi/mor fatura kutuları, doğru renge ayırma, yanlış kutuda ödül iptali veya ceza üreten sorting/categorization mekaniği eklenecek.
- UIManager eklendi; Call Customer cooldown, CustomerRequest icon ve TimeRemaining slider temeli hazır.
- CustomerPatience ve SecuritySystem eklendi; grumpy/raging müşteri durumları ve güvenlik çağırma akışı hazır.
- CustomerPatience ve QueueCustomer yaş psikolojisi çarpanları eklendi; genç 0.7x, orta yaşlı 1.0x, yaşlı 1.5x sabır düşüş hızına sahip.
- DynamicWeatherSystem eklendi; Sunny/Rainy hava dongusu, 3 sn isik gecisi, yagmur particle/audio feedback'i, Rainy durumda 1.15x sabir baskisi ve yeni spawn musterilere semsiye flag/aksesuar destegi hazir.
- ThiefEventSystem eklendi; thief spawn, Call Police, TimeManager freeze ve polis escort akışı hazır.
- LazyAssistantAI eklendi; ikinci gişede sıradaki müşteriyi yavaş servis eden geçici yardımcı akışı hazır.
- LazyAssistantAI atıştırmalık mekaniği eklendi; oyuncu `SnackDrawer`dan snack alıp asistana yedirerek görev kapasitesini +1 artırabilir, her snack asistan servis süresini 1.2x yavaşlatır ve kurabiye göstergesi açar.
- AssistantManager eklendi; kuyruk grumpiness oranına göre dolan SummonBar ve cooldown akışı hazır.
- BankTransaction ScriptableObject eklendi; 8 işlem türü için istasyon, zorluk ve süre ödülü presetleri setup aracıyla üretilecek.
- Fatura Ödeme, Döviz Bozdurma, VIP Kiralık Kasa ve Kart Şifre Blokesi için adım adım oynanış akışları BankTransaction verisine işlendi.
- VIPEscortSystem eklendi; VIP müşteriyi NavMeshAgent ile kasaya götürme, 2.5 saniye bekletme, çıkışa uğurlama ve +15 saniye ödül akışı hazır.
- VIPCustomer eklendi; VIP müşteriye parıltı/aura hook'u, daha kısa sabır süresi ve escort başlayınca kuyruk rahatlatma boost'u hazır.
- VIP için Müdür Odası / Kiralık Kasa hedefi üçgen rota mantığıyla yerleştirildi: VIP sırası giriş hattına yakın, oyuncu gişesi orta/alt hatta, Müdür Odası kapısı gişeye yakın ama girişe uzak sağ-alt bölgede.
- VIPEscortSystem üçgen rota akışına güncellendi; `PlayerCounter`, `VIPWaitingSpot`, `ManagerRoomEntrance` key transform'ları, 1.5m NavMeshAgent takip mesafesi, müdür odasında 6 saniye %50 homurdanma yavaşlatma ve oyuncuya 1.2x hız boost'u hazır.
- Müdürden Aferin boost'u eklendi; VIP tesliminde ding sesi, `BRAVO!` balonu, oyuncuda altın parıltı ve KafeinMode ile çarpan olarak stacklenen 5 saniyelik 1.2x hız artışı hazır.
- CardBlockMiniGame eklendi; oyuncuyu geçici donduran, 3 renkli pattern gösteren, doğru girişte +5 saniye veren kart blokesi mini-game akışı hazır.
- QuestPoolDirector eklendi; gün bazlı görev ağırlıkları, maksimum 5 kişilik sıra doluluğu kontrolü, hırsız aktifken spawn durdurma ve kritik sürede quick-win görev boost'u hazır.
- QuestSpawner eklendi; QuestData struct, aktif seviye görev listesi, 5-8 saniye random spawn, max queue capacity, hırsız aktifken spawn durdurma, kritik sürede cooldown azaltma ve quick-win ağırlık boost'u hazır.
- StaffInterruptionSystem eklendi; rastgele iş arkadaşı kesintisi, aktif gişe işlemini pause/resume event'leriyle duraklatma, yaşa göre sabır baskısı ve ArchiveDesk evrak teslimi hazır.
- StaffRequestUrgency eklendi; personel isteklerinde 30 saniyelik 1. derece sari ikon penceresi, 10 saniyelik 2. derece kirmizi/cift unlem kritik penceresi, basari odulleri, masa kilidi ve ManagerSatisfaction etkileri hazir.
- CashDeliverySystem eklendi; 5 işlem kapasiteli `currentVaultCash`, Request Cash Dispatch butonu, vault `NO CASH` uyarısı, zırhlı araç spawn'ı, %20 yavaşlatan Super Cash Bag teslimi, golden cash explosion ve 1.5x bonus süre akışı hazır.
- Cash flow dengesi eklendi; başarılı Deposit işlemleri kasayı +1 doldurur, Withdrawal işlemleri kasayı -1 azaltır.
- PhoneInterruptionSystem eklendi; 30-45 saniye random telefon çalma, 4 saniyelik cevap penceresi, reaction-time çarpanı, 2 saniye time freeze ve kaçırılan çağrı cooldown akışı hazır.
- TwoTierPhoneCallSystem eklendi; telefon aramalari Normal Musteri (%75, 15 sn, +40 Gold, +5 mudur memnuniyeti) ve Genel Mudurluk (%25, 7 sn, +200 Gold, +30 mudur memnuniyeti) olarak ayrildi; HQ basarisinda 20 sn 0.5x sabir ve 1.1x gold Corporate Grace, kacirmada -30 mudur memnuniyeti ve HQ Audit Failed overlay hazir.
- TeaLadyBoostSystem eklendi; 50-70 saniye TeaLadyNPC spawn, yemeni/önlük/tepsi fallback teyze görseli, yalpalama ve el sallama hissi, TeasideTable'a steam/glow TeaCup bırakma, tıklanınca 8 saniye KafeinMode, +30% hız, sarı hız izi ve 0.6x işlem süresi çarpanı hazır.
- TeaLadySystem/TeaLadyBoostSystem güncellendi; Drink seçimi 10 saniye 1.3x hız verir, Serve seçimi 3 porsiyonluk Tea Hospitality moduna geçer, müşterilere çay ikramı 40% sabır toparlar ve 8 saniye 0.7x sabır düşüşü uygular.
- TeaLadyRefillEvent eklendi; Sadiye Abla 60-80 saniyede bir giseye gelip tek tiklik Refill Brew butonu acar, cozulurse +50 Gold, +10 mudur memnuniyeti ve 15 saniyelik 0.7x Fresh Brew sabir yavaslatma boost'u verir; 15 saniye bekletilirse -15 mudur memnuniyeti uygular.
- AccountOpeningSystem eklendi; OpenAccount müşterisini 0.5 saniye stamp sonrası Relationship Manager Desk'e yönlendirir, ana gişe slotunu boşaltır ve sonraki 2 standart işlem timer'ını instant yapan Quick Boost verir.
- InsuranceReferralSystem eklendi; InsuranceReferral müşterisini Sigortacıya Aktar butonuyla InsuranceSpecialistDesk'e yönlendirir, ana gişe slotunu boşaltır ve 12 saniye counter tabanlı işlem sürelerini 0.6x hızlandıran Teamwork Boost verir.
- RedAlertRedirectionSystem eklendi; BarutCustomer 20% sabır ve 2x drain ile gelir, tıklanınca kuyruk önüne alınır, Acil Sevk ile Relationship Manager Desk'e gönderilir, +200 Gold ve kuyruktaki müşterilere 50% VIP Relief verir.
- BankCatChaosSystem eklendi; Pati kedisi 15 saniye kuyruk sabrını dondurur, sonra rastgele müşteride kedi korkusu/panik tetikler, Call Security butonunu açar, Recai Abi kovalamacası sırasında sabrı normale döndürür ve 10 saniye sonunda kedi çıkıştan kaçar.
- WireTransferMiniGame eklendi; WireTransfer müşterisi için 4-5 karakterlik alfanumerik kod, sanal klavye, Send akışı, yanlış tuş reseti ve 5 saniye/hatasız tamamlama halinde 15 saniyelik 1.2x GoldMultiplier `Perfect Transfer` boost'u hazır.
- MobileActivationMiniGame eklendi; MobileActivation müşterisi için Send SMS butonu, 0.5 sn loading, telefon ekranında 4 haneli SMS kodu, numpad doğrulama ve 15 saniye müşteri spawn interval'ini 1.3x yapan Digital Boost hazır.
- CharityDonationSystem eklendi; PhilanthropistCustomer 100% sabır ve 0.5x sabır drain ile gelir, hayvan/doğa/çocuk/sağlık bağış kategorisi seçtirir, yanlış seçimde küçük skor cezası verir, doğru bağışta kalp efekti ve 15 saniyelik 0.6x Karma Boost uygular.
- ManagerITSupportEvent eklendi; müdür bilgisayarı 45 saniyelik kontrolle %15 ihtimalle bozulur, BlueScreen/LooseCable/OverheatingFan mini-game'lerinden biri açılır, tamirde +150 Gold ve 15 saniyelik sabır freeze + 1.2x hız Manager Grace Boost verir.
- StationeryDeliverySystem eklendi; Müşteri İlişkileri veya Sigorta masası A4/Pen/Stapler isteyebilir, sorun çözülene kadar masa yönlendirme kabul etmez, doğru kırtasiye tesliminde +80 Gold ve o masaya sonraki 3 yönlendirme için 2x hızlı Efficiency Boost verir.
- CreditApplicationSystem eklendi; CreditApproval müşterisi Housing/Vehicle/Consumer alt türlerinden biriyle gelir, 0.8 sn kredi notu sorgusu 80% onay/20% red üretir, redde +30 Gold, onayda CreditSpecialistDesk yönlendirmesi ve kredi türüne göre +120/+80/+50 Gold verir; Credit Boost 15 saniye uzman masa yönlendirmelerini 1.3x hızlandırır.
- QuestAndAchievementManager eklendi; günlük görevleri ve uzun vadeli başarımları PlayerPrefs ile takip eder, başarılı işlem/event listener'larıyla ilerletir, altın ödülü verir, sağ üst bildirim banner'ı gösterir ve geçici/kalıcı passive boost uygular.
- ManagerSatisfactionSystem eklendi; müdür memnuniyet barı IT tamiri, kırtasiye teslimi, dolandırıcı yakalama, Barut müşteri sevki ve perfect işlemle dolar; gişe krizi ve dolandırıcıya yanlış onayla düşer; %100'de Staff Feast tetikleyip 20 saniye sabır freeze, 1.3x oyuncu hızı, 0.5x işlem süresi ve 2x yönlendirme hızı verir.
- ScammerDetectionSystem eklendi; ScammerCustomer normal işlem ikonu arkasına saklanır, evrak inceleme panelinde fotoğraf/tarih/sahte mühür tutarsızlığı kontrol edilir, yanlış onayda -150 Gold ve FAILED AUDIT kilidi, doğru redde +50 Gold, güvenlik çağırmada +100 Gold ve 10 saniyelik Hero Employee sabır dondurma boost'u hazır.
- CounterIncidentManager ve SecurityGuardAI eklendi; aktif gişe müşterisinin sabrı sıfırlanınca işlem UI/workflow iptali, -100 Gold cezası, AngryGesticulation feedback'i, güvenlik eskortu ve 10 saniyelik Panic Attack debuff akışı hazır.
- SecurityGuardRequestEvent eklendi; Recai Abi 70-90 saniyede bir giseye telsiz/techizat istegiyle gelir, tek tiklik Charge Radio butonu acar, cozulurse +50 Gold, +10 mudur memnuniyeti, 25 saniyelik dolandirici evrak highlight'i ve 0.7x sabir rahatlatmasi verir; bekletilirse -15 mudur memnuniyeti uygular.
- WetFloorAccidentSystem eklendi; Cayci Abla temizlik yaparken lobide islak zemin alanı olusturur, yeni giren musteriler %15 sansla kayip duser, tek tiklik Send Guard butonuyla Recai Abi kurtarmasi yapilir, +100 Gold, +15 mudur memnuniyeti ve 20 saniyelik 0.6x Compassionate Branch sabir boost'u verilir.
- HeistRaidSystem eklendi; Super Cash Bag ile şubeye girince %10-15 nadir soygun roll'u, yamuk çorap maskeli 2-3 hırsız, konuşma/el sallama cue'ları, kırmızı/mavi göz cue, oyuncu diz titreme korku efekti, %50 yavaşlama, yakalanınca 2 saniye freeze cezası, alarm butonu, polis gelince çuval fırlatma ve tutuklama akışı hazır.
- Kamera sistemi yok.
- Etkileşim sistemi sahneye bağlanmadı.
- Senaryo verileri yok.
- Müşteri tipi verileri yok.
- İşlem isteği verileri yok.
- Müşteri kuyruk sistemi sahneye bağlanmadı.
- Skor, combo ve multiplier UI'ı yok.
- Modern ama basit UI Toolkit temeli oluşturuldu; sahnelere bağlanması gerekiyor.
- Prototype scene setup aracı eklendi; Unity içinde `RushBank > Setup Prototype Scenes` ile sahneler ve örnek veriler oluşturulacak.
- TimeManager eklendi; 60 saniyelik geri sayım, süre ekleme/çıkarma ve game over event'i hazır.
- PreGameShopManager eklendi; `PlayerGold`, TimeSlow/Speed/Patience booster stokları, satın alma, run öncesi en fazla 2 booster seçme ve level başlangıcında booster tüketip uygulama akışı hazır.
- Pre-run booster etkileri eklendi; Zaman Bükücü süre akışını 0.85x, Turbo Tabanlık oyuncu hızını 1.2x, Homurdanma Önleyici global sabır düşüşünü 0.75x yapar.
- PreLevelPopupController eklendi; seviye `Oyna` butonuna basılınca sahneyi hemen yüklemek yerine booster satış pop-up'ı açar, PlayerGold/adetleri gösterir, hızlı satın al & kuşan akışını yönetir ve `Başla` ile seçili seviyeyi yükler.
- LevelDifficultyManager ve GameSettingsManager eklendi; Taşra/Şehir/Metropol branch seçimi PlayerPrefs'e kaydedilir, seçilen branch ayarları oyun sahnesinde spawn aralığı, sabır baskısı, hırsız/raid şansı ve hedef altın değerlerini otomatik ölçekler.
- TutorialManager eklendi; Eğitim Şubesi soft opening akışı, süre/sabır baskısını kapatma, gişeye yürüme, Passbook Update, Electricity Bill Payment ve tamamlanınca Taşra kilidi açma hazır.
- Chubby Toon URP shader ve kullanım rehberi eklendi; URP pipeline kurulumu yapılacak.
- İlk oynanabilir gişe görevi tanımlanmadı.
- Görsel stil ve kurgusal banka isimleri netleşmedi.
- Android build/test ayarı yapılmadı.

## Önerilen Geliştirme Sırası

0. Çalıştır ve test et: temel çekirdek
   - Önce `TutorialManager` ile Eğitim Şubesi onboarding akışını test et
   - Karakter hareketini test et
   - `QueueManager` ile müşteri çağırma akışını test et
   - `TimeManager` geri sayım ve süre ekleme davranışını test et
   - Sadece iki işlem açık kalsın: Hesap Cüzdanı ve Para Çekme/Yatırma + kasa sistemi
   - Amaç: ana ritim, koşma mesafesi, süre ödülü ve kasa döngüsü eğlenceli mi?

1. Sahne iskeleti
   - Editor setup aracı hazırlandı
   - `Boot`
   - `Login`
   - `MainMenu`
   - `Game`
   - Build Settings sırası setup aracıyla oluşturulacak

2. UI prototipi
   - Ana menü UXML/USS hazırlandı
   - Ayarlar paneli hazırlandı
   - Ses ve titreşim toggle'ları hazırlandı
   - Oyun içi HUD hazırlandı
   - UIManager ile Call Customer butonu ve süre slider'ı eklendi
   - Pre-run shop ekranı eklendi: PlayerGold göstergesi, booster satın alma kartları, level start booster toggle'ları ve EaseOutBack popup animasyonu
   - Level selection akışı eklendi: Taşra Şubesi Easy, Şehir Şubesi Medium, Metropol Şubesi Hard branch ayarları `GameSettingsManager` üzerinden run'a aktarılır
   - Sahneye UIDocument olarak bağlanacak

3. 3D prototip sahnesi
   - Basit zemin ve duvarlar
   - Gişe
   - Sıra makinesi
   - Bekleme alanı
   - Chubby Toon URP shader test materyali

4. Oyuncu sistemi
   - New Input System destekli Rigidbody top-down chubby hareket controller'ı eklendi
   - MobilePlayerController ile UI butonlu Grab/Deposit akışı eklendi
   - Basit screen joystick scripti eklendi
   - PlayerInteraction ile nesne taşıma/fırlatma eklendi
   - DeliveryPoint ile doğru objeyi teslim etme eklendi
   - Mobil joystick UI prefab/sahne bağlantısı yapılacak
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
   - QueueManager ile bekleme alanı ve gişeye çağırma eklendi
   - BankingActionSystem ile işlem bazlı görev akışı eklendi
   - DocumentProcessWorkflow ile kredi/kart evrak akışı eklendi
   - GoldExchangeWorkflow ile altın değerlendirme akışı eklendi
   - FastTrackActionSystem ile hızlı işlem akışı eklendi
   - UtilityBillSystem ile elektrik/su/telefon fatura varyasyonları ve BarcodeScanner feedback'i eklendi
   - Late game için Gelişmiş Fatura Yönetimi: renkli fatura kutuları ve doğru kutuya ayırma sistemi planlandı
   - CustomerPatience ve SecuritySystem ile sabır/güvenlik sistemi eklendi
   - ThiefEventSystem ile hırsız/polis etkinliği ve süre dondurma eklendi
   - LazyAssistantAI ile ikinci gişe yardımcısı eklendi
   - AssistantManager ile yardımcı çağırma barı ve cooldown akışı eklendi
   - BankTransaction presetleriyle işlem verisi modeli eklendi
   - VIPEscortSystem ile VIP kasa eşlik görevi eklendi
   - CardBlockMiniGame ile kart blokesi renk sırası mini-game'i eklendi
   - QuestSpawner ile görev havuzu, level progression ve dynamic pacing eklendi
   - StaffInterruptionSystem ile gişe işlemini bölen iş arkadaşı evrak akışı eklendi
   - CashDeliverySystem ile kasa boşalma ve merkezden nakit yenileme krizi eklendi
   - Cash flow balance ile deposit/withdrawal kasa döngüsü dengelendi
   - PhoneInterruptionSystem ile hızlı refleks telefon çağrısı mini-event'i eklendi
   - TeaLadyBoostSystem ile çay/kahve power-up ve KafeinMode eklendi
   - CharityDonationSystem ile hayırsever müşteri, kategori seçimi ve Karma Boost eklendi
   - ManagerITSupportEvent ve StationeryDeliverySystem ile banka içi arıza/kırtasiye koşturmacası eklendi
   - CreditApplicationSystem ile konut/taşıt/ihtiyaç kredisi sorgulama ve Kredi Uzmanı yönlendirmesi eklendi
   - ManagerSatisfactionSystem ile iç başarıların birikerek Staff Feast boost'una dönüşmesi eklendi
   - ScammerDetectionSystem ile kimlik/evrak tutarsızlığı yakalama ve güvenlik çağırma mekaniği eklendi
   - HeistRaidSystem ile nakit teslim dönüşü stealth soygun baskını eklendi
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

9. İkinci faz: kaos ve çeşitlilik
   - Altın Bozdurma ve Kredi Onayı çok istasyonlu işleri sırayla aç
   - Elektrik/Su/Telefon fatura varyasyonlarını aktif havuza ekle
   - Görev ağırlıklarını oyuncunun ritmi bozulmayacak şekilde dengele

10. Son dokunuş: kriz ve yardımcı sistemleri
   - Çaycı Abla ve Telefon gibi kısa süreli olayları aç
   - LazyAssistantAI ile sıkışma anındaki yardım akışını ve SnackDrawer ile besleme/yavaşlama riskini test et
   - Hırsız/Polis ve Heist Raid olaylarını nadir, yüksek tansiyonlu event olarak devreye al

## Pre-run Shop Satis UI Notlari

- Anti-Grumpiness / Papatya Cayi karti pre-level popup icinde `BestSellerRibbon` ile "Cok Satan!" olarak isaretlenir; kurdele `BestSellerRibbonFloat` ile hafif bobbing animasyonu yapar.
- `BuyBundleButton`, 3 booster'i 150 Altin yerine 120 Altinlik "Tontis Paket" olarak satin alir; basarili satin almada TimeSlow, Speed ve Patience stoklarina +1 eklenir ve ucu birden upcoming run icin equipped olur.
- Bundle satin alma, standart tekil booster secimindeki 2 aktif booster sinirinin ozel kampanya istisnasidir; popup konfeti particle feedback'i ve cash register sesiyle sonucu oyuncuya hissettirir.

## Karar Bekleyen Konular

- Oyuncunun gişe ekranı nasıl görünecek?
- İşlem tamamlama mini-game mi olacak, yoksa hızlı doğru seçim akışı mı olacak?
- Kamera sabit gişe kamerası mı, yoksa hafif serbest kamera mı olacak?
- Gerçek banka isimleri yerine kurgusal isimler mi kullanılacak?
- İlk senaryo para yatırma mı, para çekme mi, hesap açma mı olacak?
- Görsel stil gerçekçi mi, düşük poligon/stylized mı olacak?
