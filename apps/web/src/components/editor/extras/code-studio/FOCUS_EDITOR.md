# Focus Editor - LeetCode Style Editor

O **Focus Editor** é um tipo de painel editor que mostra um seletor de linguagem ao invés de abas de arquivos, inspirado no estilo do LeetCode.

## Características

- **Seletor de Linguagem**: Dropdown para escolher entre diferentes linguagens
- **Pasta Focus Única**: Uma pasta marcada como focus 🎯 para todos os editores
- **Configuração Simples**: Apenas marque a pasta no File Explorer
- **Agrupamento por Nome**: Arquivos com o mesmo nome mas extensões diferentes são agrupados
- **Sem Abas**: Interface limpa focada em uma única tarefa

## Como Usar

### 1. Adicionar Focus Editor

No modo de edição de layout:
1. Clique no botão **Layout** (ícone de layout)
2. Arraste o botão **Focus Editor** (ciano) para o grid
3. O painel será criado com configuração padrão

### 2. Marcar Pasta como Focus

**Para todos os tipos de focus-editor** (multiple e unique):
1. No File Explorer, clique com o botão direito na pasta desejada
2. Selecione **"Set as Focus Folder"** 🎯
3. A pasta ficará marcada com o ícone 🎯
4. Todos os focus-editors usarão esta pasta automaticamente

**Importante**: 
- Apenas uma pasta pode ser marcada como focus por vez
- Esta é a **única forma** de configurar a pasta para focus-editor
- Funciona tanto para `editorInstance: "multiple"` quanto `"unique"`

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

## Diferenças: Full Editor vs Focus Editor

| Característica | Full Editor | Focus Editor |
|----------------|-------------|--------------|
| Navegação | Abas de arquivos | Seletor de linguagem |
| Arquivos | Todos os arquivos abertos | Arquivos da pasta focus |
| Uso | Projetos gerais | Desafios/Exercícios |
| Abrir/Fechar | Via Explorer ou tabs | Automático por pasta |
| Visual | Abas horizontais | Dropdown simples |
| Configuração Pasta | N/A | Marcar pasta como focus 🎯 |

## Editor Instance: Multiple vs Unique

**Ambos usam a mesma pasta marcada como focus 🎯**

### Multiple (Compartilhado)
- Todos os focus-editors multiple compartilham o mesmo estado de arquivo ativo
- Trocar de linguagem em um painel afeta todos os outros paineis multiple
- Ideal para exercícios padronizados onde todos os displays mostram a mesma solução

### Unique (Isolado)
- Cada painel unique tem seu próprio estado de arquivo ativo independente
- Trocar linguagem em um painel não afeta outros paineis
- Todos ainda usam a mesma pasta focus, mas permitem navegação independente
- Ideal para comparar diferentes abordagens da mesma solução

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
      isOpen: true,
      isFocusFolder: true  // ← Pasta marcada como focus
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
            editorInstance: "unique"
            // Não precisa de focusIndexPath - usa a pasta marcada como focus
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
| Arquivos | Todos os arquivos abertos | Arquivos da pasta focus |
| Uso | Projetos gerais | Desafios/Exercícios |
| Abrir/Fechar | Via Explorer ou tabs | Automático por pasta |
| Visual | Abas horizontais | Dropdown simples |
| Configuração Pasta | N/A | Marcar pasta como focus 🎯 |

## Editor Instance: Multiple vs Unique

**Ambos usam a mesma pasta marcada como focus 🎯**

### Multiple (Compartilhado)
- Todos os focus-editors multiple compartilham o mesmo estado de arquivo ativo
- Trocar de linguagem em um painel afeta todos os outros paineis multiple
- Ideal para exercícios padronizados onde todos os displays mostram a mesma solução

### Unique (Isolado)
- Cada painel unique tem seu próprio estado de arquivo ativo independente
- Trocar linguagem em um painel não afeta outros paineis
- Todos ainda usam a mesma pasta focus, mas permitem navegação independente
- Ideal para comparar diferentes abordagens da mesma solução

## Boas Práticas

1. **Naming Convention**: Use o mesmo nome base para todos os arquivos de solução
2. **Pasta Única**: Mantenha arquivos de uma solução em uma pasta dedicada
3. **Marcar Focus**: Sempre marque a pasta principal como focus 🎯 no File Explorer
4. **Multiple para Sincronização**: Use `editorInstance: "multiple"` quando quiser que todos os paineis mostrem a mesma linguagem
5. **Unique para Independência**: Use `editorInstance: "unique"` quando cada painel precisar navegar independentemente
6. **Readonly**: Configure pastas como readonly quando necessário

## Workflow Recomendado

### Para Exercícios Sincronizados (Multiple)
1. Crie uma pasta `/solutions`
2. Marque-a como **Focus Folder** 🎯 no File Explorer
3. Adicione arquivos: `solution.js`, `solution.py`, `solution.cpp`
4. Use focus-editor com `editorInstance: "multiple"`
5. Todos os paineis mostrarão a mesma linguagem selecionada

### Para Comparações Independentes (Unique)
1. Crie uma pasta `/solutions`
2. Marque-a como **Focus Folder** 🎯
3. Adicione múltiplas implementações: `solution.js`, `solution.py`, `solution.cpp`
4. Use vários focus-editors com `editorInstance: "unique"`
5. Cada painel pode mostrar uma linguagem diferente para comparação

## Casos de Uso

- ✅ Plataformas de programação competitiva
- ✅ Tutoriais multi-linguagem
- ✅ Comparação de implementações
- ✅ Exercícios de código
- ✅ Code katas
- ❌ Projetos com múltiplos arquivos (use Full Editor)
