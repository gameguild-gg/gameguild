/**
 * Custom Dark Themes for Mermaid
 * 
 * These themes are darker versions of the standard Mermaid themes,
 * maintaining the characteristic colors of each theme but with darker palettes
 * for better dark mode viewing experience.
 */

export interface MermaidThemeVariables {
  primaryColor?: string
  primaryTextColor?: string
  primaryBorderColor?: string
  secondaryColor?: string
  secondaryTextColor?: string
  secondaryBorderColor?: string
  tertiaryColor?: string
  tertiaryTextColor?: string
  tertiaryBorderColor?: string
  lineColor?: string
  textColor?: string
  mainBkg?: string
  secondBkg?: string
  border1?: string
  border2?: string
  note?: string
  noteBkg?: string
  noteBorder?: string
  background?: string
  labelBackground?: string
  clusterBkg?: string
  clusterBorder?: string
  defaultLinkColor?: string
  titleColor?: string
  edgeLabelBackground?: string
  nodeTextColor?: string
  actorBkg?: string
  actorBorder?: string
  actorTextColor?: string
  actorLineColor?: string
  signalColor?: string
  signalTextColor?: string
  labelBoxBkgColor?: string
  labelBoxBorderColor?: string
  labelTextColor?: string
  loopTextColor?: string
  activationBorderColor?: string
  activationBkgColor?: string
  sequenceNumberColor?: string
}

/**
 * Default Dark Theme
 * Based on the default theme but with darker, more muted colors
 */
export const defaultDarkTheme: MermaidThemeVariables = {
  primaryColor: "#1a4d6d",
  primaryTextColor: "#e0e0e0",
  primaryBorderColor: "#2d6a8f",
  secondaryColor: "#1a4d3a",
  secondaryTextColor: "#e0e0e0",
  secondaryBorderColor: "#2d7a5f",
  tertiaryColor: "#4d1a3a",
  tertiaryTextColor: "#e0e0e0",
  tertiaryBorderColor: "#7a2d5f",
  lineColor: "#666666",
  textColor: "#e0e0e0",
  mainBkg: "#1a3a4d",
  secondBkg: "#1a4d3a",
  border1: "#2d5a6f",
  border2: "#2d6f5a",
  note: "#1a1a1a",
  noteBkg: "#2d4d3a",
  noteBorder: "#3d6f5a",
  background: "#0d1117",
  labelBackground: "#1a2533",
  clusterBkg: "#1a2d3a",
  clusterBorder: "#2d4d5f",
  defaultLinkColor: "#5d8aa8",
  titleColor: "#e0e0e0",
  edgeLabelBackground: "#1a2533",
  nodeTextColor: "#e0e0e0",
  actorBkg: "#1a3a4d",
  actorBorder: "#2d5a6f",
  actorTextColor: "#e0e0e0",
  actorLineColor: "#5d8aa8",
  signalColor: "#e0e0e0",
  signalTextColor: "#e0e0e0",
  labelBoxBkgColor: "#1a2533",
  labelBoxBorderColor: "#2d4d5f",
  labelTextColor: "#e0e0e0",
  loopTextColor: "#e0e0e0",
  activationBorderColor: "#2d5a6f",
  activationBkgColor: "#1a3a4d",
  sequenceNumberColor: "#0d1117",
}

/**
 * Forest Dark Theme
 * Based on the forest theme with darker, deeper green tones
 */
export const forestDarkTheme: MermaidThemeVariables = {
  primaryColor: "#1a3d2e",
  primaryTextColor: "#d4e6d4",
  primaryBorderColor: "#2d5f4a",
  secondaryColor: "#2d4a1a",
  secondaryTextColor: "#e6e6d4",
  secondaryBorderColor: "#4a7a2d",
  tertiaryColor: "#3d2e1a",
  tertiaryTextColor: "#e6dcd4",
  tertiaryBorderColor: "#6f5a2d",
  lineColor: "#4a6a4a",
  textColor: "#d4e6d4",
  mainBkg: "#1a2e1a",
  secondBkg: "#1a3d2e",
  border1: "#2d4a2d",
  border2: "#2d5f4a",
  note: "#1a1a1a",
  noteBkg: "#2d4a2d",
  noteBorder: "#3d6a4a",
  background: "#0d1a0d",
  labelBackground: "#1a2e1a",
  clusterBkg: "#1a3d2e",
  clusterBorder: "#2d5f4a",
  defaultLinkColor: "#5a8a6a",
  titleColor: "#d4e6d4",
  edgeLabelBackground: "#1a2e1a",
  nodeTextColor: "#d4e6d4",
  actorBkg: "#1a3d2e",
  actorBorder: "#2d5f4a",
  actorTextColor: "#d4e6d4",
  actorLineColor: "#5a8a6a",
  signalColor: "#d4e6d4",
  signalTextColor: "#d4e6d4",
  labelBoxBkgColor: "#1a2e1a",
  labelBoxBorderColor: "#2d4a2d",
  labelTextColor: "#d4e6d4",
  loopTextColor: "#d4e6d4",
  activationBorderColor: "#2d5f4a",
  activationBkgColor: "#1a3d2e",
  sequenceNumberColor: "#0d1a0d",
}

/**
 * Neutral Dark Theme
 * Based on the neutral theme with darker grays and muted tones
 */
export const neutralDarkTheme: MermaidThemeVariables = {
  primaryColor: "#2a2a2a",
  primaryTextColor: "#e0e0e0",
  primaryBorderColor: "#4a4a4a",
  secondaryColor: "#1a1a1a",
  secondaryTextColor: "#d0d0d0",
  secondaryBorderColor: "#3a3a3a",
  tertiaryColor: "#3a3a3a",
  tertiaryTextColor: "#e0e0e0",
  tertiaryBorderColor: "#5a5a5a",
  lineColor: "#5a5a5a",
  textColor: "#e0e0e0",
  mainBkg: "#1a1a1a",
  secondBkg: "#2a2a2a",
  border1: "#3a3a3a",
  border2: "#4a4a4a",
  note: "#0d0d0d",
  noteBkg: "#2a2a2a",
  noteBorder: "#4a4a4a",
  background: "#0d0d0d",
  labelBackground: "#1a1a1a",
  clusterBkg: "#2a2a2a",
  clusterBorder: "#4a4a4a",
  defaultLinkColor: "#6a6a6a",
  titleColor: "#e0e0e0",
  edgeLabelBackground: "#1a1a1a",
  nodeTextColor: "#e0e0e0",
  actorBkg: "#2a2a2a",
  actorBorder: "#4a4a4a",
  actorTextColor: "#e0e0e0",
  actorLineColor: "#6a6a6a",
  signalColor: "#e0e0e0",
  signalTextColor: "#e0e0e0",
  labelBoxBkgColor: "#1a1a1a",
  labelBoxBorderColor: "#3a3a3a",
  labelTextColor: "#e0e0e0",
  loopTextColor: "#e0e0e0",
  activationBorderColor: "#4a4a4a",
  activationBkgColor: "#2a2a2a",
  sequenceNumberColor: "#0d0d0d",
}

/**
 * Base Dark Theme
 * A minimal dark theme with subtle contrasts
 */
export const baseDarkTheme: MermaidThemeVariables = {
  primaryColor: "#1a2533",
  primaryTextColor: "#d8dee9",
  primaryBorderColor: "#2e3440",
  secondaryColor: "#1a2a33",
  secondaryTextColor: "#d8dee9",
  secondaryBorderColor: "#2e3f4a",
  tertiaryColor: "#2a1a33",
  tertiaryTextColor: "#d8dee9",
  tertiaryBorderColor: "#4a2e5a",
  lineColor: "#4c566a",
  textColor: "#d8dee9",
  mainBkg: "#1a2129",
  secondBkg: "#1a2533",
  border1: "#2e3440",
  border2: "#3b4252",
  note: "#0d1117",
  noteBkg: "#1a2533",
  noteBorder: "#2e3440",
  background: "#0d1117",
  labelBackground: "#1a2129",
  clusterBkg: "#1a2533",
  clusterBorder: "#2e3440",
  defaultLinkColor: "#5e81ac",
  titleColor: "#d8dee9",
  edgeLabelBackground: "#1a2129",
  nodeTextColor: "#d8dee9",
  actorBkg: "#1a2533",
  actorBorder: "#2e3440",
  actorTextColor: "#d8dee9",
  actorLineColor: "#5e81ac",
  signalColor: "#d8dee9",
  signalTextColor: "#d8dee9",
  labelBoxBkgColor: "#1a2129",
  labelBoxBorderColor: "#2e3440",
  labelTextColor: "#d8dee9",
  loopTextColor: "#d8dee9",
  activationBorderColor: "#2e3440",
  activationBkgColor: "#1a2533",
  sequenceNumberColor: "#0d1117",
}

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
      return defaultDarkTheme
    case "forest-dark":
      return forestDarkTheme
    case "neutral-dark":
      return neutralDarkTheme
    case "base-dark":
      return baseDarkTheme
    default:
      return defaultDarkTheme
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
    securityLevel: "loose" as const,
    fontFamily: "inherit",
    flowchart: {
      useMaxWidth: true,
      htmlLabels: true,
    },
    logLevel: "error" as const,
    suppressErrorRendering: true,
  }
}
