#!/usr/bin/env node

import { createReadStream, createWriteStream, existsSync, mkdirSync, readFileSync, writeFileSync } from 'fs'
import { createGzip } from 'zlib'
import { pipeline } from 'stream/promises'
import { fileURLToPath } from 'url'
import { dirname, join } from 'path'
import { Readable } from 'stream'
import { createHash } from 'crypto'

const __filename = fileURLToPath(import.meta.url)
const __dirname = dirname(__filename)
const rootDir = join(__dirname, '..', '..', '..') // Ajustado para apontar para o diretório raiz do projeto
const publicWasmDir = join(rootDir, 'apps/web/public', 'langs')
const hashCacheFile = join(publicWasmDir, '.wasm-hashes.json')

const WASM_FILES = [
  {
    name: 'esbuild',
    source: 'node_modules/esbuild-wasm/esbuild.wasm',
    output: 'esbuild.wasm.gz',
    compress: true,
  },
  {
    name: 'quickjs-asyncify',
    source: 'node_modules/@jitl/quickjs-wasmfile-release-asyncify/dist/emscripten-module.wasm',
    output: 'quickjs-asyncify.wasm.gz',
    compress: true,
  },
  {
    name: 'clang',
    source: 'node_modules/@runno/sandbox/dist/langs/clang.wasm',
    output: 'clang.wasm.gz',
    compress: true,
  },
  {
    name: 'wasm-ld',
    source: 'node_modules/@runno/sandbox/dist/langs/wasm-ld.wasm',
    output: 'wasm-ld.wasm.gz',
    compress: true,
  },
  {
    name: 'clang-fs',
    source: 'node_modules/@runno/sandbox/dist/langs/clang-fs.tar.gz',
    output: 'clang-fs.tar.gz',
    compress: false, // Já está compactado
  },
  {
    name: 'php-cgi',
    source: 'node_modules/@runno/sandbox/dist/langs/php-cgi-8.2.0.wasm',
    output: 'php-cgi.wasm.gz',
    compress: true,
  },
  {
    name: 'sqlite',
    source: 'node_modules/@runno/sandbox/dist/langs/sqlite.wasm',
    output: 'sqlite.wasm.gz',
    compress: true,
  },
  {
    name: 'python-wasi',
    source: 'node_modules/@runno/sandbox/dist/langs/python-3.11.3.wasm',
    output: 'python-3.11.3.wasm.gz',
    compress: true,
  },
  {
    name: 'python-wasi-fs',
    source: 'node_modules/@runno/sandbox/dist/langs/python-3.11.3.tar.gz',
    output: 'python-3.11.3.tar.gz',
    compress: false, // Já está compactado
  },
  {
    name: 'ruby',
    source: 'node_modules/@runno/sandbox/dist/langs/ruby-3.2.0.wasm',
    output: 'ruby.wasm.gz',
    compress: true,
  },
  {
    name: 'wabt',
    source: 'node_modules/wabt/index.js',
    output: 'wabt.js.gz',
    compress: true,
  },
]

const PYODIDE_VERSION = '0.26.4'
const PYODIDE_BASE_URL = `https://cdn.jsdelivr.net/pyodide/v${PYODIDE_VERSION}/full`
const PYODIDE_FILES = [
  'pyodide.asm.js',
  'pyodide.asm.wasm',
  'pyodide.js',
  'python_stdlib.zip',
  'pyodide-lock.json',
]

// Load or initialize hash cache
function loadHashCache() {
  if (existsSync(hashCacheFile)) {
    try {
      const data = readFileSync(hashCacheFile, 'utf8')
      return JSON.parse(data)
    } catch (error) {
      console.warn('⚠️  Failed to load hash cache, starting fresh')
      return {}
    }
  }
  return {}
}

// Save hash cache
function saveHashCache(cache) {
  try {
    writeFileSync(hashCacheFile, JSON.stringify(cache, null, 2), 'utf8')
  } catch (error) {
    console.warn('⚠️  Failed to save hash cache:', error.message)
  }
}

// Calculate SHA1 hash of a file
function calculateFileHash(filePath) {
  const content = readFileSync(filePath)
  return createHash('sha1').update(content).digest('hex')
}

// Calculate SHA1 hash of a buffer
function calculateBufferHash(buffer) {
  return createHash('sha1').update(buffer).digest('hex')
}

// Check if file needs update
function needsUpdate(cache, key, currentHash) {
  if (!cache[key]) {
    return true
  }
  return cache[key].sha1 !== currentHash
}

async function compressFile(source, output, sourceHash) {
  const sourcePath = join(rootDir, source)
  const outputPath = join(publicWasmDir, output)

  if (!existsSync(sourcePath)) {
    throw new Error(`Source file not found: ${sourcePath}`)
  }

  const gzip = createGzip({ level: 9 })
  const input = createReadStream(sourcePath)
  const outputStream = createWriteStream(outputPath)

  await pipeline(input, gzip, outputStream)

  return { path: outputPath, sourceHash }
}

async function getFileSize(path) {
  const { statSync } = await import('fs')
  const stats = statSync(path)
  const mb = (stats.size / 1024 / 1024).toFixed(2)
  const kb = (stats.size / 1024).toFixed(2)
  return stats.size > 1024 * 1024 ? `${mb}MB` : `${kb}KB`
}

async function downloadFile(url, outputPath) {
  const response = await fetch(url)
  if (!response.ok) {
    throw new Error(`Failed to download ${url}: ${response.statusText}`)
  }

  const arrayBuffer = await response.arrayBuffer()
  const buffer = Buffer.from(arrayBuffer)
  
  return buffer
}

async function compressAndSave(buffer, outputPath) {
  const gzip = createGzip({ level: 9 })
  const output = createWriteStream(outputPath)
  const readable = Readable.from(buffer)

  await pipeline(readable, gzip, output)
}

async function downloadPyodide(hashCache) {
  console.log(`\n🐍 Downloading Pyodide ${PYODIDE_VERSION}...\n`)

  if (!existsSync(publicWasmDir)) {
    mkdirSync(publicWasmDir, { recursive: true })
  }

  let totalOriginal = 0
  let totalCompressed = 0
  let skipped = 0

  for (const filename of PYODIDE_FILES) {
    try {
      const url = `${PYODIDE_BASE_URL}/${filename}`
      const cacheKey = `pyodide:${filename}`
      
      console.log(`📥 Checking ${filename}...`)
      
      const buffer = await downloadFile(url)
      const downloadHash = calculateBufferHash(buffer)
      const originalSize = buffer.length
      
      // Check if we need to update this file
      if (!needsUpdate(hashCache, cacheKey, downloadHash)) {
        console.log(`   ⏭️  Skipped (unchanged, SHA1: ${downloadHash.substring(0, 8)}...)`)
        
        // Still count the sizes for statistics
        const isAlreadyCompressed = filename.endsWith('.zip')
        const outputPath = (filename === 'pyodide.js')
          ? join(publicWasmDir, `${filename}.gz`)
          : join(publicWasmDir, isAlreadyCompressed ? filename : `${filename}.gz`)
        
        if (existsSync(outputPath)) {
          const { statSync } = await import('fs')
          const savedSize = statSync(outputPath).size
          totalOriginal += originalSize
          totalCompressed += savedSize
        }
        skipped++
        continue
      }
      
      totalOriginal += originalSize

      // Arquivos ZIP já são compactados, não precisam de gzip adicional
      const isAlreadyCompressed = filename.endsWith('.zip')
      
      // Todos os arquivos do Pyodide vão para /wasm/ (exceto pyodide.js que fica em /pyodide/)
      const outputPath = (filename === 'pyodide.js')
        ? join(publicWasmDir, `${filename}.gz`)
        : join(publicWasmDir, isAlreadyCompressed ? filename : `${filename}.gz`)
      
      if (isAlreadyCompressed) {
        // Salvar diretamente sem compressão adicional
        const { writeFile } = await import('fs/promises')
        await writeFile(outputPath, buffer)
        totalCompressed += originalSize
        const origMB = (originalSize / 1024 / 1024).toFixed(2)
        console.log(`   ✅ Saved: ${origMB}MB (already compressed, SHA1: ${downloadHash.substring(0, 8)}...)`)
        
        // Update cache
        hashCache[cacheKey] = {
          sha1: downloadHash,
          size: originalSize,
          timestamp: new Date().toISOString()
        }
      } else {
        // Comprimir com gzip
        await compressAndSave(buffer, outputPath)
        
        const { statSync } = await import('fs')
        const compressedSize = statSync(outputPath).size
        totalCompressed += compressedSize
        
        const ratio = ((1 - compressedSize / originalSize) * 100).toFixed(1)
        const origMB = (originalSize / 1024 / 1024).toFixed(2)
        const compMB = (compressedSize / 1024 / 1024).toFixed(2)
        console.log(`   ✅ Compressed: ${origMB}MB → ${compMB}MB (${ratio}% reduction, SHA1: ${downloadHash.substring(0, 8)}...)`)
        
        // Update cache
        hashCache[cacheKey] = {
          sha1: downloadHash,
          size: originalSize,
          compressedSize,
          timestamp: new Date().toISOString()
        }
      }
    } catch (error) {
      console.error(`❌ Failed to download ${filename}:`, error.message)
      process.exit(1)
    }
  }

  const totalRatio = totalOriginal > 0 ? ((1 - totalCompressed / totalOriginal) * 100).toFixed(1) : 0
  console.log(`\n📊 Pyodide Summary:`)
  console.log(`   Total original: ${(totalOriginal / 1024 / 1024).toFixed(2)}MB`)
  console.log(`   Total compressed: ${(totalCompressed / 1024 / 1024).toFixed(2)}MB`)
  console.log(`   Total reduction: ${totalRatio}%`)
  if (skipped > 0) {
    console.log(`   ⏭️  Skipped ${skipped} unchanged file(s)`)
  }

  return { original: totalOriginal, compressed: totalCompressed, skipped }
}

async function main() {
  console.log('🔄 Updating WASM files...\n')

  if (!existsSync(publicWasmDir)) {
    mkdirSync(publicWasmDir, { recursive: true })
    console.log(`✅ Created directory: ${publicWasmDir}\n`)
  }

  // Load hash cache
  const hashCache = loadHashCache()
  console.log(`📋 Loaded hash cache with ${Object.keys(hashCache).length} entries\n`)

  let totalOriginal = 0
  let totalCompressed = 0
  let localSkipped = 0

  // Process local WASM files from node_modules
  for (const file of WASM_FILES) {
    try {
      const sourcePath = join(rootDir, file.source)
      const cacheKey = `local:${file.name}`
      
      if (!existsSync(sourcePath)) {
        throw new Error(`Source file not found: ${sourcePath}`)
      }

      const { statSync, copyFileSync } = await import('fs')
      const originalSize = statSync(sourcePath).size
      const sourceHash = calculateFileHash(sourcePath)

      // Check if we need to update this file
      const outputPath = join(publicWasmDir, file.output)
      if (!needsUpdate(hashCache, cacheKey, sourceHash) && existsSync(outputPath)) {
        console.log(`⏭️  ${file.name}`)
        console.log(`   Skipped (unchanged, SHA1: ${sourceHash.substring(0, 8)}...)\n`)
        
        totalOriginal += originalSize
        totalCompressed += statSync(outputPath).size
        localSkipped++
        continue
      }

      if (file.compress === false) {
        // Apenas copiar sem comprimir
        copyFileSync(sourcePath, outputPath)
        const copiedSize = statSync(outputPath).size

        totalOriginal += originalSize
        totalCompressed += copiedSize

        console.log(`✅ ${file.name}`)
        console.log(`   Source: ${file.source}`)
        console.log(`   Copied: ${await getFileSize(outputPath)} (no additional compression, SHA1: ${sourceHash.substring(0, 8)}...)\n`)
        
        // Update cache
        hashCache[cacheKey] = {
          sha1: sourceHash,
          size: originalSize,
          timestamp: new Date().toISOString()
        }
      } else {
        // Comprimir normalmente
        const result = await compressFile(file.source, file.output, sourceHash)
        const compressedSize = statSync(result.path).size
        const ratio = ((1 - compressedSize / originalSize) * 100).toFixed(1)

        totalOriginal += originalSize
        totalCompressed += compressedSize

        console.log(`✅ ${file.name}`)
        console.log(`   Source: ${file.source}`)
        console.log(`   Original: ${await getFileSize(sourcePath)}`)
        console.log(`   Compressed: ${await getFileSize(result.path)}`)
        console.log(`   Compression: ${ratio}% reduction (SHA1: ${sourceHash.substring(0, 8)}...)\n`)
        
        // Update cache
        hashCache[cacheKey] = {
          sha1: sourceHash,
          size: originalSize,
          compressedSize,
          timestamp: new Date().toISOString()
        }
      }
    } catch (error) {
      console.error(`❌ Failed to process ${file.name}:`, error.message)
      process.exit(1)
    }
  }

  const localRatio = totalOriginal > 0 ? ((1 - totalCompressed / totalOriginal) * 100).toFixed(1) : 0
  const localOriginalMB = (totalOriginal / 1024 / 1024).toFixed(2)
  const localCompressedMB = (totalCompressed / 1024 / 1024).toFixed(2)

  console.log('📊 Local WASM Summary:')
  console.log(`   Total original: ${localOriginalMB}MB`)
  console.log(`   Total compressed: ${localCompressedMB}MB`)
  console.log(`   Total reduction: ${localRatio}%`)
  if (localSkipped > 0) {
    console.log(`   ⏭️  Skipped ${localSkipped} unchanged file(s)`)
  }

  // Download Pyodide from CDN
  const pyodideStats = await downloadPyodide(hashCache)

  // Save updated hash cache
  saveHashCache(hashCache)
  console.log(`\n💾 Saved hash cache with ${Object.keys(hashCache).length} entries`)

  // Grand total
  const grandOriginal = totalOriginal + pyodideStats.original
  const grandCompressed = totalCompressed + pyodideStats.compressed
  const grandRatio = grandOriginal > 0 ? ((1 - grandCompressed / grandOriginal) * 100).toFixed(1) : 0
  const totalSkipped = localSkipped + (pyodideStats.skipped || 0)

  console.log('\n🎉 Grand Total:')
  console.log(`   Total original: ${(grandOriginal / 1024 / 1024).toFixed(2)}MB`)
  console.log(`   Total compressed/saved: ${(grandCompressed / 1024 / 1024).toFixed(2)}MB`)
  console.log(`   Total reduction: ${grandRatio}%`)
  if (totalSkipped > 0) {
    console.log(`   ⏭️  Skipped ${totalSkipped} unchanged file(s)`)
  }
  console.log('\n✨ All WASM files updated successfully!')
  console.log(`   Local WASM: public/langs/`)
  console.log(`   Hash cache: ${hashCacheFile}`)
}

main().catch((error) => {
  console.error('❌ Error:', error.message)
  process.exit(1)
})
