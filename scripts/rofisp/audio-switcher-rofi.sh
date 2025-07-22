#!/bin/bash

# Mevcut varsayılan çıkışı al
CURRENT_SINK=$(pactl get-default-sink)

# Sink'leri listele ve rofi ile seçtir
SELECTED=$(pactl list short sinks | awk '{print $2}' | rofi -dmenu -p "Ses çıkışı seç (mevcut: $CURRENT_SINK):")

# Kullanıcı bir şey seçtiyse işle
if [ -n "$SELECTED" ]; then
    pactl set-default-sink "$SELECTED"

    # Tüm mevcut uygulamaları yeni çıkışa taşı
    for input in $(pactl list short sink-inputs | awk '{print $1}'); do
        pactl move-sink-input "$input" "$SELECTED"
    done

    notify-send "🔊 Ses çıkışı değiştirildi" "Yeni çıkış: $SELECTED"
fi

