#!/usr/bin/env bash
# Assembles the four CrazyGames preview videos from recorded clips (needs the game running in play mode).
#   Tools/trailer.sh                    cut + encode all four trailers (high bitrate)
#   Tools/trailer.sh medium _upload     smaller copies (<10 MB) named *_upload.mp4
# Clips are recorded with the DevConsole `clip` command (see DevCapture.cs); each clip folder in Temp/capture has an
# info.txt with its frame count, the frame where the route completed and the frame where the hero reached the trophy.
# Every video opens on the static cover from Builds/CrazyGames_Submission (CrazyGames rule), so re-run this after
# replacing the covers.
cd "$(dirname "$0")/.." || exit 1
OUT=Builds/CrazyGames_Submission
CAP=Temp/capture
FPS=30
RATE=${1:-high}
SUF=${2:-}
SHOT=98      # frames per world shot (3.3 s)
FINALE=170   # the last shot runs through the trophy grab (5.7 s)
# 1 s cover + 4 shots + finale = 592 frames = 19.7 s (CrazyGames cuts videos at 20 s)

segment() { # segment <clip> <length> <finale 0|1>  ->  clip:start:length
  local info; info=$(cat "$CAP/$1/info.txt")
  local frames complete won start
  frames=$(echo "$info" | awk '{print $2}')
  complete=$(echo "$info" | awk '{print $4}')
  won=$(echo "$info" | awk '{print $6}')
  if [ "$3" = 1 ] && [ "$won" -ge 0 ]; then
    start=$((won + 14 - $2))                  # end just after the trophy grab
  else
    start=$((complete - 40))                  # last slides, the melt, then the run home
  fi
  [ "$start" -lt 0 ] && start=0
  [ $((start + $2)) -gt "$frames" ] && start=$((frames - $2))
  echo "$1:$start:$2"
}

cut() { # cut <prefix> -> the five segments in season order
  echo "$(segment "${1}_spring" $SHOT 0) $(segment "${1}_autumn" $SHOT 0) $(segment "${1}_rain" $SHOT 0) $(segment "${1}_night" $SHOT 0) $(segment "${1}_snow" $FINALE 1)"
}

# encoding takes a few minutes, longer than Tools/dev.sh waits, so queue the commands and poll
rm -f Temp/devstate.txt
printf '%s\n' \
  "bitrate $RATE" \
  "trailer $OUT/trailer_landscape_1920x1080$SUF.mp4 1920 1080 $OUT/cover_1920x1080.png $(cut c)" \
  "trailer $OUT/trailer_portrait_1080x1620$SUF.mp4 1080 1620 $OUT/cover_800x1200.png $(cut cp)" \
  "trailer $OUT/trailer_gameplay_landscape_1920x1080$SUF.mp4 1920 1080 $OUT/cover_1920x1080.png $(cut g)" \
  "trailer $OUT/trailer_gameplay_portrait_1080x1620$SUF.mp4 1080 1620 $OUT/cover_800x1200.png $(cut gp)" \
  "bitrate high" \
  state > Temp/devcmd.txt
for i in $(seq 1 900); do [ -f Temp/devstate.txt ] && break; sleep 1; done
tail -1 Temp/devstate.txt
ls -la $OUT/*.mp4
