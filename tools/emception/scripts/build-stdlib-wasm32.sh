#!/usr/bin/env bash
# Build Rust stdlib for wasm32-unknown-emscripten and deploy to sysroot
# Usage: bash scripts/build-stdlib-wasm32.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
EMCEPTION_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
SYSROOT_DIR="$EMCEPTION_DIR/sysroot"
RUST_TARGET="wasm32-unknown-emscripten"
WASM32_LIBDIR="$SYSROOT_DIR/usr/lib/rustlib/$RUST_TARGET/lib"

echo "=== Rust $RUST_TARGET Stdlib Build ==="
echo ""

# Phase 1.1: Check toolchain
echo "[1/4] Checking Rust toolchain..."
RUSTC_SYSROOT=$(rustc --print sysroot)
echo "  Rustc sysroot: $RUSTC_SYSROOT"

# Verify target
if ! rustup target list --installed | grep -q "$RUST_TARGET"; then
    echo "  $RUST_TARGET not installed, adding..."
    rustup target add "$RUST_TARGET"
fi
echo "  ✓ $RUST_TARGET available"
echo ""

# Phase 1.2: Build stdlib
echo "[2/4] Building Rust stdlib for $RUST_TARGET..."
echo "  This may take 20-30 minutes..."
echo ""

# Determine Rust source directory
RUST_SRC="$RUSTC_SYSROOT/lib/rustlib/src/rust"
if [ ! -d "$RUST_SRC" ]; then
    echo "  ⚠ Rust source not found at $RUST_SRC"
    echo "  Trying to download via rustup..."
    rustup component add rust-src
    RUST_SRC="$RUSTC_SYSROOT/lib/rustlib/src/rust"
fi

if [ ! -f "$RUST_SRC/library/Cargo.toml" ]; then
    echo "  ✗ Could not locate Rust source. Aborting."
    exit 1
fi

echo "  Using Rust source: $RUST_SRC"

# Build with panic_abort to keep binary small
cd "$RUST_SRC"
cargo +nightly build \
    --target "$RUST_TARGET" \
    --release \
    -Z build-std=core,alloc,std,panic_abort \
    -Z build-std-features=panic_immediate_abort \
    --quiet \
    2>&1 | tee "$EMCEPTION_DIR/stdlib_build.log"

BUILD_STATUS=$?
if [ $BUILD_STATUS -ne 0 ]; then
    echo "  ✗ Build failed! See stdlib_build.log for details"
    exit 1
fi
echo "  ✓ Stdlib build complete"
echo ""

# Phase 1.3: Deploy to sysroot
echo "[3/4] Deploying stdlib to sysroot..."
mkdir -p "$WASM32_LIBDIR"

# Copy rlibs from build output
BUILD_STDLIB_DIR="$RUST_SRC/target/$RUST_TARGET/release/deps"
cp -v "$BUILD_STDLIB_DIR"/*.rlib "$WASM32_LIBDIR/" 2>/dev/null || {
    echo "  ⚠ Warning: Could not find rlibs in expected location"
    echo "    Trying alternative path..."
    find "$BUILD_STDLIB_DIR" -name "*.rlib" -exec cp {} "$WASM32_LIBDIR/" \;
}

echo "  ✓ Stdlib deployed"
echo ""

# Phase 1.4: Verify
echo "[4/4] Verifying deployment..."
RLIB_COUNT=$(find "$WASM32_LIBDIR" -name "*.rlib" | wc -l)
if [ "$RLIB_COUNT" -lt 4 ]; then
    echo "  ✗ Expected at least 4 rlibs (core, alloc, std, panic_abort)"
    echo "  Found: $RLIB_COUNT"
    exit 1
fi

echo "  ✓ Found $RLIB_COUNT rlibs"
echo ""
echo "  Stdlib files:"
ls -lh "$WASM32_LIBDIR"/*.rlib | awk '{print "    " $9 " (" $5 ")"}'
echo ""

TOTAL_SIZE=$(du -sh "$WASM32_LIBDIR" | awk '{print $1}')
echo "  Total size: $TOTAL_SIZE"
echo ""

echo "=== SUCCESS ==="
echo "Rust $RUST_TARGET stdlib built and deployed to:"
echo "  $WASM32_LIBDIR"
echo ""
echo "Next: npm run build:cdn && npm run e2e:rust"
