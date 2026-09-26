#!/usr/bin/env bash
# Small helpers for driving the Unity Editor from the CLI during development.
#   source Tools/u.sh
#   ucmd <command> [args...]      run an Editor command (json)
#   ueval '<C# code>'             evaluate C# in the Editor, prints the result
#   uwait_compile                 wait until scripts finish compiling; prints errors
#   ushot <name>                  capture the game view to Assets/Temp/shots/<name>.png
#   uerrors                       print recent console errors
UPROJ="C:/project/VishwaMoger_MiniPlatformer"

ucmd() { unity command "$@" --no-banner --format json --project-path "$UPROJ" 2>&1; }

ueval() {
  ucmd eval --code "$1" | python -c "import sys,json
d=json.load(sys.stdin)
r=(d.get('data') or {}).get('result') or {}
if not d.get('success'): print('EVAL FAILED:', d.get('errors'))
else:
  diag=r.get('diagnostics') or []
  for x in diag: print('diag:', x)
  print(r.get('result'))"
}

uwait_compile() {
  sleep 2
  for i in $(seq 1 90); do
    s=$(ucmd console_status | grep -E '"compiling"' | head -1)
    case "$s" in *false*) break;; esac
    sleep 2
  done
  ucmd console_status | grep -E 'compilationFailed'
}

uerrors() {
  ucmd console --level error --tail "${1:-30}" | grep -o '"message": *"[^"]*' | sed 's/"message": *"//' | cut -c1-300 | sort | uniq -c | sort -rn | head -25
}

ushot() {
  mkdir -p "$UPROJ/Assets/Temp/shots"
  ucmd capture_game_view --source screen --save_path "Temp/shots/$1.png" --width "${2:-1600}" --height "${3:-900}" | grep -E '"savedPath"|message'
}
