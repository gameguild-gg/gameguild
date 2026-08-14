/**
 * Custom Dark Themes for Mermaid
 *
 * These themes are darker versions of the standard Mermaid themes,
 * maintaining the characteristic colors of each theme but with darker palettes
 * for better dark mode viewing experience.
 */

export interface MermaidThemeVariables {
  primaryColor?: string;
  primaryTextColor?: string;
  primaryBorderColor?: string;
  secondaryColor?: string;
  secondaryTextColor?: string;
  secondaryBorderColor?: string;
  tertiaryColor?: string;
  tertiaryTextColor?: string;
  tertiaryBorderColor?: string;
  lineColor?: string;
  textColor?: string;
  mainBkg?: string;
  secondBkg?: string;
  border1?: string;
  border2?: string;
  note?: string;
  noteBkg?: string;
  noteBorder?: string;
  background?: string;
  labelBackground?: string;
  clusterBkg?: string;
  clusterBorder?: string;
  defaultLinkColor?: string;
  titleColor?: string;
  edgeLabelBackground?: string;
  nodeTextColor?: string;
  actorBkg?: string;
  actorBorder?: string;
  actorTextColor?: string;
  actorLineColor?: string;
  signalColor?: string;
  signalTextColor?: string;
  labelBoxBkgColor?: string;
  labelBoxBorderColor?: string;
  labelTextColor?: string;
  loopTextColor?: string;
  activationBorderColor?: string;
  activationBkgColor?: string;
  sequenceNumberColor?: string;
}

/**
 * Default Dark Theme
 * Based on the default theme but with darker, more muted colors
 */
export const defaultDarkTheme: MermaidThemeVariables = {
  primaryColor: "#2a5d7d",
  primaryTextColor: "#e8e8e8",
  primaryBorderColor: "#3d7a9f",
  secondaryColor: "#2a5d4a",
  secondaryTextColor: "#e8e8e8",
  secondaryBorderColor: "#3d8a6f",
  tertiaryColor: "#5d2a4a",
  tertiaryTextColor: "#e8e8e8",
  tertiaryBorderColor: "#8a3d6f",
  lineColor: "#7a7a7a",
  textColor: "#e8e8e8",
  mainBkg: "#2a4a5d",
  secondBkg: "#2a5d4a",
  border1: "#3d6a7f",
  border2: "#3d7f6a",
  note: "#2a2a2a",
  noteBkg: "#3d5d4a",
  noteBorder: "#4d7f6a",
  background: "#1a2530",
  labelBackground: "#2a3543",
  clusterBkg: "#2a3d4a",
  clusterBorder: "#3d5d6f",
  defaultLinkColor: "#6d9ab8",
  titleColor: "#e8e8e8",
  edgeLabelBackground: "#2a3543",
  nodeTextColor: "#e8e8e8",
  actorBkg: "#2a4a5d",
  actorBorder: "#3d6a7f",
  actorTextColor: "#e8e8e8",
  actorLineColor: "#6d9ab8",
  signalColor: "#e8e8e8",
  signalTextColor: "#e8e8e8",
  labelBoxBkgColor: "#2a3543",
  labelBoxBorderColor: "#3d5d6f",
  labelTextColor: "#e8e8e8",
  loopTextColor: "#e8e8e8",
  activationBorderColor: "#3d6a7f",
  activationBkgColor: "#2a4a5d",
  sequenceNumberColor: "#1a2530",
};

/**
 * Forest Dark Theme
 * Based on the forest theme with darker, deeper green tones
 */
export const forestDarkTheme: MermaidThemeVariables = {
  primaryColor: "#2a4d3e",
  primaryTextColor: "#dceadc",
  primaryBorderColor: "#3d6f5a",
  secondaryColor: "#3d5a2a",
  secondaryTextColor: "#eaeadc",
  secondaryBorderColor: "#5a8a3d",
  tertiaryColor: "#4d3e2a",
  tertiaryTextColor: "#eadcdc",
  tertiaryBorderColor: "#7f6a3d",
  lineColor: "#5a7a5a",
  textColor: "#dceadc",
  mainBkg: "#2a3e2a",
  secondBkg: "#2a4d3e",
  border1: "#3d5a3d",
  border2: "#3d6f5a",
  note: "#2a2a2a",
  noteBkg: "#3d5a3d",
  noteBorder: "#4d7a5a",
  background: "#1a2a1a",
  labelBackground: "#2a3e2a",
  clusterBkg: "#2a4d3e",
  clusterBorder: "#3d6f5a",
  defaultLinkColor: "#6a9a7a",
  titleColor: "#dceadc",
  edgeLabelBackground: "#2a3e2a",
  nodeTextColor: "#dceadc",
  actorBkg: "#2a4d3e",
  actorBorder: "#3d6f5a",
  actorTextColor: "#dceadc",
  actorLineColor: "#6a9a7a",
  signalColor: "#dceadc",
  signalTextColor: "#dceadc",
  labelBoxBkgColor: "#2a3e2a",
  labelBoxBorderColor: "#3d5a3d",
  labelTextColor: "#dceadc",
  loopTextColor: "#dceadc",
  activationBorderColor: "#3d6f5a",
  activationBkgColor: "#2a4d3e",
  sequenceNumberColor: "#1a2a1a",
};

/**
 * Neutral Dark Theme
 * Based on the neutral theme with darker grays and muted tones
 */
export const neutralDarkTheme: MermaidThemeVariables = {
  primaryColor: "#3a3a3a",
  primaryTextColor: "#e8e8e8",
  primaryBorderColor: "#5a5a5a",
  secondaryColor: "#2a2a2a",
  secondaryTextColor: "#d8d8d8",
  secondaryBorderColor: "#4a4a4a",
  tertiaryColor: "#4a4a4a",
  tertiaryTextColor: "#e8e8e8",
  tertiaryBorderColor: "#6a6a6a",
  lineColor: "#6a6a6a",
  textColor: "#e8e8e8",
  mainBkg: "#2a2a2a",
  secondBkg: "#3a3a3a",
  border1: "#4a4a4a",
  border2: "#5a5a5a",
  note: "#1a1a1a",
  noteBkg: "#3a3a3a",
  noteBorder: "#5a5a5a",
  background: "#1a1a1a",
  labelBackground: "#2a2a2a",
  clusterBkg: "#3a3a3a",
  clusterBorder: "#5a5a5a",
  defaultLinkColor: "#7a7a7a",
  titleColor: "#e8e8e8",
  edgeLabelBackground: "#2a2a2a",
  nodeTextColor: "#e8e8e8",
  actorBkg: "#3a3a3a",
  actorBorder: "#5a5a5a",
  actorTextColor: "#e8e8e8",
  actorLineColor: "#7a7a7a",
  signalColor: "#e8e8e8",
  signalTextColor: "#e8e8e8",
  labelBoxBkgColor: "#2a2a2a",
  labelBoxBorderColor: "#4a4a4a",
  labelTextColor: "#e8e8e8",
  loopTextColor: "#e8e8e8",
  activationBorderColor: "#5a5a5a",
  activationBkgColor: "#3a3a3a",
  sequenceNumberColor: "#1a1a1a",
};

/**
 * Base Dark Theme
 * A minimal dark theme with subtle contrasts
 */
export const baseDarkTheme: MermaidThemeVariables = {
  primaryColor: "#2a3543",
  primaryTextColor: "#e0e6ed",
  primaryBorderColor: "#3e4450",
  secondaryColor: "#2a3a43",
  secondaryTextColor: "#e0e6ed",
  secondaryBorderColor: "#3e4f5a",
  tertiaryColor: "#3a2a43",
  tertiaryTextColor: "#e0e6ed",
  tertiaryBorderColor: "#5a3e6a",
  lineColor: "#5c6678",
  textColor: "#e0e6ed",
  mainBkg: "#2a3139",
  secondBkg: "#2a3543",
  border1: "#3e4450",
  border2: "#4b5262",
  note: "#1a2530",
  noteBkg: "#2a3543",
  noteBorder: "#3e4450",
  background: "#1a2530",
  labelBackground: "#2a3139",
  clusterBkg: "#2a3543",
  clusterBorder: "#3e4450",
  defaultLinkColor: "#6e91bc",
  titleColor: "#e0e6ed",
  edgeLabelBackground: "#2a3139",
  nodeTextColor: "#e0e6ed",
  actorBkg: "#2a3543",
  actorBorder: "#3e4450",
  actorTextColor: "#e0e6ed",
  actorLineColor: "#6e91bc",
  signalColor: "#e0e6ed",
  signalTextColor: "#e0e6ed",
  labelBoxBkgColor: "#2a3139",
  labelBoxBorderColor: "#3e4450",
  labelTextColor: "#e0e6ed",
  loopTextColor: "#e0e6ed",
  activationBorderColor: "#3e4450",
  activationBkgColor: "#2a3543",
  sequenceNumberColor: "#1a2530",
};

/**
 * Apply a custom dark theme to Mermaid
 * @param themeName - Name of the theme (default-dark, forest-dark, neutral-dark, base-dark)
 * @returns Theme variables object
 */
export function getMermaidDarkTheme(
  themeName: "default-dark" | "forest-dark" | "neutral-dark" | "base-dark",
): MermaidThemeVariables {
  switch (themeName) {
    case "default-dark":
      return defaultDarkTheme;
    case "forest-dark":
      return forestDarkTheme;
    case "neutral-dark":
      return neutralDarkTheme;
    case "base-dark":
      return baseDarkTheme;
    default:
      return defaultDarkTheme;
  }
}

/**
 * Get Mermaid initialization config with custom dark theme
 */
export function getMermaidConfigWithDarkTheme(
  themeName: "default-dark" | "forest-dark" | "neutral-dark" | "base-dark",
) {
  return {
    theme: "base" as const,
    themeVariables: getMermaidDarkTheme(themeName),
    startOnLoad: false,
    securityLevel: "strict" as const,
    fontFamily: "inherit",
    flowchart: {
      useMaxWidth: true,
      htmlLabels: false,
    },
    logLevel: "error" as const,
    suppressErrorRendering: true,
  };
}
