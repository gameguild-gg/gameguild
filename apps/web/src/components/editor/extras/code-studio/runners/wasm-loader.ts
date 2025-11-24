import { ungzip } from 'pako'

const wasmCache: Map<string, WebAssembly.Module> = new Map()

export async function loadCompressedWasm(path: string): Promise<ArrayBuffer> {
  const response = await fetch(path)
  if (!response.ok) {
    throw new Error(`Failed to fetch ${path}: ${response.statusText}`)
  }

  const compressed = await response.arrayBuffer()
  const decompressed = ungzip(new Uint8Array(compressed))
  
  return decompressed.buffer
}

export async function loadCompressedScript(url: string): Promise<void> {
  const response = await fetch(url)
  if (!response.ok) {
    throw new Error(`Failed to fetch ${url}: ${response.statusText}`)
  }
  
  const compressed = await response.arrayBuffer()
  const decompressed = ungzip(new Uint8Array(compressed))
  const code = new TextDecoder().decode(decompressed)
  
  // Execute the script in global scope
  const script = document.createElement('script')
  script.textContent = code
  document.head.appendChild(script)
}

export async function loadAndCacheWasm(
  name: string,
  path: string
): Promise<WebAssembly.Module> {
  if (wasmCache.has(name)) {
    return wasmCache.get(name)!
  }

  const buffer = await loadCompressedWasm(path)
  const module = await WebAssembly.compile(buffer)
  wasmCache.set(name, module)

  return module
}
