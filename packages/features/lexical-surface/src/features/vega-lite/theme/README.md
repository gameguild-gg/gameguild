# Customização de Temas Vega-Lite

Este guia explica como personalizar manualmente as cores e estilos dos temas Vega-Lite no editor.

## Arquivo de Configuração

O arquivo `vega-theme-overrides.ts` contém todas as personalizações de temas.

## Como Funciona

1. **Temas Base**: Os temas são carregados da biblioteca `vega-themes`
2. **Overrides Manuais**: Você pode sobrescrever qualquer propriedade do tema
3. **Aplicação Automática**: Os overrides são aplicados automaticamente quando o tema é usado

## Estrutura de um Override

```typescript
"nome-do-tema": {
  background: "#0a0a0a",              // Cor de fundo
  view: {
    fill: "#0a0a0a",                  // Preenchimento da área de visualização
    stroke: "#404040"                  // Borda da visualização
  },
  axis: {
    domainColor: "#666666",            // Cor da linha do eixo
    gridColor: "#2a2a2a",              // Cor das linhas de grade
    tickColor: "#666666",              // Cor dos marcadores
    labelColor: "#d0d0d0",             // Cor dos rótulos
    titleColor: "#ffffff"              // Cor do título do eixo
  },
  legend: {
    labelColor: "#d0d0d0",             // Cor dos rótulos da legenda
    titleColor: "#ffffff"              // Cor do título da legenda
  },
  title: {
    color: "#ffffff",                  // Cor do título do gráfico
    fontSize: 16,                      // Tamanho da fonte
    fontWeight: "bold"                 // Peso da fonte
  },
  text: {
    fill: "#d0d0d0"                   // Cor do texto geral
  },
  range: {
    category: [                        // Paleta de cores categóricas
      "#5b9bd5",
      "#ed7d31",
      "#a5a5a5"
    ]
  }
}
```

## Propriedades Disponíveis

### Cores Base

- `background`: Cor de fundo do gráfico
- `view.fill`: Cor de preenchimento da área do gráfico
- `view.stroke`: Cor da borda da área do gráfico

### Eixos

- `axis.domainColor`: Linha principal do eixo
- `axis.gridColor`: Linhas de grade
- `axis.tickColor`: Marcadores no eixo
- `axis.labelColor`: Rótulos dos valores
- `axis.titleColor`: Título do eixo

### Legenda

- `legend.labelColor`: Rótulos da legenda
- `legend.titleColor`: Título da legenda

### Título

- `title.color`: Cor do título
- `title.fontSize`: Tamanho da fonte
- `title.fontWeight`: Peso da fonte ("normal", "bold", 100-900)

### Texto

- `text.fill`: Cor padrão do texto

### Paletas de Cores

- `range.category`: Array de cores para dados categóricos
- `range.diverging`: Array de cores para escalas divergentes
- `range.heatmap`: Array de cores para heatmaps
- `range.ordinal`: Array de cores para dados ordinais
- `range.ramp`: Array de cores para gradientes

## Exemplos Práticos

### Exemplo 1: Melhorar Contraste do Tema Dark

```typescript
"dark": {
  background: "#0a0a0a",      // Fundo mais escuro
  axis: {
    gridColor: "#2a2a2a",      // Grade mais sutil
    labelColor: "#d0d0d0"      // Texto mais claro
  }
}
```

### Exemplo 2: Customizar Paleta de Cores

```typescript
"excel-dark": {
  range: {
    category: [
      "#5b9bd5",  // Azul
      "#ed7d31",  // Laranja
      "#a5a5a5",  // Cinza
      "#ffc000",  // Amarelo
      "#4472c4",  // Azul escuro
      "#70ad47"   // Verde
    ]
  }
}
```

### Exemplo 3: Ajustar Tipografia

```typescript
"powerbi-dark": {
  title: {
    color: "#ffffff",
    fontSize: 18,
    fontWeight: 600
  },
  text: {
    fill: "#d0d0d0"
  }
}
```

## Temas Disponíveis

### Temas Light (já funcionam bem, raramente precisam de overrides)

- `default`
- `excel`
- `ggplot2`
- `quartz`
- `vox`
- `fivethirtyeight`
- `latimes`
- `urbaninstitute`
- `googlecharts`
- `powerbi`

### Temas Dark (recomendado adicionar overrides)

- `dark`
- `excel-dark`
- `ggplot2-dark`
- `quartz-dark`
- `vox-dark`
- `fivethirtyeight-dark`
- `latimes-dark`
- `urbaninstitute-dark`
- `googlecharts-dark`
- `powerbi-dark`

## Dicas de Personalização

### Para Melhor Legibilidade em Dark Mode:

1. **Fundo**: Use cores muito escuras (#0a0a0a - #1a1a1a)
2. **Texto**: Use cinza claro (#cccccc - #ffffff)
3. **Grade**: Use cinza muito escuro (#2a2a2a - #3a3a3a)
4. **Contraste**: Mantenha pelo menos 4.5:1 entre texto e fundo

### Para Acessibilidade:

1. Use paletas de cores que funcionem para daltônicos
2. Evite apenas vermelho/verde para diferenciar dados
3. Garanta contraste suficiente

### Testando Suas Mudanças:

1. Edite o arquivo `vega-theme-overrides.ts`
2. Salve o arquivo
3. Recarregue o editor Vega-Lite
4. Selecione o tema modificado
5. Observe as mudanças no preview

## Recursos Adicionais

- [Vega-Lite Themes Documentation](https://vega.github.io/vega-lite/docs/config.html#themes)
- [Vega Themes Gallery](https://vega.github.io/vega-themes/)
- [Color Contrast Checker](https://webaim.org/resources/contrastchecker/)
- [Colorblind Safe Palettes](https://davidmathlogic.com/colorblind/)
