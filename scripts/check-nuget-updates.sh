#!/usr/bin/env bash
# Reports newer versions of pinned dependencies without modifying anything.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PACKAGES_PROPS="$ROOT/Directory.Packages.props"

echo "=== Pinned package versions (Directory.Packages.props) ==="
grep 'PackageVersion' "$PACKAGES_PROPS" || true
echo

check_nuget_package() {
  local id="$1" pinned="$2"
  local latest
  latest="$(curl -fsSL "https://api.nuget.org/v3-flatcontainer/${id,,}/index.json" 2>/dev/null | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['versions'][-1])" 2>/dev/null || echo "unknown")"
  if [[ "$latest" == "unknown" ]]; then
    echo "[nuget] $id — could not query nuget.org"
  elif [[ "$latest" == "$pinned" ]]; then
    echo "[nuget] $id — up to date ($pinned)"
  else
    echo "[nuget] $id — update available: pinned $pinned -> latest $latest"
  fi
}

while IFS= read -r line; do
  id="$(echo "$line" | sed -n 's/.*Include="\([^"]*\)".*/\1/p')"
  pinned="$(echo "$line" | sed -n 's/.*Version="\([^"]*\)".*/\1/p')"
  check_nuget_package "$id" "$pinned"
done < <(grep 'PackageVersion Include=' "$PACKAGES_PROPS")

echo
echo "Run scripts/bootstrap-deps.sh after manually bumping versions in Directory.Packages.props."
