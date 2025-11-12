import type { ButtonSize, ButtonVariant, IconSize, IconPosition } from "@/components/editor/nodes/button-node"

/**
 * Retorna as classes de tamanho do botão baseado no tamanho e se o ícone está em posição vertical
 */
export function getSizeStyles(size: ButtonSize, isVerticalIcon: boolean): string {
  const sizeMap = {
    sm: isVerticalIcon ? "min-h-12 py-2 px-4 text-sm" : "h-9 px-4 text-sm",
    md: isVerticalIcon ? "min-h-16 py-3 px-6 text-base" : "h-12 px-6 text-base",
    lg: isVerticalIcon ? "min-h-20 py-4 px-8 text-lg" : "h-16 px-8 text-lg",
    xl: isVerticalIcon ? "min-h-28 py-6 px-12 text-2xl" : "h-24 px-12 text-2xl",
    xxl: isVerticalIcon ? "min-h-36 py-8 px-16 text-3xl" : "h-32 px-16 text-3xl",
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
 * Classes base compartilhadas para todos os botões
 */
export const BASE_BUTTON_STYLES = "inline-flex items-center justify-center rounded-md font-medium transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 cursor-pointer"
