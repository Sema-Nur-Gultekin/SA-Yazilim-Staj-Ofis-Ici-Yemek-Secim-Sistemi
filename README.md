# 🍽️ Ofis İçi Yemek Seçim Sistemi

> Kurumsal ofislerde personelin haftalık yemek menülerini görüntüleyebildiği, tercihini
> sayfa yenilenmeden kaydedebildiği; yöneticilerin ise menü, stok, reçete ve üretim
> süreçlerini uçtan uca yönetip gün sonu raporlarını Excel olarak dışa aktarabildiği,
> çok kiracılı (Multi-Tenant) bir ASP.NET MVC uygulaması.

Bu proje, **staj dönemimde** temel bir MVP (Minimum Viable Product) olarak
hayata geçirilmiş, **staj sonrasında ise** kişisel geliştirme sürecimde kapsamlı bir
şekilde olgunlaştırılarak üretime hazır bir seviyeye taşınmıştır. Bu doküman,
hangi çalışmanın hangi döneme ait olduğunu net bir şekilde ayırt edebilmeniz için
iki ana bölüm halinde hazırlanmıştır.

---

## İçindekiler

- [Genel Bakış](#genel-bakış)
- [Kullanılan Teknolojiler](#kullanılan-teknolojiler)
- [Mimari](#mimari)
- [🎓 Staj Döneminde Yapılanlar](#-staj-döneminde-yapılanlar)
- [🚀 Staj Sonrası Yapılan Geliştirmeler](#-staj-sonrası-yapılan-geliştirmeler)
- [Güvenlik](#güvenlik)
- [Kurulum](#kurulum)
- [Proje Yapısı](#proje-yapısı)
- [Sürüm Notu](#sürüm-notu)

---

## Genel Bakış

Sistem; yönetici ve personel olmak üzere rol bazlı iki farklı kullanıcı deneyimi sunar:

- Personel haftalık menüyü görüntüler, yemek seçimini AJAX ile sayfa yenilenmeden kaydeder.
- Yönetici; menü, yemek, kategori, öğün, stok, reçete, üretim, kullanıcı ve şirket
  ayarlarını tek bir panelden yönetir.
- Gün sonu raporları Excel olarak dışa aktarılabilir.
- Aynı veritabanı üzerinde birden fazla şirketi destekleyen Multi-Tenant mimariye sahiptir.
- Yönetim paneli; günlük özet istatistikler, 7 günlük işlem grafiği, düşük stok ve
  eksik menü kurulumu gibi konularda **anlık ve kalıcı uyarılar** sunar.

---

## Kullanılan Teknolojiler

| Katman | Teknoloji |
|---|---|
| Framework | ASP.NET MVC 5 (.NET Framework 4.7.2) |
| ORM | Entity Framework 6 (Code First + Migrations) |
| Database | SQL Server |
| Authentication | Forms Authentication |
| UI | Bootstrap 5, Font Awesome |
| AJAX | jQuery AJAX |
| Tablolar | DataTables.js (sıralama, sayfalama) |
| Grafikler | Chart.js |
| Bildirim | Toastr.js & SweetAlert2 |
| Excel Export | ClosedXML |
| Parola Güvenliği | BCrypt.Net |
| E-posta (SMTP) | System.Net.Mail |

---

## Mimari

### Multi-Tenant

Her şirket kendi kullanıcılarını, menülerini, stoklarını ve seçimlerini izole
şekilde yönetebilir; tüm sorgular `CompanyID` üzerinden filtrelenir.

### Flat-Table Veritabanı Tasarımı

Bu projede performans ve raporlama kolaylığı amacıyla **Foreign Key ilişkileri
bilinçli olarak kullanılmamıştır.** Tablolar doğrudan ID alanları üzerinden
çalışacak şekilde tasarlanmıştır.

Avantajları:

- Daha öngörülebilir sorgular
- Karmaşık JOIN bağımlılığının azaltılması
- Daha basit raporlama
- İş kurallarının uygulama katmanında yönetilmesi

### Arayüz

**Admin**
- Sidebar destekli yönetim paneli
- Menü, öğün, yemek, kategori, kullanıcı, stok, reçete, üretim yönetimi
- Gün sonu raporu, personel seçimleri, değerlendirmeler
- Şirket ayarları
- Özet istatistik kartları, grafikler ve kalıcı uyarı bildirimleri içeren dashboard

**Personel**
- Sade, sidebar içermeyen, haftalık menü odaklı ekran
- Tek tıklamayla seçim, tek tıklamayla değerlendirme

---

## 🎓 Staj Döneminde Yapılanlar

Aşağıdaki başlıklar, stajım süresince tasarlanıp geliştirilen **temel mimari ve
çekirdek işlevselliği** kapsar.

### Temel Altyapı
- ASP.NET MVC 5 (.NET Framework 4.7.2) proje iskeletinin kurulması
- Entity Framework 6 Code First ile Multi-Tenant, flat-table veritabanı tasarımı
- Forms Authentication ile kimlik doğrulama, rol bazlı (Admin/User) yetkilendirme
- BCrypt ile parola hashleme
- CSRF koruması (`ValidateAntiForgeryToken`)
- Yetkisiz erişim (401/403) sayfası

### Yönetim Paneli (Admin)
- Sidebar destekli yönetim arayüzü
- Haftalık menü yönetimi (ekleme/düzenleme)
- Yemek kataloğu yönetimi
- Yemek kategori yönetimi
- Kullanıcı (personel) yönetimi
- Personel seçimlerinin listelenmesi
- Yemek değerlendirmelerinin listelenmesi
- Gün sonu raporu ve **Excel çıktısı** (ClosedXML)
- Soft Delete (pasife alma) yaklaşımının temel modüllerde uygulanması
- Geçmiş tarihli menülerin düzenlenmesinin engellenmesi

### Personel Ekranı
- Sade, sidebar içermeyen haftalık menü görünümü
- AJAX tabanlı, sayfa yenilenmeden yemek seçimi

### Planlanan Ancak Aktif Olmayan Modüller
Aşağıdaki modüllerin **veritabanı modelleri ve iş mantığı kodları staj döneminde
yazılmış**, ancak arayüze bağlanmadan, kod içinde yorum satırına alınmış
("gizlenmiş") ve aktif sürüme dahil edilmemiş şekilde bırakılmıştır:

- Stok Yönetimi
- Reçete Yönetimi
- Üretim Yönetimi
- Şirket Ayarları

> Bu dört modülün **etkinleştirilmesi, hatalarının giderilmesi ve sistemin geri
> kalanıyla tam entegre hale getirilmesi** staj sonrası döneme aittir (aşağıya bakınız).

---

## 🚀 Staj Sonrası Yapılan Geliştirmeler

Staj sonrasında proje, kişisel geliştirme sürecimde **16 maddelik bir iyileştirme
planı** çerçevesinde uçtan uca gözden geçirilmiş; onlarca yeni özellik eklenmiş,
gizli kalmış modüller etkinleştirilmiş ve kapsamlı bir güvenlik/veri bütünlüğü
denetiminden geçirilmiştir.

### 1. Arayüz ve Kullanılabilirlik Standardizasyonu
- Şirket logosuna uyarlanabilir, tek noktadan yönetilen **kurumsal renk paleti** (CSS değişkenleri)
- Tüm listelerde **sıralanabilir sütun başlıkları** (DataTables)
- Sistem genelinde **standart, tek renkli, ikon tabanlı** Düzenle / Durum Değiştir / Sil butonları
- **Çift tıklama koruması**: hem modal formlarında hem genel formlarda, gönderim sırasında buton otomatik devre dışı kalır
- Tam **responsive (mobil/tablet)** uyum: tablolar, kartlar ve haftalık menü ızgarası taşırmadan daralır
- **Doğrudan yazdırma** desteği (CSS print + `window.print()`) — tüm yönetim sayfalarında
- Personel ekranındaki yemek kartlarının admin ekranına göre daha kompakt tasarlanması
- Haftalık menünün **Pazartesi–Pazar** (7 gün) olarak gösterilmesi

### 2. Kullanıcı Logları ve Veri Bütünlüğü
- `ActivityLog` altyapısı: sistemdeki her ekleme/düzenleme/silme/durum değişikliği
  **kim, ne zaman, hangi kayıt üzerinde** bilgisiyle kayıt altına alınır (hassas veri
  hiçbir zaman loglanmaz)
- **Silme koruması**: menüde veya personel seçimlerinde kullanılan bir yemek/malzeme
  silinemez; açıklayıcı hata mesajıyla engellenir
- **Kullanıcı soft-delete**: personel silindiğinde artık kalıcı olarak silinmez,
  pasife alınır — aksi halde geçmiş seçim/değerlendirme kayıtları sahipsiz kalıp
  raporları bozardı (staj sonrası tespit edilen bir veri bütünlüğü hatasının düzeltilmesi)
- Pasife alınan kullanıcılar artık sisteme giriş yapamaz

### 3. Bildirimler
- "Seçim yapmayan personel" listesi (admin panelinde)
- Personel ekranında, seçim yapılana kadar ekranda kalan **kalıcı uyarı**
- Admin dashboard'da, günün menü kurulumunda eksik kalan öğün/kategori/düşük stok
  durumları için **kalıcı, çok noktadan beslenen uyarı bandı**

### 4. Dashboard ve Raporlama
- Özet istatistik kartları (bugünkü işlem sayısı, aktif yemek/personel sayısı, günün menüsü)
- **En çok kullanılan işlemler** (son 30 gün) listesi
- **Son 7 gün işlem yoğunluğu** çubuk grafiği (Chart.js)
- **Bugün seçim yapan personel oranı** göstergesi
- **Son işlemler** akışı (kim, ne zaman, ne yaptı)

### 5. Haftalık Menü Görünümü (Admin)
- Kullanıcı ekranındakine benzer, ancak admin için büyütülmüş, salt-okunur haftalık
  menü özet ekranı; hafta ileri/geri gezinme ve yazdırma desteğiyle

### 6. Görsel Desteği
- Yemeklere fotoğraf yükleme (uzantı + boyut + **dosya imzası/magic-number** doğrulamalı)
- Yönetim listelerinde ve personel ekranında görsellerin gösterimi

### 7. Sunucu Taraflı Sayfalama
- Yemek kataloğunda, kayıt sayısı arttıkça performansı korumak için gerçek
  sunucu taraflı (`Skip`/`Take`) sayfalama altyapısı

### 8. SMTP ile Şifremi Unuttum
- Güvenli, tek kullanımlık, süre sınırlı token mekanizması
- BCrypt ile şifre hash'leme, sunucu taraflı token doğrulama
- Google Workspace/Gmail SMTP ile e-posta gönderimi

### 9. Gizli Kalmış Modüllerin Etkinleştirilmesi ve Entegrasyonu
Staj döneminde yazılıp yorum satırına alınmış dört modül, tek tek incelenip
hataları giderilerek etkinleştirilmiş ve **birbiriyle bağlantılı çalışan tek bir
sistem** haline getirilmiştir:

- **Stok Yönetimi**: kategori bazlı organizasyon, kontrollü "stok girişi" akışı,
  **stok hareket geçmişi (ledger)**, düşük stok uyarısı
- **Reçete Yönetimi**: yemek başına malzeme/miktar tanımı
- **Üretim Yönetimi**: üretim kaydı girildiğinde, ilgili reçeteye göre **stoktan
  otomatik düşüm**; kayıt silinirse/düzenlenirse stoğun otomatik iadesi/düzeltilmesi
- **Şirket Ayarları**: şirket unvanı, adres, iletişim bilgilerinin yönetimi
- **Merkezi Birim Dönüşüm Servisi**: tüm birimler Ağırlık / Hacim / Adet olmak üzere
  üç kategoriye ayrılmış; reçete ile stok kalemi arasında **kategori uyuşmazlığı
  olan bir dönüşüm asla yapılamaz** (hem sunucu tarafında hem arayüzde engellenir)

### 10. Menü Kontenjanı (Kapasite Planlaması)
- Staj döneminden kalma, hiç kullanılmayan bir alanın tamamlanması: yöneticiler artık
  bir yemek için azami porsiyon sayısı (kontenjan) tanımlayabilir
- Kontenjan dolduğunda personel ekranında seçim otomatik olarak kapanır
- Üretim planlamasıyla doğrudan bağlantılı, aşırı üretim/israfı önleyici bir özellik

### 11. Alerjen Görünürlüğü
- Veritabanında var olan ama personele hiç gösterilmeyen alerjen bilgisi artık
  her yemeğin altında açıkça listelenir

### 12. Güvenlik Sertleştirmesi (Audit)
- Script enjeksiyonuna (stored XSS) karşı JSON verilerinin `<script>` bloklarına
  güvenli şekilde gömülmesi
- Görsel yükleme uçlarında dosya imzası doğrulaması
- Kullanıcı ekleme formunda e-posta format ve parola uzunluğu doğrulamalarının
  eklenmesi
- Tüm silme işlemlerinde native `confirm()` yerine tutarlı SweetAlert2 onay pencereleri
- Eksik kalan `ActivityLog` kayıtlarının tamamlanması

---

## Güvenlik

- BCrypt parola hashleme
- HttpOnly Cookie
- `ValidateAntiForgeryToken` (CSRF koruması) — tüm POST uçlarında
- Rol bazlı Authorization
- Soft Delete yaklaşımı (Yemek, Öğün, Kategori, Stok, **Kullanıcı**)
- Dosya yükleme uçlarında uzantı + boyut + **dosya imzası** doğrulaması
- Kullanıcı girdilerinin script bağlamına güvenli şekilde aktarılması (XSS sertleştirmesi)
- Şifre sıfırlama token'larının sunucu taraflı, süre sınırlı doğrulanması
- Kapsamlı, kim-ne-zaman-ne-yaptı bilgisi tutan kullanıcı logları

---

## Kurulum

```bash
git clone <repo-url>
```

Visual Studio 2019/2022 ile açın.

NuGet paketlerini geri yükleyin:

```powershell
Update-Package -reinstall
```

`Web.config` içindeki Connection String'i ve (isteğe bağlı) SMTP ayarlarını düzenleyin.

Migration'ları uygulayın:

```powershell
Add-Migration TumGuncellemeler
Update-Database
```

Projeyi IIS Express üzerinden çalıştırın.

---

## Proje Yapısı

```text
Controllers/
Models/
Views/
Data/
Filters/
Services/       (staj sonrası: ActivityLogger, EmailService, UnitHelper)
App_Start/
Migrations/
Content/
Scripts/
```

---

## Sürüm Notu

Bu README, projenin **staj dönemi çıktısı** ile **staj sonrası bağımsız geliştirme
sürecimin** çıktısını net bir şekilde ayırt edebilmeniz amacıyla hazırlanmıştır.
Roadmap'te "altyapı seviyesinde planlanmış, aktif sürümün parçası değil" olarak
belirtilen dört modül (Stok, Reçete, Üretim, Şirket Ayarları), staj sonrası süreçte
etkinleştirilerek sistemin ayrılmaz bir parçası haline getirilmiştir.
