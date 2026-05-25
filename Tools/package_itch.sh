#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
ITCH_DIR="$ROOT/Builds/Itch"
PACKAGES_DIR="$ITCH_DIR/packages"
PRODUCT="School Project 2025"

mkdir -p "$PACKAGES_DIR"

package_mac() {
  local app="$ITCH_DIR/macOS/$PRODUCT.app"
  local zip_path="$PACKAGES_DIR/$PRODUCT macOS.zip"

  if [[ ! -d "$app" ]]; then
    echo "macOS app not found: $app"
    return 0
  fi

  xattr -cr "$app" || true
  rm -f "$zip_path"
  (cd "$ITCH_DIR/macOS" && zip -qry --symlinks "$zip_path" "$PRODUCT.app")
  echo "macOS package: $zip_path"
}

package_windows() {
  local win_dir="$ITCH_DIR/Windows"
  local exe="$win_dir/$PRODUCT.exe"
  local data_dir="$win_dir/${PRODUCT}_Data"
  local zip_path="$PACKAGES_DIR/$PRODUCT Windows.zip"

  if [[ ! -f "$exe" || ! -d "$data_dir" ]]; then
    echo "Windows build not found yet: $exe + $data_dir"
    return 0
  fi

  rm -f "$zip_path"
  (cd "$win_dir" && zip -qry "$zip_path" . -x "*.DS_Store" "*BurstDebugInformation_DoNotShip*" "*DoNotShip*")
  echo "Windows package: $zip_path"
}

package_mac
package_windows
