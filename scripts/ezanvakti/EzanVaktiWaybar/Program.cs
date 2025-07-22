using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

class Program
{
    static readonly Dictionary<string, string> TurkceKisaltmalar = new()
    {
        {"Fajr", "Sb"},    // İmsak - Sabah
        {"Sunrise", "Gd"}, // Güneş doğuşu
        {"Dhuhr", "Öğ"},   // Öğle
        {"Asr", "İk"},     // İkindi
        {"Maghrib", "Ak"}, // Akşam
        {"Isha", "Yt"}     // Yatsı
    };

    static async Task Main()
    {
        string city = "Istanbul";
        string country = "Turkey";
        int method = 13;

        try
        {
            string url = $"https://api.aladhan.com/v1/timingsByCity?city={city}&country={country}&method={method}";
            using var client = new HttpClient();
            var response = await client.GetStringAsync(url);

            using var doc = JsonDocument.Parse(response);
            var timings = doc.RootElement.GetProperty("data").GetProperty("timings");

            var now = DateTime.Now;
           var nowTimeSpan = now.TimeOfDay;

            var vakitSirasi = new[] { "Fajr", "Sunrise", "Dhuhr", "Asr", "Maghrib", "Isha" };

            (string name, TimeSpan time)? nextVakit = null;

            foreach (var vakit in vakitSirasi)
            {
                if (TimeSpan.TryParse(timings.GetProperty(vakit).GetString(), out var vakitTime))
                {
                    if (vakitTime > nowTimeSpan)
                    {
                        nextVakit = (vakit, vakitTime);
                        break;
                    }
                }
            }

            string kisaltma;
            int kalanDakika;

            if (nextVakit == null)
            {
                // Bugün bitmiş, yarının ilk vakti İmsak
                var imsakStr = timings.GetProperty("Fajr").GetString();
                TimeSpan imsakTime = TimeSpan.Parse(imsakStr);
                var kalan = (TimeSpan.FromHours(24) - nowTimeSpan) + imsakTime;

                kalanDakika = (int)kalan.TotalMinutes;
                kisaltma = TurkceKisaltmalar["Fajr"];
            }
            else
            {
                var kalan = nextVakit.Value.time - nowTimeSpan;
                kalanDakika = (int)kalan.TotalMinutes;
                kisaltma = TurkceKisaltmalar.ContainsKey(nextVakit.Value.name) ? TurkceKisaltmalar[nextVakit.Value.name] : nextVakit.Value.name;
            }

            string kalanStr = FormatKalanSure(kalanDakika);

            var output = new
            {
                text = $"{kisaltma} — {kalanStr} kaldı",
                minutes_left = kalanDakika,
                vakit = kisaltma
            };

            string jsonOutput = JsonSerializer.Serialize(output);
            Console.WriteLine(jsonOutput);
        }
        catch
        {
            Console.WriteLine("{\"text\":\"Ezan vakti alınamadı.\",\"minutes_left\":-1,\"vakit\":\"\"}");
        }
    }

    static string FormatKalanSure(int totalMinutes)
    {
        if (totalMinutes >= 60)
            return $"{totalMinutes / 60} saat {totalMinutes % 60} dk";
        else
            return $"{totalMinutes} dk";
    }
}

