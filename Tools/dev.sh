#!/usr/bin/env bash
# Send commands to the running game's DevConsole (editor play mode) and print the resulting state.
#   Tools/dev.sh "level 0" "wait 3" "hold right 2" state
P="C:/project/VishwaMoger_MiniPlatformer/Temp"
rm -f "$P/devstate.txt"
printf '%s\n' "$@" > "$P/devcmd.txt"
for i in $(seq 1 240); do [ -f "$P/devstate.txt" ] && break; sleep 0.5; done
cat "$P/devstate.txt" 2>/dev/null || echo "(no state - is the game running?)"
