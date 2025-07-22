#!/bin/bash

tempfile=$(mktemp --suffix=.png)
grim -g "$(slurp)" - > "$tempfile" && swappy -f "$tempfile" && notify-send "✏️ Swappy Başlatıldı" "Seçili alanı düzenle"

