using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Linq;

class Program
{
    // Türkçe vakit isimleri sözlüğü
    static readonly Dictionary<string, string> TurkceVakitler = new()
    {
        {"Fajr", "İmsak"},
        {"Sunrise", "Güneş"},
        {"Dhuhr", "Öğle"},
        {"Asr", "İkindi"},
        {"Maghrib", "Akşam"},
        {"Isha", "Yatsı"}
    };

    // Ayarları buradan değiştirebilirsiniz
    static readonly string Sehir = "Istanbul";
    static readonly string Ulke = "Turkey";
    static readonly int KontrolAraligiSaniye = 30;

    static Dictionary<string, DateTime> _gunlukVakitler = new();
    static HashSet<string> _gonderilenBildirimler = new();
    static DateTime _sonYuklemeTarihi = DateTime.MinValue;

    static async Task Main()
    {
        await Task.Delay(TimeSpan.FromSeconds(15)); 
        Console.WriteLine("Ezan Vakti Bildirici başlatıldı. Arka planda çalışıyor...");
        
        while (true)
        {
            try
            {
                if (DateTime.Now.Date > _sonYuklemeTarihi.Date)
                {
                    await VakitleriYukle();
                }

                // YENİ BİLDİRİM MANTIĞI BURADA ÇALIŞIYOR
                VaktiYaklasanIcinBildir();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ana döngüde hata: {ex.Message}");
                await Task.Delay(TimeSpan.FromMinutes(5));
            }

            await Task.Delay(TimeSpan.FromSeconds(KontrolAraligiSaniye));
        }
    }

    static async Task VakitleriYukle()
    {
        Console.WriteLine($"{DateTime.Now}: Yeni gün için vakitler yükleniyor...");
        string url = $"https://api.aladhan.com/v1/timingsByCity?city={Sehir}&country={Ulke}&method=13";

        using var client = new HttpClient();
        var response = await client.GetStringAsync(url);
        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;

        if (root.GetProperty("code").GetInt32() == 200)
        {
            var timings = root.GetProperty("data").GetProperty("timings");
            var today = DateTime.Now;

            _gunlukVakitler.Clear();
            foreach (var time in timings.EnumerateObject())
            {
                if (TurkceVakitler.ContainsKey(time.Name))
                {
                    string timeStr = time.Value.GetString()!.Split(" ")[0];
                    if (DateTime.TryParseExact(timeStr, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTime))
                    {
                        var vakitZamani = new DateTime(today.Year, today.Month, today.Day, parsedTime.Hour, parsedTime.Minute, 0);
                        _gunlukVakitler[TurkceVakitler[time.Name]] = vakitZamani;
                    }
                }
            }

            _sonYuklemeTarihi = DateTime.Now.Date;
            _gonderilenBildirimler.Clear();
            Console.WriteLine("Vakitler başarıyla yüklendi.");
            
            string sonrakiVakitMesaji = SonrakiVakitBilgisiniGetir();
            BildirimGonder("🕌 Ezan Vakti", $"Uygulama aktif. {sonrakiVakitMesaji}");
        }
        else
        {
            Console.WriteLine("API'den veri alınamadı.");
        }
    }

    // === DEĞİŞEN KISIM ===
    // Bu fonksiyon artık vakit girmeden önce haber veriyor.
    static void VaktiYaklasanIcinBildir()
    {
        var now = DateTime.Now;
        foreach (var vakit in _gunlukVakitler)
        {
            // Güneş vakti için bildirim göndermeyi atla
            if (vakit.Key == "Güneş") continue;

            // Her vakit için bildirim süresini belirle
            int oncedenBildirDakika;
            if (vakit.Key == "İkindi")
            {
                oncedenBildirDakika = 75; // İkindi için 1 saat 15 dakika (75 dk)
            }
            else
            {
                oncedenBildirDakika = 45; // Diğer tüm vakitler için 45 dakika
            }

            // Bildirimin gönderileceği tam zamanı hesapla
            DateTime bildirimZamani = vakit.Value.AddMinutes(-oncedenBildirDakika);

            // Bildirim zamanı geldiyse VE bu bildirim daha önce gönderilmediyse
            if (now >= bildirimZamani && now < vakit.Value && !_gonderilenBildirimler.Contains(vakit.Key))
            {
                TimeSpan kalanSure = vakit.Value - now;
                string mesaj = $"{vakit.Key} vaktine yaklaşık {Math.Ceiling(kalanSure.TotalMinutes)} dakika kaldı.";
                
                Console.WriteLine($"Bildirim gönderiliyor: {mesaj}");
                BildirimGonder($"🕌 {vakit.Key} Vaktine Az Kaldı", mesaj);
                
                // Bildirimin gönderildiğini işaretle ki tekrar göndermesin
                _gonderilenBildirimler.Add(vakit.Key);
            }
        }
    }

    static string SonrakiVakitBilgisiniGetir()
    {
        var now = DateTime.Now;
        var sonrakiVakit = _gunlukVakitler
            .Where(v => v.Value > now)
            .OrderBy(v => v.Value)
            .FirstOrDefault();

        if (sonrakiVakit.Key != null)
        {
            TimeSpan kalan = sonrakiVakit.Value - now;
            return $"Sonraki vakit {sonrakiVakit.Key}, kalan süre: {kalan.Hours} saat {kalan.Minutes} dakika.";
        }
        return "Bugün için tüm vakitler geçti.";
    }

    static void BildirimGonder(string baslik, string mesaj)
    {
        var escapedBaslik = baslik.Replace("\"", "\\\"");
        var escapedMesaj = mesaj.Replace("\"", "\\\"");

        var processInfo = new ProcessStartInfo
        {
            FileName = "notify-send",
            Arguments = $"\"{escapedBaslik}\" \"{escapedMesaj}\" -i /home/ahmet/.config/scripts/ezanvakti/EzanVakti/bildrm.png",
            UseShellExecute = false,
        };

        try
        {
            Process.Start(processInfo);
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Bildirim gönderilemedi: {ex.Message}");
        }
    }
}
