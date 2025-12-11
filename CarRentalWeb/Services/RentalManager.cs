using CarRentalWeb.Models;
using System.Text.Json;

namespace CarRentalWeb.Services
{
    public class RentalManager
    {
        private static List<Car> _cars = new();
        private static List<Reservation> _reservations = new();
        private readonly string _dataPath;

        public RentalManager(string dataPath = "")
        {
            // Birden fazla olası yol dene
            if (string.IsNullOrEmpty(dataPath))
            {
                var baseDir = Directory.GetCurrentDirectory();
                var possiblePaths = new[]
                {
                    Path.Combine(baseDir, "Data", "data.json"),
                    Path.Combine(baseDir, "CarRentalWeb", "Data", "data.json"),
                    Path.Combine(baseDir, "..", "CarRentalWeb", "Data", "data.json"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "data.json")
                };

                _dataPath = possiblePaths.FirstOrDefault(File.Exists) ?? possiblePaths[0];
            }
            else
            {
                _dataPath = dataPath;
            }

            LoadData();
        }

        // ==================== CAR MANAGEMENT METHODS ====================

        /// <summary>
        /// Verilen tarih aralığında müsait araçları getirir.
        /// </summary>
        public List<Car> MusaitAraclariGetir(DateTime baslangic, DateTime bitis)
        {
            if (baslangic >= bitis)
                return new List<Car>();

            var musaitAraclar = new List<Car>();

            foreach (var arac in _cars.Where(a => a.AktifMi))
            {
                if (AracMusaitMi(arac.Plaka, baslangic, bitis))
                {
                    musaitAraclar.Add(arac);
                }
            }

            return musaitAraclar;
        }

        /// <summary>
        /// Belirli bir araç ve tarih aralığında müsait olup olmadığını kontrol eder.
        /// </summary>
        public bool AracMusaitMi(string plaka, DateTime bas, DateTime bit)
        {
            if (bas >= bit)
                return false;

            var arac = _cars.FirstOrDefault(a => a.Plaka == plaka);
            if (arac == null || !arac.AktifMi)
                return false;

            // Çakışan rezervasyonları kontrol et
            var cakilanRezervasyonlar = _reservations.Where(r =>
                r.Plaka == plaka &&
                !(r.BitisTarihi <= bas || r.BaslangicTarihi >= bit)
            ).ToList();

            return !cakilanRezervasyonlar.Any();
        }

        /// <summary>
        /// Belirli bir araçın günlük fiyatını getirir.
        /// </summary>
        public double AracGunlukFiyatiniGetir(string plaka)
        {
            var arac = _cars.FirstOrDefault(a => a.Plaka == plaka);
            return arac?.GunlukFiyat ?? 0;
        }

        // ==================== RESERVATION METHODS ====================

        /// <summary>
        /// Yeni bir rezervasyon ekler.
        /// </summary>
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

        /// <summary>
        /// Belirli bir tarih aralığı için rezervasyon ücretini hesaplar.
        /// </summary>
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

            return gunlukFiyat * gunSayisi;
        }

        /// <summary>
        /// Belirli bir plaka için en son rezervasyonu iptal eder.
        /// </summary>
        public void RezervasyonIptal(string plaka)
        {
            var rezervasyon = _reservations.LastOrDefault(r => r.Plaka == plaka);
            if (rezervasyon != null)
            {
                _reservations.Remove(rezervasyon);
                SaveData();
            }
        }

        // ==================== REPORTING METHODS ====================

        /// <summary>
        /// Toplam geliri hesaplar (tüm rezervasyonlardan).
        /// </summary>
        public double ToplamGelir()
        {
            return _reservations.Sum(r => r.ToplamUcret);
        }

        /// <summary>
        /// Belirli bir müşterinin tüm rezervasyonlarını getirir.
        /// </summary>
        public List<Reservation> MusteriRezervasyonlariniGetir(string musteri)
        {
            return _reservations.Where(r => r.MusteriAdi.ToLower() == musteri.ToLower()).ToList();
        }

        /// <summary>
        /// En çok kiralanan araçın plakasını getirir.
        /// </summary>
        public string EnCokKiralananArac()
        {
            var gruplanmis = _reservations.GroupBy(r => r.Plaka)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            return gruplanmis?.Key ?? "Veri Yok";
        }

        // ==================== DATA PERSISTENCE ====================

        /// <summary>
        /// Verileri JSON dosyasından yükler.
        /// </summary>
        private void LoadData()
        {
            try
            {
                if (File.Exists(_dataPath))
                {
                    var json = File.ReadAllText(_dataPath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var data = JsonSerializer.Deserialize<DataContainer>(json, options);
                    
                    if (data != null)
                    {
                        _cars = data.Cars ?? new List<Car>();
                        _reservations = data.Reservations ?? new List<Reservation>();
                    }
                }
                else
                {
                    InitializeDefaultData();
                    SaveData();
                }
            }
            catch
            {
                InitializeDefaultData();
            }
        }

        /// <summary>
        /// Verileri JSON dosyasına kaydeder.
        /// </summary>
        private void SaveData()
        {
            try
            {
                var directory = Path.GetDirectoryName(_dataPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory!);

                var data = new DataContainer { Cars = _cars, Reservations = _reservations };
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_dataPath, json);
            }
            catch
            {
                // Sessiz hata yönetimi
            }
        }

        /// <summary>
        /// Varsayılan örnek veriler ile başlatır.
        /// </summary>
        private static void InitializeDefaultData()
        {
            _cars = new List<Car>
            {
                new Car("34ABC123", "Toyota Corolla 2024", 2500, "https://images.unsplash.com/photo-1621007947382-bb3c3994e3fb?w=800&h=600&fit=crop", "Sedan"),
                new Car("34DEF456", "Renault Clio 5", 2650, "https://images.unsplash.com/photo-1617814076367-b759c7d7e738?w=800&h=600&fit=crop", "Hatchback"),
                new Car("34GHI789", "BMW 3 Series", 3800, "https://images.unsplash.com/photo-1555215695-3004980ad54e?w=800&h=600&fit=crop", "Sedan"),
                new Car("34JKL012", "Ford EcoSport", 2700, "https://images.unsplash.com/photo-1611859266238-4b98091d9d9b?w=800&h=600&fit=crop", "SUV"),
                new Car("34VWX234", "Hyundai i20", 2550, "https://images.unsplash.com/photo-1619767886558-efdc259cde1a?w=800&h=600&fit=crop", "Hatchback")
            };

            _reservations = new List<Reservation>
            {
                new Reservation("Ahmet Yılmaz", "34ABC123", DateTime.Now.AddDays(-5), DateTime.Now.AddDays(-2), 750),
                new Reservation("Fatma Özdemir", "34DEF456", DateTime.Now.AddDays(-3), DateTime.Now, 1050)
            };
        }

        // ==================== HELPER METHODS ====================

        /// <summary>
        /// Tüm araçları getirir.
        /// </summary>
        public List<Car> TumAraclariGetir()
        {
            return _cars.Where(a => a.AktifMi).ToList();
        }

        /// <summary>
        /// Tüm rezervasyonları getirir.
        /// </summary>
        public List<Reservation> TumRezervasyonlariGetir()
        {
            return _reservations.ToList();
        }

        /// <summary>
        /// Container class for JSON serialization
        /// </summary>
        public class DataContainer
        {
            public List<Car>? Cars { get; set; }
            public List<Reservation>? Reservations { get; set; }
        }
    }
}
