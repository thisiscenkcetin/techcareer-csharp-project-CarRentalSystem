using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CarRentalWeb.Models;
using CarRentalWeb.Services;
using System.Text.Json;

namespace CarRentalWeb.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly RentalManager _rentalManager;

    public HomeController(ILogger<HomeController> logger, RentalManager rentalManager)
    {
        _logger = logger;
        _rentalManager = rentalManager;
    }

    // ==================== VIEW ACTIONS ====================

    public IActionResult Index()
    {
        var cars = _rentalManager.TumAraclariGetir();
        return View(cars);
    }

    // ==================== API ACTIONS ====================

    /// <summary>
    /// Belirli tarih aralığında müsait araçları döndürür (AJAX için)
    /// </summary>
    [HttpPost]
    public IActionResult CheckAvailability(string startDate, string endDate)
    {
        try
        {
            if (!DateTime.TryParse(startDate, out var bas) || !DateTime.TryParse(endDate, out var bit))
            {
                return BadRequest(new { success = false, message = "Geçersiz tarih formatı" });
            }

            var musaitAraclar = _rentalManager.MusaitAraclariGetir(bas, bit);
            return Json(new { success = true, cars = musaitAraclar });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Belirli bir araç ve tarih aralığı için fiyat hesaplar
    /// </summary>
    [HttpPost]
    public IActionResult CalculatePrice(string plaka, string startDate, string endDate)
    {
        try
        {
            if (!DateTime.TryParse(startDate, out var bas) || !DateTime.TryParse(endDate, out var bit))
            {
                return BadRequest(new { success = false, message = "Geçersiz tarih formatı" });
            }

            double ucret = _rentalManager.RezervasyonUcretiHesapla(plaka, bas, bit);
            return Json(new { success = true, price = ucret });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Yeni bir rezervasyon oluşturur
    /// </summary>
    [HttpPost]
    public IActionResult BookCar(string musteri, string plaka, string startDate, string endDate)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(musteri) || string.IsNullOrWhiteSpace(plaka))
            {
                return BadRequest(new { success = false, message = "Müşteri adı ve araç plakası gereklidir" });
            }

            if (!DateTime.TryParse(startDate, out var bas) || !DateTime.TryParse(endDate, out var bit))
            {
                return BadRequest(new { success = false, message = "Geçersiz tarih formatı" });
            }

            if (bas >= bit)
            {
                return BadRequest(new { success = false, message = "Bitiş tarihi başlangıç tarihinden sonra olmalıdır" });
            }

            // Müsaitlik kontrolü
            if (!_rentalManager.AracMusaitMi(plaka, bas, bit))
            {
                return BadRequest(new { success = false, message = "Araç seçili tarih aralığında müsait değildir" });
            }

            _rentalManager.RezervasyonEkle(musteri, plaka, bas, bit);
            
            var ucret = _rentalManager.RezervasyonUcretiHesapla(plaka, bas, bit);
            return Json(new { success = true, message = "Rezervasyon başarıyla oluşturuldu!", price = ucret });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Rapor verilerini döndürür (gelir ve en çok kiralanan araç)
    /// </summary>
    [HttpGet]
    public IActionResult GetReport()
    {
        try
        {
            var toplamGelir = _rentalManager.ToplamGelir();
            var enCokKiralananArac = _rentalManager.EnCokKiralananArac();
            var toplamRezervasyonSayisi = _rentalManager.TumRezervasyonlariGetir().Count;

            return Json(new 
            { 
                success = true, 
                totalIncome = toplamGelir, 
                topCar = enCokKiralananArac,
                totalBookings = toplamRezervasyonSayisi
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Belirli bir araç için müsaitlik durumunu kontrol eder
    /// </summary>
    [HttpPost]
    public IActionResult CheckCarAvailability(string plaka, string startDate, string endDate)
    {
        try
        {
            if (!DateTime.TryParse(startDate, out var bas) || !DateTime.TryParse(endDate, out var bit))
            {
                return BadRequest(new { success = false, message = "Geçersiz tarih formatı" });
            }

            bool available = _rentalManager.AracMusaitMi(plaka, bas, bit);
            return Json(new { success = true, available = available });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
