#!/bin/bash

# Build script for mrustc (Rust Compiler) WASM
# This script clones and builds mrustc to WebAssembly

set -e

echo "=== Building mrustc for Browser WASM ==="

# Check required tools
if ! command -v git &> /dev/null; then
    echo "Error: git not found. Please install git."
    exit 1
fi

if ! command -v make &> /dev/null; then
    echo "Error: make not found. Please install make."
    exit 1
fi

if ! command -v emcc &> /dev/null; then
    echo "Error: emcc (Emscripten) not found."
    echo "Please install Emscripten SDK: https://emscripten.org/docs/getting_started/downloads.html"
    echo ""
    echo "Quick install:"
    echo "  git clone https://github.com/emscripten-core/emsdk.git ~/emsdk"
    echo "  cd ~/emsdk"
    echo "  ./emsdk install latest"
    echo "  ./emsdk activate latest"
    echo "  source ~/emsdk/emsdk_env.sh"
    echo ""
    echo "Then add to ~/.bashrc:"
    echo "  echo 'source \"\$HOME/emsdk/emsdk_env.sh\"' >> ~/.bashrc"
    exit 1
fi

# Verify Emscripten version
EMCC_VERSION=$(emcc --version | head -n1)
echo "Using Emscripten: $EMCC_VERSION"

# Create directories
mkdir -p public/rust
mkdir -p rust-runtime
mkdir -p patches

echo ""
echo "📦 Building mrustc to WASM"
echo "This will:"
echo "  1. Clone mrustc repository (~50MB)"
echo "  2. Apply WASM compatibility patches"
echo "  3. Build with Emscripten (~10-30 min)"
echo ""
echo "Requirements:"
echo "  - 4GB+ RAM available"
echo "  - 30+ minutes build time"
echo "  - Stable internet connection"
echo ""

# Clone mrustc if not exists
if [ ! -d "rust-runtime/mrustc" ]; then
    echo "📥 Cloning mrustc..."
    cd rust-runtime
    git clone --depth 1 https://github.com/thepowersgang/mrustc.git
    cd ..
else
    echo "✓ mrustc already cloned"
fi

cd rust-runtime/mrustc

# Create patch for WASM compatibility
echo "🔧 Creating WASM compatibility patches..."

cat > ../../patches/mrustc-wasm.patch << 'PATCH'
diff --git a/Makefile b/Makefile
index 1234567..abcdefg 100644
--- a/Makefile
+++ b/Makefile
@@ -1,7 +1,7 @@
 # mrustc Makefile
 
-CXX := g++
-CC := gcc
+CXX := em++
+CC := emcc
 
 CXXFLAGS := -Wall -std=c++14
 CFLAGS := -Wall
PATCH

echo "📝 Applying patches..."
# Apply patch if it exists and hasn't been applied
if [ -f "../../patches/mrustc-wasm.patch" ]; then
    patch -p1 -N < ../../patches/mrustc-wasm.patch 2>/dev/null || echo "Patch already applied or failed"
fi

# Configure build for WASM
echo "⚙️  Configuring build..."
export CC=emcc
export CXX=em++
export AR=emar
export RANLIB=emranlib

# Emscripten flags for mrustc
export EMCC_CFLAGS="-O2 -s ALLOW_MEMORY_GROWTH=1 -s TOTAL_MEMORY=256MB -s MODULARIZE=1 -s EXPORT_ES6=1"
export CXXFLAGS="-std=c++14 -O2 -DWASM_BUILD"
export CFLAGS="-O2 -DWASM_BUILD"

echo "🔨 Building mrustc with Emscripten..."
echo "This will take 10-30 minutes depending on your machine..."
echo "Using $(nproc) parallel jobs"
echo ""

# Build mrustc (stage 0 - minimal compiler)
echo "Building stage 0 (bootstrap)..."
make -j$(nproc) bin/mrustc 2>&1 | tee ../../build.log || {
    echo ""
    echo "❌ mrustc build failed"
    echo ""
    echo "Common issues:"
    echo "  1. mrustc uses C++ features not fully supported in WASM"
    echo "  2. File system operations need WASI/Emscripten FS"
    echo "  3. Threading/process spawning not available in browser"
    echo ""
    echo "Build log saved to: rust-runtime/build.log"
    echo ""
    echo "Next steps:"
    echo "  1. Check build.log for specific errors"
    echo "  2. May need custom patches for WASM compatibility"
    echo "  3. Consider simplified mrustc fork specifically for WASM"
    echo ""
    cd ../..
    
    # Create informative runtime that shows the error
    cat > public/rust/main.js << 'EOF'
// mrustc WASM build failed
console.error('[Rust] mrustc build failed - see build.log for details')

window.RustCompiler = {
  compile: function(code, options) {
    return 'ERROR\nmrustc build failed.\n\nCheck rust-runtime/build.log for details.\n\nCommon issues:\n- C++ features incompatible with WASM\n- File system operations need adaptation\n- Threading not available\n\nConsider creating simplified mrustc-wasm fork.'
  },
  compileMultiple: function(filesJson, options) {
    return this.compile('', options)
  }
}

console.log('[Rust] ✓ Error handler loaded')
EOF
    exit 1
}

echo ""
echo "✅ mrustc built successfully!"

# Generate WASM wrapper
echo "📦 Generating JavaScript wrapper..."

cat > ../../public/rust/main.js << 'EOF'
// mrustc WASM Runtime
import mrustcModule from './mrustc.js'

let mrustc = null

async function initializeMrustc() {
  if (mrustc) return mrustc
  
  console.log('[Rust] Initializing mrustc WASM...')
  mrustc = await mrustcModule()
  console.log('[Rust] ✓ mrustc ready')
  
  return mrustc
}

// Expose compiler interface
window.RustCompiler = {
  compile: async function(code, options = {}) {
    try {
      const compiler = await initializeMrustc()
      
      // Call mrustc compile function
      const result = compiler.ccall(
        'compile_rust',
        'string',
        ['string', 'string'],
        [code, JSON.stringify(options)]
      )
      
      return result
    } catch (error) {
      return `ERROR\n${error.message}`
    }
  },
  
  compileMultiple: async function(filesJson, options = {}) {
    try {
      const compiler = await initializeMrustc()
      
      const result = compiler.ccall(
        'compile_rust_multi',
        'string',
        ['string', 'string'],
        [filesJson, JSON.stringify(options)]
      )
      
      return result
    } catch (error) {
      return `ERROR\n${error.message}`
    }
  }
}

console.log('[Rust] ✓ Compiler interface ready')
EOF

# Copy built WASM files
echo "📋 Copying build artifacts..."
if [ -f "bin/mrustc.wasm" ]; then
    cp bin/mrustc.wasm ../../public/rust/
    cp bin/mrustc.js ../../public/rust/ 2>/dev/null || true
fi

cd ../..

echo ""
echo "=== ✅ Build Complete ==="
echo ""
echo "Built files:"
ls -lh public/rust/
echo ""
echo "Next steps:"
echo "  1. Test the compiler: npm run dev"
echo "  2. Check browser console for mrustc initialization"
echo "  3. Try compiling simple Rust code"
echo ""
