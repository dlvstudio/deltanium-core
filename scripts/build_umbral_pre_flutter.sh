#!/usr/bin/env bash
set -euo pipefail

# Build Umbral PRE native library for Flutter (Android/macOS optional iOS placeholder)
# Usage:
#   scripts/build_umbral_pre_flutter.sh [--android] [--macos] [--ios] --app-dir /path/to/deltanium-app
# Example:
#   scripts/build_umbral_pre_flutter.sh --android --macos --app-dir ../deltanium-app

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
CRATE_DIR="${ROOT_DIR}/native/umbral_pre"
APP_DIR=""
DO_ANDROID=false
DO_MACOS=false
DO_IOS=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --app-dir)
      APP_DIR="$2"; shift 2 ;;
    --android)
      DO_ANDROID=true; shift ;;
    --macos)
      DO_MACOS=true; shift ;;
    --ios)
      DO_IOS=true; shift ;;
    *)
      echo "[WARN] Unknown arg: $1"; shift ;;
  esac
done

if [[ -z "${APP_DIR}" ]]; then
  echo "[ERROR] --app-dir is required (path to deltanium-app)" >&2
  exit 1
fi

if ! command -v cargo >/dev/null 2>&1; then
  echo "[ERROR] cargo not found. Install Rust toolchain (e.g., rustup)." >&2
  exit 1
fi

echo "[INFO] Using CRATE_DIR=${CRATE_DIR}"
echo "[INFO] Using APP_DIR=${APP_DIR}"

pushd "${CRATE_DIR}" >/dev/null

if ${DO_ANDROID}; then
  echo "[INFO] Building Android .so via cargo-ndk..."
  if ! command -v cargo-ndk >/dev/null 2>&1; then
    echo "[ERROR] cargo-ndk not found. Install with: cargo install cargo-ndk" >&2
    exit 1
  fi
  AND_JNI_DIR="${APP_DIR}/android/app/src/main/jniLibs"
  mkdir -p "${AND_JNI_DIR}/arm64-v8a" "${AND_JNI_DIR}/armeabi-v7a" "${AND_JNI_DIR}/x86_64"
  cargo ndk -t arm64-v8a -o "${AND_JNI_DIR}/arm64-v8a" build --release
  cargo ndk -t armeabi-v7a -o "${AND_JNI_DIR}/armeabi-v7a" build --release || true
  cargo ndk -t x86_64 -o "${AND_JNI_DIR}/x86_64" build --release
  # cargo-ndk places lib in per-abi folder automatically
  echo "[OK] Android libs copied to ${AND_JNI_DIR}/{arm64-v8a,armeabi-v7a,x86_64}"
fi

if ${DO_MACOS}; then
  echo "[INFO] Building macOS .dylib..."
  # Try native arch first
  cargo build --release
  DYLIB_PATH_NATIVE="${CRATE_DIR}/target/release/libumbral_pre.dylib"
  if [[ ! -f "${DYLIB_PATH_NATIVE}" ]]; then
    echo "[ERROR] Build failed or lib not found at ${DYLIB_PATH_NATIVE}" >&2
    exit 1
  fi
  MACOS_DEST_DIR="${APP_DIR}/macos" # Place in project; you'll need to embed/copy at bundle step
  mkdir -p "${MACOS_DEST_DIR}/Frameworks"
  cp -f "${DYLIB_PATH_NATIVE}" "${MACOS_DEST_DIR}/Frameworks/libumbral_pre.dylib"
  echo "[OK] macOS dylib copied to ${MACOS_DEST_DIR}/Frameworks/libumbral_pre.dylib"
  echo "[NOTE] Ensure the dylib is embedded in the app bundle (via Xcode or CMake) or adjust loader to absolute path."
fi

if ${DO_IOS}; then
  echo "[INFO] Building iOS static lib (arm64 + simulator) via cargo-lipo (if installed)..."
  if ! command -v cargo-lipo >/dev/null 2>&1; then
    echo "[WARN] cargo-lipo not found. Install with: cargo install cargo-lipo" >&2
  else
    RUSTFLAGS="-C lto=fat" cargo lipo --release
    # Place artifact hint; integration into Xcode project remains manual
    IOS_OUT_DIR="${CRATE_DIR}/target/universal/release"
    echo "[OK] iOS universal static library at ${IOS_OUT_DIR}/libumbral_pre.a (if supported by crate)."
    echo "[NOTE] Add the static lib/xcframework to Runner and link in Build Phases."
  fi
fi

popd >/dev/null

echo "[DONE] build_umbral_pre_flutter.sh finished."


