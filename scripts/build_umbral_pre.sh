#!/usr/bin/env bash
set -euo pipefail

# Auto-fetch rust-umbral, build a C-ABI shim (umbral_pre), and place the lib next to a target output.
# Source: rust-umbral (Umbral PRE): https://github.com/nucypher/rust-umbral
# This script expects Rust toolchain installed (cargo/rustc). On macOS: brew install rust or use rustup.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
VENDOR_DIR="${PROJECT_ROOT}/.vendor"
SHIM_DIR="${PROJECT_ROOT}/native/umbral_pre"

CONFIG="${1:-Debug}"    # Debug or Release (controls copy target)
TFM="net7.0"             # Default target framework

# Optional explicit output directory for the native library (second param)
DEST_DIR_ARG="${2:-}"

if ! command -v cargo >/dev/null 2>&1; then
  echo "[ERROR] cargo not found. Please install Rust toolchain (e.g., rustup)." >&2
  exit 1
fi

OS="$(uname -s)"
LIB_FILENAME=""
case "$OS" in
  Darwin) LIB_FILENAME="libumbral_pre.dylib" ;;
  Linux)  LIB_FILENAME="libumbral_pre.so"    ;;
  MINGW*|MSYS*|CYGWIN*) LIB_FILENAME="umbral_pre.dll" ;;
  *) echo "[ERROR] Unsupported OS: $OS" >&2; exit 1 ;;
esac

echo "[INFO] Project root: ${PROJECT_ROOT}"
echo "[INFO] Vendor dir:   ${VENDOR_DIR}"
echo "[INFO] Shim dir:     ${SHIM_DIR}"

mkdir -p "${VENDOR_DIR}"
if [ ! -d "${VENDOR_DIR}/rust-umbral" ]; then
  echo "[INFO] Cloning rust-umbral..."
  git clone --depth=1 https://github.com/nucypher/rust-umbral "${VENDOR_DIR}/rust-umbral"
else
  echo "[INFO] Updating rust-umbral..."
  (cd "${VENDOR_DIR}/rust-umbral" && git pull --ff-only)
fi

if [ ! -d "${SHIM_DIR}" ]; then
  echo "[ERROR] Shim directory missing: ${SHIM_DIR}" >&2
  exit 1
fi

echo "[INFO] Building shim (${CONFIG})..."
(cd "${SHIM_DIR}" && cargo build --release)

ARTIFACT_PATH="${SHIM_DIR}/target/release/${LIB_FILENAME}"
if [ ! -f "${ARTIFACT_PATH}" ]; then
  echo "[ERROR] Built artifact not found: ${ARTIFACT_PATH}" >&2
  exit 1
fi

if [ -n "${DEST_DIR_ARG}" ]; then
  OUT_DIR="${DEST_DIR_ARG}"
else
  OUT_DIR="${PROJECT_ROOT}/bin/${CONFIG}/${TFM}"
fi
mkdir -p "${OUT_DIR}"
cp -f "${ARTIFACT_PATH}" "${OUT_DIR}/"
echo "[OK] Copied ${LIB_FILENAME} -> ${OUT_DIR}/"

echo "Done. Next: ensure your .NET app loads ${LIB_FILENAME} from its output folder."


