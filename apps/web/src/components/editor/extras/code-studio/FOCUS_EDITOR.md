# Focus Editor - LeetCode Style Editor

O **Focus Editor** é um tipo de painel editor que mostra um seletor de linguagem ao invés de abas de arquivos, inspirado no estilo do LeetCode.

## Características

- **Seletor de Linguagem**: Dropdown para escolher entre diferentes linguagens
- **Pasta Índice**: Os arquivos são filtrados por uma pasta específica configurável
- **Agrupamento por Nome**: Arquivos com o mesmo nome mas extensões diferentes são agrupados
- **Sem Abas**: Interface limpa focada em uma única tarefa

## Como Usar

### 1. Adicionar Focus Editor

No modo de edição de layout:
1. Clique no botão **Layout** (ícone de layout)
2. Arraste o botão **Focus Editor** (ciano) para o grid
3. O painel será criado com configuração padrão

### 2. Configurar Pasta Índice

Com o modo de edição ativo:
1. No painel Focus Editor, você verá uma barra amarela no topo
2. Clique no dropdown "Index Folder"
3. Selecione a pasta que contém os arquivos de solução

### 3. Estrutura de Arquivos

Os arquivos na pasta índice devem ter o **mesmo nome base** mas **extensões diferentes**:

```
/solutions
  ├── solution.js    (JavaScript)
  ├── solution.py    (Python)
  ├── solution.cpp   (C++)
  └── solution.java  (Java)
```

### 4. Selecionar Linguagem

- Use o dropdown de linguagem na barra superior
- Mostra a extensão do arquivo (JS, PY, CPP, etc.)
- Ao selecionar, o editor muda para o arquivo correspondente

## Exemplo de Uso

### Problema de Programação Competitiva

```typescript
const codeStudioData = {
  mode: "execution",
  files: [
    {
      id: "1",
      name: "solution.js",
      path: "/solutions/solution.js",
      content: "function solve(input) {\n  return input * 2;\n}",
      language: "javascript"
    },
    {
      id: "2", 
      name: "solution.py",
      path: "/solutions/solution.py",
      content: "def solve(input):\n    return input * 2",
      language: "python"
    },
    {
      id: "3",
      name: "solution.cpp",
      path: "/solutions/solution.cpp",
      content: "#include <iostream>\nint solve(int input) {\n  return input * 2;\n}",
      language: "cpp"
    }
  ],
  folders: [
    {
      id: "folder-1",
      name: "solutions",
      path: "/solutions",
      isOpen: true
    }
  ],
  layout: {
    displays: [
      {
        id: "display-1",
        name: "Practice",
        aspectRatio: "1:1",
        panels: [
          {
            id: "focus-1",
            type: "focus-editor",
            row: 0,
            col: 0,
            rowSpan: 8,
            colSpan: 12,
            editorInstance: "unique",
            focusIndexPath: "/solutions"  // ← Pasta índice configurada
          },
          {
            id: "output-1",
            type: "output",
            row: 8,
            col: 0,
            rowSpan: 4,
            colSpan: 12
          }
        ]
      }
    ],
    activeDisplayId: "display-1",
    editMode: false
  }
}
```

## Diferenças: Full Editor vs Focus Editor

| Característica | Full Editor | Focus Editor |
|----------------|-------------|--------------|
| Navegação | Abas de arquivos | Seletor de linguagem |
| Arquivos | Todos os arquivos abertos | Arquivos da pasta índice |
| Uso | Projetos gerais | Desafios/Exercícios |
| Abrir/Fechar | Via Explorer ou tabs | Automático por pasta |
| Visual | Abas horizontais | Dropdown simples |

## Boas Práticas

1. **Naming Convention**: Use o mesmo nome base para todos os arquivos de solução
2. **Pasta Única**: Mantenha arquivos de uma solução em uma pasta dedicada
3. **Instance Unique**: Use `editorInstance: "unique"` para isolamento por display
4. **Readonly**: Configure pastas como readonly quando necessário

## Casos de Uso

- ✅ Plataformas de programação competitiva
- ✅ Tutoriais multi-linguagem
- ✅ Comparação de implementações
- ✅ Exercícios de código
- ✅ Code katas
- ❌ Projetos com múltiplos arquivos (use Full Editor)
