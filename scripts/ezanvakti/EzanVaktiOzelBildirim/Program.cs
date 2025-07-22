using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;

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
        {"Isha", "Yatsı"},
        {"Midnight", "Gece Yarısı"},
        {"Firstthird", "Gece 1/3"},
        {"Lastthird", "Gece 2/3"}
    };

    static async Task Main()
    {
        string city = "Istanbul";
        string country = "Turkey";
        int method = 13; // Diyanet yöntemi

        try
        {
            string url = $"https://api.aladhan.com/v1/timingsByCity?city={city}&country={country}&method={method}";
            using var client = new HttpClient();
            var response = await client.GetStringAsync(url);

            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            if (root.GetProperty("code").GetInt32() != 200)
            {
                Console.WriteLine("Veri alınamadı.");
                return;
            }

            var data = root.GetProperty("data");
            var date = data.GetProperty("date").GetProperty("readable").GetString();
            var timings = data.GetProperty("timings");

            Console.WriteLine($"🕌 {city} için Ezan Vakitleri - {date}\n");

            var now = DateTime.Now;
            DateTime? sonrakiVakitZamani = null;
            string sonrakiVakitAdi = "";

            foreach (var time in timings.EnumerateObject())
            {
                string turkceIsim = TurkceVakitler.ContainsKey(time.Name) ? TurkceVakitler[time.Name] : time.Name;
                string timeStr = time.Value.GetString()!.Split(" ")[0]; // "04:21 (+03)" gibi olabilir
                Console.WriteLine($"{turkceIsim,-12}: {timeStr}");

                // Sadece geçerli formatta saatleri kontrol et
                if (DateTime.TryParseExact(timeStr, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var vakitZamani))
                {
                    // Bugünün tarihiyle birleştir
                    vakitZamani = new DateTime(now.Year, now.Month, now.Day, vakitZamani.Hour, vakitZamani.Minute, 0);

                    if (vakitZamani > now && (sonrakiVakitZamani == null || vakitZamani < sonrakiVakitZamani))
                    {
                        sonrakiVakitZamani = vakitZamani;
                        sonrakiVakitAdi = turkceIsim;
                    }
                }
            }

            if (sonrakiVakitZamani != null)
            {
                TimeSpan kalanSure = sonrakiVakitZamani.Value - now;
                Console.WriteLine($"\n⏳ Sonraki vakit: {sonrakiVakitAdi} - {sonrakiVakitZamani:HH:mm}");
                Console.WriteLine($"🕒 Kalan süre: {kalanSure.Hours} saat {kalanSure.Minutes} dakika");
            }
            else
            {
                Console.WriteLine("\nBugün için tüm vakitler geçmiş.");
            }
            if (sonrakiVakitZamani != null)
            {
                TimeSpan kalanSure = sonrakiVakitZamani.Value - now;
                string mesaj = $"Sonraki vakit: {sonrakiVakitAdi} - {sonrakiVakitZamani:HH:mm}\nKalan süre: {kalanSure.Hours} saat {kalanSure.Minutes} dakika";

                Console.WriteLine($"\n⏳ {mesaj}");

                // Bildirim gönder
                Process.Start("notify-send", $"\"🕌 Ezan Vakti\" \"{mesaj}\"");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Hata oluştu: " + ex.Message);
        }
    }
}


