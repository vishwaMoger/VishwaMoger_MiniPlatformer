#!/usr/bin/env bash
# Enter play mode and wait until the game has booted (UI exists), then optionally run DevConsole commands
# and wait (up to 15 min) for them to finish.
#   Tools/play.sh                     just enter play mode
#   Tools/play.sh "scene s1_shores T1" "sheet T1 12"
cd "$(dirname "$0")/.." || exit 1
source Tools/u.sh
r=$(ueval 'return UnityEditor.EditorApplication.isPlaying.ToString();' 2>/dev/null | tail -1)
if [ "$r" != "True" ]; then
  rm -f Temp/devcmd.txt Temp/devstate.txt
  ueval 'UnityEditor.EditorApplication.isPlaying = true; return "ok";' >/dev/null
fi
for i in $(seq 1 60); do
  r=$(ueval 'return UnityEditor.EditorApplication.isPlaying + " " + (UnityEngine.GameObject.Find("UI") != null);' 2>/dev/null | tail -1)
  [ "$r" = "True True" ] && break
  sleep 2
done
sleep 2
[ $# -eq 0 ] && { echo "playing"; exit 0; }
P=Temp
rm -f "$P/devstate.txt"
printf '%s\n' "$@" > "$P/devcmd.txt"
for i in $(seq 1 900); do [ -f "$P/devstate.txt" ] && break; sleep 1; done
cat "$P/devstate.txt" 2>/dev/null || echo "(no state after 15 min)"
