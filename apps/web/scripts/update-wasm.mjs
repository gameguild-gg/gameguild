#!/usr/bin/env node

import { createReadStream, createWriteStream, existsSync, mkdirSync } from 'fs'
import { createGzip } from 'zlib'
import { pipeline } from 'stream/promises'
import { fileURLToPath } from 'url'
import { dirname, join } from 'path'

const __filename = fileURLToPath(import.meta.url)
const __dirname = dirname(__filename)
const rootDir = join(__dirname, '..')
const publicWasmDir = join(rootDir, 'public', 'wasm')

const WASM_FILES = [
  {
    name: 'esbuild',
    source: 'node_modules/esbuild-wasm/esbuild.wasm',
    output: 'esbuild.wasm.gz',
  },
  {
    name: 'quickjs-asyncify',
    source: 'node_modules/@jitl/quickjs-wasmfile-release-asyncify/dist/emscripten-module.wasm',
    output: 'quickjs-asyncify.wasm.gz',
  },
]

async function compressFile(source, output) {
  const sourcePath = join(rootDir, source)
  const outputPath = join(publicWasmDir, output)

  if (!existsSync(sourcePath)) {
    throw new Error(`Source file not found: ${sourcePath}`)
  }

  const gzip = createGzip({ level: 9 })
  const input = createReadStream(sourcePath)
  const outputStream = createWriteStream(outputPath)

  await pipeline(input, gzip, outputStream)

  return outputPath
}

async function getFileSize(path) {
  const { statSync } = await import('fs')
  const stats = statSync(path)
  const mb = (stats.size / 1024 / 1024).toFixed(2)
  const kb = (stats.size / 1024).toFixed(2)
  return stats.size > 1024 * 1024 ? `${mb}MB` : `${kb}KB`
}

async function main() {
  console.log('🔄 Updating WASM files...\n')

  if (!existsSync(publicWasmDir)) {
    mkdirSync(publicWasmDir, { recursive: true })
    console.log(`✅ Created directory: ${publicWasmDir}\n`)
  }

  let totalOriginal = 0
  let totalCompressed = 0

  for (const file of WASM_FILES) {
    try {
      const sourcePath = join(rootDir, file.source)
      const outputPath = await compressFile(file.source, file.output)

      const { statSync } = await import('fs')
      const originalSize = statSync(sourcePath).size
      const compressedSize = statSync(outputPath).size
      const ratio = ((1 - compressedSize / originalSize) * 100).toFixed(1)

      totalOriginal += originalSize
      totalCompressed += compressedSize

      console.log(`✅ ${file.name}`)
      console.log(`   Source: ${file.source}`)
      console.log(`   Original: ${await getFileSize(sourcePath)}`)
      console.log(`   Compressed: ${await getFileSize(outputPath)}`)
      console.log(`   Compression: ${ratio}% reduction\n`)
    } catch (error) {
      console.error(`❌ Failed to compress ${file.name}:`, error.message)
      process.exit(1)
    }
  }

  const totalRatio = ((1 - totalCompressed / totalOriginal) * 100).toFixed(1)
  const totalOriginalMB = (totalOriginal / 1024 / 1024).toFixed(2)
  const totalCompressedMB = (totalCompressed / 1024 / 1024).toFixed(2)

  console.log('📊 Summary:')
  console.log(`   Total original: ${totalOriginalMB}MB`)
  console.log(`   Total compressed: ${totalCompressedMB}MB`)
  console.log(`   Total reduction: ${totalRatio}%`)
  console.log('\n✨ WASM files updated successfully!')
}

main().catch((error) => {
  console.error('❌ Error:', error.message)
  process.exit(1)
})
