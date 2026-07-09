"use client"

import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import {
  Palette,
  Type,
  Hash,
  WrapText,
  Map as MapIcon,
  Indent,
  Space,
  ScanLine,
} from "lucide-react"
import {
  SHIKI_THEME_CONFIGS,
  hasDualModes,
  type ShikiTheme,
  type ShikiThemeConfig,
} from "@/components/block-content-editor/lib/shiki/themes"
import type {
  MonacoOptionsPreferences,
  RenderWhitespace,
  RenderLineHighlight,
} from "@/components/block-content-editor/lib/storage/editor/editor-preferences"

interface MonacoOptionsFormProps {
  /** Stable id prefix so labels/inputs stay unique when two forms (editor + preview) are mounted. */
  scope: 'editor' | 'preview'
  /** Resolved options snapshot, or `null` while preferences hydrate. */
  options: MonacoOptionsPreferences | null
  /** Atomic per-key setter that persists globally and notifies subscribers. */
  onChange: <K extends keyof MonacoOptionsPreferences>(key: K, value: MonacoOptionsPreferences[K]) => Promise<void> | void
}

const TAB_SIZE_CHOICES = [2, 4, 8] as const
const WHITESPACE_CHOICES: { value: RenderWhitespace; label: string }[] = [
  { value: 'none', label: 'Off' },
  { value: 'boundary', label: 'Boundary' },
  { value: 'all', label: 'All' },
]
const LINE_HIGHLIGHT_CHOICES: { value: RenderLineHighlight; label: string }[] = [
  { value: 'none', label: 'Off' },
  { value: 'gutter', label: 'Gutter only' },
  { value: 'line', label: 'Line only' },
  { value: 'all', label: 'Gutter + Line' },
  { value: 'rectangle', label: 'Rectangle outline' },
]

/**
 * Build the visible `<option>` label for a theme, prefixing icons that
 * communicate at-a-glance properties:
 *   ◐  → theme has genuinely different dark/light variants
 *   ◉  → theme is high-contrast / accessibility-tuned
 */
function formatThemeLabel(config: ShikiThemeConfig): string {
  const markers: string[] = []
  if (hasDualModes(config)) markers.push('\u25D0')
  if (config.highContrast) markers.push('\u25C9')
  return markers.length > 0 ? `${markers.join(' ')}  ${config.label}` : config.label
}

/**
 * Single, didactic form that renders every Monaco-surface preference in
 * a consistent layout. Used twice by the settings menu — once for the
 * `editor` scope and once for `preview` — so the user encounters the
 * exact same set of controls in both tabs.
 *
 * The form fires one persisted write per interaction via `onChange`, so
 * cross-editor reactivity (pub/sub) immediately reflects the new value
 * in every open Monaco surface.
 */
export function MonacoOptionsForm({ scope, options, onChange }: MonacoOptionsFormProps) {
  if (options === null) {
    return <div className="text-sm text-gray-500">Loading…</div>
  }

  const idFor = (key: string) => `${scope}-${key}`

  return (
    <div className="space-y-4">
      {/* Theme */}
      <section className="space-y-2">
        <div className="flex items-center gap-2">
          <Palette className="h-4 w-4 text-indigo-500" />
          <Label htmlFor={idFor('shikiTheme')} className="text-sm font-medium">
            Syntax Theme
          </Label>
        </div>
        <select
          id={idFor('shikiTheme')}
          value={options.shikiTheme}
          onChange={(e) => void onChange('shikiTheme', e.target.value as ShikiTheme)}
          className="w-full px-2 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
        >
          {Object.entries(SHIKI_THEME_CONFIGS).map(([key, config]) => (
            <option key={key} value={key}>{formatThemeLabel(config)}</option>
          ))}
        </select>
        <p className="text-[11px] text-gray-500 dark:text-gray-400">
          <span className="font-medium">{'\u25D0'}</span> dark/light variants
          <span className="mx-1">·</span>
          <span className="font-medium">{'\u25C9'}</span> high contrast
        </p>
      </section>

      <div className="border-t border-gray-200 dark:border-gray-700" />

      {/* Font size */}
      <section className="space-y-2">
        <div className="flex items-center gap-2">
          <Type className="h-4 w-4 text-purple-500" />
          <Label htmlFor={idFor('fontSize')} className="text-sm font-medium">
            Font Size: {options.fontSize}px
          </Label>
        </div>
        <input
          id={idFor('fontSize')}
          type="range"
          min={10}
          max={24}
          step={1}
          value={options.fontSize}
          onChange={(e) => void onChange('fontSize', parseInt(e.target.value, 10))}
          className="w-full accent-orange-500"
        />
      </section>

      {/* Line numbers */}
      <section className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Hash className="h-4 w-4 text-blue-500" />
          <Label htmlFor={idFor('lineNumbers')} className="text-sm font-medium cursor-pointer">
            Line Numbers
          </Label>
        </div>
        <Switch
          id={idFor('lineNumbers')}
          checked={options.lineNumbers}
          onCheckedChange={(checked) => void onChange('lineNumbers', checked)}
        />
      </section>

      {/* Word wrap */}
      <section className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <WrapText className="h-4 w-4 text-emerald-500" />
          <Label htmlFor={idFor('wordWrap')} className="text-sm font-medium cursor-pointer">
            Word Wrap
          </Label>
        </div>
        <Switch
          id={idFor('wordWrap')}
          checked={options.wordWrap}
          onCheckedChange={(checked) => void onChange('wordWrap', checked)}
        />
      </section>

      {/* Minimap */}
      <section className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <MapIcon className="h-4 w-4 text-amber-500" />
          <Label htmlFor={idFor('minimap')} className="text-sm font-medium cursor-pointer">
            Minimap
          </Label>
        </div>
        <Switch
          id={idFor('minimap')}
          checked={options.minimap}
          onCheckedChange={(checked) => void onChange('minimap', checked)}
        />
      </section>

      {/* Tab size */}
      <section className="space-y-2">
        <div className="flex items-center gap-2">
          <Indent className="h-4 w-4 text-cyan-500" />
          <Label htmlFor={idFor('tabSize')} className="text-sm font-medium">
            Tab Size
          </Label>
        </div>
        <select
          id={idFor('tabSize')}
          value={options.tabSize}
          onChange={(e) => void onChange('tabSize', parseInt(e.target.value, 10))}
          className="w-full px-2 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
        >
          {TAB_SIZE_CHOICES.map((n) => (
            <option key={n} value={n}>{n} spaces</option>
          ))}
        </select>
      </section>

      {/* Render whitespace */}
      <section className="space-y-2">
        <div className="flex items-center gap-2">
          <Space className="h-4 w-4 text-rose-500" />
          <Label htmlFor={idFor('renderWhitespace')} className="text-sm font-medium">
            Show Whitespace
          </Label>
        </div>
        <select
          id={idFor('renderWhitespace')}
          value={options.renderWhitespace}
          onChange={(e) => void onChange('renderWhitespace', e.target.value as RenderWhitespace)}
          className="w-full px-2 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
        >
          {WHITESPACE_CHOICES.map(({ value, label }) => (
            <option key={value} value={value}>{label}</option>
          ))}
        </select>
      </section>

      {/* Active-line highlight */}
      <section className="space-y-2">
        <div className="flex items-center gap-2">
          <ScanLine className="h-4 w-4 text-pink-500" />
          <Label htmlFor={idFor('renderLineHighlight')} className="text-sm font-medium">
            Active Line Highlight
          </Label>
        </div>
        <select
          id={idFor('renderLineHighlight')}
          value={options.renderLineHighlight}
          onChange={(e) => void onChange('renderLineHighlight', e.target.value as RenderLineHighlight)}
          className="w-full px-2 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
        >
          {LINE_HIGHLIGHT_CHOICES.map(({ value, label }) => (
            <option key={value} value={value}>{label}</option>
          ))}
        </select>
      </section>
    </div>
  )
}
