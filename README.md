# 🍽️ Ofis İçi Yemek Seçim Sistemi

> Kurumsal ofislerde personelin haftalık yemek menülerini
> görüntüleyebildiği, tercihini sayfa yenilenmeden kaydedebildiği ve
> yöneticilerin gün sonu üretim planlamasını Excel raporlarıyla
> yönetebildiği çok kiracılı (Multi-Tenant) bir ASP.NET MVC uygulaması.

------------------------------------------------------------------------

## İçindekiler

-   Genel Bakış
-   Teknolojiler
-   Mimari
-   Özellikler
-   Güvenlik
-   Kurulum
-   Proje Yapısı
-   Roadmap

## Genel Bakış

Bu proje **ASP.NET MVC 5 (.NET Framework 4.7.2)** kullanılarak
geliştirilmiş kurumsal bir **Ofis İçi Yemek Seçim Sistemi**dir.

Sistem; yönetici ve personel olmak üzere rol bazlı iki farklı kullanıcı
deneyimi sunar.

-   Personel haftalık menüyü görüntüler.
-   Yemek seçimini AJAX ile sayfa yenilenmeden kaydeder.
-   Yönetici menü, kullanıcı ve raporları yönetir.
-   Gün sonu raporları Excel olarak dışa aktarılabilir.
-   Aynı veritabanı üzerinde birden fazla şirketi destekleyen
    Multi-Tenant mimariye sahiptir.

------------------------------------------------------------------------

## Kullanılan Teknolojiler

  Katman           Teknoloji
  ---------------- --------------------------------------
  Framework        ASP.NET MVC 5 (.NET Framework 4.7.2)
  ORM              Entity Framework 6
  Database         SQL Server
  Authentication   Forms Authentication
  UI               Bootstrap 5
  AJAX             jQuery AJAX
  Notification     Toastr.js & SweetAlert2
  Excel Export     ClosedXML
  Password Hash    BCrypt.Net

------------------------------------------------------------------------

## Mimari

### Multi-Tenant

Her şirket kendi kullanıcılarını, menülerini ve seçimlerini izole
şekilde yönetebilir.

### Flat-Table Veritabanı Tasarımı

Bu projede performans ve raporlama kolaylığı amacıyla **Foreign Key
ilişkileri bilinçli olarak kullanılmamıştır.**

Tablolar doğrudan ID alanları üzerinden çalışacak şekilde
tasarlanmıştır.

Avantajları:

-   Daha öngörülebilir sorgular
-   Karmaşık JOIN bağımlılığının azaltılması
-   Daha basit raporlama
-   İş kurallarının uygulama katmanında yönetilmesi

### Arayüz

**Admin**

-   Sidebar destekli yönetim paneli
-   Menü yönetimi
-   Yemek kataloğu
-   Kategori yönetimi
-   Kullanıcı yönetimi
-   Gün sonu raporu
-   Personel seçimleri
-   Değerlendirmeler

**Personel**

-   Sidebar bulunmaz
-   Haftalık menü odaklı sade ekran
-   Tek tıklamayla seçim

------------------------------------------------------------------------

## Temel Özellikler

-   Forms Authentication
-   Rol bazlı yetkilendirme
-   AJAX ile yemek seçimi
-   Haftalık menü yönetimi
-   Yemek kategori yönetimi
-   Yemek yönetimi
-   Gün sonu raporları
-   Excel çıktısı
-   Soft Delete desteği
-   Çok kiracılı mimari
-   BCrypt ile parola güvenliği
-   CSRF koruması
-   Yetkisiz erişim sayfası
-   Geçmiş tarih kontrolleri

------------------------------------------------------------------------

## Güvenlik

-   BCrypt parola hashleme
-   HttpOnly Cookie
-   ValidateAntiForgeryToken
-   Rol bazlı Authorization
-   Soft Delete yaklaşımı

------------------------------------------------------------------------

## Kurulum

``` bash
git clone <repo-url>
```

Visual Studio 2019/2022 ile açın.

NuGet:

``` powershell
Update-Package -reinstall
```

Connection String'i düzenleyin.

Migration çalıştırın.

``` powershell
Update-Database
```

Projeyi IIS Express üzerinden çalıştırın.

------------------------------------------------------------------------

## Proje Yapısı

``` text
Controllers/
Models/
Views/
Data/
Filters/
App_Start/
Migrations/
Content/
Scripts/
```

------------------------------------------------------------------------

## Roadmap

Aşağıdaki modüller altyapı seviyesinde planlanmış olup aktif sürümün
parçası değildir.

-   Stok Yönetimi
-   Reçete Yönetimi
-   Üretim Yönetimi
-   Şirket Ayarları

------------------------------------------------------------------------

## Lisans

Bu proje eğitim ve geliştirme amaçlı hazırlanmıştır.
