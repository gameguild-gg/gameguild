import { ungzip } from 'pako'

interface CachedWasm {
  binary: ArrayBuffer
  hash: string
  timestamp: number
}

const WASM_CACHE_DB = 'wasm-cache'
const WASM_CACHE_STORE = 'modules'
const WASM_CACHE_VERSION = 1

async function openDB(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(WASM_CACHE_DB, WASM_CACHE_VERSION)
    
    request.onerror = () => reject(request.error)
    request.onsuccess = () => resolve(request.result)
    
    request.onupgradeneeded = (event) => {
      const db = (event.target as IDBOpenDBRequest).result
      if (!db.objectStoreNames.contains(WASM_CACHE_STORE)) {
        db.createObjectStore(WASM_CACHE_STORE, { keyPath: 'name' })
      }
    }
  })
}

async function getCachedWasm(name: string): Promise<CachedWasm | null> {
  const db = await openDB()
  return new Promise((resolve, reject) => {
    const transaction = db.transaction(WASM_CACHE_STORE, 'readonly')
    const store = transaction.objectStore(WASM_CACHE_STORE)
    const request = store.get(name)
    
    request.onerror = () => reject(request.error)
    request.onsuccess = () => resolve(request.result || null)
  })
}

async function setCachedWasm(name: string, cached: CachedWasm): Promise<void> {
  const db = await openDB()
  return new Promise((resolve, reject) => {
    const transaction = db.transaction(WASM_CACHE_STORE, 'readwrite')
    const store = transaction.objectStore(WASM_CACHE_STORE)
    const request = store.put({ name, ...cached })
    
    request.onerror = () => reject(request.error)
    request.onsuccess = () => resolve()
  })
}

async function calculateSHA256(buffer: ArrayBuffer): Promise<string> {
  const hashBuffer = await crypto.subtle.digest('SHA-256', buffer)
  const hashArray = Array.from(new Uint8Array(hashBuffer))
  return hashArray.map(b => b.toString(16).padStart(2, '0')).join('')
}

async function fetchRemoteHash(path: string): Promise<string | null> {
  try {
    const hashPath = path.replace('.wasm.gz', '.wasm.gz.sha256')
    const response = await fetch(hashPath)
    if (!response.ok) return null
    return (await response.text()).trim()
  } catch {
    return null
  }
}

export async function loadCompressedWasm(path: string): Promise<ArrayBuffer> {
  const name = path.split('/').pop()!.replace('.wasm.gz', '')
  
  // Check cache
  const cached = await getCachedWasm(name)
  
  // Fetch remote hash
  const remoteHash = await fetchRemoteHash(path)
  
  // Use cache if hash matches
  if (cached && remoteHash && cached.hash === remoteHash) {
    console.log(`✓ Using cached WASM: ${name}`)
    return cached.binary
  }
  
  // Download WASM
  console.log(`↓ Downloading WASM: ${name}`)
  const response = await fetch(path)
  if (!response.ok) {
    throw new Error(`Failed to fetch ${path}: ${response.statusText}`)
  }

  const compressed = await response.arrayBuffer()
  const decompressed = ungzip(new Uint8Array(compressed))
  const binary = decompressed.buffer
  
  // Calculate hash
  const hash = remoteHash || await calculateSHA256(binary)
  
  // Cache for future use
  await setCachedWasm(name, {
    binary,
    hash,
    timestamp: Date.now(),
  })
  
  console.log(`✓ Cached WASM: ${name} (${hash.substring(0, 8)}...)`)
  
  return binary
}

export async function loadAndCacheWasm(
  name: string,
  path: string
): Promise<WebAssembly.Module> {
  const buffer = await loadCompressedWasm(path)
  return await WebAssembly.compile(buffer)
}

export async function clearWasmCache(): Promise<void> {
  const db = await openDB()
  return new Promise((resolve, reject) => {
    const transaction = db.transaction(WASM_CACHE_STORE, 'readwrite')
    const store = transaction.objectStore(WASM_CACHE_STORE)
    const request = store.clear()
    
    request.onerror = () => reject(request.error)
    request.onsuccess = () => resolve()
  })
}
