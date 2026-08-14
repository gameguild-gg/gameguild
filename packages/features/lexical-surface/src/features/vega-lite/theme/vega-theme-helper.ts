/**
 * Helper para calcular temas light e dark baseado no tema selecionado e modo
 */

export type VegaThemeBase =
  | "default"
  | "excel"
  | "ggplot2"
  | "quartz"
  | "vox"
  | "fivethirtyeight"
  | "latimes"
  | "urbaninstitute"
  | "googlecharts"
  | "powerbi";

export type ThemeMode = "system" | "only-light" | "only-dark";

export interface ThemePair {
  themeLight: string;
  themeDark: string;
}

/**
 * Mapeia um tema base para suas variantes light e dark
 */
export function getThemePair(
  themeBase: VegaThemeBase,
  mode: ThemeMode = "system",
): ThemePair {
  // Mapear tema base para variante dark (adicionar -dark ao nome)
  const darkVariant = themeBase === "default" ? "dark" : `${themeBase}-dark`;

  switch (mode) {
    case "system":
      // Em modo system: usa o tema base para light e sua variante dark para dark
      return {
        themeLight: themeBase,
        themeDark: darkVariant,
      };

    case "only-light":
      // Em modo only-light: usa o tema base para ambos
      return {
        themeLight: themeBase,
        themeDark: themeBase,
      };

    case "only-dark":
      // Em modo only-dark: usa a variante dark para ambos
      return {
        themeLight: darkVariant,
        themeDark: darkVariant,
      };
  }
}

/**
 * Lista de todos os temas disponíveis
 */
export const AVAILABLE_THEMES: VegaThemeBase[] = [
  "default",
  "excel",
  "ggplot2",
  "quartz",
  "vox",
  "fivethirtyeight",
  "latimes",
  "urbaninstitute",
  "googlecharts",
  "powerbi",
];

/**
 * Descrições dos temas para exibição na UI
 */
export const THEME_DESCRIPTIONS: Record<VegaThemeBase, string> = {
  default: "Default",
  excel: "Excel",
  ggplot2: "ggplot2",
  quartz: "Quartz",
  vox: "Vox",
  fivethirtyeight: "FiveThirtyEight",
  latimes: "LA Times",
  urbaninstitute: "Urban Institute",
  googlecharts: "Google Charts",
  powerbi: "Power BI",
};

/**
 * Descrições dos modos de tema
 */
export const THEME_MODE_DESCRIPTIONS: Record<
  ThemeMode,
  { label: string; description: string }
> = {
  system: {
    label: "System",
    description: "Light theme for light mode, dark theme for dark mode",
  },
  "only-light": {
    label: "Light Only",
    description: "Use light theme for both light and dark modes",
  },
  "only-dark": {
    label: "Dark Only",
    description: "Use dark theme for both light and dark modes",
  },
};
