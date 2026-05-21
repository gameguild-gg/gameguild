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

export const SHIKI_THEME_CONFIGS: Record<ShikiTheme, { label: string; dark: string; light: string }> = {
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
}

/** Resolve a ShikiTheme to a concrete theme name based on the active color mode. */
export function getShikiThemeName(shikiTheme: ShikiTheme, isDark: boolean): string {
  const config = SHIKI_THEME_CONFIGS[shikiTheme]
  return isDark ? config.dark : config.light
}
