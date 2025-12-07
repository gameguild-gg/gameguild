#!/bin/bash
set -e

# Build mrustc compiler to WASM
# Based on emception's build-cpython.sh approach

if [ "$(uname)" == "Darwin" ]; then
    alias nproc="sysctl -n hw.ncpu"
fi

SRC=$(dirname $0)
BUILD="$1"
MRUSTC_SRC="$2"

if [ "$MRUSTC_SRC" == "" ]; then
    MRUSTC_SRC=$(pwd)/upstream/mrustc
fi

if [ "$BUILD" == "" ]; then
    BUILD=$(pwd)/build
fi

# Ensure folders exist
mkdir -p "$BUILD"

SRC=$(realpath "$SRC")
BUILD=$(realpath "$BUILD")
MRUSTC_BUILD=$BUILD/mrustc

echo "=== Building mrustc to WASM ==="
echo "Source: $SRC"
echo "Build: $BUILD"
echo "mrustc source: $MRUSTC_SRC"

# Clone mrustc if needed
if [ ! -d $MRUSTC_SRC/ ]; then
    echo "Cloning mrustc..."
    git clone https://github.com/thepowersgang/mrustc.git "$MRUSTC_SRC/" --depth 1
fi

# Create build directory
mkdir -p $MRUSTC_BUILD

# Source Emscripten environment
if [ -f ~/emsdk/emsdk_env.sh ]; then
    echo "Loading Emscripten SDK..."
    source ~/emsdk/emsdk_env.sh
else
    echo "ERROR: Emscripten SDK not found at ~/emsdk/"
    echo "Please install: git clone https://github.com/emscripten-core/emsdk.git ~/emsdk"
    exit 1
fi

# Get list of all source files
cd $MRUSTC_SRC
echo "Collecting source files..."

# Apply patch to rename main() to mrustc_main()
if [ -f "$SRC/patches/rename-main.patch" ]; then
    if ! grep -q "mrustc_main" src/main.cpp 2>/dev/null; then
        echo "Patching main.cpp to rename main() to mrustc_main()..."
        patch -p1 < "$SRC/patches/rename-main.patch" || true
    fi
fi

# ALL mrustc sources - EVERYTHING including main.cpp
# Now that main() is renamed to mrustc_main(), there's no conflict
SOURCES=$(find src tools/common -name "*.cpp" 2>/dev/null | grep -v "test" | tr '\n' ' ')

# Count files
NUM_FILES=$(echo $SOURCES | wc -w)
echo "Found $NUM_FILES source files"

# Compile to WASM
echo "Compiling mrustc to WASM..."
cd $MRUSTC_BUILD

# Generate version macros (from mrustc Makefile)
cd $MRUSTC_SRC
GIT_HASH=$(git show --pretty=%H -s --no-show-signature 2>/dev/null || echo "unknown")
GIT_BRANCH=$(git symbolic-ref -q --short HEAD 2>/dev/null || echo "main")
GIT_SHORT=$(git show -s --pretty=%h --no-show-signature 2>/dev/null || echo "unknown")
BUILD_TIME=$(date -u +"%Y-%m-%d_%H:%M:%S_UTC")
git diff-index --quiet HEAD 2>/dev/null
GIT_DIRTY=$?

echo "Version info:"
echo "  Git hash: $GIT_HASH"
echo "  Branch: $GIT_BRANCH"
echo "  Build time: $BUILD_TIME"

# Emscripten flags - Critical for C++ exceptions and large AST structures
EMFLAGS="-std=c++14 -O2 -fPIC"

# C++ Exception handling (CRITICAL - without this exceptions abort silently)
EMFLAGS="$EMFLAGS -fexceptions -frtti"
EMFLAGS="$EMFLAGS -s DISABLE_EXCEPTION_CATCHING=0"
EMFLAGS="$EMFLAGS -s WASM_BIGINT=1"

# Debug and assertions (ESSENTIAL while AST is not stable)
EMFLAGS="$EMFLAGS -s ASSERTIONS=1"

# Memory settings (CRITICAL for mrustc's deep recursion and large AST)
EMFLAGS="$EMFLAGS -s STACK_SIZE=64MB"
EMFLAGS="$EMFLAGS -s ALLOW_MEMORY_GROWTH=1"
EMFLAGS="$EMFLAGS -s INITIAL_MEMORY=256MB"
EMFLAGS="$EMFLAGS -s MAXIMUM_MEMORY=4GB"

# Disable debug/trace to reduce binary size and avoid undefined symbols
EMFLAGS="$EMFLAGS -DDISABLE_DEBUG -DDISABLE_TRACE"

# WASM mode for AST-only compilation (no codegen, no linking, no external tools)
EMFLAGS="$EMFLAGS -DMRUSTC_WASM=1"

# WebAssembly and filesystem (FORCE_FILESYSTEM is essential for AST)
EMFLAGS="$EMFLAGS -s WASM=1"
EMFLAGS="$EMFLAGS -s EXPORTED_RUNTIME_METHODS='[\"FS\",\"callMain\"]'"
EMFLAGS="$EMFLAGS -s FORCE_FILESYSTEM=1"
EMFLAGS="$EMFLAGS -s NODERAWFS=1"
EMFLAGS="$EMFLAGS -s USE_ZLIB=1"

# Include paths
EMFLAGS="$EMFLAGS -I$MRUSTC_SRC/src"
EMFLAGS="$EMFLAGS -I$MRUSTC_SRC/tools/common"
EMFLAGS="$EMFLAGS -I$MRUSTC_SRC/src/include"

# Number of parallel jobs
JOBS=$(nproc)
echo "Using $JOBS parallel jobs"
echo ""

# Build
OUTPUT="$SRC/public/rust/mrustc.js"
echo "Output: $OUTPUT"

# Convert source paths to absolute
ABS_SOURCES=""
for src in $SOURCES; do
    ABS_SOURCES="$ABS_SOURCES $MRUSTC_SRC/$src"
done

# Add wrapper, symbol forcer, and parse bridge
# Debug functions are already in mrustc's debug.cpp
ABS_SOURCES="$ABS_SOURCES $SRC/rust-runtime/mrustc-wrapper.cpp"
ABS_SOURCES="$ABS_SOURCES $SRC/rust-runtime/force-symbols.cpp"
ABS_SOURCES="$ABS_SOURCES $SRC/rust-runtime/parse-bridge.cpp"
NUM_FILES=$((NUM_FILES + 3))

echo "Including wrapper, symbol forcer, and parse bridge"
echo "Total files: $NUM_FILES"
echo ""

echo "Running em++ (this will take several minutes)..."
echo "Compiling $NUM_FILES C++ files to WASM with $JOBS parallel jobs..."
echo ""

# Create object files directory
OBJ_DIR=$MRUSTC_BUILD/objects
mkdir -p $OBJ_DIR

# Compile each source file to object file in parallel
echo "Phase 1: Compiling to object files..."
COMPILE_COUNT=0
for src in $ABS_SOURCES; do
    # Get base filename for runtime files, otherwise keep directory structure
    if [[ "$src" == *"/rust-runtime/"* ]]; then
        # For runtime files, just use the filename
        BASENAME=$(basename "$src" .cpp)
        REL_PATH="runtime_${BASENAME}"
    else
        # Keep directory structure to avoid name collisions (e.g., expand/mod.cpp vs macro_rules/mod.cpp)
        REL_PATH="${src#$MRUSTC_SRC/src/}"
        REL_PATH="${REL_PATH#$MRUSTC_SRC/tools/common/}"
    fi
    
    OBJ_FILE="$OBJ_DIR/${REL_PATH%.cpp}.o"
    OBJ_DIR_FOR_FILE=$(dirname "$OBJ_FILE")
    mkdir -p "$OBJ_DIR_FOR_FILE"
    
    # Compile in background
    (
        em++ -c $EMFLAGS \
          -DVERSION_GIT_FULLHASH="\"$GIT_HASH\"" \
          -DVERSION_GIT_BRANCH="\"$GIT_BRANCH\"" \
          -DVERSION_GIT_SHORTHASH="\"$GIT_SHORT\"" \
          -DVERSION_BUILDTIME="\"$BUILD_TIME\"" \
          -DVERSION_GIT_ISDIRTY=$GIT_DIRTY \
          "$src" -o "$OBJ_FILE" 2>&1 | while IFS= read -r line; do
            if echo "$line" | grep -qE "(error:)"; then
                echo "ERROR in $REL_PATH: $line"
            fi
        done
        echo "  ✓ $REL_PATH"
    ) &
    
    COMPILE_COUNT=$((COMPILE_COUNT + 1))
    
    # Limit parallel jobs
    if [ $((COMPILE_COUNT % JOBS)) -eq 0 ]; then
        wait
    fi
done

# Wait for all compilations to finish
wait

echo ""
echo "Phase 2: Linking..."

# Collect all object files recursively
OBJ_FILES=$(find $OBJ_DIR -name "*.o" -type f | tr '\n' ' ')

# Link all object files into final WASM
# CRITICAL FLAGS for C++ exceptions and AST debugging
em++ $OBJ_FILES -o $OUTPUT \
  -s ERROR_ON_UNDEFINED_SYMBOLS=0 \
  -s ALLOW_MEMORY_GROWTH=1 \
  -s STACK_SIZE=64MB \
  -s INITIAL_MEMORY=512MB \
  -s MAXIMUM_MEMORY=4GB \
  -s ENVIRONMENT=web \
  -s WASM=1 \
  -s WASM_BIGINT=1 \
  -s ASSERTIONS=1 \
  -s EXPORTED_RUNTIME_METHODS='["FS","ccall","cwrap"]' \
  -s EXPORTED_FUNCTIONS='["_compileRust","_compileRustMulti","_mrustc_parse_crate","_mrustc_free_crate","_bridge_parse_crate","_malloc","_free"]' \
  -s FORCE_FILESYSTEM=1 \
  -s USE_ZLIB=1 \
  -s DYNAMIC_EXECUTION=0 \
  -s TEXTDECODER=2 \
  -s DISABLE_EXCEPTION_CATCHING=0 \
  -fexceptions \
  -frtti \
  -Wl,--allow-undefined \
  -Wl,--no-gc-sections \
  -Wl,--export-dynamic \
  2>&1 | tee $MRUSTC_BUILD/link.log

echo ""
echo ""

if [ $? -eq 0 ]; then
    echo ""
    echo "✓ Build successful!"
    echo "Output files:"
    ls -lh $SRC/public/rust/mrustc.*
    echo ""
    echo "To use:"
    echo "  npm run build"
    echo "  cd ../../apps/web && npm run dev"
else
    echo "✗ Build failed"
    exit 1
fi
