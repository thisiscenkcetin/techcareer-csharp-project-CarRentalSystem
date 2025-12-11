# Akıllı Araç Kiralama Rezervasyon Sistemi

![C#](https://img.shields.io/badge/C%23-v11-blue?logo=csharp&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-7.0-green?logo=dotnet&logoColor=white)
![Lisans](https://img.shields.io/badge/Lisans-MIT-yellow)
![Durum](https://img.shields.io/badge/Durum-Üretim%20Hazır-brightgreen)

## Özet

**Akıllı Araç Kiralama Rezervasyon Sistemi**, TechCareer'in C# Yazılım Geliştirme programı kapsamında bir bitirme projesidir. Özgün görev bir konsol uygulaması olmakla birlikte, bu uygulama **ASP.NET Core MVC** üzerinde inşa edilmiş modern, duyarlı bir web uygulamasına dönüştürülmüştür.

Sistem, özgün görevden alınan tüm zorunlu işlevsel gereksinimleri uygularken, Nesne Yönelimli Programlama (OOP) ilkeleri ve SOLID tasarım desenleri ile kurumsal mimariye sıkı sıkıya bağlı kalmaktadır.

---

## Temel Özellikler

### Araç Yönetimi
- Tarihi aralıklara göre gerçek zamanlı müsaitlik kontrolü
- Araç başına dinamik günlük ücret yönetimi
- 5+ araçtan oluşan filo ile detaylı özellikler
- Kategori sınıflandırması (Sedan, SUV, Hatchback)

### Rezervasyon Motoru
- Çift kiralama yapılmasını önleyen akıllı çakışma çözümü
- Kiralama süresine bağlı otomatik fiyat hesaplama
- JSON tabanlı kalıcı veri depolaması
- Müşteri rezervasyon takibi ve geçmişi

### Analitik ve Raporlama
- Gerçek zamanlı toplam gelir hesaplaması
- En sık kiralanan araç tanımlama
- Kapsamlı rezervasyon istatistikleri
- Veri çıkartma için RESTful API uç noktaları

---

## Teknik Mimarisi

### Teknoloji Yığını
```
Arka Uç:    ASP.NET Core 7.0 MVC, C# 11
Veritabanı: JSON Dosya Tabanlı Depolama
Ön Uç:      Duyarlı Bootstrap 5.3.0, Vanilla JavaScript
İnşa:       .NET SDK 7.0+
```

### Proje Yapısı
```
CarRentalWeb/
├── Models/
│   ├── Car.cs                 # Araç veri modeli
│   └── Reservation.cs         # Rezervasyon veri modeli
├── Services/
│   └── RentalManager.cs       # Çekirdek iş mantığı (278 satır)
├── Controllers/
│   └── HomeController.cs      # API uç noktaları ve yönlendirme
├── Views/
│   └── Home/
│       └── Index.cshtml       # Tek sayfalı uygulama arayüzü
├── Data/
│   └── data.json             # Kalıcı veri deposu
└── Program.cs                # Bağımlılık enjeksiyonu yapılandırması
```

---

## Zorunlu İşlevsellik Uygulaması

### 1. Müsait Araçları Getirme
**Fonksiyon**: `MusaitAraclariGetir`  
**Konum**: `Services/RentalManager.cs` (Satırlar 41-56)  
**Amaç**: Verilen bir tarih aralığı için tüm müsait araçları getirmek

```csharp
public List<Car> MusaitAraclariGetir(DateTime baslangic, DateTime bitis)
{
    if (baslangic >= bitis)
        return new List<Car>();

    var musaitAraclar = new List<Car>();
    foreach (var arac in _cars.Where(a => a.AktifMi))
    {
        if (AracMusaitMi(arac.Plaka, baslangic, bitis))
            musaitAraclar.Add(arac);
    }
    return musaitAraclar;
}
```

**API Entegrasyonu**: `POST /Home/CheckAvailability`  
**Yanıt**: Tüm detayları ile müsait araçların JSON dizisi

---

### 2. Araç Müsaitliğini Doğrulama
**Fonksiyon**: `AracMusaitMi`  
**Konum**: `Services/RentalManager.cs` (Satırlar 60-78)  
**Amaç**: Belirli bir araçın verilen tarihi aralıkta müsait olup olmadığını doğrulamak

```csharp
public bool AracMusaitMi(string plaka, DateTime bas, DateTime bit)
{
    if (bas >= bit)
        return false;

    var arac = _cars.FirstOrDefault(a => a.Plaka == plaka);
    if (arac == null || !arac.AktifMi)
        return false;

    // Aralık mantığı kullanarak çakışan rezervasyonları algıla
    var cakilanRezervasyonlar = _reservations.Where(r =>
        r.Plaka == plaka &&
        !(r.BitisTarihi <= bas || r.BaslangicTarihi >= bit)
    ).ToList();

    return !cakilanRezervasyonlar.Any();
}
```

**Ana Özellik**: Aralık çakışma algılaması çift kiralama yapılmasını önler  
**Algoritma**: Çakışmaları belirlemek için matematiksel aralık kesişim noktasını kullanır

---

### 3. Rezervasyon Ücretini Hesaplama
**Fonksiyon**: `RezervasyonUcretiHesapla`  
**Konum**: `Services/RentalManager.cs` (Satırlar 113-129)  
**Amaç**: Bir tarih aralığı için toplam kiralama ücretini hesaplamak

```csharp
public double RezervasyonUcretiHesapla(string plaka, DateTime bas, DateTime bit)
{
    if (bas >= bit)
        return 0;

    var gunlukFiyat = AracGunlukFiyatiniGetir(plaka);
    if (gunlukFiyat <= 0)
        return 0;

    var gunSayisi = (bit - bas).Days;
    if (gunSayisi == 0)
        gunSayisi = 1;

    return gunlukFiyat * gunSayisi;  // Günlük ücret × gün sayısı
}
```

**Formula**: `Toplam Ücret = Günlük Ücret (₺) × Kiralama Süresi (gün)`  
**API Uç Noktası**: `POST /Home/CalculatePrice`  
**Ön Uç**: Onaylamadan önce gerçek zamanlı fiyat önizlemesi

---

### 4. Rezervasyon Oluşturma
**Fonksiyon**: `RezervasyonEkle`  
**Konum**: `Services/RentalManager.cs` (Satırlar 97-111)  
**Amaç**: Kapsamlı doğrulama ile yeni rezervasyon oluşturmak

```csharp
public void RezervasyonEkle(string musteri, string plaka, DateTime bas, DateTime bit)
{
    if (!AracMusaitMi(plaka, bas, bit))
        throw new InvalidOperationException("Araç bu tarih aralığında müsait değildir.");

    if (bas >= bit)
        throw new InvalidOperationException("Bitiş tarihi başlangıç tarihinden sonra olmalıdır.");

    var arac = _cars.FirstOrDefault(a => a.Plaka == plaka);
    if (arac == null)
        throw new InvalidOperationException("Araç bulunamadı.");

    double ucret = RezervasyonUcretiHesapla(plaka, bas, bit);
    var rezervasyon = new Reservation(musteri, plaka, bas, bit, ucret);

    _reservations.Add(rezervasyon);
    SaveData();
}
```

**Doğrulama Zinciri**:
1. `AracMusaitMi` aracılığıyla müsaitlik kontrolü
2. Tarih aralığı doğrulaması
3. Araç varlığı doğrulaması
4. Otomatik ücret hesaplaması
5. JSON'a kalıcı depolama

**API Uç Noktası**: `POST /Home/BookCar`

---

### 5. Toplam Geliri Hesaplama
**Fonksiyon**: `ToplamGelir`  
**Konum**: `Services/RentalManager.cs` (Satırlar 151-155)  
**Amaç**: Tüm rezervasyonlardan elde edilen toplam geliri hesaplamak

```csharp
public double ToplamGelir()
{
    return _reservations.Sum(r => r.ToplamUcret);
}
```

**Uygulama**: Tüm rezervasyon ücretleri genelinde LINQ Sum toplaması  
**API Uç Noktası**: `GET /Home/GetReport`  
**Kullanım Alanı**: Gerçek zamanlı işletme analitikleri ve pano ölçümleri

---

### 6. En Çok Kiralanan Aracı Tanımlama
**Fonksiyon**: `EnCokKiralananArac`  
**Konum**: `Services/RentalManager.cs` (Satırlar 167-174)  
**Amaç**: En sık kiralanan aracı belirlemek

```csharp
public string EnCokKiralananArac()
{
    var gruplanmis = _reservations.GroupBy(r => r.Plaka)
        .OrderByDescending(g => g.Count())
        .FirstOrDefault();

    return gruplanmis?.Key ?? "Veri Yok";
}
```

**Algoritma**: Gruplandırma toplamalaması ile azalan sıklık sıralaması  
**Uygulama**: Filonun optimize edilmesi ve talep tahmini

---

### 7. Günlük Fiyatı Getirme
**Fonksiyon**: `AracGunlukFiyatiniGetir`  
**Konum**: `Services/RentalManager.cs` (Satırlar 81-86)  
**Amaç**: Belirli bir araç için günlük kiralama ücretini almak

```csharp
public double AracGunlukFiyatiniGetir(string plaka)
{
    var arac = _cars.FirstOrDefault(a => a.Plaka == plaka);
    return arac?.GunlukFiyat ?? 0;
}
```

**Dönüş Değeri**: Türk Lirası cinsinden günlük ücret (₺)  
**Hata Yönetimi**: Mevcut olmayan araçlar için 0 döndürür

---

## Veri Modelleri

### Araç Varlığı
```csharp
public class Car
{
    public string Plaka { get; set; }           // Plaka (birincil tanımlayıcı)
    public string MarkaModel { get; set; }      // Marka ve model
    public double GunlukFiyat { get; set; }     // Günlük kiralama ücreti (₺)
    public string GorselUrl { get; set; }       // Araç resmi URL'si
    public string Kategori { get; set; }        // Kategori (Sedan, SUV, Hatchback)
    public bool AktifMi { get; set; }           // Aktiflik durumu bayrağı
}
```

### Rezervasyon Varlığı
```csharp
public class Reservation
{
    public string MusteriAdi { get; set; }      // Müşteri adı
    public string Plaka { get; set; }           // Araç plakası
    public DateTime BaslangicTarihi { get; set; } // Giriş tarihi
    public DateTime BitisTarihi { get; set; }   // Çıkış tarihi
    public double ToplamUcret { get; set; }     // Toplam kiralama ücreti
    public DateTime OlusturulmaTarihi { get; set; } // Rezervasyon zaman damgası
}
```

---

## API Referansı

### Araç İşlemleri

| Uç Nokta | Metod | İstek | Yanıt |
|----------|-------|-------|--------|
| `/Home/CheckAvailability` | POST | başlangıçTarihi, bitisTarihi | {başarılı, araçlar[], mesaj} |
| `/Home/CheckCarAvailability` | POST | plaka, başlangıçTarihi, bitisTarihi | {başarılı, müsait, mesaj} |
| `/Home/CalculatePrice` | POST | plaka, başlangıçTarihi, bitisTarihi | {başarılı, fiyat, mesaj} |

### Rezervasyon Yönetimi

| Uç Nokta | Metod | İstek | Yanıt |
|----------|-------|-------|--------|
| `/Home/BookCar` | POST | müşteri, plaka, başlangıçTarihi, bitisTarihi | {başarılı, mesaj, fiyat} |

### Analitik

| Uç Nokta | Metod | Yanıt |
|----------|-------|--------|
| `/Home/GetReport` | GET | {başarılı, toplamGelir, enCokKiralanan, toplamRezervasyonlar} |

---

## Başlarken

### Ön Koşullar
- .NET SDK 7.0 veya daha yüksek
- Modern web tarayıcısı (Chrome, Firefox, Safari, Edge)
- 100 MB boş disk alanı

### Kurulum

1. **Depoyu Klonlayın**
   ```bash
   git clone https://github.com/thisiscenkcetin/techcareer-csharp-project.git
   cd techcareer-csharp-project/CarRentalWeb
   ```

2. **Bağımlılıkları Geri Yükleyin**
   ```bash
   dotnet restore
   ```

3. **Projeyi İnşa Edin**
   ```bash
   dotnet build
   ```

4. **Uygulamayı Çalıştırın**
   ```bash
   dotnet run --urls "http://localhost:5189"
   ```

5. **Tarayıcıda Erişin**
   ```
   http://localhost:5189
   ```

---

## Kullanım Örnekleri

### Müsait Araçları Kontrol Edin
```javascript
const response = await fetch('/Home/CheckAvailability', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
        startDate: '2024-12-15',
        endDate: '2024-12-20'
    })
});

const data = await response.json();
```

### Araç Kiralayın
```javascript
const response = await fetch('/Home/BookCar', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
        musteri: "Ahmet Yılmaz",
        plaka: "34ABC123",
        startDate: '2024-12-15',
        endDate: '2024-12-20'
    })
});

const result = await response.json();
```

### İşletme Analitiklerini Alın
```javascript
const response = await fetch('/Home/GetReport');
const report = await response.json();

console.log(`Toplam Gelir: ₺${report.totalIncome}`);
console.log(`En Çok Kiralanan: ${report.topCar}`);
console.log(`Toplam Rezervasyonlar: ${report.totalBookings}`);
```

---

## Tasarım Desenleri

- **Singleton Deseni**: RentalManager, DI'da hizmet tekil nesnesi olarak kaydedilir
- **Depo Deseni**: JSON dosyası sanal depo olarak işlev görmektedir
- **Doğrulama Deseni**: Açıklayıcı mesajlarla istisnaya dayalı hata yönetimi
- **Tarih Aralığı Mantığı**: Çakışma algılaması için matematiksel aralık karşılaştırması
- **Endişelerin Ayrılması**: Kontrolörler → Hizmetler → Modeller → Veri

---

## Performans

| İşlem | Karmaşıklık | Notlar |
|-------|-------------|--------|
| Müsaitlik Kontrolü | O(n) | Tüm rezervasyonları iteratif olarak inceler |
| Gelir Hesaplaması | O(n) | LINQ Sum toplamması |
| Araç Araması | O(n) | Doğrusal plaka araması |

**Ölçeklenebilirlik**: KOBİ işletmeleri için <100 araç ve <10.000 rezervasyonla uygun

---

## Hata Yönetimi

Sistem kapsamlı hata yönetimi uygular:

```csharp
try
{
    _rentalManager.RezervasyonEkle(musteri, plaka, bas, bit);
    return Json(new { success = true, message = "Başarılı" });
}
catch (InvalidOperationException ex)
{
    return BadRequest(new { success = false, message = ex.Message });
}
```

**Yönetilen Senaryolar**:
- Geçersiz tarih aralıkları (başlangıç ≥ bitiş)
- Mevcut olmayan araçlar
- Çift kiralama denemeleri
- Eksik müşteri bilgileri
- Geçersiz giriş biçimleri

---

## Lisans

MIT Lisansı - Ayrıntılar için [LİSANS](LICENSE) dosyasına bakın

---

## Proje Bilgileri

- **Görev**: TechCareer C# Bitirme Projesi
- **Teslim Edilen**: SPA Arayüzü ile Web Uygulaması
- **Geliştirici**: Cenk ÇETİN [thisiscenkcetin](https://github.com/thisiscenkcetin)


### 1. Genel Senaryo Gereksinimleri
| Durum | Gereksinim | Açıklama |
| :---: | :--- | :--- |
| ✔️ | **Müsaitlik Kontrolü** | Araçların seçilen tarihlerdeki uygunluk durumu kontrol edilmektedir. |
| ✔️ | **Rezervasyon Oluşturma** | Müşteriler müsait araçlar için rezervasyon kaydı oluşturabilmektedir. |
| ✔️ | **Çakışma Engelleme** | Aynı araca ait tarih aralığı çakışmaları (Overbooking) algoritma ile engellenmiştir. |
| ✔️ | **Fiyat Hesaplama** | Kiralama süresi ve araç bazlı fiyatlandırma otomatik hesaplanmaktadır. |
| ✔️ | **Gelir Raporlama** | Firmanın toplam gelir durumu ve finansal raporları sunulmaktadır. |

### 2. Temel Fonksiyonel Özellikler

#### 🚗 Araç Yönetimi
- ✔️ **Araç Bilgileri:** Araçların plaka, marka/model ve günlük fiyat bilgileri veritabanında tutulmaktadır.
- ✔️ **Dinamik Müsaitlik:** Belirli tarihler arasında aracın kiralı/müsait olma durumu anlık sorgulanır.

#### 📅 Rezervasyon Yönetimi
- ✔️ **Yeni Ekleme:** Arayüz üzerinden hızlı ve kolay yeni rezervasyon girişi.
- ✔️ **Çakışma Kontrolü:** Seçilen tarih aralığında araç doluysa sistem uyarı verir ve işlem engellenir.
- ✔️ **Otomatik Ücret:** `(Bitiş Tarihi - Başlangıç Tarihi) * Günlük Fiyat` formülü ile hatasız hesaplama.
- ✔️ **İptal İşlemi:** Mevcut rezervasyonlar sistem üzerinden iptal edilebilir ve araç tekrar boşa çıkar.

#### 📊 Raporlama ve Analiz
- ✔️ **Toplam Gelir:** Tamamlanan kiralamalardan elde edilen ciro hesaplanmaktadır.
- ✔️ **Müşteri Geçmişi:** Belirli bir müşteriye ait tüm eski ve yeni rezervasyonlar listelenir.
- ✔️ **Popüler Araç:** İstatistiksel olarak en çok kiralanan araç/model analiz edilip gösterilir.

---

**Son Güncelleme**: 11 Aralık 2024

