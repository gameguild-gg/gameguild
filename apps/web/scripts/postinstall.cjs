#!/usr/bin/env node

const WASM_PACKAGES = ['esbuild-wasm', 'quickjs-emscripten', '@runno/sandbox', 'wabt']

const changedPackages = process.env.npm_package_json
  ? require(process.env.npm_package_json).dependencies || {}
  : {}

const needsUpdate = WASM_PACKAGES.some(pkg => pkg in changedPackages)

if (needsUpdate) {
  console.log('\n⚠️  WASM packages detected!')
  console.log('💡 Run: npm run update-wasm')
  console.log('   To update compressed WASM files in public/wasm/\n')
}
