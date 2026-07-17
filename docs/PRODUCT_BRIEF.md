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

## Birinci Aşama MVP Son Durum Analizi

İlk aşama için mekanik havuzu tamamlanmış kabul edilir. Prototipin amacı artık yeni sistem eklemekten çok, eldeki sistemleri Unity sahnesinde çalıştırmak, ritimlerini test etmek ve mobil oynanış hissini parlatmaktır.

**Temel İşlemler:** Hesap Cüzdanı hızlı nefes alma görevi olarak, Para Yatırma/Çekme ise orta zorlukta ve kasa kapasitesine bağlı ana işlem olarak MVP'nin çekirdek bankacılık ritmini kurar.

**Ayrışan İşlemler:** Elektrik, Su ve Telefon olmak üzere 3 farklı fatura türü; renk, ikon ve barkod okuyucu feedback'iyle hızlı görsel eşleştirme sağlar.

**Kriz Anları:** Hırsız/Polis zaman dondurma olayı, Telefon çalması refleks testi ve nadir Soygun Baskını gizlilik mini-event'i oyuncunun dikkatini sürekli canlı tutan yüksek tansiyon anlarıdır.

**Stratejik Destekler:** Tembel Yardımcı homurdanma baskısı artınca devreye giren yavaş otomasyon desteğidir. Çaycı Abla hız ve işlem boost'u verir. VIP Müşteri ise Müdür Odasına üçgen rota ile götürülerek hem riskli bir önceliklendirme görevi hem de rahatlama boost'u sağlar.

Bu kapsamla tasarım tarafında açık kalan büyük bir mekanik boşluk yoktur. Bir sonraki odak, bu havuzu daraltılmış MVP sahnesinde çalıştırmak, hangi sistemlerin ilk build'de aktif olacağını seçmek ve oynanış temposunu test etmektir.

## Temel Oyun Döngüsü

1. Oyun turu başlar.
2. Müşteri şubeye girer ve sıraya eklenir.
3. Sıradaki müşteri gişeye gelir.
4. Müşteri tipi ve işlem isteği gösterilir.
5. Oyuncu işlemi hedef süre içinde tamamlamaya çalışır.
6. İşlem bitince skor hesaplanır.
7. Hedef süreden kalan zaman varsa combo ilerler ve multiplier artar.
8. Yeni müşteri çağrılır.

## Oyun Döngüsünün Güçlü Yönleri

**Ritim ve Çeşitlilik Dengesi:** Oyuncunun elinde hem mikro saniyelerle yarışacağı hızlı işler (Hesap Cüzdanı, Fatura Ödeme) hem de stratejik koşturmaca gerektiren uzun vadeli yatırımlar (Kredi, Altın, VIP) bulunur. Bu yapı tekdüze oynanışı kırar ve her müşteri gelişinde farklı bir karar temposu yaratır.

**Duygusal İniş Çıkışlar:** Telefonun aniden çalması refleks testi yaratır, soygun baskını gizlilik heyecanı ekler, Çaycı Abla ise tatlı bir rahatlama ve hızlanma anı verir. Oyuncu sadece evrak taşımaz; ritim, panik, rahatlama ve kriz arasında gidip gelir.

**Stratejik Karar Anları:** "Müşteriyi mi bitireyim, telefona mı bakayım?", "Kasayı şimdi mi doldurayım, yoksa uykucu yardımcıyı çağırıp o sırada mı halledeyim?" gibi kararlar oyunu sıradan bir tıklama akışından çıkarıp strateji-aksiyon hissine taşır.

**Mükemmel Senkronizasyon Hissi:** Başarılı oyuncu sadece işleri bitirmez, oyunun temposunu kendi lehine hızlandırır. VIP'yi alıp müdür odasına yetiştirdiğinde gelen `ding!`, `BRAVO!` balonu ve `1.2x` hız boost'u; yolda yakalanan KafeinMode ile birleşirse oyuncu gişeye neredeyse uçarak döner. Bu sırada hızlı fatura tarama, telefon çağrısına yetişme veya tembel yardımcıyı gofretle sahada tutma gibi kararlar peş peşe bağlanır. Ana his şudur: verimli bankacı daha hızlı bankacıdır; iyi oynayan oyuncu kaosu sadece söndürmez, ritmi kendi hızına çeker.

## Meta-Game ve Pre-Run Booster Pazarı

Oyuncunun kazandığı oyun içi para (`PlayerGold`) sadece skor hissi olarak kalmaz; seviye başlamadan önce taktiksel hazırlığa dönüşür. Giriş / seviye seçimi ekranında `Seviye Öncesi Güçlendirici Pazarı` bulunur. Oyuncu tek kullanımlık booster kartları satın alır, run başlamadan en fazla 2 tanesini aktif eder ve seçilen booster'lar seviye başlangıcında tüketilerek uygulanır.

| Booster | Etki | Tontiş Görsel Kimlik |
| --- | --- | --- |
| Zaman Bükücü | Ana süre sayacının akış hızını seviye boyunca `0.85x` yapar; süre %15 daha yavaş akar. | Köstekli cep saati veya eriyen tontiş saat |
| Turbo Tabanlık | Oyuncunun hareket hızını seviye boyunca `1.2x` artırır. | Kanatlı tontiş spor ayakkabı |
| Homurdanma Önleyici | Tüm müşterilerin sabır/homurdanma düşüş hızını `0.75x` yapar; sabır %25 daha yavaş azalır. | Kulaklık takmış gülen teyze veya papatya çayı |

Teknik olarak `PreGameShopManager`, `PlayerPrefs` üzerinde `PlayerGold`, `Booster_TimeSlow_Qty`, `Booster_Speed_Qty` ve `Booster_Patience_Qty` değerlerini saklar. Shop ekranı `BuyBooster(string boosterType, int cost)` ile satın alma yapar. Level start ekranı `ToggleBoosterForRun(string boosterType, bool equipped)` ile en fazla 2 booster seçtirir. Seviye başında `ApplyActiveBoosters()` çalışır; seçilen booster miktarı 1 azalır, seçim sıfırlanır ve ilgili runtime çarpanı uygulanır.

**Seviye Öncesi Satış Pop-Up'ı:** Oyuncu seviye seçip `Oyna` butonuna bastığında sahne hemen yüklenmez; önce `PreLevelBoosterPopup` paneli açılır. Başlık tonu "Zorlu Bir Gün Seni Bekliyor! Hazırlıklı mısın?" şeklindedir. Panel mevcut `PlayerGold` değerini, 3 booster kartını ve eldeki adetleri gösterir. Kartta stok varsa tıklama doğrudan booster'ı kuşanır; stok yoksa kart `Satın Al & Kuşan` davranışına geçer, yeterli altın varsa satın alır ve anında run için equip eder. En alttaki `Başla` butonu seçili booster durumlarını `GameManager` üzerinde pending run verisi olarak işaretler ve seçilen banka seviyesini yükler. Popup açılırken 0'dan 1'e gelen hafif zıplamalı `EaseOutBack` ölçek animasyonu kullanılır.

## Şube Seçimi ve Zorluk Eğrisi

Oyuncu oyuna girmeden önce çalışacağı şubeyi seçer. Şube seçimi sadece `Easy / Medium / Hard` etiketi değildir; görsel tema, teknoloji seviyesi, müşteri temposu, homurdanma baskısı ve kriz olasılığı birlikte değişir. `LevelDifficultyManager`, seçilen `BranchType` ayarını `GameSettingsManager` singleton'ına yazar; oyun sahnesi yüklenince `QuestSpawner`, `QueueManager`, `TimeManager`, `ThiefEventSystem`, `HeistRaidSystem` ve `ScoreManager` bu ayarı okuyup kendini otomatik ölçekler.

| Şube | Zorluk | Görsel Kimlik | Tempo Ayarları |
| --- | --- | --- | --- |
| Taşra Şubesi | Easy | Ahşap mobilyalar, tüplü monitörler, yavaş vantilatör, sıcak kasaba atmosferi | Sabır çarpanı `1.0x`, müşteri geliş aralığı `15s`, hırsız/raid şansı `0.02`, hedef altın `120` |
| Şehir Şubesi | Medium | Modern ama mütevazı gişeler, LCD ekranlar, standart numaratör ve klima | Sabır çarpanı `1.5x`, müşteri geliş aralığı `8s`, hırsız/raid şansı `0.10`, hedef altın `240` |
| Metropol Şubesi | Hard | LED ışıklı gişeler, dokunmatik ekranlar, cam gökdelen hissi, ileri teknoloji | Sabır çarpanı `2.2x`, müşteri geliş aralığı `4s`, hırsız/raid şansı `0.25`, hedef altın `420` |

Teknolojik evrim oynanış hissine de yansır: Taşra'da eski yazıcılar ve manuel kaşe gibi yavaş ama sakin işlemler, Metropol'de lazer yazıcı ve dijital imza pedi gibi hızlı cihazlar bulunur. Ancak Metropol'de müşteri yoğunluğu çok yüksek olduğu için bu teknolojik avantaj bile oyuncuya yalnızca nefes alacak kadar alan bırakır.

## Dinamik Hava Durumu

`DynamicWeatherSystem`, subenin atmosferini iki basit hava durumu arasinda dondurur: `Sunny` ve `Rainy`. Sunny varsayilan rahat durumdur; pencere/ortam isigi sicak sari tonda kalir ve musteri sabir carpani `1.0x` olur. Rainy kisa sureli cozy kriz durumudur; isik 3 saniyede mavimsi ve daha sakin bir tona iner, pencere yagmur particle efektleri acilir, loop yagmur sesi calar ve `activePatienceMultiplier = 1.15f` olur.

Yagmur dengesi bilincli olarak hafiftir: homurdanma yalnizca %15 hizlanir. Bu etki Cay Hospitality, Karma/Bagis Boost, Homurdanma Onleyici booster veya Staff Feast sabir freeze'i ile kolayca telafi edilebilir. Rainy sure varsayilanda yaklasik 35 saniye surer; sonra sistem yagmur efektlerini ve sesi fade out eder, isigi tekrar sicak Sunny tona ceker ve sabir carpani `1.0x` seviyesine doner. Yagmur aktifken yeni spawn olan musteriler `hasUmbrella` flag'i alir ve sahnede bagli semsiye aksesuarini veya fallback semsiye gorselini kullanabilir.

## Eğitim Şubesi - Soft Opening

Oyuncu ilk kez oyuna girdiğinde resmi şubelerden önce `Eğitim Şubesi`ne alınır. Burası henüz açılmamış, kolileri duran, balonlar ve "Yakında Açılıyoruz!" afişleriyle süslenmiş, tek gişeli sıfır stres alanıdır. Bu bölümde `TimeManager` geri sayımı durdurulur ve müşteri sabrı/homurdanma barları donuktur; oyuncu panik olmadan hareket, etkileşim ve işlem mantığını öğrenir.

`TutorialManager` akışı dört state üzerinden yönetir: `MoveToCounter`, `SimpleTransaction`, `TwoStepTransaction`, `Completed`. İlk adımda oyuncu gişeye yürür ve yerde parlak halka / pointer görür. İkinci adımda tek müşterilik `Passbook Update` benzeri basit işlem öğretilir. Üçüncü adımda zorunlu `Electricity Bill Payment` seçilerek fatura alma, `BarcodeScanner`da tarama ve müşteriye teslim etme akışı gösterilir. Son adımda müdür NPC tebrik mesajı gösterir, `TutorialCompleted = true` ve `Branch_Tasra_Unlocked = true` PlayerPrefs'e yazılır; ardından oyuncu ana seviye seçim ekranına döner ve Taşra Şubesi açılmış olur.

## Fazlı Geliştirme ve Test Planı

**Adım 1 - Temel Çekirdek:** İlk hedef oyunun kalbini çalıştırıp test etmektir. Bu fazda karakter hareketi (`MobilePlayerController` / top-down controller), müşteri sırası (`QueueManager`), zaman sayacı (`TimeManager`) ve sadece iki işlem aktif tutulur: hızlı işlem olarak Hesap Cüzdanı, orta işlem olarak Para Çekme/Yatırma + kasa sistemi. Bu aşamada amaç ana ritmin, koşma mesafelerinin, süre ödüllerinin ve müşteri akışının keyifli olup olmadığını ölçmektir.

**Adım 2 - Kaos ve Çeşitlilik:** Temel döngü tıkır tıkır çalıştıktan sonra çok istasyonlu işler eklenir: Altın Bozdurma, Kredi Onayı ve ardından elektrik/su/telefon varyasyonlu Fatura Ödeme. Bu faz, oyuncunun istasyon okuma, görev önceliklendirme ve görsel ikon eşleştirme becerisini genişletir.

**Adım 3 - Kriz ve Yardımcı Sistemleri:** Ana oynanış oturduktan sonra heyecan sosları açılır: Çaycı Abla, Telefon Kesintisi, Tembel Yardımcı, Hırsız/Polis etkinlikleri ve nadir Heist Raid. Bu sistemler core loop'u bozmayacak şekilde, oyuncunun ustalaştığı ritme sürpriz ve risk katmak için sonradan devreye alınır.

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

## Oyun İçi Ekonomi ve Zaman Dengesi

| İşlem Türü | Gerekli İstasyonlar | Zorluk Derecesi | Kazandıracağı Süre |
| --- | --- | --- | --- |
| Hesap Cüzdanı | Cüzdan Yazıcısı (Gişe içi) | Çok Kolay | +4 saniye |
| Fatura Ödeme | Barkod Okuyucu (Gişe içi) | Çok Kolay | +4 saniye |
| Kart Şifre Blokesi Kaldırma | Mini Terminal (Gişe içi - Mini Game) | Kolay | +5 saniye |
| Para Çekme/Yatırma | Gişe <-> Ana Kasa (Vault) | Orta | +7 saniye |
| Döviz Bozdurma | Gişe <-> Kur Çevirici Masası | Orta | +7 saniye |
| Altın Bozdurma | Gişe -> Ekspertiz Masası -> Gişe | Zor | +10 saniye |
| Kredi Onayı | Gişe -> İmza -> Müdür Odası -> Gişe | Zor | +12 saniye |
| VIP Kiralık Kasa | VIP müşteriye kasa odasına kadar eşlik etme | Çok Zor | +15 saniye |

### Görev Havuzu ve Seviye Kilidi

Her gün aynı görevler gelmez. `QuestSpawner`, gün/seviye seviyesine göre aktif `QuestData` listesini ve görev ağırlıklarını kullanır:

- 1. Gün (Eğitim): Hesap Cüzdanı %60, Para Çekme/Yatırma %40.
- 5. Gün (Kaos): Altın Bozdurma, VIP Kiralık Kasa ve Hırsız gibi zor görevler havuza eklenir; basit görevlerin ağırlığı düşer.

### Dynamic Pacing

Müşteri gelişi sabit değildir:

- Sıra doluluğu: Sırada maksimum 5 kişi varsa yeni müşteri üretimi duraklatılır.
- Hırsız etkinliği: Hırsız/polis olayı aktifken normal müşteri üretimi durur.
- Kalan süre baskısı: Süre 15 saniyenin altına düşerse Hesap Cüzdanı, Fatura Ödeme ve Kart Şifre Blokesi gibi quick-win görevlerin ağırlığı artar.

Spawn karar akışı:

```text
[Spawn zamanı geldi]
        |
[Sıra dolu mu?] -- Evet --> [Bekle ve tekrar kontrol et]
        |
       Hayır
        |
[Hırsız etkinliği aktif mi?] -- Evet --> [Müşteri üretimini durdur]
        |
       Hayır
        |
[Seviyeye uygun rastgele görev seç]
        |
[Rastgele tontiş karakter modeli seç]
        |
[Karakteri kapıda spawn et ve sıraya gönder]
```

### Görsel ve Görev Eşleştirmesi

Sistem önce görev seçer, sonra rastgele yaş/cinsiyet görsel profili seçerek müşteriyi şubeye sokar. Örneğin arka planda Kredi Onayı seçildiğinde müşteri modeli rastgele belirlenir, müşteri kapıdan girer, kafasında kredi/request ikonu belirir ve sıraya katılır.

### Netleşen İşlem Akışları

**Fatura Ödeme:** Müşteri elektrik, su veya telefon faturasıyla gelir. Sistem fatura tipini rastgele seçer ve müşterinin kafasında hızlı okunabilir bir ikon gösterir: elektrik için sarı şimşek, su için mavi damla, telefon için mor/pembe ahize-sinyal işareti. Oyuncu faturayı alır, bankodaki `BarcodeScanner` cihazına götürür ve taratır. Barkod okuyucu elektrik faturasında sarı ışık ve "bzzzt" hissi, su faturasında mavi ışık ve "glug glug" hissi, telefon faturasında mor ışık ve "bip-bip" hissi verir. 0.5 saniyelik işlemden sonra oyuncu işlenmiş faturayı müşteriye geri teslim eder, karakter üstünde tipe özel kıvılcım/baloncuk/sinyal halkası feedback'i oynar ve +4 saniye kazanılır.

| Fatura Türü | İkon Kimliği | Barkod Okuyucu Tepkisi | Tamamlama Efekti |
| --- | --- | --- | --- |
| Elektrik Faturası | Sarı şimşek | Sarı ışık patlaması, bzzzt hissi | Küçük sarı kıvılcımlar |
| Su Faturası | Mavi damla | Mavi ışık patlaması, glug glug hissi | Minik su baloncukları |
| Telefon Faturası | Mor/pembe ahize-sinyal | Mor ışık patlaması, bip-bip hissi | Mor sinyal halkaları |

**Gelişmiş Fatura Yönetimi / Sorting:** Erken oyunda fatura ödeme bilerek hızlı tutulur: oyuncu faturayı alır, `BarcodeScanner` ile taratır, müşteriye teslim eder ve +4 saniye kazanır. Late game'de bu işlem beceri tavanı oluşturmak için genişletilebilir: gişe arkasına sarı, mavi ve mor olmak üzere 3 fatura kutusu eklenir. Oyuncu taranmış faturayı doğrudan müşteriye vermek yerine doğru renkteki kutuya atar; elektrik sarı kutuya, su mavi kutuya, telefon mor kutuya gitmelidir. Doğru ayrıştırma ödülü korur, yanlış kutu ise süre ödülünü iptal eder veya küçük ceza üretir. Bu özellik ilk prototipte kapalı tutulur; oyuncu ritmi öğrendikten sonra seviye/gün ilerlemesiyle açılacak bir zorluk katmanı olarak planlanır.

**Fatura Difficulty Curve:** Early game'de fatura ödeme "quick win" görevidir ve oyuncuya ritim, renk eşleştirme ve tatminli feedback öğretir. Late game'de sorting/categorization adımı eklenerek oyuncudan aynı anda hız, dikkat ve doğru renk/şekil eşleştirmesi beklenir.

**Döviz Bozdurma:** Müşteri dolar veya euro simgeli yabancı para çuvalıyla gelir. Oyuncu çuvalı alır, gişe arkasındaki Kur Çevirici masasına koyar, cihazın yeşil/mavi ışık veya sembol feedback'ini takip eder ve oluşan yerel para makbuzunu müşteriye teslim eder. Bu işlem orta zorluktadır ve +7 saniye kazandırır.

**VIP Kiralık Kasa Ziyareti:** Çok tontiş ve süslü VIP müşteri büyük anahtar simgesiyle gelir. Oyuncu bankodan çıkar, müşterinin yanına gider, `Action` ile escort başlatır ve VIP müşteri `NavMeshAgent` ile oyuncuyu yakından takip eder. Oyuncu VIP'yi şubenin arkasındaki Kiralık Kasa Odası'na götürür, kasa içinde 2.5 saniyelik bekleme başlar ve ardından VIP müşteriyi çıkış trigger'ına kadar uğurlar. Bu işlem riskli ama yüksek ödüllüdür ve +15 saniye kazandırır.

**VIP Müşteri - Altın Sarısı Sorun:** VIP müşteri sıradan müşteriden hem görsel hem davranışsal olarak ayrılır. Parlak takım elbise, güneş gözlüğü, küçük evrak çantası ve hafif yıldız/parıltı aurası oyuncuya anında "öncelikli kriz" sinyali verir. VIP'nin sabır süresi normal müşteriye göre yaklaşık yarı yarıya kısa tutulur; böylece oyuncu "şu an neyi bırakıp VIP'yi kurtarmalıyım?" kararını hızlı vermek zorunda kalır. VIP ile escort başladığında `QueueManager` kısa süreli bir rahatlama boost'u uygular: aktif/kuyruktaki müşterilerin sabrı toparlanır ve homurdanma hızı geçici olarak yavaşlar. Bu ödül, VIP işini sadece +15 saniyelik bir görev değil, aynı zamanda kriz anını söndüren stratejik bir hamle haline getirir.

**Müdür Odası ve Triangle Route:** VIP escort hedefi sahnede `SafeDepositVault` veya `ManagerRoomDoor` gibi uzak bir trigger'a bağlanabilir. En iyi seviye hissi için giriş kapısı, oyuncu gişesi ve Müdür Odası/Kiralık Kasa kapısı birbirinden uzak ama akıcı bir üçgen rota oluşturmalıdır. Müdür odası girişe çok yakın olmamalı; oyuncu VIP'yi alırken bankosundan uzaklaşmalı, fakat dönüş yolu mobil kontrolde gereksiz zikzak yaratmayacak kadar okunabilir kalmalıdır.

Önerilen VIP rota şeması:

```text
       [ ŞUBE GİRİŞ KAPISI ] (Müşteriler buradan girer)
               |
               | (Müşteri sırayla yürür)
               v
       [ VIP MÜŞTERİ SIRASI ]
               ^
               | (1. Adım: Oyuncu gişeden çıkıp VIP'yi alır)
               |
      [ OYUNCU GİŞESİ ] <------------------------+
               |                                 |
               | (2. Adım: VIP ile              | (3. Adım: Hızlıca
               |  Müdür Odasına yürüyüş)         |  gişeye geri dönüş)
               v                                 |
       [ MÜDÜR ODASI ] --------------------------+
       (Gişeye yakın, girişe uzak)
```

Prototype sahnede bu akış için VIP bekleme noktası müşteri giriş hattına yakın, Müdür Odası kapısı ise gişeye yakın ama girişe uzak sağ-alt bölgede konumlandırılır. Böylece oyuncu önce gişeden ayrılıp VIP'yi alır, kısa ama stresli bir eskort yürüyüşü yapar ve işi başlatır başlatmaz gişesine hızlıca dönebilecek bir üçgen rota yakalar.

Adım adım üçgen hareket döngüsü:

1. **Kurtarma:** Oyuncu kendi `PlayerCounter` alanından çıkar, sıradaki `VIPWaitingSpot` noktasındaki VIP müşterinin yanına gider ve etkileşim butonuna basar.
2. **Eşlik Etme:** `StartEscort()` tetiklenir; VIP müşteri `NavMeshAgent` ile oyuncuya bağlanır ve yaklaşık 1.5 metre takip mesafesiyle arkasından gelir. Oyuncu VIP'yi `ManagerRoomEntrance` trigger alanına götürür.
3. **Rahatlama ve Geri Dönüş:** Oyuncu ve VIP Müdür Odası girişine ulaşınca VIP içeri girer, zengin el sallama animasyonu oynar ve kapı kapanır. Bu anda `ApplyReliefBoost()` tüm bekleyen müşterilerin homurdanma hızını 6 saniyeliğine %50 azaltır; `ApplyPraiseBoost()` ise oyuncuya 5 saniyelik `1.2x` hız boost'u verir. Oyuncu bu fırsatla hızlıca gişesine döner ve normal işlemlere devam eder.

**Müdürden Aferin! Boostu:** VIP müşterinin müdür odasına teslim edildiği ve kapının kapandığı anda oyuncu anında geri bildirim alır. Müdür odası kapısının üstünde tontiş bir `BRAVO!` / `Well Done!` balonu belirir, kısa ve tatmin edici bir `ding!` sesi çalar ve oyuncunun üzerinde sarı-altın bir parıltı efekti patlar. Bu boost `MobilePlayerController` ve top-down controller hız çarpanını `1.2x` olarak 5 saniyeliğine artırır. Hız etkisi KafeinMode ile çarpan mantığıyla üst üste binebilir; şanslı oyuncu aynı anda kahve + müdür övgüsü yakalarsa dönüş koşusunu çok daha akıcı hisseder.

**Kart Şifre Blokesi Kaldırma:** Müşteri bloke olmuş kartını uzatır. Oyuncu kartı alır, gişedeki mini terminale takar ve mini-game açılırken hareketi geçici olarak donar. Ekranda kırmızı, yeşil ve mavi butonlardan oluşan 3 haneli renk sırası gösterilir; oyuncu doğru sırayla dokunursa kart müşteriye geri verilir ve işlem +5 saniye kazandırır. Yanlış girişte pattern sıfırlanır ve kısa bekleme cezasından sonra tekrar denenir.

**İş Arkadaşı Kesintisi:** 45-60 saniyede bir şirin ve tontiş bir banka çalışanı oyuncunun gişe alanına doğrudan girerek sırayı bypass eder. Aktif gişe işlemi duraklatılır; müşteri beklerken sabır barı yaşına göre baskı yemeye devam eder: yaşlı müşteriler 1.5x, orta yaşlılar 1.0x, gençler 0.7x hızla homurdanır. Oyuncu "Acil Ofis Evrakı"nı alıp `ArchiveDesk` alanına teslim eder, +5 saniye kazanır ve ana işlem kaldığı yerden devam eder.

**Personel İçi Aciliyet Seviyeleri:** `StaffRequestUrgency`, gişeye acil iş getiren personelin durumunu iki kademeye ayırır. 1. derece aciliyet 30 saniyelik rutin penceredir; personelin üstünde sabit sarı evrak/stamp ikonu görünür ve zamanında çözülürse +50 Gold ile +10 müdür memnuniyeti verir. Süre kaçarsa istek 2. derece kritik hale gelir: personelin yüz/material rengi 2 saniyede kızarır, ikon büyüyüp kırmızıya döner, çift ünlem particle efektleri ve gergin ayak sesi çalar. Kritik 10 saniye içinde kurtarılırsa +100 Gold, `Phew! Thank you!` feedback'i ve +25 müdür memnuniyeti kazanılır; kaçırılırsa ilgili masa kilitlenir, failure buzzer çalar ve `ManagerSatisfactionSystem` 25 puan düşer.

**Müşteri Yaşı ve Sabır Psikolojisi:** Sabır düşüş hızı karakter yaşına göre doğrudan çarpanla hesaplanır: Genç müşteri telefonuna/kulaklığına gömüldüğü için `0.7x`, orta yaşlı müşteri standart `1.0x`, yaşlı müşteri ise çok daha hızlı homurdanarak `1.5x` sabır kaybeder. Kod tarafındaki temel formül `patienceDrain = baseDrainPerSecond * ageMultiplier * temporaryMultiplier` şeklindedir; iş arkadaşı kesintisi gibi özel durumlar sadece `temporaryMultiplier` ile ekstra baskı ekler.

**Merkezden Nakit Talebi:** Oyuncunun gişe kasası sınırlı sayıda para çekme işlemi yapabilir; prototip varsayılanı 5 işlemdir. Oyuncu veya LazyAssistantAI tarafından tamamlanan her para çekme işlemi `currentVaultCash` değerini 1 azaltır. Kasa boşaldığında para çekme işlemi yapılamaz, vault üstünde `NO CASH` uyarısı belirir ve ekranda `Request Cash Dispatch` butonu açılır. Butona basılınca müşterilerin girdiği ana kapının önüne şirin, beyaz, dondurma arabası boyutunda zırhlı nakit aracı gelir. Bu acil görev sırasında aktif müşteri işlemleri duraklar ama müşterilerin sabır barları yaş psikolojisine göre düşmeye devam eder. Oyuncu dışarı çıkıp ağır `Super Cash Bag` çuvalını alır; çuval taşınırken hareket hızı %20 azalır. Çuval kasaya teslim edilince kasa yeniden dolar, vault çevresinde altın nakit patlama efekti oynar ve mevcut para çekme/yatırma ödülünün `1.5x` katı kadar bonus süre kazanılır.

**Telefon Kesintisi:** Her 30-45 saniyede bir küçük köşe bildirimiyle telefon çalar. Oyuncunun 4 saniyelik halka dolum süresi içinde butona basması gerekir. 1 saniyeden hızlı cevap `2.0x`, 1-2.5 saniye arası `1.5x`, 2.5-4 saniye arası `1.1x` süre çarpanı verir. Cevaplanınca `TimeManager` geri sayımı 2 saniyeliğine donar, oyuncu hareket etmeye ve mevcut işini bitirmeye devam eder ama yeni iş çağırma kontrolleri kilitlenir. Komik hızlı konuşma bittikten sonra `basePhoneReward * multiplier` kadar süre eklenir; kaçırılan çağrıda ödül verilmez ve busy-line feedback'i oynar.

**İki Seviyeli Telefon Araması:** `TwoTierPhoneCallSystem`, gişe telefonunu rutin müşteri araması ve Genel Müdürlük araması olarak iki seviyeye ayırır. Normal müşteri araması sarı `TEL?` sinyaliyle gelir, oyuncuya 15 saniye cevap penceresi verir; cevaplanırsa kısa konuşma feedback'i oynar, +40 Gold ve +5 müdür memnuniyeti kazanılır. Genel Müdürlük araması kırmızı `HQ!!` sinyali, hızlı/tiz zil ve daha sert shake/pulse efektiyle gelir; oyuncunun yalnızca 7 saniyesi vardır. Zamanında cevaplanırsa +200 Gold, +30 müdür memnuniyeti ve 20 saniyelik `Corporate Grace Boost` kazanılır: tüm sıranın sabır düşüşü %50 yavaşlar ve aktif işlem altın ödülleri `1.1x` olur. Kaçırılırsa Genel Müdürlük cezası olarak müdür memnuniyeti 30 puan düşer ve ekranda 3 saniyelik kırmızı `HQ AUDIT FAILED` overlay'i yanıp söner.

**Çaycı Abla / KafeinMode:** Her 50-70 saniyede bir TeaLadyNPC oyuncunun gişesine yakın `TeasideTable` noktasına gelir. Teyze başında tontiş yemeni/eşarp, üzerinde çiçekli önlük ve elinde küçük çay tepsisiyle hafif yalpalayarak yürür; bardağı bırakınca kısa bir el sallama animasyonu oynar ve çıkar. Masaya bırakılan `TeaCup` veya `CoffeeMug` sürekli tüten stylized steam particle ve glow hissiyle tıklanabilir power-up olduğunu belli eder. Oyuncu bardağa doğrudan tıklayınca 8 saniyelik `KafeinMode` başlar: hareket hızı `1.3x` olur, işlem süreleri `0.6x` çarpanla hızlanır, oyuncunun arkasından sarı şimşek/rüzgar çizgileri çıkar ve yumuşak altın overlay ile kalan süre slider'ı görünür. Boost bitince hız, işlem çarpanları ve hız efekti eski haline döner.

**Tea Hospitality Seçimi:** Çaycı Abla artık tepsiyi bıraktığında oyuncuya iki seçenek sunar. `Drink` seçimi oyuncuya 10 saniye `1.3x` hız boost'u verir. `Serve` seçimi oyuncuyu `CarryingTray` animasyon durumuna alır, normal gişe görevlerini geçici kapatır ve 3 porsiyon çay verir. Oyuncu sıradaki müşterilere çay ikram ettikçe her müşteri 40% sabır toparlar, 8 saniye boyunca sabır düşüşü 30% yavaşlar ve üstlerinde buharlı çay/kalp feedback'i oynar. 3 porsiyon bitince veya 12 saniye geçince hospitality modu kapanır.

**Çaycı Abla Ocak Yenileme / Fresh Brew:** `TeaLadyRefillEvent`, Şadiye Abla'nın ara sıra mutfaktan çıkıp oyuncunun gişesine kadar gelmesini sağlar. Elinde boş demlik/bardak ikonu belirir ve ekranın sağ tarafında tek tıklık yeşil `Refill Brew` butonu açılır. Oyuncu butona 15 saniye içinde basarsa tatlı kaynama sesi oynar, Şadiye Abla mutlu şekilde mutfağa döner, +50 Gold ve +10 müdür memnuniyeti kazanılır. Ardından `Fresh Brew Boost` tetiklenir: 15 saniye boyunca sıradaki müşterilerin sabır düşüş hızı %30 yavaşlar ve kuyruk alanında sıcak çay buharı efekti oynar. Oyuncu bekletirse Şadiye Abla iç çekip döner ve müdür memnuniyeti 15 puan düşer.

**Güvenlik Recai Abi Teçhizat Yenileme / High Vigilance:** `SecurityGuardRequestEvent`, Recai Abi'nin ara sıra gişeye telsiz bataryası veya küçük ekipman onayı istemek için gelmesini sağlar. Üstünde düşük pil/telsiz ikonu belirir ve ekranda mavi `Charge Radio` butonu açılır. Oyuncu 15 saniye içinde tek tıkla çözerse telsiz cızırtısı feedback'i oynar, Recai Abi selam verip devriyeye döner, +50 Gold ve +10 müdür memnuniyeti kazanılır. Ardından `High Vigilance Boost` 25 saniye aktif olur: dolandırıcı müşterilerin evrak tutarsızlıkları otomatik vurgulanır ve sıradaki müşterilerin sabır düşüş hızı %30 yavaşlar. Bekletilirse Recai Abi iç çekip devriyeye döner ve müdür memnuniyeti 15 puan düşer.

**Islak Zemin Kazası ve Kurtarma:** `WetFloorAccidentSystem`, Şadiye Abla'nın ara sıra lobiyi paspaslamasını ve geçici ıslak zemin alanı oluşturmasını sağlar. Bu sırada içeri giren müşterilerden biri nadiren kayıp düşebilir; müşteri yerdeyken hareketi durur, üstünde yardım ikonu / yıldız feedback'i görünür ve sabır düşüş hızı `3.0x` olur. Oyuncunun ekranında tek bir kırmızı `Send Guard` butonu belirir. Butona basıldığında Recai Abi koşarak müşterinin yanına gider, 1.5 saniyelik yardım animasyonu oynar ve müşteri tam sabırla tekrar sıraya döner. Başarılı kurtarma +100 Gold, +15 müdür memnuniyeti ve 20 saniyelik `Compassionate Branch Boost` verir; bu boost sırasında tüm sıranın sabır düşüş hızı %40 yavaşlar.

**Hesap Açılışı Yönlendirme:** `AccountOpeningSystem`, `OpenAccount` isteğiyle gelen müşterinin kafasında folder+pen ikonu gösterir. Oyuncu 0.5 saniyelik ıslak kaşe/stamp etkileşimini tamamlayınca müşteri ana gişe slotundan çıkarılır ve `relationshipManagerDesk` noktasına yönlendirilir. Bu hızlı yönlendirme oyuncuya 2 kullanımlık `Quick Boost` verir; sonraki iki standart işlemde tarama/basma gibi processing timer'ları anında tamamlanır.

**Sigorta Yönlendirme / Teamwork Boost:** `InsuranceReferralSystem`, `InsuranceReferral` müşterisi gişeye geldiğinde şemsiye + ev/araba ikonunu gösterir ve oyuncunun terminalinde `Sigortacıya Aktar` butonunu açar. Oyuncu butona basınca tontiş evrak/faks sesi oynar, müşteri ana gişe slotundan çıkarılır ve `InsuranceSpecialistDesk` noktasına yürür. Başarılı yönlendirme `Teamwork Speed Boost` verir: sonraki 12 saniye boyunca fatura tarama, kaşe/mühür, hızlı evrak basma, kart blokesi, para transferi süre baskısı, evrak onayları ve altın ekspertiz gibi counter tabanlı işlem süreleri `0.6x` çarpanla 40% hızlanır. Oyuncunun ellerinde swirl/gold-dust efekti gösterilebilir.

**Barut Müşteri / Acil Sevk:** `RedAlertRedirectionSystem`, kapıdan zaten çok öfkeli gelen `BarutCustomer` tipini yönetir. Bu müşteri kafasında yanıp sönen kırmızı acil ünlem ikonu taşır, sabrı 20% seviyesinden başlar ve 2x hızla erir. Oyuncu kuyruktaki müşteriye doğrudan tıklayarak onu sıranın önüne alabilir; gişeye geldiğinde `Acil Sevk` butonu açılır. Butona basılınca 0.3 saniyelik hızlı yönlendirme animasyonu oynar, müşteri `relationshipManagerDesk` noktasına gönderilir ve gişe slotu boşaltılır. Başarılı kurtarma anında `+200 Gold` verir ve `VIP Relief` uygular: standart kuyruktaki tüm müşterilerin sabır/grumpiness barı 50% toparlanır, ekranda rahatlama/sparkle efekti oynar. Oyuncu Barut müşteriyi bekletirse ve sabrı sıfırlanırsa sistem `CounterIncidentManager` üzerinden gişe skandalı/güvenlik krizini tetikleyebilir.

**Bankadaki Kedi Kaosu / Pati:** `BankCatChaosSystem`, kapıdan içeri giren tontiş sokak kedisi Pati'yi yönetir. `SpawnCat()` çağrıldığında kedi girişten doğar ve 15 saniye boyunca bekleyen müşterilerin arasında dolaşır; bu sırada kuyruktaki müşterilerin sabır düşüşü donar. Süre bitince rastgele bir müşteri `Scared` animasyonuna geçer, kafasında `No Cats` uyarısı çıkar ve panik yüzünden kuyruk sabrı 2x hızla düşmeye başlar. Ekranda yanıp sönen `Call Security` butonu belirir. Oyuncu butona basınca Recai Abi kedinin peşine düşer; kedi kaçış waypoint'leri arasında güvenlikten 1.5x hızlı koşar, güvenlik `Chasing/Reaching` ve komik `Stumble` animasyonları oynar. Kovalamaca izleyenleri eğlendirdiği için sabır düşüşü normale döner. 10 saniye sonra kedi ana kapıdan kaçar, güvenlik `Tired/Panting` animasyonuyla yerine döner ve tüm çarpanlar resetlenir.

**Para Transferi Mini Oyunu:** `WireTransferMiniGame`, `WireTransfer` müşterisi gişeye geldiğinde para + sağ ok ikonunu gösterir ve ekranda el yazısı hissinde 4-5 karakterlik alfanumerik transfer kodu üretir. Oyuncu renkli sanal klavyeden kodu sırayla girer; doğru tuşlar mekanik `Clack!` hissi verir, yanlış tuş resetler. Kod tamamlanınca yeşil `Send` butonu transferi bitirir. Oyuncu sıfır hata ve 5 saniye altında tamamlarsa `Perfect Transfer` boost'u açılır; sonraki 15 saniye `ScoreManager.GoldMultiplier = 1.2x` olur ve işlemlerden 20% fazla altın kazanılır.

**Mobil Bankacılık SMS Aktivasyonu:** `MobileActivationMiniGame`, `MobileActivation` müşterisi gişeye geldiğinde kilit + çarklı akıllı telefon ikonunu gösterir ve terminalde `Send SMS Activation` butonunu açar. Oyuncu butona basınca dijital `Swoosh-beep` sesi ve 0.5 saniyelik loading bar oynar; ardından müşteri telefonunu uzatır ve telefon ekranındaki SMS balonunda 4 haneli rastgele kod görünür. Oyuncu temiz bir numpad ile kodu girer; yanlış rakam telefon UI'ını titreştirir ve input'u resetler, doğru kod `Verify` ile onaylanır. İşlem tamamlanınca müşteri ayrılır ve `Digital Boost` tetiklenir: sonraki 15 saniye boyunca aktif müşteri spawn interval'i 1.3x olur, yani yeni müşteri gelişleri 30% gecikir ve oyuncuya sırayı eritmek için nefes alanı açılır.

**Bağış ve Yardımlaşma / Charity Donation:** `CharityDonationSystem`, `PhilanthropistCustomer` tipindeki yardımsever müşterileri yönetir. Bu müşteriler pamuk gibi davranır; sabırları 100% başlar ve sabır düşüş hızları 50% daha yavaştır. Kafalarında sevgi/bağış kutusu sinyali bulunur. Gişeye geldiklerinde bağış terminali açılır ve müşteri konuşma balonunda bağış kategorisini gösterir: `PAW` hayvanlar/barınaklar, `TREE` doğa/orman, `TOY` çocuklar/yetimhaneler, `CARE` hastalar ve ihtiyaç sahipleri. Oyuncu doğru renkli kategori butonunu seçer, bağış miktarını numpad ile girer ve `DONATE` butonuyla işlemi tamamlar. Yanlış kategori yumuşak hata sesi ve küçük skor cezası verir. Doğru bağışta kalp parçacıkları patlar, sıcak bir chime çalar ve `Karma Boost` uygulanır: 15 saniye boyunca sıradaki müşterilerin sabır tüketimi 40% yavaşlar. Late game'de bu sistem bağış evraklarını doğru kutuya ayırma gibi daha detaylı sorting/categorization bulmacasına büyütülebilir.

**Müdürün Bilgisayar Arızası / Manager IT Support:** `ManagerITSupportEvent`, seviye sırasında her 45 saniyede bir 15% olasılıkla müdür odasında bilgisayar çökmesi tetikler. Müdür kapısının üstünde kırmızı ünlem yanıp söner, kıvılcım/fizzle sesi çalar ve oyuncunun gişeden ayrılıp müdür bilgisayarına gitmesi gerekir. Etkileşimde rastgele mini-game açılır: `BlueScreen` için `Ctrl -> Alt -> Del` sırasına basılır, `LooseCable` için kablo/priz etkileşimi tamamlanır, `OverheatingFan` için basılı tutarak fan temizleme barı doldurulur. Tamir bitince oyuncu +150 Gold kazanır ve `Manager Grace Boost` başlar: 15 saniye boyunca tüm kuyruk sabır tüketimi donar, oyuncu hareket hızı 1.2x olur ve müdürden gelen güçlü övgü hissi oyuncuya nefes aldırır.

**Kırtasiye Desteği / Stationery Delivery:** `StationeryDeliverySystem`, Müşteri İlişkileri veya Sigorta Personeli masasında dönemsel kırtasiye eksikliği yaratır. Eksik masanın üstünde `A4`, `PEN` veya `STAPLER` ikonu çıkar; o masa sorun çözülene kadar yeni yönlendirilmiş müşteri kabul etmez. Oyuncu `SupplyCabinet` menüsünden doğru malzemeyi seçer, karakter `CarryingBox` animasyonuna geçer ve seçilen paketi ilgili masaya taşır. Teslimde +80 Gold kazanılır ve o masaya özel `Efficiency Boost` uygulanır: sonraki 3 müşteri yönlendirmesi masaya 2x hızla akar, yani personelin toparlanmış ve daha verimli çalıştığı hissedilir.

**Kredi Başvurusu / Credit Specialist:** `CreditApplicationSystem`, `CreditApproval` müşterisini üç alt kredi türünden biriyle yönetir: `Housing` için ev/anahtar, `Vehicle` için araba, `Consumer` için alışveriş torbası/nakit sinyali. Müşteri gişeye geldiğinde `Check Credit Score` butonu açılır ve 0.8 saniyelik sorgu barı çalışır. Sonuç 80% ihtimalle `APPROVED`, 20% ihtimalle `DENIED` olur. Reddedilen başvuruda oyuncu `Reject Application` ile müşteriyi gönderir ve +30 Gold kazanır. Onaylanan başvuruda `Refer to Credit Specialist` açılır; müşteri `CreditSpecialistDesk` masasına yönlendirilir ve kredi türüne göre ödül alınır: Konut +120 Gold, Taşıt +80 Gold, İhtiyaç +50 Gold. Başarılı yönlendirme `Credit Boost` tetikler; 15 saniye boyunca Müşteri İlişkileri, Sigorta ve Kredi Uzmanı gibi iç uzman masalarına yapılan yönlendirmeler 1.3x hızlanır.

**Müdür Memnuniyeti / Staff Feast:** `ManagerSatisfactionSystem`, şubenin iç düzenini izleyen 0-100 arası bir müdür memnuniyet barı tutar. Müdür bilgisayarı tamiri +25, kırtasiye teslimi +15, dolandırıcıyı güvenliğe yakalatma +20, Barut müşteri krizini patlamadan sevk etme +15 ve perfect işlem +5 memnuniyet verir. Gişe skandalı -30, dolandırıcıya yanlış onay -25 memnuniyet düşürür. Bar 100 olduğunda sıfırlanır ve `Staff Feast` tetiklenir: müdür şubeye lahmacun/pizza kutuları ve kutlama havası getirir. 20 saniye boyunca oyuncu hızı 1.3x olur, counter tabanlı işlem süreleri 0.5x'e iner, yönlendirme masalarına müşteri akışı 2x hızlanır ve kuyruktaki müşterilerin sabır tüketimi tamamen donar.

**Günlük Görevler ve Başarımlar:** `QuestAndAchievementManager`, oyuncunun zaten yaptığı işleri ekstra hedeflere bağlar. Günlük görev havuzu `Gune Tontis Basla` (3 müşteri memnun yolla), `Sicak Ikram` (1 kez çay dağıt), `Mudurun Gozdesi` (1 BT tamiri), `Sifreyi Coz` (2 SMS/transfer şifre işi), `Dogru Adres` (2 kredi sevki) ve `Pati Sevgisi` (kedi kaosunda güvenlik çağırma) gibi kısa hedefler içerir. Tamamlanan görevler PlayerPrefs'e kaydedilir, altın verir, sağ üstten kayan `GOAL COMPLETED` bildirimi oynatır ve kısa süreli sabır, hız, işlem veya spawn aralığı boost'u uygular. Uzun vadeli achievement listesi dolandırıcı yakalama, perfect tuşlama, Barut müşteri sevki, Staff Feast tetikleme, kırtasiye teslimi, bağış işlemleri ve toplam müşteri servislerini izler; kalıcı yürüme hızı, VIP sevk hızı, ziyafet süresi veya ortam müziği gibi meta ödüller açabilir.

**Dolandırıcı Müşteri / Scammer Detection:** `ScammerDetectionSystem`, `ScammerCustomer` tipini normal bir müşteri gibi sıraya sokar; gişeye geldiğinde dışarıdan standart işlem ikonu taşır ama içeride fraud flag'i aktiftir. Oyuncu evrak inceleme panelini açınca sol tarafta müşterinin gerçek avatar ipuçları, sağ tarafta kimlik kartı gösterilir. Dolandırıcı vakalarının yarısında tek bir tutarsızlık üretilir: fotoğraf uyuşmazlığı, 1999/silinmiş son kullanma tarihi veya resmi mühür yerine sahte patili damga. Oyuncu yanlışlıkla `Approve` ederse dolandırıcı kaçar, `-150 Gold` cezası ve 3 saniyelik `FAILED AUDIT` kilidi gelir. `Decline` doğru kullanılırsa +50 Gold kazanılır. Tutarsızlık varken `Call Security` seçilirse Recai Abi dolandırıcıyı yakalar, oyuncu +100 Gold alır ve `Hero Employee` boost'u tetiklenir; 10 saniye boyunca sıradaki müşterilerin homurdanma düşüşü tamamen donar.

**Gişe Skandalı / Counter Meltdown:** Aktif gişedeki müşterinin sabır barı işlem sırasında sıfırlanırsa `CounterIncidentManager` devreye girer. Açık işlem UI'ları ve workflow'lar iptal edilir, müşteri `AngryGesticulation` animasyonuna geçer, kırmızı ünlem feedback'i ve bağırma sesi oynar. Oyuncunun `PlayerGold` bakiyesinden 100 altın düşülür ve ekranda kırmızı `-100 Gold` feedback'i çıkar. `SecurityGuardAI` gişeye sprint atar, müşteriyi koluna takıp ana kapıdan dışarı çıkarır ve sonra güvenlik noktasına döner. Bu sırada oyuncuya 10 saniyelik `Panic Attack` debuff uygulanır: hareket hızı 30% düşer, fatura tarama, hızlı işlem, evrak onayı, altın ekspertiz ve kaşe süreleri 25% uzar; oyuncunun kafasında sweat-drop efektleri gösterilebilir.

**Tembel Yardımcı Atıştırmalık Mekaniği:** Oyuncunun gişe çekmecesinde varsayılan 3 adet snack stoğu olan `SnackDrawer` bulunur. Yardımcı yan gişede çalışırken oyuncu çekmeceden `Tontiş Atıştırmalık` alıp asistana götürebilir. Asistan aktifken ve oyuncu snack taşırken etkileşim yapılırsa snack tüketilir, asistan komik bir yeme/munch animasyonu oynar ve molaya çıkmadan önce yapabileceği görev kapasitesi `+1` artar. Bunun bedeli rehavettir: her snack, asistanın servis süresine `1.2x` yavaşlama cezası ekler. Kafasının üstünde küçük kurabiye/bisküvi göstergesi görünür; oyuncu isterse onu daha uzun süre sahada tutar, ama daha uyuşuk çalışmasını göze alır.

**Nadir Soygun Baskını / Heist Raid:** Oyuncu beyaz zırhlı araçtan aldığı ağır `Super Cash Bag` ile şubeye geri girdiğinde nadiren `%10-15` olasılıkla tontiş maskeli bir hırsız ekibi baskın yapar. Baskın sırasında ana süre donar, ışıklar hafif kararır, oyuncu korku ve ağır çuval etkisiyle `%50` yavaşlar; prototipte diz titreme/korku cue'su görünür. Hırsızlar lobide 2-3 kişi olarak durur, kafalarına yamuk yumuk göz delikli çorap maskesi geçirmiştir ve "ELLER!" tarzı sevimli konuşma cue'larıyla kollarını sallayarak tehdit ederler. 3-4 saniyede bir yön değiştirirler; kafalarındaki göz cue kırmızı/açıkken oyuncuya bakarlar, mavi/kapalıyken arkalarını dönerler. Oyuncu kırmızı görüş alanında hareket eder veya butona basmaya çalışırsa `PlayerSpotted` olur, ünlem feedback'i çıkar, 2 saniye donar ve biraz geri resetlenir. Hırsızlar bakmıyorken gişedeki `Police Alarm Button` noktasına sızıp alarmı basarsa sirenler çalar, polisler içeri dalar; panikleyen hırsızlar çuvallarını havaya fırlatıp ellerini kaldırır, tutuklanır, kasa yeniden dolar ve cash delivery ödülü `2.0x` dev bonusla verilir.

### Cash Flow Balance

Kasa döngüsü oyunu cezalandıran değil, taktik karar aldıran bir sistem olarak dengelenir:

- Para yatırma işlemi başarıyla tamamlandığında `currentVaultCash +1` olur ve değer maksimum kasa kapasitesini aşmaz.
- Para çekme işlemi başarıyla tamamlandığında `currentVaultCash -1` olur.
- Kasa sıfıra indiğinde oyuncu dışarı çıkıp zırhlı araçtan nakit almak zorunda kalır; bu sırada gişe boş kalır ve müşteriler homurdanmaya devam eder.
- Büyük nakit çuvalı geri getirildiğinde risk ödüllendirilir: standart işlem ödülü `1.5x` çarpanla bonus süreye dönüşür.

## Codex Geliştirme Sıralaması

1. `MobilePlayerController`: Dokunmatik hareket, elde obje taşıma ve bırakma.
2. `TimeManager`: Süre sistemi, bonus süre feedback'i ve zamanı dondurma desteği.
3. `QueueManager` ve `QuestSpawner`: Tontiş müşterilerin sırayla gelmesi, görev havuzu ve zorluk eğrisi.
4. `BankingActionSystem` ve `CashDeliverySystem`: Para yatırma/çekme, kasa boşalması, dışarıdan beyaz araçla nakit taşıma.
5. `StaffInterruptionSystem` ve `LazyAssistantAI`: Araya giren ofis arkadaşları ve çok sıkışınca gelen tembel yardımcı.

## Teknik Yön

- Ana teknoloji: Unity 6 LTS
- Hedef platform: Android öncelikli mobil
- Dil: C#
- UI: İlk aşamada Unity UI veya UI Toolkit
- Sahne akışı: `Boot -> Login -> MainMenu -> Game`
- Geliştirme akışı: Feature branch + Pull Request

## Marka ve İçerik Notu

Türkiye'deki bankacılık deneyimlerinden esinlenilecek, ancak gerçek banka adları, logoları ve birebir marka benzerlikleri kullanılmadan önce hukuki ve ürün kararı verilecektir. İlk prototipte kurgusal banka isimleriyle ilerlemek daha güvenlidir.
