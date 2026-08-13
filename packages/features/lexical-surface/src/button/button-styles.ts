import type { ButtonSize, ButtonVariant, IconSize, IconPosition, ColorPalette, FontFamily, FontSize } from "./button-node"

/**
 * Retorna as classes de tamanho do botão baseado no tamanho e se o ícone está em posição vertical
 */
export function getSizeStyles(size: ButtonSize, isVerticalIcon: boolean): string {
  const sizeMap = {
    sm: isVerticalIcon ? "min-h-12 py-2 px-4" : "h-9 px-4",
    md: isVerticalIcon ? "min-h-16 py-3 px-6" : "h-12 px-6",
    lg: isVerticalIcon ? "min-h-20 py-4 px-8" : "h-16 px-8",
    xl: isVerticalIcon ? "min-h-28 py-6 px-12" : "h-24 px-12",
    xxl: isVerticalIcon ? "min-h-36 py-8 px-16" : "h-32 px-16",
  }
  return sizeMap[size]
}

/**
 * Retorna a largura da borda para o estilo outline baseado no tamanho do botão
 */
export function getOutlineBorderWidth(size: ButtonSize): string {
  const borderMap = {
    sm: "border-2",
    md: "border-2",
    lg: "border-[3px]",
    xl: "border-4",
    xxl: "border-[5px]",
  }
  return borderMap[size]
}

/**
 * Retorna a largura da borda inferior para o estilo minimal baseado no tamanho do botão
 */
export function getMinimalBorderWidth(size: ButtonSize): string {
  const borderMap = {
    sm: "border-b-2",
    md: "border-b-2",
    lg: "border-b-[3px]",
    xl: "border-b-4",
    xxl: "border-b-[5px]",
  }
  return borderMap[size]
}

/**
 * Retorna as classes base de estilo para cada variante de botão
 */
export function getVariantBaseStyles(variant: ButtonVariant, size: ButtonSize): string {
  const outlineBorderWidth = getOutlineBorderWidth(size)
  const minimalBorderWidth = getMinimalBorderWidth(size)

  const variantMap = {
    solid: "bg-gradient-to-r text-white shadow-lg hover:shadow-2xl hover:scale-105 active:scale-95 transition-all duration-200",
    outline: `${outlineBorderWidth} bg-transparent hover:shadow-md transition-all duration-200`,
    soft: "hover:shadow-sm transition-all duration-200",
    minimal: `bg-transparent ${minimalBorderWidth} border-transparent rounded-none px-2 transition-all duration-200`,
  }
  return variantMap[variant]
}

/**
 * Retorna as classes de layout do flexbox baseado na posição do ícone
 */
export function getLayoutStyles(iconPosition: IconPosition): string {
  const layoutMap = {
    top: "flex-col-reverse",
    bottom: "flex-col",
    left: "flex-row-reverse",
    right: "flex-row",
  }
  return layoutMap[iconPosition]
}

/**
 * Retorna as classes de espaçamento do ícone baseado na posição
 */
export function getIconSpacingClass(iconPosition: IconPosition): string {
  const spacingMap = {
    top: "mb-2",
    bottom: "mt-2",
    left: "mr-2",
    right: "ml-2",
  }
  return spacingMap[iconPosition]
}

/**
 * Mapa de tamanhos de ícone baseado no tamanho do botão e no tamanho relativo do ícone
 */
export function getIconSizeClass(buttonSize: ButtonSize, iconSize: IconSize): string {
  const iconSizeMap: Record<ButtonSize, Record<IconSize, string>> = {
    sm: { sm: "h-3 w-3", md: "h-3.5 w-3.5", lg: "h-4 w-4" },
    md: { sm: "h-4 w-4", md: "h-5 w-5", lg: "h-6 w-6" },
    lg: { sm: "h-5 w-5", md: "h-6 w-6", lg: "h-7 w-7" },
    xl: { sm: "h-6 w-6", md: "h-8 w-8", lg: "h-10 w-10" },
    xxl: { sm: "h-8 w-8", md: "h-10 w-10", lg: "h-12 w-12" },
  }
  return iconSizeMap[buttonSize][iconSize]
}

/**
 * Retorna as classes de cor baseado na paleta de cores e variante
 */
export function getColorStyles(colorPalette: ColorPalette, variant: ButtonVariant): string {
  const palettes = {
    blue: {
      solid: "from-blue-600 to-indigo-600 shadow-blue-500/30 hover:shadow-blue-500/40 hover:from-blue-700 hover:to-indigo-700",
      outline: "border-blue-600 text-blue-600 dark:text-blue-400 dark:border-blue-400 hover:bg-blue-600 hover:text-white dark:hover:bg-blue-500 dark:hover:text-white",
      soft: "bg-blue-100 text-blue-900 dark:bg-blue-900/30 dark:text-blue-100 hover:bg-blue-200 dark:hover:bg-blue-800/40",
      minimal: "text-blue-600 dark:text-blue-400 hover:border-blue-600 dark:hover:border-blue-400",
    },
    green: {
      solid: "from-green-600 to-emerald-600 shadow-green-500/30 hover:shadow-green-500/40 hover:from-green-700 hover:to-emerald-700",
      outline: "border-green-600 text-green-600 dark:text-green-400 dark:border-green-400 hover:bg-green-600 hover:text-white dark:hover:bg-green-500 dark:hover:text-white",
      soft: "bg-green-100 text-green-900 dark:bg-green-900/30 dark:text-green-100 hover:bg-green-200 dark:hover:bg-green-800/40",
      minimal: "text-green-600 dark:text-green-400 hover:border-green-600 dark:hover:border-green-400",
    },
    orange: {
      solid: "from-orange-600 to-amber-600 shadow-orange-500/30 hover:shadow-orange-500/40 hover:from-orange-700 hover:to-amber-700",
      outline: "border-orange-600 text-orange-600 dark:text-orange-400 dark:border-orange-400 hover:bg-orange-600 hover:text-white dark:hover:bg-orange-500 dark:hover:text-white",
      soft: "bg-orange-100 text-orange-900 dark:bg-orange-900/30 dark:text-orange-100 hover:bg-orange-200 dark:hover:bg-orange-800/40",
      minimal: "text-orange-600 dark:text-orange-400 hover:border-orange-600 dark:hover:border-orange-400",
    },
    red: {
      solid: "from-red-600 to-rose-600 shadow-red-500/30 hover:shadow-red-500/40 hover:from-red-700 hover:to-rose-700",
      outline: "border-red-600 text-red-600 dark:text-red-400 dark:border-red-400 hover:bg-red-600 hover:text-white dark:hover:bg-red-500 dark:hover:text-white",
      soft: "bg-red-100 text-red-900 dark:bg-red-900/30 dark:text-red-100 hover:bg-red-200 dark:hover:bg-red-800/40",
      minimal: "text-red-600 dark:text-red-400 hover:border-red-600 dark:hover:border-red-400",
    },
    custom: {
      solid: "from-blue-600 to-indigo-600 shadow-blue-500/30 hover:shadow-blue-500/40 hover:from-blue-700 hover:to-indigo-700",
      outline: "border-blue-600 text-blue-600 dark:text-blue-400 dark:border-blue-400 hover:bg-blue-600 hover:text-white dark:hover:bg-blue-500 dark:hover:text-white",
      soft: "bg-blue-100 text-blue-900 dark:bg-blue-900/30 dark:text-blue-100 hover:bg-blue-200 dark:hover:bg-blue-800/40",
      minimal: "text-blue-600 dark:text-blue-400 hover:border-blue-600 dark:hover:border-blue-400",
    },
  }
  
  return palettes[colorPalette][variant]
}

/**
 * Retorna a classe de família de fonte
 */
export function getFontFamilyClass(fontFamily: FontFamily): string {
  const fontMap = {
    sans: "font-sans",
    display: "font-bold tracking-tight",
    roboto: "font-roboto",
  }
  return fontMap[fontFamily]
}

/**
 * Retorna a classe de tamanho de fonte baseado no tamanho do botão e tamanho relativo da fonte
 */
export function getFontSizeClass(buttonSize: ButtonSize, fontSize: FontSize): string {
  const fontSizeMap: Record<ButtonSize, Record<FontSize, string>> = {
    sm: { sm: "text-xs", md: "text-sm", lg: "text-base" },
    md: { sm: "text-sm", md: "text-base", lg: "text-lg" },
    lg: { sm: "text-base", md: "text-lg", lg: "text-xl" },
    xl: { sm: "text-lg", md: "text-2xl", lg: "text-3xl" },
    xxl: { sm: "text-2xl", md: "text-3xl", lg: "text-4xl" },
  }
  return fontSizeMap[buttonSize][fontSize]
}

/**
 * Classes base compartilhadas para todos os botões
 */
export const BASE_BUTTON_STYLES = "inline-flex items-center justify-center rounded-md font-medium transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 cursor-pointer"
