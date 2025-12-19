#!/usr/bin/env bash

set -euo pipefail

show_help() {
  cat <<'EOF'
Usage: kill-target.sh [--force] <path-to-locked-file>

Stops any process that has loaded the given file. Without --force a prompt
is shown before killing processes.
EOF
}

force=false
target=""

while (($# > 0)); do
  case "$1" in
    -f|--force)
      force=true
      shift
      ;;
    -h|--help)
      show_help
      exit 0
      ;;
    *)
      if [[ -n "$target" ]]; then
        echo "Unexpected argument: $1" >&2
        show_help
        exit 1
      fi
      target="$1"
      shift
      ;;
  esac
done

if [[ -z "$target" ]]; then
  echo "Error: target file is required" >&2
  show_help
  exit 1
fi

if command -v realpath >/dev/null 2>&1; then
  target_path=$(realpath "$target")
else
  target_path=$(python - "$target" <<'PY'
import os, sys
print(os.path.abspath(sys.argv[1]))
PY
)
fi

pids=$(lsof -t -- "$target_path" 2>/dev/null || true)

if [[ -z "$pids" ]]; then
  echo "No processes are locking $target_path"
  exit 0
fi

if ! $force; then
  echo "Processes locking $target_path:"
  while IFS= read -r pid; do
    ps -o pid= -o comm= -p "$pid" || echo "  $pid (unable to resolve name)"
  done <<< "$pids"

  read -r -p "Kill these processes? [y/N] " reply
  if [[ ! "$reply" =~ ^[Yy](es)?$ ]]; then
    echo "Aborting without killing processes."
    exit 0
  fi
fi

while IFS= read -r pid; do
  kill -- "$pid" || true
done <<< "$pids"
