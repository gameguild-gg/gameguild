#!/usr/bin/env bash

set -euo pipefail

# This script generates a video of the contributors to the project using gource.
SOURCE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT_DIR="${OUTPUT_DIR:-${TMPDIR:-/tmp}/gameguild-gource}"

rm -rf "${OUTPUT_DIR}"
mkdir -p "${OUTPUT_DIR}"

find "${SOURCE_DIR}" -maxdepth 1 -type f \
  ! -name 'gource.sh' \
  ! -name 'gource.txt' \
  ! -name 'gource.mp4' \
  ! -name 'gource.gif' \
  -exec cp '{}' "${OUTPUT_DIR}/" \;

gource --output-custom-log "${OUTPUT_DIR}/gource.txt"

while IFS='|' read -r wrong_name name; do
  sed -i "s/|${wrong_name}|/|${name}|/g" "${OUTPUT_DIR}/gource.txt"
done < "${SOURCE_DIR}/name_replacements.txt"

gource -1920x1080 \
  --caption-file "${SOURCE_DIR}/captions.txt" \
  --title GameGuild \
  --user-image-dir "${SOURCE_DIR}" \
  --auto-skip-seconds 0.1 \
  --multi-sampling \
  --stop-at-end \
  --key \
  --highlight-users \
  --hide mouse,filenames \
  --file-idle-time 0 \
  --max-files 0 \
  --seconds-per-day 0.05 \
  --user-scale 2.0 \
  --bloom-multiplier 0.5 \
  --output-ppm-stream - "${OUTPUT_DIR}/gource.txt" \
  | ffmpeg -y -r 60 -f image2pipe -vcodec ppm -i - -vcodec libx264 -crf 28 -pix_fmt yuv420p -threads 0 -bf 0 "${OUTPUT_DIR}/gource.mp4"

ffmpeg -y -i "${OUTPUT_DIR}/gource.mp4" -loop 0 \
  -filter_complex "fps=10,scale=-1:240[s];[s]setpts=PTS/4[speedup];[speedup]split[a][b];[a]palettegen[palette];[b][palette]paletteuse" \
  "${OUTPUT_DIR}/gource.gif"

printf 'Gource assets generated in %s\n' "${OUTPUT_DIR}"