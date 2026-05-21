/**
 * Shiki/Monaco syntax theme catalog shared across all block editors that use
 * Monaco (code-studio, mermaid, html, markdown, vega-lite, …).
 *
 * The theme catalog and resolver live here rather than inside any single
 * editor (e.g. `code-studio/types.ts`) so every Monaco-using block editor can
 * reuse the same set of themes and persist user choices at the global
 * preferences level.
 */

export type ShikiTheme =
  // Original set
  | "github"
  | "github-default"
  | "github-dimmed"
  | "plus"
  | "catppuccin"
  | "vitesse"
  | "monokai"
  | "solarized"
  | "dracula"
  | "nord"
  // New themes
  | "tokyo-night"
  | "one"
  | "material"
  | "rose-pine"
  | "gruvbox"
  | "night-owl"
  // High-contrast themes
  | "github-hc"
  | "min"
  | "slack"
  | "red"

export interface ShikiThemeConfig {
  label: string
  dark: string
  light: string
  /** Marks themes designed for high-contrast / accessibility. */
  highContrast?: boolean
}

export const SHIKI_THEME_CONFIGS: Record<ShikiTheme, ShikiThemeConfig> = {
  // ── Original set ─────────────────────────────────────────────────
  github: { label: "GitHub", dark: "github-dark", light: "github-light" },
  "github-default": { label: "GitHub Default", dark: "github-dark-default", light: "github-light-default" },
  "github-dimmed": { label: "GitHub Dimmed", dark: "github-dark-dimmed", light: "github-light-default" },
  plus: { label: "Plus", dark: "dark-plus", light: "light-plus" },
  catppuccin: { label: "Catppuccin", dark: "catppuccin-mocha", light: "catppuccin-latte" },
  vitesse: { label: "Vitesse", dark: "vitesse-dark", light: "vitesse-light" },
  monokai: { label: "Monokai", dark: "monokai", light: "monokai" },
  solarized: { label: "Solarized", dark: "solarized-dark", light: "solarized-light" },
  dracula: { label: "Dracula", dark: "dracula", light: "dracula" },
  nord: { label: "Nord", dark: "nord", light: "nord" },
  // ── New themes ───────────────────────────────────────────────────
  "tokyo-night": { label: "Tokyo Night", dark: "tokyo-night", light: "tokyo-night" },
  one: { label: "One", dark: "one-dark-pro", light: "one-light" },
  material: { label: "Material", dark: "material-theme-ocean", light: "material-theme-lighter" },
  "rose-pine": { label: "Rosé Pine", dark: "rose-pine", light: "rose-pine-dawn" },
  gruvbox: { label: "Gruvbox", dark: "gruvbox-dark-medium", light: "gruvbox-light-medium" },
  "night-owl": { label: "Night Owl", dark: "night-owl", light: "night-owl" },
  // ── High-contrast themes ─────────────────────────────────────────
  "github-hc": {
    label: "GitHub High Contrast",
    dark: "github-dark-high-contrast",
    light: "github-light-high-contrast",
    highContrast: true,
  },
  min: { label: "Min", dark: "min-dark", light: "min-light", highContrast: true },
  slack: { label: "Slack", dark: "slack-dark", light: "slack-ochin", highContrast: true },
  red: { label: "Red", dark: "red", light: "red", highContrast: true },
}

/**
 * `true` when a theme provides genuinely distinct dark and light variants
 * (as opposed to a single-mode theme reused in both color schemes).
 */
export function hasDualModes(config: ShikiThemeConfig): boolean {
  return config.dark !== config.light
}

/** Resolve a ShikiTheme to a concrete theme name based on the active color mode. */
export function getShikiThemeName(shikiTheme: ShikiTheme, isDark: boolean): string {
  const config = SHIKI_THEME_CONFIGS[shikiTheme]
  return isDark ? config.dark : config.light
}
