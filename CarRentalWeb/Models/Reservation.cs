namespace CarRentalWeb.Models
{
    public class Reservation
    {
        public string MusteriAdi { get; set; } = string.Empty;
        public string Plaka { get; set; } = string.Empty;
        public DateTime BaslangicTarihi { get; set; }
        public DateTime BitisTarihi { get; set; }
        public double ToplamUcret { get; set; }
        public DateTime OlusturulmaTarihi { get; set; }

        public Reservation() 
        { 
            OlusturulmaTarihi = DateTime.Now;
        }

        public Reservation(string musteriAdi, string plaka, DateTime baslangic, DateTime bitis, double toplam)
        {
            MusteriAdi = musteriAdi;
            Plaka = plaka;
            BaslangicTarihi = baslangic;
            BitisTarihi = bitis;
            ToplamUcret = toplam;
            OlusturulmaTarihi = DateTime.Now;
        }
    }
}
