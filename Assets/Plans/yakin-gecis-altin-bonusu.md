# Project Overview
- **Game Title:** endless (mevcut productName).
- **High-Level Concept:** Mevcut sonsuz koşu oyununa, engellere temas etmeden çok yakın geçen oyuncuyu ekstra altınla ödüllendiren küçük bir özellik eklenir. Hareket, engel üretimi, çarpışma, iksir, skor ve kayıt sistemleri yeniden tasarlanmaz.
- **Players:** Mevcut tek oyunculu ve ağ üzerinden çok oyunculu modlar korunur. Tek oyunculu mod da MultiplayerManager.StartOfflineGame üzerinden NGO host başlattığı için ağ uyumu gereklidir.
- **Inspiration / Reference Games:** Yeni referans oyun belirtilmedi; mevcut oyunun risk–ödül döngüsü temel alınır.
- **Tone / Art Direction:** Mevcut görsel stil korunur; kısa, okunaklı altın renkli bildirim.
- **Target Platform:** Android.
- **Screen Orientation / Resolution:** Portre; mevcut HUD referansı 1080 × 1920, mevcut ekran ayarları değişmez.
- **Render Pipeline:** URP / Mobile_RPAsset; Unity 6000.3.10f1, URP 17.3.0.

# Game Mechanics
## Core Gameplay Loop
Koş → engelin yanından temas etmeden kaç → başarılı yakın geçiş tamamlanınca 3 ek altın kazan → “Yakın geçtin!” bildirimi gör → koşuya devam et. Riskli fakat başarılı kaçışın anlık geri bildirimi MDA açısından beceri ve heyecanı destekler.

### Kullanıcının seçtiği yaklaşım
- **Ayrı mesafe sistemi:** Mevcut fizik colliderlarının boyutu, trigger durumu ve katmanları değiştirilmez. Mesafe karakter/engel pivotları arasında değil, mevcut collider yüzeyleri arasındaki boşluk olarak yorumlanır.
- **Ödül:** Varsayılan 3 altın, Inspector’dan değiştirilebilir. Bu özellik için sabit bonus uygulanır; GoldCollectible ve mevcut iksir çarpanı davranışı değiştirilmez.
- Alternatif ek trigger alanları değerlendirildi ancak prefab/fizik yapılarına daha fazla müdahale gerektirdiği için seçilmedi.

### Başarı kuralları
- Yakınlık eşiği dünya birimi cinsindendir; daha büyük değer daha kolay bonus verir. Başlangıç önerisi 1.0; gerçek collider ölçekleriyle test edilerek ayarlanır.
- Oyuncu geçiş esnasında en az bir kez eşik içindeki pozitif yüzey boşluğuna girmeli ve engeli tamamen geçmelidir. Yaklaşırken, henüz kaçış tamamlanmadan ödeme yapılmaz.
- Mevcut koşu ekseni X’tir: oyuncular azalan X yönünde koşar, engeller artan X yönünde hareket eder. Engel colliderının arka sınırı oyuncu colliderının önünden tamamen ayrıldığında geçiş tamamlanır.
- Engelin yanından yakın geçiş esas alınır; uzaktan paralel koşma veya engelin çok üstünden atlama yakın geçiş sayılmaz. X örtüşme penceresinde gerçek 3B yüzey yakınlığı doğrulanır.
- Aynı engel örneği / oyuncu çifti en fazla bir kez ödüllendirilir. Birden fazla collider ödülü çoğaltmaz; farklı engeller ayrı değerlendirilir.
- Gerçek temas/örtüşme varsa ödül verilmez; ölüm, can hakkıyla kurtulma ve iksir sayesinde içinden geçme bonus üretmez. Temas kaydı ölüm colliderı trigger’a çevrilmeden ve/veya obje silinmeden korunur.
- Ölü oyuncu, duraklatılmış oyun, bağlantısı kesilmiş oyuncu, disable/despawn edilen engel ödül üretmez. Kaybolan engel için bekleyen yakınlık otomatik başarıya dönüştürülmez.
- Ağ açıkken yalnızca sunucu karar verir; ödül yalnızca ilgili OwnerClientId’ye iletilir. İstemciler kendileri altın miktarı veya başarı talebi göndermez.

## Controls and Input Methods
Yeni kontrol eklenmez. Mevcut sürükleme, swipe ile zıplama ve PlayerInput event akışı korunur. ProjectSettings içindeki activeInputHandler=1 doğrulandı; Input System 1.18.0 kullanılıyor.

# UI
Mevcut uGUI HUD üzerine geçici, etkileşimsiz bir TextMeshProUGUI etiketi eklenir; menüler ve mevcut sayaçların yerleri değiştirilmez.

```text
┌──────────────────────────────┐
│ Mevcut skor / altın / pause   │
│                              │
│        Yakın geçtin!         │ ← Üst-orta, sayaçların altında
│                              │
│       Mevcut oyun alanı       │
│                              │
│        Mevcut iksir HUD       │
└──────────────────────────────┘
```
- Metin tam olarak **Yakın geçtin!** olur. Varsayılan gösterim süresi 1 saniye, kısa fade-out; süre Inspector’dan ayarlanabilir.
- Başlangıçta görünmez. Yeni başarı geldiğinde aynı etiketin süresi yenilenir; sınırsız coroutine/etiket birikmez.
- Raycast Target kapalı; dokunma, pause ve swipe kontrollerini engellemez.
- Mevcut LiberationSans SDF fontu yeniden kullanılır; “Yakın geçtin!” karakter desteği read-only sorguyla doğrulandı. Paylaşılan font assetleri değiştirilmez; sabit font boyutu, mevcut HUD ile uyumlu renk/outline kullanılır.
- Sahneye bağlı UI event abonelikleri OnDisable’da temizlenir. Ölüm, menüye dönüş ve yeni koşuda bildirim sıfırlanır. Pause esnasında yeni bildirim/ödül oluşmaz.
- Yerleşim mevcut HUD_Canvas altında izole bir alt grup olur; diğer Canvas ve CanvasScaler ayarlarına dokunulmaz. Mevcut HUD_Canvas YAML localScale değeri sıfır göründüğü için uygulama sırasında gerçek Play Mode görünümü doğrulanır; bu özellik bahanesiyle tüm HUD yeniden düzenlenmez.

# Key Asset & Context
## İncelenen mevcut bağlam
- `Assets/Scenes/SampleScene.unity`: Oyun sahnesi; HUD_Canvas Screen Space Overlay, 1080×1920. EngelYoneticisi ve ağ sistemi bulunuyor.
- `Assets/Scenes/MainMenuScene.unity`: Açık sahne, kapsam dışında; ana menüden oyun sahnesine geçiş testi yapılır.
- `Assets/Scripts/EngelKontrol.cs`, `BariyerKontrol.cs`: Engel hareketi, OnTriggerEnter/OnCollisionEnter, iksir/ölüm davranışları.
- `Assets/Scripts/EngelYoneticisi.cs`: Ortam prefab havuzundan engel üretir; spawn/hız sistemi korunur.
- `Assets/Scripts/KarakterKontrol.cs`: `AktifKarakterler`, `IsAlive`, `OwnerClientId`, koşu yönü ve ölümde `SaveRunGold()`.
- `Assets/Scripts/GoldManager.cs`: `AddGold(int)` koşu altınını, HUD eventini ve gold görev ilerlemesini günceller. Bonus bu mevcut giriş noktasını kullanır; ayrıca PlayerPrefs yazılmaz.
- `Assets/Scripts/GoldCollectible.cs`: Mevcut sunucu kararı + hedefli ClientRpc altın akışı örnek alınır; dosya değiştirilmez.
- `Assets/Scripts/MultiplayerManager.cs`, `PauseManager.cs`, `GoldUI.cs`, `PlayerPotionController.cs`: Yaşam döngüsü, pause, HUD ve iksir bağlantıları incelendi.
- NGO 2.13.1, uGUI 2.0.0, Localization 1.5.12, Test Framework 1.6.0 mevcut; paket eklenmez/değiştirilmez.

## Oluşturulacak dosyalar
1. `Assets/Scripts/YakinGecisYoneticisi.cs`: Scene-bound NetworkBehaviour; merkezi Inspector ayarları, sunucu/bağımsız mod kontrolü, oyuncu–engel çiftinin durumu, tek seferlik ödül ve hedefli RPC.
2. `Assets/Scripts/YakinGecisEngeli.cs`: Hafif MonoBehaviour; mevcut colliderları önbelleğe alır, aktif engel kaydı tutar, temas bilgisini iletir, disable/destroy temizliği yapar. Yeni fizik colliderı eklemez.
3. `Assets/Scripts/YakinGecisUI.cs`: Yerel başarı eventini dinleyip TMP bildirimi gösterir/gizler.
4. Gerekirse `Assets/Scripts/YakinGecisMesafe.cs`: Saf geometri ve geçiş durumu yardımcıları; renderer/pivot mesafesi yerine collider doğruluğu.
5. `Assets/Tests/Editor/YakinGecisTests.cs` ve yalnızca gerekiyorsa test asmdef: Projedeki test derleme düzeni doğrulandıktan sonra geometri ve durum testleri.

## Değiştirilecek assetler
- `Assets/Scenes/SampleScene.unity`: Yeni YakinGecisYoneticisi scene NetworkObject’ı, ayarlar ve HUD etiketi/referansları. Mevcut ağ sahne spawn düzeni doğrulanarak yeni scene object doğru şekilde spawn edilir; eski NetworkBehaviour sırası değiştirilmez.
- `Assets/Prefabs/DikenliTop.prefab`: YakinGecisEngeli bileşeni; SphereCollider trigger durumu ve büyük mevcut ölçek korunur.
- `Assets/Prefabs/barrier2.prefab`: YakinGecisEngeli bileşeni; BoxCollider trigger durumu/ölçeği korunur.
- Bu iki prefab Ortam_1–Ortam_9 tarafından kullanılır; ortak bileşen tüm ilgili ortamlara otomatik uygulanır. AssetDatabase dependency/dependent incelemesi yapıldı.
- `Assets/Obstacle Pack/Prefabs/barrier2.prefab`, demo/recovery sahneleri, dört karakter prefabı, ortam verileri ve mevcut kaynak dosyaları kapsam dışında tutulur. Yeni collider temas gözlemcisi yeterli olduğu sürece EngelKontrol/BariyerKontrol koduna müdahale edilmez. Beklenmedik bir zorunlu genişleme olursa plan revize edilip tekrar onay istenir.

# Implementation Steps
1. **Bağlantı ve impact kontrolünü tamamla.** Scene NetworkManager’ın scene management/spawn düzenini ve yeni scene NetworkObject’ın host/client üzerinde spawn olmasını doğrula; var olan kaynak/assetler için bağımlılık kontrolünü uygulama öncesinde yenile. Baseline Console, mevcut test düzeni ve HUD görünümünü kaydet. **Dependencies:** Plan onayı.
2. **Engel kayıt bileşenini ekle.** YakinGecisEngeli colliderları cache’ler; OnEnable/OnDisable/OnDestroy kaydı temizler. Collision/trigger girişleri GetComponentInParent<KarakterKontrol> ile oyuncuyu çözer ve temas eden çifti iptal eder; mevcut ölüm/iksir davranışlarını tüketmez veya değiştirmez. İki aktif engel prefabına bağla. **Dependencies:** Adım 1.
3. **Mesafe/geçiş algılamasını uygula.** YakinGecisYoneticisi AktifKarakterler ve kayıtlı engelleri kullanır; sahneyi her frame FindObjects ile taramaz. Önce X/bounds geniş faz elemesi, ardından mevcut box–box ve sphere–box şekillerinde yüzey boşluğu/örtüşme kontrolü yapılır. Bounds tek başına döndürülmüş collider veya küre köşesi için gerçek yüzey mesafesi sayılmaz. ComputePenetration/şekle uygun geometri ile temas ve mesafe ayrılır. Önceki konumlar saklanır; hızlı engellerin pencereyi iki kare arasında atlaması swept/substep değerlendirmeyle ele alınır, gerçek transformlar geçici olarak oynatılmaz. Pair state: yaklaşma → yakın aday → temasla iptal veya geçiş sonrası tek ödeme. LateUpdate değerlendirmesi collider hareketi ve fizik callback sırasıyla test edilir. **Dependencies:** Adım 2.
4. **Ödül otoritesini ekle.** Ağ açıkken server-only değerlendirme ve OwnerClientId hedefli ClientRpc; bağımsız testte doğrudan yerel akış. Önce pair ödüllendirilmiş işaretlenir, sonra alıcıda bir kere GoldManager.AddGold uygulanıp yerel başarı event’i yayınlanır. Host için ayrıca ikinci doğrudan ödeme yapılmaz. Uzak oyuncu başarısı yerel altını/UI’ı değiştirmez. **Dependencies:** Adım 3.
5. **HUD ve Inspector bağlantılarını ekle.** Merkezi `yakinlikMesafesi` (öneri 1.0), `bonusAltin` (3) ve etkinlik anahtarı; UI `gosterimSuresi` (1.0 saniye), TMP referansı ve fade süresi. Tooltip ve Min/OnValidate ile negatif değerleri engelle; 0 eşik/0 ödül bonusu kapatsın. Bildirimi HUD_Canvas’a bağla, pause/ölüm/reset temizliği yap. **Dependencies:** Adım 4.
6. **Regresyon ve teslim.** Aşağıdaki testleri yap; gerçek değişiklik listesi ve Inspector’da ayarın konumunu kullanıcıya bildir. Başarısı doğrulanmayan multiplayer/build testlerini yapılmış gibi raporlama. **Dependencies:** Adımlar 1–5.

# Verification & Testing
- **Temel başarı:** Dikenli top ve bariyerin iki yanından eşik içi temassız geçiş → tam 3 altın ve bir bildirim. Eşik dışı geçiş → 0 altın/0 bildirim.
- **Mesafe sınırları:** Eşikte, çok az altında/üstünde; collider ölçeği/rotation değişikliği; küre köşelerinde AABB yanlış pozitifleri; yakın fakat henüz geçilmemiş engel → erken ödeme yok.
- **Tek ödeme:** Yakın bölgede birkaç frame kalma, tekrar içeri girme, çoklu collider, aynı frame iki callback → aynı çift için en fazla 3 altın. İki ayrı başarılı engel → 6 altın.
- **Çarpışma:** Doğrudan temas, temas sonrası can hakkıyla kurtulma, görünmezlik/süzülme/ölümsüzlükte engelin içinden geçme → yakın geçiş ödülü yok; mevcut iksir/ölüm işleyişi aynı kalır.
- **Yaşam döngüsü:** Ölü oyuncu, pause, despawn/destroy/disable engel, client disconnect, sahne yeniden başlatma, ana menü dönüşü → artık event/pair kaydı veya eski bildirim kalmaz.
- **Yüksek hız:** Mevcut hız iksiri ve yüksek engel hızlarında, 30/60 FPS’de geçiş penceresi atlanmadan doğru sonuç; sahte swept temasları nedeniyle uzaktaki oyuncuya ödül verilmez.
- **Ağ:** Tek oyunculu host; iki oyunculu host+client oturumu. Her biri aynı engeli ayrı geçebilir. Sadece kazananın koşu altını ve bildirimi güncellenir; host çift ödül almaz; client yerel geometri/RPC çağrısıyla ödül üretemez. Sahne object spawn ve RPC teslimatı doğrulanır.
- **Kayıt/görev regresyonu:** Normal altın toplama değişmez; bonus mevcut gold görev ilerlemesine AddGold üzerinden bir kez yansır; ölümde mevcut SaveRunGold toplamı doğru kaydeder, yeniden koşuda sıfırlanır.
- **Karakter/ortam:** Dört seçilebilir karakter prefabı ve iki engeli kullanan ortamlar; player collider boyutları farklı olsa da ayar yüzey boşluğu anlamını korur.
- **UI:** 1080×1920 ve farklı portre oranlarında okunaklı tam Türkçe metin; HUD/pause/iksir üst üste binmez; TMP fontu değişmez; etiket touch engellemez; art arda bildirimde coroutine birikmez.
- **Inspector:** Mesafeyi artırmak bonus alanını büyütür, azaltmak daraltır; ödül/süre değişimi işler, negatif/0 değerler güvenlidir.
- **Derleme:** Unity Console’da yeni hata yok; EditMode testleri Test Runner’dan, davranış testleri Play Mode’da çalıştırılır. Android build/smoke test mevcut SDK ve build ortamı elveriyorsa yapılır. Paket kaynakları, define sembolleri ve proje render/input ayarları değiştirilmez.
