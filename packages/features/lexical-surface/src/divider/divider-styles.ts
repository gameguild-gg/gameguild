import type {
  DividerColorPalette,
  DividerSpacing,
  DividerStyle,
  DividerThickness,
} from "./divider-node"

/**
 * Retorna as classes de espessura do divider
 */
export function getThicknessStyles(thickness: DividerThickness, style: DividerStyle): string {
  // Para gradient e custom styles, usar altura ao invés de border
  if (style === "gradient") {
    const heightMap = {
      thin: "h-px",
      medium: "h-0.5",
      thick: "h-1",
    }
    return heightMap[thickness]
  }
  
  // Para estilos de borda tradicionais
  const borderMap = {
    thin: "border-t",
    medium: "border-t-2",
    thick: "border-t-4",
  }
  return borderMap[thickness]
}

/**
 * Retorna as classes de espaçamento vertical
 */
export function getSpacingStyles(spacing: DividerSpacing): string {
  const spacingMap = {
    xs: "my-4",
    sm: "my-8",
    md: "my-12",
    lg: "my-16",
    xl: "my-24",
  }
  return spacingMap[spacing]
}

/**
 * Retorna as classes de cor baseado na paleta
 */
export function getColorStyles(colorPalette: DividerColorPalette, style: DividerStyle): string {
  // Para gradient, retornar classes especiais
  if (style === "gradient") {
    const gradientMap = {
      blue: "bg-gradient-to-r from-transparent via-blue-500 to-transparent",
      green: "bg-gradient-to-r from-transparent via-green-500 to-transparent",
      orange: "bg-gradient-to-r from-transparent via-orange-500 to-transparent",
      red: "bg-gradient-to-r from-transparent via-red-500 to-transparent",
      purple: "bg-gradient-to-r from-transparent via-purple-500 to-transparent",
      custom: "bg-gradient-to-r from-transparent via-gray-500 to-transparent", // Will be overridden by inline styles
    }
    return gradientMap[colorPalette]
  }
  
  // Para estilos normais de borda
  const colorMap = {
    blue: "border-blue-500 dark:border-blue-400",
    green: "border-green-500 dark:border-green-400",
    orange: "border-orange-500 dark:border-orange-400",
    red: "border-red-500 dark:border-red-400",
    purple: "border-purple-500 dark:border-purple-400",
    custom: "border-gray-500", // Will be overridden by inline styles
  }
  return colorMap[colorPalette]
}

/**
 * Retorna as classes de estilo do divider
 */
export function getStyleClasses(style: DividerStyle): string {
  const styleMap = {
    simple: "",
    double: "border-double",
    dashed: "border-dashed",
    dotted: "border-dotted",
    gradient: "",
  }
  return styleMap[style]
}

/**
 * Gera o SVG para divider wavy
 */
export function getWavySVG(color: string): string {
  return `data:image/svg+xml,%3Csvg width='100%25' height='100%25' xmlns='http://www.w3.org/2000/svg'%3E%3Cpath d='M0 5 Q 10 0, 20 5 T 40 5 T 60 5 T 80 5 T 100 5' stroke='${encodeURIComponent(color)}' fill='none' stroke-width='2' vector-effect='non-scaling-stroke'/%3E%3C/svg%3E`
}

/**
 * Gera o padrão para divider zigzag
 */
export function getZigzagPattern(color: string): string {
  return `repeating-linear-gradient(45deg, ${color} 0, ${color} 2px, transparent 2px, transparent 8px, ${color} 8px, ${color} 10px, transparent 10px, transparent 16px)`
}

/**
 * Retorna a cor hexadecimal baseada na paleta de cores
 */
export function getPaletteColor(colorPalette: DividerColorPalette, customColor?: string): string {
  if (colorPalette === "custom" && customColor) {
    return customColor
  }
  
  const colorMap = {
    blue: "#3b82f6",
    green: "#10b981",
    orange: "#f97316",
    red: "#ef4444",
    purple: "#a855f7",
    custom: "#3b82f6", // fallback
  }
  
  return colorMap[colorPalette]
}
