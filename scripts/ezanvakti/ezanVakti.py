import requests
from datetime import datetime

# Şehrinizi ve ülkenizi girin
city = "Istanbul"
country = "Turkey"
method = 13  # Diyanet için: 13

def get_prayer_times():
    url = f"https://api.aladhan.com/v1/timingsByCity?city={city}&country={country}&method={method}"
    response = requests.get(url)
    data = response.json()

    timings = data['data']['timings']
    print(f"\n📿 {city} için Ezan Vakitleri - {data['data']['date']['readable']}")
    for name, time in timings.items():
        print(f"{name:<8}: {time}")

if __name__ == "__main__":
    get_prayer_times()

