/**
 * Editor Preferences Manager
 * Gerencia configurações globais e específicas por tipo de node
 */

import type { ShikiTheme } from "@/components/block-content-editor/lib/shiki/themes"

export type ModalSize = 'compact' | 'widescreen' | 'ultrawide' | 'fullscreen'

export interface EditorPreferences {
  // Modal size configuration
  modalSize: ModalSize
  /**
   * Syntax theme used inside the editor modal for any Monaco-backed block
   * (code-studio, mermaid, html, markdown, vega-lite, …). Always global.
   */
  shikiTheme: ShikiTheme
  /**
   * Syntax theme used to render Monaco-using blocks in document preview /
   * read-only view (including the code-studio "base" display, which mirrors
   * what students will see). Always global.
   */
  previewShikiTheme: ShikiTheme
}

export interface NodeTypePreferences {
  [nodeType: string]: Partial<EditorPreferences>
}

export interface AllPreferences {
  global: EditorPreferences
  nodeTypes: NodeTypePreferences
}

const DB_NAME = 'editor-preferences'
const DB_VERSION = 1
const STORE_NAME = 'preferences'
const PREFERENCES_KEY = 'editor-prefs'

// Default preferences
const DEFAULT_PREFERENCES: EditorPreferences = {
  modalSize: 'widescreen',
  shikiTheme: 'github',
  previewShikiTheme: 'github',
}

// IndexedDB helper
class PreferencesDB {
  private dbPromise: Promise<IDBDatabase> | null = null

  private async getDB(): Promise<IDBDatabase> {
    if (this.dbPromise) return this.dbPromise

    this.dbPromise = new Promise((resolve, reject) => {
      const request = indexedDB.open(DB_NAME, DB_VERSION)

      request.onerror = () => reject(request.error)
      request.onsuccess = () => resolve(request.result)

      request.onupgradeneeded = (event) => {
        const db = (event.target as IDBOpenDBRequest).result
        if (!db.objectStoreNames.contains(STORE_NAME)) {
          db.createObjectStore(STORE_NAME)
        }
      }
    })

    return this.dbPromise
  }

  async get(): Promise<AllPreferences> {
    try {
      const db = await this.getDB()
      return new Promise((resolve, reject) => {
        const transaction = db.transaction(STORE_NAME, 'readonly')
        const store = transaction.objectStore(STORE_NAME)
        const request = store.get(PREFERENCES_KEY)

        request.onsuccess = () => {
          const data = request.result
          if (data) {
            // Merge persisted globals with defaults so newly-added preference
            // keys (e.g. `shikiTheme`) get sane values for users whose DB
            // entry predates them.
            resolve({
              global: { ...DEFAULT_PREFERENCES, ...data.global },
              nodeTypes: data.nodeTypes ?? {},
            })
          } else {
            // Return default preferences
            resolve({
              global: DEFAULT_PREFERENCES,
              nodeTypes: {},
            })
          }
        }

        request.onerror = () => reject(request.error)
      })
    } catch (error) {
      console.error('Failed to get preferences:', error)
      return {
        global: DEFAULT_PREFERENCES,
        nodeTypes: {},
      }
    }
  }

  async set(preferences: AllPreferences): Promise<void> {
    try {
      const db = await this.getDB()
      return new Promise((resolve, reject) => {
        const transaction = db.transaction(STORE_NAME, 'readwrite')
        const store = transaction.objectStore(STORE_NAME)
        const request = store.put(preferences, PREFERENCES_KEY)

        request.onsuccess = () => resolve()
        request.onerror = () => reject(request.error)
      })
    } catch (error) {
      console.error('Failed to set preferences:', error)
      throw error
    }
  }
}

const db = new PreferencesDB()

// Lightweight pub/sub so every open editor refreshes its resolved
// preferences when one of them writes a new value (e.g. the user picks a
// new global theme inside an html-editor while a mermaid-editor is also
// mounted somewhere on the page).
type PreferencesListener = () => void
const listeners = new Set<PreferencesListener>()

function notifyPreferencesChanged(): void {
  listeners.forEach((fn) => {
    try {
      fn()
    } catch (error) {
      console.error('Preferences listener threw:', error)
    }
  })
}

export function subscribeToPreferences(listener: PreferencesListener): () => void {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

// Public API
export async function getEditorPreferences(nodeType?: string): Promise<EditorPreferences> {
  const allPrefs = await db.get()
  
  if (nodeType && allPrefs.nodeTypes[nodeType]) {
    // Merge node-specific preferences with global defaults
    return {
      ...allPrefs.global,
      ...allPrefs.nodeTypes[nodeType],
    }
  }
  
  return allPrefs.global
}

/**
 * Returns the raw `AllPreferences` snapshot (global + per-nodeType
 * overrides). Use this when the caller needs to distinguish a nodeType
 * override from a global value — e.g. to drive a "this node type only"
 * toggle in the settings UI.
 */
export async function getAllPreferences(): Promise<AllPreferences> {
  return db.get()
}

export async function setGlobalPreference<K extends keyof EditorPreferences>(
  key: K,
  value: EditorPreferences[K]
): Promise<void> {
  const allPrefs = await db.get()
  allPrefs.global[key] = value
  
  // Clear node-specific overrides for this key when setting global
  for (const nodeType in allPrefs.nodeTypes) {
    const nodePrefs = allPrefs.nodeTypes[nodeType]
    if (nodePrefs && nodePrefs[key] !== undefined) {
      delete nodePrefs[key]
      // Remove empty node type entries
      if (Object.keys(nodePrefs).length === 0) {
        delete allPrefs.nodeTypes[nodeType]
      }
    }
  }
  
  await db.set(allPrefs)
  notifyPreferencesChanged()
}

export async function setNodeTypePreference<K extends keyof EditorPreferences>(
  nodeType: string,
  key: K,
  value: EditorPreferences[K]
): Promise<void> {
  const allPrefs = await db.get()
  
  if (!allPrefs.nodeTypes[nodeType]) {
    allPrefs.nodeTypes[nodeType] = {}
  }
  
  allPrefs.nodeTypes[nodeType][key] = value
  await db.set(allPrefs)
  notifyPreferencesChanged()
}

export async function clearNodeTypePreference(
  nodeType: string,
  key: keyof EditorPreferences
): Promise<void> {
  const allPrefs = await db.get()
  
  if (allPrefs.nodeTypes[nodeType]) {
    delete allPrefs.nodeTypes[nodeType][key]
    
    // Remove empty node type entries
    if (Object.keys(allPrefs.nodeTypes[nodeType]).length === 0) {
      delete allPrefs.nodeTypes[nodeType]
    }
  }
  
  await db.set(allPrefs)
  notifyPreferencesChanged()
}

export async function clearAllNodeTypePreferences(nodeType: string): Promise<void> {
  const allPrefs = await db.get()
  delete allPrefs.nodeTypes[nodeType]
  await db.set(allPrefs)
  notifyPreferencesChanged()
}

export async function hasNodeTypePreference(nodeType: string, key: keyof EditorPreferences): Promise<boolean> {
  const allPrefs = await db.get()
  return !!(allPrefs.nodeTypes[nodeType] && allPrefs.nodeTypes[nodeType][key] !== undefined)
}

// Modal size helper
export function getModalSizeClasses(size: ModalSize): { container: string; modal: string } {
  switch (size) {
    case 'compact':
      return {
        container: 'p-4',
        modal: 'w-full max-w-7xl h-[90vh]',
      }
    case 'widescreen':
      return {
        container: 'p-0',
        modal: 'w-full max-w-[1920px] h-[100vh]',
      }
    case 'ultrawide':
      return {
        container: 'p-0',
        modal: 'w-full max-w-[2560px] h-[100vh]',
      }
    case 'fullscreen':
      return {
        container: 'p-0',
        modal: 'w-screen h-screen',
      }
  }
}

export const MODAL_SIZE_LABELS: Record<ModalSize, string> = {
  compact: 'Compact',
  widescreen: 'Widescreen',
  ultrawide: 'Ultra-wide',
  fullscreen: 'Full Screen',
}
