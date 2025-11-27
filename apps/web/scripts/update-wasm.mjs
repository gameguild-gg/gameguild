#!/usr/bin/env node

import { createReadStream, createWriteStream, existsSync, mkdirSync } from 'fs'
import { createGzip } from 'zlib'
import { pipeline } from 'stream/promises'
import { fileURLToPath } from 'url'
import { dirname, join } from 'path'
import { Readable } from 'stream'

const __filename = fileURLToPath(import.meta.url)
const __dirname = dirname(__filename)
const rootDir = join(__dirname, '..', '..', '..') // Ajustado para apontar para o diretório raiz do projeto
const publicWasmDir = join(rootDir, 'apps/web/public', 'langs')

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

async function downloadPyodide() {
  console.log(`\n🐍 Downloading Pyodide ${PYODIDE_VERSION}...\n`)

  if (!existsSync(publicWasmDir)) {
    mkdirSync(publicWasmDir, { recursive: true })
  }

  let totalOriginal = 0
  let totalCompressed = 0

  for (const filename of PYODIDE_FILES) {
    try {
      const url = `${PYODIDE_BASE_URL}/${filename}`
      console.log(`📥 Downloading ${filename}...`)
      
      const buffer = await downloadFile(url)
      const originalSize = buffer.length
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
        console.log(`   ✅ Saved: ${origMB}MB (already compressed)`)
      } else {
        // Comprimir com gzip
        await compressAndSave(buffer, outputPath)
        
        const { statSync } = await import('fs')
        const compressedSize = statSync(outputPath).size
        totalCompressed += compressedSize
        
        const ratio = ((1 - compressedSize / originalSize) * 100).toFixed(1)
        const origMB = (originalSize / 1024 / 1024).toFixed(2)
        const compMB = (compressedSize / 1024 / 1024).toFixed(2)
        console.log(`   ✅ Compressed: ${origMB}MB → ${compMB}MB (${ratio}% reduction)`)
      }
    } catch (error) {
      console.error(`❌ Failed to download ${filename}:`, error.message)
      process.exit(1)
    }
  }

  const totalRatio = ((1 - totalCompressed / totalOriginal) * 100).toFixed(1)
  console.log(`\n📊 Pyodide Summary:`)
  console.log(`   Total original: ${(totalOriginal / 1024 / 1024).toFixed(2)}MB`)
  console.log(`   Total compressed: ${(totalCompressed / 1024 / 1024).toFixed(2)}MB`)
  console.log(`   Total reduction: ${totalRatio}%`)

  return { original: totalOriginal, compressed: totalCompressed }
}

async function main() {
  console.log('🔄 Updating WASM files...\n')

  if (!existsSync(publicWasmDir)) {
    mkdirSync(publicWasmDir, { recursive: true })
    console.log(`✅ Created directory: ${publicWasmDir}\n`)
  }

  let totalOriginal = 0
  let totalCompressed = 0

  // Process local WASM files from node_modules
  for (const file of WASM_FILES) {
    try {
      const sourcePath = join(rootDir, file.source)
      
      if (!existsSync(sourcePath)) {
        throw new Error(`Source file not found: ${sourcePath}`)
      }

      const { statSync, copyFileSync } = await import('fs')
      const originalSize = statSync(sourcePath).size

      if (file.compress === false) {
        // Apenas copiar sem comprimir
        const outputPath = join(publicWasmDir, file.output)
        copyFileSync(sourcePath, outputPath)
        const copiedSize = statSync(outputPath).size

        totalOriginal += originalSize
        totalCompressed += copiedSize

        console.log(`✅ ${file.name}`)
        console.log(`   Source: ${file.source}`)
        console.log(`   Copied: ${await getFileSize(outputPath)} (no additional compression)\n`)
      } else {
        // Comprimir normalmente
        const outputPath = await compressFile(file.source, file.output)
        const compressedSize = statSync(outputPath).size
        const ratio = ((1 - compressedSize / originalSize) * 100).toFixed(1)

        totalOriginal += originalSize
        totalCompressed += compressedSize

        console.log(`✅ ${file.name}`)
        console.log(`   Source: ${file.source}`)
        console.log(`   Original: ${await getFileSize(sourcePath)}`)
        console.log(`   Compressed: ${await getFileSize(outputPath)}`)
        console.log(`   Compression: ${ratio}% reduction\n`)
      }
    } catch (error) {
      console.error(`❌ Failed to process ${file.name}:`, error.message)
      process.exit(1)
    }
  }

  const localRatio = ((1 - totalCompressed / totalOriginal) * 100).toFixed(1)
  const localOriginalMB = (totalOriginal / 1024 / 1024).toFixed(2)
  const localCompressedMB = (totalCompressed / 1024 / 1024).toFixed(2)

  console.log('📊 Local WASM Summary:')
  console.log(`   Total original: ${localOriginalMB}MB`)
  console.log(`   Total compressed: ${localCompressedMB}MB`)
  console.log(`   Total reduction: ${localRatio}%`)

  // Download Pyodide from CDN
  const pyodideStats = await downloadPyodide()

  // Grand total
  const grandOriginal = totalOriginal + pyodideStats.original
  const grandCompressed = totalCompressed + pyodideStats.compressed
  const grandRatio = ((1 - grandCompressed / grandOriginal) * 100).toFixed(1)

  console.log('\n🎉 Grand Total:')
  console.log(`   Total original: ${(grandOriginal / 1024 / 1024).toFixed(2)}MB`)
  console.log(`   Total compressed/saved: ${(grandCompressed / 1024 / 1024).toFixed(2)}MB`)
  console.log(`   Total reduction: ${grandRatio}%`)
  console.log('\n✨ All WASM files updated successfully!')
  console.log(`   Local WASM: public/langs/`)
}

main().catch((error) => {
  console.error('❌ Error:', error.message)
  process.exit(1)
})
