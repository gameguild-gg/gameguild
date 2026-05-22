#!/usr/bin/env node
/**
 * Copies MathLive's KaTeX font files (and sound effects) from the
 * installed `mathlive` package into the Next.js `public/mathlive/`
 * directory so they are served from the same origin as the app.
 *
 * MathLive needs these assets at runtime; without them the editor
 * falls back to broken glyphs and floods the console with errors.
 * We avoid pointing the runtime at a CDN to keep the app fully
 * offline-capable.
 *
 * Run automatically as part of `postinstall`.
 */

const { cpSync, existsSync, mkdirSync, rmSync } = require("fs")
const { dirname, resolve } = require("path")

const webDir = resolve(__dirname, "..")
const repoRoot = resolve(webDir, "..", "..")

function findMathLive() {
  const candidates = [
    resolve(webDir, "node_modules/mathlive"),
    resolve(repoRoot, "node_modules/mathlive"),
  ]
  for (const candidate of candidates) {
    if (existsSync(candidate)) return candidate
  }
  return null
}

const mathliveDir = findMathLive()
if (!mathliveDir) {
  console.warn("\n⚠️  mathlive package not found. Skipping math asset copy.\n")
  process.exit(0)
}

const publicMathlive = resolve(webDir, "public/mathlive")
const targets = [
  { from: resolve(mathliveDir, "fonts"), to: resolve(publicMathlive, "fonts") },
  { from: resolve(mathliveDir, "sounds"), to: resolve(publicMathlive, "sounds") },
]

for (const { from, to } of targets) {
  if (!existsSync(from)) {
    console.warn(`⚠️  ${from} not found, skipping`)
    continue
  }
  mkdirSync(dirname(to), { recursive: true })
  rmSync(to, { recursive: true, force: true })
  cpSync(from, to, { recursive: true })
  console.log(`✅ Copied ${from} → ${to}`)
}
