/**
 * Editor Preferences Manager
 * Gerencia configurações globais e específicas por tipo de node
 */

import {
  SHIKI_THEME_CONFIGS,
  type ShikiTheme,
} from "@/components/block-content-editor/lib/shiki/themes"

export type ModalSize = 'compact' | 'widescreen' | 'ultrawide' | 'fullscreen'
export type WordWrap = 'on' | 'off'
export type RenderWhitespace = 'none' | 'boundary' | 'all'
/**
 * `'none' | 'gutter' | 'line' | 'all'` map 1:1 to Monaco's native values.
 * `'rectangle'` is a custom mode: it reuses Monaco's `'line'` highlight
 * and overlays a 1px outline (border) on the active line via a CSS hook
 * applied through `applyLineHighlightDecoration`.
 */
export type RenderLineHighlight = 'none' | 'gutter' | 'line' | 'all' | 'rectangle'

/**
 * The full bag of Monaco-surface options the user can customize. Stored
 * twice on `EditorPreferences` — once under `editor` (applied to every
 * editable Monaco surface) and once under `preview` (applied to the
 * read-only render of any Monaco-using block, including the code-studio
 * "base" display that mirrors the student view).
 *
 * Keeping the two scopes structurally identical lets the settings UI
 * render the same form twice and keeps consumer code mechanical: each
 * editor wrapper just picks `settings.editor` or `settings.preview` and
 * spreads it into Monaco's options.
 */
export interface MonacoOptionsPreferences {
  /** Shiki syntax theme. Resolved to a light/dark variant at render time. */
  shikiTheme: ShikiTheme
  /** Font size in pixels. Clamped 10–24 by the UI slider. */
  fontSize: number
  /** When `false`, the gutter line-number column is hidden. */
  lineNumbers: boolean
  /** When `true`, long lines wrap at the viewport edge. */
  wordWrap: boolean
  /** When `true`, the minimap overview ruler is shown on the right. */
  minimap: boolean
  /** Width of a tab in spaces. UI slider exposes 2 / 4 / 8. */
  tabSize: number
  /**
   * Whitespace visualization: `'none'` hides it; `'boundary'` shows it
   * only between non-whitespace tokens; `'all'` shows every space and
   * tab as a dot/arrow.
   */
  renderWhitespace: RenderWhitespace
  /**
   * How the active line is highlighted: `'none'` disables it; `'gutter'`
   * highlights only the line number column; `'line'` highlights only
   * the text area; `'all'` highlights both.
   */
  renderLineHighlight: RenderLineHighlight
}

export interface EditorPreferences {
  /** Modal sizing for any block-editor shell. */
  modalSize: ModalSize
  /**
   * Options applied to every editable Monaco surface (code-studio
   * secondary displays, html, markdown, mermaid, vega-lite, …).
   */
  editor: MonacoOptionsPreferences
  /**
   * Options applied to the read-only render of Monaco-using blocks in
   * document preview, and to the code-studio "base" display — i.e.
   * everywhere students see Monaco rather than editing it.
   */
  preview: MonacoOptionsPreferences
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

// Default Monaco-surface options. Editor and preview share the same
// shape; in practice the preview defaults to slightly more reader-
// friendly values (smaller font, minimap off) but the user can match
// them perfectly through the settings UI.
const DEFAULT_EDITOR_OPTIONS: MonacoOptionsPreferences = {
  shikiTheme: 'github',
  fontSize: 14,
  lineNumbers: true,
  wordWrap: true,
  minimap: false,
  tabSize: 2,
  renderWhitespace: 'none',
  renderLineHighlight: 'line',
}

const DEFAULT_PREVIEW_OPTIONS: MonacoOptionsPreferences = {
  shikiTheme: 'github',
  fontSize: 13,
  lineNumbers: true,
  wordWrap: true,
  minimap: false,
  tabSize: 2,
  renderWhitespace: 'none',
  renderLineHighlight: 'none',
}

// Default preferences
const DEFAULT_PREFERENCES: EditorPreferences = {
  modalSize: 'widescreen',
  editor: DEFAULT_EDITOR_OPTIONS,
  preview: DEFAULT_PREVIEW_OPTIONS,
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
            // Merge persisted globals with defaults so newly-added
            // preference keys backfill cleanly when the user's DB entry
            // predates them. The two MonacoOptions groups are merged
            // independently so adding a new option (e.g. `tabSize`) on
            // top of an older persisted value doesn't drop the user's
            // existing customizations.
            const persistedGlobal = (data.global ?? {}) as Partial<EditorPreferences>
            const merged: EditorPreferences = {
              ...DEFAULT_PREFERENCES,
              ...persistedGlobal,
              editor: {
                ...DEFAULT_EDITOR_OPTIONS,
                ...(persistedGlobal.editor ?? {}),
              },
              preview: {
                ...DEFAULT_PREVIEW_OPTIONS,
                ...(persistedGlobal.preview ?? {}),
              },
            }
            resolve({
              global: merged,
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

type StoredEditorPreferencesInput = {
  readonly modalSize?: string
  readonly editor?: Record<string, unknown>
  readonly preview?: Record<string, unknown>
}

function isModalSize(value: unknown): value is ModalSize {
  return value === 'compact' || value === 'widescreen' || value === 'ultrawide' || value === 'fullscreen'
}

function isShikiTheme(value: unknown): value is ShikiTheme {
  return typeof value === 'string' && value in SHIKI_THEME_CONFIGS
}

function isRenderWhitespace(value: unknown): value is RenderWhitespace {
  return value === 'none' || value === 'boundary' || value === 'all'
}

function isRenderLineHighlight(value: unknown): value is RenderLineHighlight {
  return value === 'none' || value === 'gutter' || value === 'line' || value === 'all' || value === 'rectangle'
}

function getStoredMonacoOptions(value: Record<string, unknown>): Partial<MonacoOptionsPreferences> {
  const shikiTheme = value.shikiTheme
  const fontSize = value.fontSize
  const lineNumbers = value.lineNumbers
  const wordWrap = value.wordWrap
  const minimap = value.minimap
  const tabSize = value.tabSize
  const renderWhitespace = value.renderWhitespace
  const renderLineHighlight = value.renderLineHighlight

  return {
    ...(isShikiTheme(shikiTheme) ? { shikiTheme } : {}),
    ...(typeof fontSize === 'number' && Number.isFinite(fontSize) ? { fontSize } : {}),
    ...(typeof lineNumbers === 'boolean' ? { lineNumbers } : {}),
    ...(typeof wordWrap === 'boolean' ? { wordWrap } : {}),
    ...(typeof minimap === 'boolean' ? { minimap } : {}),
    ...(typeof tabSize === 'number' && Number.isFinite(tabSize) ? { tabSize } : {}),
    ...(isRenderWhitespace(renderWhitespace) ? { renderWhitespace } : {}),
    ...(isRenderLineHighlight(renderLineHighlight) ? { renderLineHighlight } : {}),
  }
}

export async function applyStoredGlobalPreferences(
  input: StoredEditorPreferencesInput,
): Promise<void> {
  const allPrefs = await db.get()
  const modalSize = isModalSize(input.modalSize) ? input.modalSize : allPrefs.global.modalSize
  const editor = input.editor
    ? { ...allPrefs.global.editor, ...getStoredMonacoOptions(input.editor) }
    : allPrefs.global.editor
  const preview = input.preview
    ? { ...allPrefs.global.preview, ...getStoredMonacoOptions(input.preview) }
    : allPrefs.global.preview

  allPrefs.global = { modalSize, editor, preview }
  await db.set(allPrefs)
  notifyPreferencesChanged()
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

/**
 * Updates a single key inside one of the two Monaco-options groups
 * (`editor` or `preview`) without disturbing the others. This is the
 * preferred way for the settings UI to update font size, line numbers,
 * theme, etc. because it keeps the write atomic and avoids races where
 * two controls would otherwise overwrite each other's group object.
 */
export async function setMonacoOption<K extends keyof MonacoOptionsPreferences>(
  scope: 'editor' | 'preview',
  key: K,
  value: MonacoOptionsPreferences[K],
): Promise<void> {
  const allPrefs = await db.get()
  allPrefs.global[scope] = {
    ...allPrefs.global[scope],
    [key]: value,
  }
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
