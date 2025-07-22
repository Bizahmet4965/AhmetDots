#!/bin/bash

PROGRAM="dotnet /home/ahmet/.config/scripts/ezanvakti/EzanVaktiWaybar/publish/EzanVaktiWaybar.dll"
TMPFILE="/tmp/ezan_notify_last"

last_notify=""
if [[ -f $TMPFILE ]]; then
    last_notify=$(cat $TMPFILE)
fi

while true; do
    json=$($PROGRAM)
    vakit=$(echo "$json" | jq -r '.vakit')
    kalan_dk=$(echo "$json" | jq -r '.minutes_left')

    if [[ "$kalan_dk" == "-1" ]]; then
        echo "Ezan vakti alınamadı."
        sleep 60
        continue
    fi

    notify=0
    yeni_notify=""

    if [[ "$vakit" == "İk" ]]; then
        if (( kalan_dk <= 45 )) && [[ "$last_notify" != "$vakit-45" ]]; then
            notify=1
            yeni_notify="$vakit-45"
        fi
    else
        if (( kalan_dk <= 20 )) && [[ "$last_notify" != "$vakit-20" ]]; then
            notify=1
            yeni_notify="$vakit-20"
        elif (( kalan_dk <= 15 )) && [[ "$last_notify" != "$vakit-15" ]]; then
            notify=1
            yeni_notify="$vakit-15"
        fi
    fi

    if (( notify == 1 )); then
        notify-send "Ezan Vakti Yaklaşıyor" "$vakit vakti, $kalan_dk dakika kaldı."
        echo "$yeni_notify" > $TMPFILE
        last_notify="$yeni_notify"
    fi

    echo "$json"  # Waybar için çıktı

    sleep 60
done

