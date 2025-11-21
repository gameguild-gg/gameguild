#!/usr/bin/env node

import { createReadStream, createWriteStream, existsSync, mkdirSync, writeFileSync, readFileSync } from 'fs'
import { createGzip, createGunzip } from 'zlib'
import { createHash } from 'crypto'
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

async function calculateFileHash(filePath) {
  return new Promise((resolve, reject) => {
    const hash = createHash('sha256')
    const stream = createReadStream(filePath)
    
    stream.on('data', (chunk) => hash.update(chunk))
    stream.on('end', () => resolve(hash.digest('hex')))
    stream.on('error', reject)
  })
}

async function calculateDecompressedHash(compressedPath) {
  return new Promise((resolve, reject) => {
    const hash = createHash('sha256')
    const input = createReadStream(compressedPath)
    const gunzip = createGunzip()
    
    gunzip.on('data', (chunk) => hash.update(chunk))
    gunzip.on('end', () => resolve(hash.digest('hex')))
    gunzip.on('error', reject)
    input.on('error', reject)
    
    input.pipe(gunzip)
  })
}

async function saveHashFile(wasmPath, hash) {
  const hashPath = wasmPath + '.sha256'
  writeFileSync(hashPath, hash)
  return hashPath
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

      // Calculate hash of decompressed WASM (what browser will use)
      const hash = await calculateDecompressedHash(outputPath)
      const hashPath = await saveHashFile(outputPath, hash)

      totalOriginal += originalSize
      totalCompressed += compressedSize

      console.log(`✅ ${file.name}`)
      console.log(`   Source: ${file.source}`)
      console.log(`   Original: ${await getFileSize(sourcePath)}`)
      console.log(`   Compressed: ${await getFileSize(outputPath)}`)
      console.log(`   Compression: ${ratio}% reduction`)
      console.log(`   Hash (decompressed): ${hash.substring(0, 16)}...`)
      console.log(`   Hash file: ${hashPath}\n`)
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
