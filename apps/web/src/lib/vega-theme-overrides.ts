/**
 * Overrides manuais para temas Vega-Lite
 * 
 * Este arquivo permite ajustar manualmente as cores e estilos dos temas,
 * especialmente as variantes dark que podem não ter cores adequadas.
 * 
 * Para cada tema, você pode definir overrides para:
 * - Versão light (opcional)
 * - Versão dark (recomendado para melhorar contraste)
 */

export interface ThemeOverride {
  background?: string
  view?: {
    fill?: string
    stroke?: string
  }
  axis?: {
    domainColor?: string
    gridColor?: string
    tickColor?: string
    labelColor?: string
    titleColor?: string
  }
  legend?: {
    labelColor?: string
    titleColor?: string
  }
  title?: {
    color?: string
    fontSize?: number
    fontWeight?: string | number
  }
  text?: {
    fill?: string
  }
  mark?: {
    color?: string
    strokeWidth?: number
    opacity?: number
    fill?: string
  }
  line?: {
    strokeWidth?: number
    stroke?: string
  }
  bar?: {
    cornerRadiusTopLeft?: number
    cornerRadiusTopRight?: number
    [key: string]: any
  }
  point?: {
    filled?: boolean
    size?: number
    [key: string]: any
  }
  range?: {
    category?: string[]
    diverging?: string[]
    heatmap?: string[]
    ordinal?: string[]
    ramp?: string[]
  }
  style?: {
    [key: string]: any
  }
}

/**
 * Overrides para as versões light dos temas
 * Geralmente não é necessário, mas pode ser ajustado se precisar
 */
export const LIGHT_THEME_OVERRIDES: Record<string, ThemeOverride> = {
  // Exemplo: ajustar o tema default light
  // "default": {
  //   background: "#ffffff",
  //   title: {
  //     color: "#000000",
  //     fontSize: 16
  //   }
  // },
  
  "ggplot2": {
  range: {
    category: [
      "#4cc9f0", // azul-claro
      "#f72585", // magenta
      "#b5179e", // violeta
      "#7209b7", // roxo
      "#560bad", // roxo escuro
      "#480ca8", // azul arroxeado
      "#3a0ca3", // azul royal
      "#4361ee", // azul médio
      "#4895ef", // azul claro
      "#4cc9f0"  // ciano
    ],
    heatmap: ["#0d0887", "#6a00a8", "#b12a90", "#e16462", "#fca636", "#f0f921"]
  },
  axis: {
    gridColor: "#f3f4f6",
  },
  line: {
    strokeWidth: 2
  },

  },
}

/**
 * Overrides para as versões dark dos temas
 * Aqui você pode ajustar cores para melhor contraste e legibilidade
 */
export const DARK_THEME_OVERRIDES: Record<string, ThemeOverride> = {
  // Tema dark padrão - melhorado para melhor contraste
  "dark": {
    background: "#0a0a0a",
    view: {
      fill: "#0a0a0a",
      stroke: "#404040"
    },
    axis: {
      domainColor: "#222222",
      gridColor: "#222222",
      tickColor: "#222222",
      labelColor: "#d0d0d0",
      titleColor: "#222222"
    },
    legend: {
      labelColor: "#d0d0d0",
      titleColor: "#ffffff"
    },
    title: {
      color: "#ffffff",
      fontSize: 16
    },
    text: {
      fill: "#d0d0d0"
    }
  },

  // Excel dark - melhorar cores para melhor legibilidade
  "excel-dark": {
    background: "#1a1a1a",
    view: {
      fill: "#1a1a1a",
      stroke: "#3a3a3a"
    },
    axis: {
      domainColor: "#5a5a5a",
      gridColor: "#2a2a2a",
      tickColor: "#5a5a5a",
      labelColor: "#cccccc",
      titleColor: "#e0e0e0"
    },
    legend: {
      labelColor: "#cccccc",
      titleColor: "#e0e0e0"
    },
    title: {
      color: "#f0f0f0"
    },
    text: {
      fill: "#cccccc"
    },
    range: {
      category: ["#5b9bd5", "#ed7d31", "#a5a5a5", "#ffc000", "#4472c4", "#70ad47"]
    }
  },

  // ggplot2 dark - ajustar para fundo escuro
  "ggplot2-dark": {
  range: {
    category: [
      "#4cc9f0", // azul-claro
      "#f72585", // magenta
      "#b5179e", // violeta
      "#7209b7", // roxo
      "#560bad", // roxo escuro
      "#480ca8", // azul arroxeado
      "#3a0ca3", // azul royal
      "#4361ee", // azul médio
      "#4895ef", // azul claro
      "#4cc9f0"  // ciano
    ],
    heatmap: ["#0d0887", "#6a00a8", "#b12a90", "#e16462", "#fca636", "#f0f921"]
  },
  line: {
    stroke: "#ffffff",
    strokeWidth: 2
  },
  mark: {
    fill: "#4cc9f0",
  }

  },

  // Quartz dark - ajustar cores
  "quartz-dark": {
    background: "#0f0f0f",
    view: {
      fill: "#0f0f0f",
      stroke: "#404040"
    },
    axis: {
      domainColor: "#606060",
      gridColor: "#252525",
      tickColor: "#606060",
      labelColor: "#cccccc",
      titleColor: "#e0e0e0"
    },
    legend: {
      labelColor: "#cccccc",
      titleColor: "#e0e0e0"
    },
    title: {
      color: "#f0f0f0"
    },
    text: {
      fill: "#cccccc"
    }
  },

  // Vox dark - melhorar contraste
  "vox-dark": {
    background: "#141414",
    view: {
      fill: "#141414",
      stroke: "#3a3a3a"
    },
    axis: {
      domainColor: "#555555",
      gridColor: "#2a2a2a",
      tickColor: "#555555",
      labelColor: "#d0d0d0",
      titleColor: "#ffffff"
    },
    legend: {
      labelColor: "#d0d0d0",
      titleColor: "#ffffff"
    },
    title: {
      color: "#ffffff"
    },
    text: {
      fill: "#d0d0d0"
    }
  },

  // FiveThirtyEight dark - ajustar para fundo escuro
  "fivethirtyeight-dark": {
    background: "#1a1a1a",
    view: {
      fill: "#1a1a1a",
      stroke: "#3a3a3a"
    },
    axis: {
      domainColor: "#555555",
      gridColor: "#2a2a2a",
      tickColor: "#555555",
      labelColor: "#cccccc",
      titleColor: "#e0e0e0"
    },
    legend: {
      labelColor: "#cccccc",
      titleColor: "#e0e0e0"
    },
    title: {
      color: "#f0f0f0",
      fontSize: 18,
      fontWeight: "bold"
    },
    text: {
      fill: "#cccccc"
    }
  },

  // LA Times dark - melhorar legibilidade
  "latimes-dark": {
    background: "#0f0f0f",
    view: {
      fill: "#0f0f0f",
      stroke: "#404040"
    },
    axis: {
      domainColor: "#606060",
      gridColor: "#252525",
      tickColor: "#606060",
      labelColor: "#d0d0d0",
      titleColor: "#ffffff"
    },
    legend: {
      labelColor: "#d0d0d0",
      titleColor: "#ffffff"
    },
    title: {
      color: "#ffffff",
      fontSize: 20
    },
    text: {
      fill: "#d0d0d0"
    }
  },

  // Urban Institute dark - ajustar cores
  "urbaninstitute-dark": {
    background: "#121212",
    view: {
      fill: "#1a1a1a",
      stroke: "#3a3a3a"
    },
    axis: {
      domainColor: "#555555",
      gridColor: "#2a2a2a",
      tickColor: "#555555",
      labelColor: "#cccccc",
      titleColor: "#e0e0e0"
    },
    legend: {
      labelColor: "#cccccc",
      titleColor: "#e0e0e0"
    },
    title: {
      color: "#f0f0f0"
    },
    text: {
      fill: "#cccccc"
    },
    range: {
      category: ["#1696d2", "#fdbf11", "#d2d2d2", "#ec008b", "#55b748", "#5c5859"]
    }
  },

  // Google Charts dark - melhorar para fundo escuro
  "googlecharts-dark": {
    background: "#1a1a1a",
    view: {
      fill: "#1a1a1a",
      stroke: "#3a3a3a"
    },
    axis: {
      domainColor: "#555555",
      gridColor: "#2a2a2a",
      tickColor: "#555555",
      labelColor: "#cccccc",
      titleColor: "#e0e0e0"
    },
    legend: {
      labelColor: "#cccccc",
      titleColor: "#e0e0e0"
    },
    title: {
      color: "#f0f0f0"
    },
    text: {
      fill: "#cccccc"
    }
  },

  // Power BI dark - ajustar para melhor contraste
  "powerbi-dark": {
    background: "#0f0f0f",
    view: {
      fill: "#0f0f0f",
      stroke: "#404040"
    },
    axis: {
      domainColor: "#606060",
      gridColor: "#252525",
      tickColor: "#606060",
      labelColor: "#d0d0d0",
      titleColor: "#ffffff"
    },
    legend: {
      labelColor: "#d0d0d0",
      titleColor: "#ffffff"
    },
    title: {
      color: "#ffffff"
    },
    text: {
      fill: "#d0d0d0"
    },
    range: {
      category: ["#118dff", "#12239e", "#e66c37", "#6b007b", "#e044a7", "#744ec2"]
    }
  }
}

/**
 * Aplica overrides a um tema base
 * Faz deep merge das propriedades
 */
export function applyThemeOverrides(baseTheme: any, overrides?: ThemeOverride): any {
  if (!overrides) return baseTheme

  return {
    ...baseTheme,
    ...(overrides.background !== undefined && { background: overrides.background }),
    view: {
      ...baseTheme.view,
      ...overrides.view
    },
    axis: {
      ...baseTheme.axis,
      ...overrides.axis
    },
    legend: {
      ...baseTheme.legend,
      ...overrides.legend
    },
    title: {
      ...baseTheme.title,
      ...overrides.title
    },
    text: {
      ...baseTheme.text,
      ...overrides.text
    },
    ...(overrides.mark && { mark: { ...baseTheme.mark, ...overrides.mark } }),
    ...(overrides.line && { line: { ...baseTheme.line, ...overrides.line } }),
    ...(overrides.range && { range: { ...baseTheme.range, ...overrides.range } })
  }
}
