#!/bin/bash

filename="$HOME/Resimler/EkranG-$(date +%Y%m%d-%H%M%S).png"
grim "$filename" && notify-send "📸 Ekran Görüntüsü Alındı" "$filename"
