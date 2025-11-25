import { ungzip } from 'pako'

const wasmCache: Map<string, WebAssembly.Module> = new Map()

// IndexedDB para cache persistente de arquivos descomprimidos
const DB_NAME = 'wasm-cache'
const DB_VERSION = 1
const STORE_NAME = 'decompressed-files'

interface CachedFile {
  url: string
  data: ArrayBuffer
  timestamp: number
  version: string // Para invalidar cache quando necessário
}

let dbPromise: Promise<IDBDatabase> | null = null

async function getDB(): Promise<IDBDatabase> {
  if (dbPromise) return dbPromise

  dbPromise = new Promise((resolve, reject) => {
    const request = indexedDB.open(DB_NAME, DB_VERSION)

    request.onerror = () => reject(request.error)
    request.onsuccess = () => resolve(request.result)

    request.onupgradeneeded = (event) => {
      const db = (event.target as IDBOpenDBRequest).result
      if (!db.objectStoreNames.contains(STORE_NAME)) {
        db.createObjectStore(STORE_NAME, { keyPath: 'url' })
      }
    }
  })

  return dbPromise
}

async function getCachedFile(url: string): Promise<ArrayBuffer | null> {
  try {
    const db = await getDB()
    return new Promise((resolve, reject) => {
      const transaction = db.transaction(STORE_NAME, 'readonly')
      const store = transaction.objectStore(STORE_NAME)
      const request = store.get(url)

      request.onsuccess = () => {
        const cached: CachedFile | undefined = request.result
        if (cached) {
          console.log(`[WASM Cache] Loading from cache: ${url}`)
          resolve(cached.data)
        } else {
          resolve(null)
        }
      }
      request.onerror = () => reject(request.error)
    })
  } catch (error) {
    console.warn('[WASM Cache] Error reading cache:', error)
    return null
  }
}

async function setCachedFile(url: string, data: ArrayBuffer): Promise<void> {
  try {
    const db = await getDB()
    return new Promise((resolve, reject) => {
      const transaction = db.transaction(STORE_NAME, 'readwrite')
      const store = transaction.objectStore(STORE_NAME)
      
      const cached: CachedFile = {
        url,
        data,
        timestamp: Date.now(),
        version: '1.0', // Incrementar para invalidar cache
      }

      const request = store.put(cached)
      request.onsuccess = () => {
        console.log(`[WASM Cache] Saved to cache: ${url}`)
        resolve()
      }
      request.onerror = () => reject(request.error)
    })
  } catch (error) {
    console.warn('[WASM Cache] Error writing cache:', error)
  }
}

// Callback para notificar sobre downloads
type DownloadNotificationCallback = (message: string, isDownloading: boolean) => void
let downloadNotificationCallback: DownloadNotificationCallback | null = null

export function setDownloadNotificationCallback(callback: DownloadNotificationCallback | null): void {
  downloadNotificationCallback = callback
}

export async function loadCompressedWasm(path: string): Promise<ArrayBuffer> {
  // Tentar carregar do cache primeiro
  const cached = await getCachedFile(path)
  if (cached) {
    return cached
  }

  // Se não estiver em cache, notificar que vai baixar
  const fileName = path.split('/').pop() || path
  if (downloadNotificationCallback) {
    downloadNotificationCallback(`Downloading ${fileName}...`, true)
  }
  
  console.log(`[WASM Cache] Downloading and decompressing: ${path}`)
  
  try {
    const response = await fetch(path)
    if (!response.ok) {
      throw new Error(`Failed to fetch ${path}: ${response.statusText}`)
    }

    const compressed = await response.arrayBuffer()
    const decompressed = ungzip(new Uint8Array(compressed))
    const buffer = decompressed.buffer

    // Salvar no cache para uso futuro
    await setCachedFile(path, buffer)
    
    if (downloadNotificationCallback) {
      downloadNotificationCallback(`${fileName} ready`, false)
    }

    return buffer
  } catch (error) {
    if (downloadNotificationCallback) {
      downloadNotificationCallback(`Failed to download ${fileName}`, false)
    }
    throw error
  }
}

export async function loadCompressedScript(url: string): Promise<void> {
  // Tentar carregar do cache primeiro
  const cached = await getCachedFile(url)
  let decompressed: Uint8Array

  if (cached) {
    decompressed = new Uint8Array(cached)
  } else {
    // Se não estiver em cache, notificar que vai baixar
    const fileName = url.split('/').pop() || url
    if (downloadNotificationCallback) {
      downloadNotificationCallback(`Downloading ${fileName}...`, true)
    }
    
    console.log(`[WASM Cache] Downloading and decompressing script: ${url}`)
    
    try {
      const response = await fetch(url)
      if (!response.ok) {
        throw new Error(`Failed to fetch ${url}: ${response.statusText}`)
      }

      const compressed = await response.arrayBuffer()
      decompressed = ungzip(new Uint8Array(compressed))

      // Converter para ArrayBuffer antes de salvar no cache
      const buffer = new ArrayBuffer(decompressed.byteLength)
      new Uint8Array(buffer).set(decompressed)
      await setCachedFile(url, buffer)
      
      if (downloadNotificationCallback) {
        downloadNotificationCallback(`${fileName} ready`, false)
      }
    } catch (error) {
      if (downloadNotificationCallback) {
        downloadNotificationCallback(`Failed to download ${fileName}`, false)
      }
      throw error
    }
  }

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

// Função auxiliar para limpar cache antigo (opcional)
export async function clearWasmCache(): Promise<void> {
  try {
    const db = await getDB()
    return new Promise((resolve, reject) => {
      const transaction = db.transaction(STORE_NAME, 'readwrite')
      const store = transaction.objectStore(STORE_NAME)
      const request = store.clear()
      
      request.onsuccess = () => {
        console.log('[WASM Cache] Cache cleared')
        resolve()
      }
      request.onerror = () => reject(request.error)
    })
  } catch (error) {
    console.warn('[WASM Cache] Error clearing cache:', error)
  }
}
