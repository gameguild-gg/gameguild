# Importar Dados CSV e JSON no Vega-Lite Editor

## Como Usar

### 1. Abrir o Gerenciador de Dados

No editor Vega-Lite, clique no botão **"Dados"** na barra de ferramentas (ao lado de "Change Template").

### 2. Enviar Arquivos

O gerenciador possui duas abas: **CSV** e **JSON**.

#### Enviar CSV:
1. Vá para a aba **CSV**
2. Clique em **"Selecionar"** ou no campo de input
3. Escolha um ou mais arquivos `.csv` do seu computador
4. Os arquivos serão carregados e salvos junto com a especificação

#### Enviar JSON:
1. Vá para a aba **JSON**
2. Clique em **"Selecionar"** ou no campo de input
3. Escolha um ou mais arquivos `.json` do seu computador
4. Os arquivos devem conter JSON válido (será validado automaticamente)

### 3. Usar no Spec

Após enviar arquivos, use a URL especial no seu spec:

**Para CSV:**
```json
{
  "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
  "data": {
    "url": "data:vendas.csv"
  },
  "mark": "bar",
  "encoding": {
    "x": {"field": "mes", "type": "ordinal"},
    "y": {"field": "valor", "type": "quantitative"}
  }
}
```

**Para JSON:**
```json
{
  "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
  "data": {
    "url": "data:dados.json"
  },
  "mark": "point",
  "encoding": {
    "x": {"field": "x", "type": "quantitative"},
    "y": {"field": "y", "type": "quantitative"}
  }
}
```

### Formato da URL

A URL deve seguir o padrão: `data:nome-do-arquivo.extensão`

- **Prefixo obrigatório**: `data:`
- **Nome do arquivo**: Exatamente como foi enviado (case-sensitive)
- **Extensão**: `.csv` ou `.json`

### Exemplo Completo CSV

**Arquivo CSV** (`clima.csv`):
```csv
cidade,temperatura,precipitacao
São Paulo,22,120
Rio de Janeiro,25,80
Brasília,24,150
```

**Spec Vega-Lite**:
```json
{
  "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
  "title": "Clima das Cidades",
  "data": {
    "url": "data:clima.csv"
  },
  "mark": "point",
  "encoding": {
    "x": {"field": "temperatura", "type": "quantitative"},
    "y": {"field": "precipitacao", "type": "quantitative"},
    "color": {"field": "cidade", "type": "nominal"}
  }
}
```

### Exemplo Completo JSON

**Arquivo JSON** (`carros.json`):
```json
[
  {"marca": "Toyota", "vendas": 150, "ano": 2024},
  {"marca": "Ford", "vendas": 120, "ano": 2024},
  {"marca": "Honda", "vendas": 135, "ano": 2024}
]
```

**Spec Vega-Lite**:
```json
{
  "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
  "title": "Vendas por Marca",
  "data": {
    "url": "data:carros.json"
  },
  "mark": "bar",
  "encoding": {
    "x": {"field": "marca", "type": "nominal"},
    "y": {"field": "vendas", "type": "quantitative"}
  }
}
```

## Funcionalidades

### Gerenciador de Arquivos

- **Duas abas**: CSV e JSON separados para organização
- **Visualizar arquivos**: Lista todos os arquivos enviados com informações (linhas, tamanho)
- **Copiar URL**: Botão para copiar rapidamente a URL `data:arquivo.csv` ou `data:arquivo.json`
- **Remover arquivo**: Excluir arquivos que não são mais necessários
- **Múltiplos arquivos**: Envie vários arquivos para usar em diferentes specs

### Como Funciona

1. Quando você envia um CSV ou JSON, o conteúdo é armazenado internamente
2. Ao renderizar o gráfico, o sistema:
   - Detecta URLs no formato `data:arquivo.csv` ou `data:arquivo.json`
   - **CSV**: Faz parse usando d3-dsv e converte para array de objetos JSON
   - **JSON**: Valida e carrega diretamente
   - Substitui automaticamente por `values: [...]` inline
3. Nos exports (SVG/PNG), os dados são incluídos no arquivo final

### Benefícios

✅ **Portabilidade**: Tudo é salvo junto (spec + dados)  
✅ **Sem URLs externas**: Não depende de servidores externos  
✅ **Preview instantâneo**: Dados carregados localmente  
✅ **Export completo**: SVG e PNG incluem os dados  
✅ **Suporte para CSV e JSON**: Flexibilidade no formato dos dados

## Limitações

- ⚠️ Apenas arquivos `.csv` e `.json` são suportados
- ⚠️ Arquivos muito grandes (>5MB) podem deixar o editor lento
- ⚠️ CSV deve ter formato válido (cabeçalho obrigatório)
- ⚠️ JSON deve ser válido e será validado ao enviar

## Dicas

💡 **Nome de arquivos**: Use nomes descritivos como `vendas-2024.csv` ou `dados-clima.json` em vez de `dados.csv`

💡 **Múltiplos datasets**: Você pode usar vários arquivos (CSV e JSON) no mesmo spec:
```json
{
  "layer": [
    {
      "data": { "url": "data:vendas.csv" },
      "mark": "line"
    },
    {
      "data": { "url": "data:metas.json" },
      "mark": "rule"
    }
  ]
}
```

💡 **Formato CSV**: Certifique-se de que:
- A primeira linha contém os nomes das colunas
- Valores separados por vírgula
- Use aspas duplas para textos com vírgula: `"São Paulo, SP"`

💡 **Formato JSON**: Pode ser:
- Array de objetos: `[{...}, {...}]`
- Objeto único: `{...}` (será convertido para array automaticamente)

## Troubleshooting

**Problema**: "Arquivo não encontrado"
- **Solução**: Verifique se o nome do arquivo na URL corresponde exatamente ao nome enviado (incluindo extensão)

**Problema**: Gráfico não renderiza
- **Solução**: Abra o console do navegador (F12) e veja se há erro no parse dos dados

**Problema**: "Verifique se é um JSON válido"
- **Solução**: Valide seu JSON em https://jsonlint.com/ antes de enviar

**Problema**: Dados CSV não aparecem
- **Solução**: Verifique se o CSV tem cabeçalho e se os nomes dos campos no `encoding` estão corretos

**Problema**: Dados JSON em formato errado
- **Solução**: Vega-Lite espera um array. Se seu JSON é um objeto, envolva-o em um array: `[{...}]`
