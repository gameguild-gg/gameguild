#!/usr/bin/env node

const { existsSync } = require('fs')
const { resolve } = require('path')
const { spawnSync } = require('child_process')

const webDir = resolve(__dirname, '..')
const repoRoot = resolve(webDir, '..', '..')
const dotnetWasmDir = resolve(repoRoot, 'packages/dotnet-wasm')

function getNpmInvocation() {
  if (process.env.npm_execpath) {
    return {
      command: process.execPath,
      args: [process.env.npm_execpath],
    }
  }

  return {
    command: process.platform === 'win32' ? 'npm.cmd' : 'npm',
    args: [],
  }
}

function runNpmScript(cwd, scriptName) {
  const npm = getNpmInvocation()
  const result = spawnSync(npm.command, [...npm.args, 'run', scriptName], {
    cwd,
    env: process.env,
    stdio: 'inherit',
  })

  if (result.status !== 0) {
    process.exit(result.status || 1)
  }
}

if (!existsSync(dotnetWasmDir)) {
  console.warn('\n⚠️  packages/dotnet-wasm not found. Skipping WASM preparation.\n')
  process.exit(0)
}

console.log('\n🔧 Building dotnet-wasm assets...')
runNpmScript(dotnetWasmDir, 'setup')

console.log('\n📦 Updating web WASM assets...')
runNpmScript(webDir, 'update-wasm')
