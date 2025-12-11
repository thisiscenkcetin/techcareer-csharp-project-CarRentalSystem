namespace CarRentalWeb.Models
{
    public class Car
    {
        public string Plaka { get; set; } = string.Empty;
        public string MarkaModel { get; set; } = string.Empty;
        public double GunlukFiyat { get; set; }
        public string GorselUrl { get; set; } = string.Empty;
        public string Kategori { get; set; } = "Sedan"; // SUV, Sedan, Hatchback
        public bool AktifMi { get; set; } = true;

        public Car() { }

        public Car(string plaka, string markaModel, double gunlukFiyat, string gorselUrl, string kategori = "Sedan")
        {
            Plaka = plaka;
            MarkaModel = markaModel;
            GunlukFiyat = gunlukFiyat;
            GorselUrl = gorselUrl;
            Kategori = kategori;
        }
    }
}
