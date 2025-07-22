#!/bin/bash

# Cihaz tanımları
SPEAKER="alsa_output.pci-0000_00_1f.3-platform-skl_hda_dsp_generic.HiFi__Speaker__sink"
HEADPHONES="alsa_output.usb-0600_KYRUSE-00.analog-stereo"

# Mevcut varsayılan sink
CURRENT=$(pactl get-default-sink)

# Geçiş yap
if [ "$CURRENT" = "$SPEAKER" ]; then
    pactl set-default-sink "$HEADPHONES"
    NEW="$HEADPHONES"
else
    pactl set-default-sink "$SPEAKER"
    NEW="$SPEAKER"
fi

# Açık ses akışlarını yeni sink'e taşı
for input in $(pactl list short sink-inputs | awk '{print $1}'); do
    pactl move-sink-input "$input" "$NEW"
done

# Bildirim gönder
notify-send "🎧 Ses çıkışı değiştirildi" "Yeni çıkış: $NEW"

