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
  | "tokyo-night"
  | "one"
  | "material"
  | "rose-pine"
  | "gruvbox"
  | "night-owl"
  | "github-hc"
  | "min"
  | "slack"
  | "red";

export interface ShikiThemeConfig {
  label: string;
  dark: string;
  light: string;
  highContrast?: boolean;
}

export const SHIKI_THEME_CONFIGS: Record<ShikiTheme, ShikiThemeConfig> = {
  github: { label: "GitHub", dark: "github-dark", light: "github-light" },
  "github-default": {
    label: "GitHub Default",
    dark: "github-dark-default",
    light: "github-light-default",
  },
  "github-dimmed": {
    label: "GitHub Dimmed",
    dark: "github-dark-dimmed",
    light: "github-light-default",
  },
  plus: { label: "Plus", dark: "dark-plus", light: "light-plus" },
  catppuccin: {
    label: "Catppuccin",
    dark: "catppuccin-mocha",
    light: "catppuccin-latte",
  },
  vitesse: {
    label: "Vitesse",
    dark: "vitesse-dark",
    light: "vitesse-light",
  },
  monokai: { label: "Monokai", dark: "monokai", light: "monokai" },
  solarized: {
    label: "Solarized",
    dark: "solarized-dark",
    light: "solarized-light",
  },
  dracula: { label: "Dracula", dark: "dracula", light: "dracula" },
  nord: { label: "Nord", dark: "nord", light: "nord" },
  "tokyo-night": {
    label: "Tokyo Night",
    dark: "tokyo-night",
    light: "tokyo-night",
  },
  one: { label: "One", dark: "one-dark-pro", light: "one-light" },
  material: {
    label: "Material",
    dark: "material-theme-ocean",
    light: "material-theme-lighter",
  },
  "rose-pine": {
    label: "Rose Pine",
    dark: "rose-pine",
    light: "rose-pine-dawn",
  },
  gruvbox: {
    label: "Gruvbox",
    dark: "gruvbox-dark-medium",
    light: "gruvbox-light-medium",
  },
  "night-owl": {
    label: "Night Owl",
    dark: "night-owl",
    light: "night-owl",
  },
  "github-hc": {
    label: "GitHub High Contrast",
    dark: "github-dark-high-contrast",
    light: "github-light-high-contrast",
    highContrast: true,
  },
  min: {
    label: "Min",
    dark: "min-dark",
    light: "min-light",
    highContrast: true,
  },
  slack: {
    label: "Slack",
    dark: "slack-dark",
    light: "slack-ochin",
    highContrast: true,
  },
  red: {
    label: "Red",
    dark: "red",
    light: "red",
    highContrast: true,
  },
};

export const SHIKI_THEME_KEYS = Object.keys(
  SHIKI_THEME_CONFIGS,
) as ShikiTheme[];

export const SHIKI_THEME_NAMES = Array.from(
  new Set(
    Object.values(SHIKI_THEME_CONFIGS).flatMap(({ dark, light }) => [
      dark,
      light,
    ]),
  ),
);

export function isShikiTheme(value: unknown): value is ShikiTheme {
  return typeof value === "string" && value in SHIKI_THEME_CONFIGS;
}

export function hasDualModes(config: ShikiThemeConfig): boolean {
  return config.dark !== config.light;
}

export function getShikiThemeName(theme: ShikiTheme, isDark: boolean): string {
  const config = SHIKI_THEME_CONFIGS[theme];
  return isDark ? config.dark : config.light;
}
