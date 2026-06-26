# Project Navigation Guide

Este documento explica as diferentes formas de abrir e navegar entre projetos no sistema Block Content Editor.

## Visão Geral

O sistema oferece **duas formas principais** de abrir projetos no Studio (editor) ou Viewer (visualizador):

1. **Navegação por URL** - Usando hash na URL para acesso direto
2. **Navegação por Interface** - Usando dialogs e a página manager

---

## 1. Navegação por URL (Hash-based)

### Como Funciona

Você pode acessar diretamente um projeto específico adicionando seu ID após o símbolo `#` na URL:

```
/block-content-editor/studio#PROJECT_ID
/block-content-editor/viewer#PROJECT_ID
```

### Exemplos

```
https://example.com/block-content-editor/studio#67445
https://example.com/block-content-editor/viewer#abc-123-def-456
```

### Quando o Hash é Lido

O sistema verifica a presença do hash na URL nos seguintes momentos:

- **Ao carregar a página** - Durante a inicialização do componente
- **Prioridade máxima** - Hash tem prioridade sobre localStorage

### Fluxo de Carregamento

1. Usuário acessa URL com hash (ex: `/studio#67445`)
2. Sistema detecta o hash no `useEffect` de inicialização
3. Extrai o ID do projeto do hash: `window.location.hash.replace('#', '')`
4. Busca o projeto no IndexedDB usando `storageAdapter.load(projectId)`
5. Carrega o projeto no editor/viewer
6. Mostra notificação de sucesso

### Comportamento Especial

- **Projeto não encontrado**: Mostra erro e não carrega nada
- **Dados inválidos**: Mostra erro específico sobre o problema
- **Fallback**: Se hash falhar, tenta carregar do localStorage

### Atualização Automática do Hash

O hash da URL é atualizado automaticamente quando:

- Um projeto é aberto através do dialog "Open Project"
- Um novo projeto é criado
- Um projeto é carregado do localStorage
- Um projeto é selecionado na sidebar (viewer)

```javascript
// Atualização do hash
window.history.pushState(null, '', `#${projectData.id}`)

// Limpeza do hash ao criar novo projeto
window.history.pushState(null, '', window.location.pathname)
```

### Vantagens

✅ **Compartilhamento fácil** - Link direto para projeto específico  
✅ **Bookmarking** - Salvar projeto como favorito no navegador  
✅ **Deep linking** - Acesso direto sem navegação  
✅ **Histórico** - Botão "voltar" do navegador funciona  
✅ **Tabs múltiplas** - Abrir vários projetos simultaneamente

---

## 2. Navegação por Interface

### Página Manager (`/block-content-editor`)

A página principal que lista todos os projetos disponíveis.

#### Funcionalidades

- **Grid/List View** - Visualização em grade ou lista
- **Busca e Filtros** - Pesquisar por nome, tags, tipo de storage
- **Ações nos Cards**:
  - **Open in Studio** - Abre no editor
  - **View** - Abre no visualizador
  - **Information** - Ver/editar informações do projeto
  - **Download** - Baixar projeto como JSON
  - **Delete** - Excluir projeto

#### Fluxo de Abertura

1. Usuário clica em "Open in Studio" ou "View"
2. Sistema carrega dados do projeto via `loadProject(projectId)`
3. Salva projeto no localStorage: `localStorage.setItem('selectedProject', JSON.stringify(projectData))`
4. Redireciona para `/studio` ou `/viewer`
5. Studio/Viewer detecta projeto no localStorage e carrega
6. **Após carregar, o hash da URL é atualizado com o ID do projeto**

```javascript
// Manager
const handleProjectOpen = async (projectId: string) => {
  const projectData = await loadProject(projectId)
  localStorage.setItem('selectedProject', JSON.stringify(projectData))
  window.location.href = `/block-content-editor/studio`
}

// Studio/Viewer
useEffect(() => {
  const selectedProjectData = localStorage.getItem('selectedProject')
  if (selectedProjectData) {
    const projectData = JSON.parse(selectedProjectData)
    localStorage.removeItem('selectedProject') // Limpa após usar
    // Carrega projeto...
    window.history.pushState(null, '', `#${projectData.id}`) // Atualiza URL
  }
}, [])
```

### Dialog "Open Project" (Studio/Viewer)

Disponível dentro do Studio e Viewer através do botão "Open".

#### Componentes

- **Studio**: `OpenProjectDialog`
- **Viewer**: `OpenProjectDialogPreview`

#### Funcionalidades

- Busca por nome
- Filtro por tags (all/any)
- Filtro por storage type (local/cloud/drive)
- Ordenação (nome, data, tamanho)
- Visualização de informações do projeto
- Download e exclusão de projetos

#### Fluxo de Abertura

1. Usuário abre dialog e seleciona projeto
2. Dialog carrega dados completos do projeto
3. Chama callback `onProjectLoad(projectData)`
4. Projeto é carregado no editor/viewer atual
5. **Hash da URL é atualizado** com `window.history.pushState(null, '', `#${projectData.id}`)`

### Sidebar de Projetos (Viewer)

Lista de projetos na lateral esquerda do viewer.

#### Funcionalidades

- Lista todos os projetos
- Destaque do projeto atual
- Busca rápida
- Clique para trocar de projeto
- Responsivo (mobile: overlay)

#### Fluxo de Troca

1. Usuário clica em projeto da lista
2. Sistema carrega novo projeto
3. Atualiza visualização
4. **Atualiza hash da URL**

---

## Prioridade de Carregamento

Quando Studio/Viewer inicializam, a ordem de verificação é:

### Prioridade 1: Hash na URL
```javascript
const hash = window.location.hash.replace('#', '')
if (hash) {
  const projectData = await storageAdapter.load(hash)
  // Carrega projeto...
}
```

### Prioridade 2: LocalStorage
```javascript
const selectedProjectData = localStorage.getItem('selectedProject')
if (selectedProjectData) {
  const projectData = JSON.parse(selectedProjectData)
  localStorage.removeItem('selectedProject')
  // Carrega projeto...
}
```

### Prioridade 3: Estado Padrão
```javascript
// Nenhum projeto carregado
// Studio: Mostra editor vazio
// Viewer: Mostra tela de seleção
```

---

## Comparação dos Métodos

| Aspecto | URL (Hash) | Interface |
|---------|-----------|-----------|
| **Acesso Direto** | ✅ Sim | ❌ Não |
| **Compartilhável** | ✅ Sim | ❌ Não |
| **Requer Navegação** | ❌ Não | ✅ Sim |
| **Busca/Filtros** | ❌ Não | ✅ Sim |
| **Atualização Auto** | ✅ Sim | ✅ Sim |
| **Histórico Browser** | ✅ Sim | ⚠️ Parcial |
| **Múltiplas Tabs** | ✅ Fácil | ⚠️ Complexo |

---

## Casos de Uso

### Usar Navegação por URL quando:

- Compartilhar projeto específico com alguém
- Salvar projeto como bookmark
- Abrir múltiplos projetos em tabs diferentes
- Integração com sistemas externos
- Links em documentação/emails

### Usar Navegação por Interface quando:

- Explorar projetos disponíveis
- Buscar projeto por nome/tags
- Comparar informações de projetos
- Gerenciar múltiplos projetos
- Descobrir conteúdo

---

## Fluxo Completo de Exemplo

### Exemplo 1: Compartilhamento

```
1. Usuário A abre projeto no Studio
2. URL atualiza para: /studio#project-123
3. Usuário A copia URL e envia para Usuário B
4. Usuário B clica no link
5. Sistema detecta hash e carrega projeto-123 automaticamente
6. Usuário B vê o mesmo projeto
```

### Exemplo 2: Exploração

```
1. Usuário acessa /block-content-editor (Manager)
2. Usa filtros para encontrar "Tutorial" tag
3. Vê lista de projetos filtrados
4. Clica "Open in Studio" em um projeto
5. Sistema salva no localStorage
6. Redireciona para /studio
7. Studio carrega do localStorage
8. URL atualiza para /studio#tutorial-project-456
9. Usuário pode compartilhar a URL atual
```

---

## Implementação Técnica

### Studio e Viewer

Ambos compartilham lógica similar para detecção e carregamento:

```typescript
useEffect(() => {
  if (!isDbInitialized) return
  
  const checkSelectedProject = async () => {
    // 1. Verifica hash
    const hash = window.location.hash.replace('#', '')
    if (hash) {
      const projectData = await storageAdapter.load(hash)
      if (projectData) {
        // Carrega projeto...
        window.history.pushState(null, '', `#${projectData.id}`)
        return
      }
    }
    
    // 2. Verifica localStorage
    const selectedProjectData = localStorage.getItem('selectedProject')
    if (selectedProjectData) {
      const projectData = JSON.parse(selectedProjectData)
      localStorage.removeItem('selectedProject')
      // Carrega projeto...
      window.history.pushState(null, '', `#${projectData.id}`)
      return
    }
  }
  
  checkSelectedProject()
}, [isDbInitialized])
```

### Manager

```typescript
const handleProjectOpen = async (projectId: string, event?: React.MouseEvent) => {
  const projectData = await loadProject(projectId)
  
  // Salva no localStorage como ponte
  localStorage.setItem('selectedProject', JSON.stringify(projectData))
  
  // Redireciona (ou abre nova tab se Ctrl/Cmd pressionado)
  if (event?.ctrlKey || event?.metaKey) {
    window.open(`/block-content-editor/studio`, '_blank')
  } else {
    window.location.href = `/block-content-editor/studio`
  }
}
```

---

## Considerações

### Segurança

- IDs de projeto são gerados com `crypto.randomUUID()`
- Dados armazenados localmente no IndexedDB
- Sem autenticação de URL (projetos públicos localmente)

### Performance

- Hash é verificado apenas na inicialização
- LocalStorage usado como ponte temporária
- Limpeza automática após uso

### UX

- Notificações toast para feedback
- Mensagens de erro descritivas
- Loading states durante carregamento
- Histórico do navegador preservado

---

## Futuras Melhorias

Possíveis melhorias para o sistema:

1. **Query Parameters** - Adicionar parâmetros adicionais (`?mode=edit&highlight=section1`)
2. **Short URLs** - Sistema de URLs curtas para compartilhamento
3. **Project Slugs** - URLs amigáveis (`/studio/my-project-name`)
4. **Version History** - Hash com versão específica (`/studio#project-123/v5`)
5. **Collaborative Links** - Links com permissões de edição
