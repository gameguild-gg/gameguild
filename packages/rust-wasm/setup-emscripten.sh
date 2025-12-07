#!/bin/bash

# Setup script for Emscripten SDK
# This automates the installation of Emscripten for building WASM

set -e

EMSDK_DIR="$HOME/emsdk"

echo "=== Emscripten SDK Setup ==="
echo ""

# Check if already installed
if command -v emcc &> /dev/null; then
    EMCC_VERSION=$(emcc --version | head -n1)
    echo "✓ Emscripten already installed: $EMCC_VERSION"
    echo ""
    echo "To use in this session:"
    echo "  source $EMSDK_DIR/emsdk_env.sh"
    exit 0
fi

echo "Emscripten SDK not found. Installing..."
echo ""
echo "This will:"
echo "  1. Clone emsdk to $EMSDK_DIR (~100MB)"
echo "  2. Install latest Emscripten (~500MB)"
echo "  3. Configure environment"
echo ""
read -p "Continue? (y/N) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Installation cancelled."
    echo ""
    echo "Manual installation:"
    echo "  git clone https://github.com/emscripten-core/emsdk.git $EMSDK_DIR"
    echo "  cd $EMSDK_DIR"
    echo "  ./emsdk install latest"
    echo "  ./emsdk activate latest"
    echo "  source ./emsdk_env.sh"
    exit 0
fi

# Clone emsdk
if [ ! -d "$EMSDK_DIR" ]; then
    echo "📥 Cloning Emscripten SDK..."
    git clone https://github.com/emscripten-core/emsdk.git "$EMSDK_DIR"
else
    echo "✓ emsdk directory already exists"
fi

cd "$EMSDK_DIR"

# Install latest
echo ""
echo "📦 Installing Emscripten (this may take 5-10 minutes)..."
./emsdk install latest

# Activate
echo ""
echo "⚙️  Activating Emscripten..."
./emsdk activate latest

# Source environment
echo ""
echo "✓ Emscripten installed successfully!"
echo ""
echo "To use Emscripten in this session, run:"
echo "  source $EMSDK_DIR/emsdk_env.sh"
echo ""
echo "To make it permanent, add to your ~/.bashrc or ~/.zshrc:"
echo "  echo 'source \"$EMSDK_DIR/emsdk_env.sh\"' >> ~/.bashrc"
echo ""
echo "Then reload your shell:"
echo "  source ~/.bashrc"
echo ""

# Offer to add to bashrc
read -p "Add to ~/.bashrc automatically? (y/N) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    if ! grep -q "emsdk_env.sh" ~/.bashrc; then
        echo "" >> ~/.bashrc
        echo "# Emscripten SDK" >> ~/.bashrc
        echo "source \"$EMSDK_DIR/emsdk_env.sh\" 2>/dev/null" >> ~/.bashrc
        echo "✓ Added to ~/.bashrc"
    else
        echo "✓ Already in ~/.bashrc"
    fi
fi

echo ""
echo "=== Setup Complete ==="
echo ""
echo "Next steps:"
echo "  1. source $EMSDK_DIR/emsdk_env.sh"
echo "  2. cd packages/rust-wasm"
echo "  3. npm run build-mock"
echo ""
