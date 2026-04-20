# Code Studio - Asset Integration

## Visão Geral

O Code Studio agora suporta a integração com o sistema de assets, permitindo que usuários adicionem arquivos de código existentes de assets ou façam upload de novos arquivos, de forma similar ao comportamento do editor de Media.

**Otimização de Armazenamento**: 
- Arquivos não modificados de assets armazenam apenas uma referência (`asset://id`) em vez de duplicar o conteúdo, economizando memória e evitando redundância.
- **Armazenamento de Texto**: Arquivos de código são armazenados como texto plano em assets (não base64), permitindo fácil inspeção e debugging.

## Funcionalidades Implementadas

### 1. Adicionar Arquivos de Assets

Quando o usuário clica no botão de adicionar arquivo (+), um menu dropdown aparece com duas opções:

- **Create New File**: Comportamento padrão anterior - cria um novo arquivo vazio
- **Add from Assets**: Abre o dialog de upload/seleção de assets para escolher arquivos existentes ou fazer upload

O mesmo menu também está disponível no menu de contexto das pastas (botão "⋮").

### 2. Referência a Assets (Copy-on-Write Otimizado)

Quando um arquivo é adicionado de assets:

- O arquivo mantém uma referência ao asset original através do campo `assetId`
- O campo `content` armazena `asset://assetId` em vez do conteúdo completo
- Conteúdo é resolvido dinamicamente quando necessário (visualização, execução)
- É marcado com badge visual **"A"** (azul)

Quando o usuário modifica um arquivo que veio de assets:

- Na primeira modificação, a referência é substituída pelo conteúdo real
- Badge **"M"** (amarelo) aparece indicando modificação
- Ao salvar: `assetId` é removido, arquivo torna-se local com **mesmo nome**
- Asset original permanece intacto

### 3. Indicadores Visuais

Os arquivos que vêm de assets são identificados visualmente:

- **No File Explorer**: Badge "A" ao lado do nome do arquivo
- **Nas File Tabs**: Badge "A" na aba do arquivo
- **Quando Modificado**: Badge "M" adicional indicando que foi alterado

## Estrutura de Dados

### Tipo `CodeFile` (types.ts)

```typescript
export interface CodeFile {
  id: string
  name: string
  content: string // Pode ser conteúdo real OU referência "asset://id"
  language: SupportedLanguage
  isMain: boolean
  isVisible: boolean
  path: string
  assetId?: string // ID do asset original (para tracking)
  isModified?: boolean // Flag indicando se foi modificado
}
```

### Formato de Referência

- **Não modificado**: `content = "asset://abc123def456"`
- **Modificado**: `content = "código real do arquivo..."`

### Asset Storage Types

Arquivos podem ser armazenados de duas formas em assets:

**`AssetMetadata.storageType`:**
- **`'text'`**: Armazenado como texto plano (para arquivos de código)
  - Vantagens: Fácil inspeção, menor overhead, compatível com ferramentas
  - Usado para: `.js`, `.ts`, `.py`, `.txt`, `.md`, `.json`, etc.
- **`'dataurl'`**: Armazenado como base64 (para imagens e binários)
  - Vantagens: Suporta qualquer tipo de arquivo
  - Usado para: Imagens, vídeos, arquivos binários

## Sistema de Assets - Armazenamento de Texto

### Tipos de Armazenamento (`AssetMetadata.storageType`)

**1. Text Storage (`'text'`)**
- Arquivo armazenado como texto plano no IndexedDB
- Detecção automática por MIME type ou extensão
- Extensões suportadas: `.txt`, `.md`, `.js`, `.ts`, `.jsx`, `.tsx`, `.json`, `.xml`, `.html`, `.css`, `.py`, `.java`, `.c`, `.cpp`, `.rs`, `.go`, `.rb`, `.php`, `.sh`, `.yml`, `.yaml`, `.sql`, `.lua`, `.r`, `.swift`, `.kt`, `.cs`, e muitas outras
- MIME types: `text/*`, `application/javascript`, `application/json`, `application/xml`, `application/typescript`

**2. DataURL Storage (`'dataurl'`)**
- Arquivo armazenado como base64
- Usado para imagens, vídeos, e arquivos binários
- Formato: `data:image/png;base64,iVBORw0KG...`

### Forçar Text Storage

No `MediaUploadDialog`, use `forceTextStorage={true}`:

```tsx
<MediaUploadDialog
  forceTextStorage={true}  // Força armazenamento como texto
  acceptTypes="*/*"
  // ... outras props
/>
```

### AssetManager - Métodos de Detecção

**`isTextFile(file: File): boolean`**
- Verifica se arquivo deve ser armazenado como texto
- Checa MIME type e extensão
- Automático para arquivos de código

**`fileToDataUrl(file: File, asText: boolean): Promise<string>`**
- `asText=true`: retorna texto plano
- `asText=false`: retorna dataURL base64
- Usado internamente pelo `saveAsset()`

## Componentes Modificados

### 1. `FileSourceMenu` (novo)

Menu dropdown que oferece opções para criar novo arquivo ou adicionar de assets.

**Props:**
- `onCreateNew`: Callback quando usuário seleciona "Create New File"
- `onAddFromAssets`: Callback quando usuário seleciona "Add from Assets"
- `trigger`: Elemento customizável do trigger (opcional)

### 2. `FileExplorer`

**Nova prop:**
- `onAddFileFromAsset?: (path: string, assetId: string, fileName: string, content: string) => void`

**Mudanças:**
- Integra `FileSourceMenu` no botão principal de adicionar arquivo
- Adiciona opção "Add from Assets" nos menus de contexto das pastas
- Integra `MediaUploadDialog` para seleção de arquivos
- Mostra badges visuais para arquivos de assets

### 3. `CodeStudioEditor`

**Estado adicional:**
- `resolvedContents: Record<string, string>` - Cache de conteúdos resolvidos

**Novos handlers:**
- `handleAddFileFromAsset`: Adiciona arquivo a partir de asset
- Modifica `handleCodeChange`: Na primeira edição, substitui referência por conteúdo real
- Modifica `handleSaveClick`: Remove `assetId` de arquivos modificados (não cria cópia)
- Modifica `handleExecute`: Usa conteúdos resolvidos para execução

**useEffect para resolução:**
- Resolve referências `asset://id` automaticamente
- Cache de conteúdos para performance
- Re-resolve quando arquivos mudam

### 4. `FileTabs`

**Mudanças:**
- Mostra badges "A" e "M" nas abas dos arquivos

## Operações em `file-operations.ts`

### Novas Funções

#### `addFileFromAsset`

Adiciona um arquivo a partir de um asset existente, armazenando apenas referência.

```typescript
export function addFileFromAsset(
  draft: CodeStudioData,
  path: string,
  assetId: string,
  fileName: string,
  content: string, // Não usado diretamente, armazena referência
  activeDisplayId: string = 'display-1'
): void
```

**Comportamento:**
- Cria `CodeFile` com `content = "asset://assetId"`
- Define `assetId` e `isModified = false`
- Não duplica conteúdo do arquivo

#### `isAssetReference`

Verifica se um conteúdo é referência a asset.

```typescript
export function isAssetReference(content: string): boolean
// Retorna: content.startsWith('asset://')
```

#### `extractAssetId`

Extrai o ID do asset de uma referência.

```typescript
export function extractAssetId(content: string): string | null
// 'asset://abc123' → 'abc123'
```

#### `resolveFileContent`

Resolve o conteúdo de um arquivo, buscando do asset se necessário.

```typescript
export async function resolveFileContent(file: CodeFile): Promise<string>
```

**Comportamento:**
- Se `content` não é referência → retorna direto
- Se é `asset://id` → busca do assetManager
- Converte dataURL para texto se necessário
- Fallback para string vazia em caso de erro

#### `markFileAsModified`

Marca um arquivo de asset como modificado (primeira edição).

```typescript
export function markFileAsModified(
  draft: CodeStudioData,
  fileId: string
): void
```

#### `createCopyOnSave`

Converte arquivo modificado de asset para local ao salvar.

```typescript
export function createCopyOnSave(
  draft: CodeStudioData,
  fileId: string
): string | null
```

**Comportamento Simplificado:**
- Não cria novo arquivo com sufixo
- Apenas remove `assetId` do arquivo existente
- Mantém mesmo nome e conteúdo
- Arquivo passa a ser local (sem referência ao asset)

## Fluxo de Uso

### Adicionar Arquivo de Asset

1. Usuário clica no botão "+" no File Explorer
2. Menu aparece com opções "Create New File" e "Add from Assets"
3. Usuário seleciona "Add from Assets"
4. `MediaUploadDialog` abre permitindo:
   - Selecionar arquivo existente em assets
   - Fazer upload de novo arquivo
5. Arquivo é adicionado ao projeto:
   - `content` = `"asset://assetId"` (apenas referência)
   - `assetId` = ID do asset original
   - Badge "A" visível

### Visualizar e Editar

1. Arquivo é aberto no editor
2. `resolveFileContent()` busca conteúdo real do asset
3. Editor mostra conteúdo resolvido (não a referência)
4. Ao modificar pela primeira vez:
   - `content` substituído pelo conteúdo real
   - `isModified = true`, badge "M" aparece
   - Asset original não é afetado

### Salvar Modificações

1. Usuário clica em "Save"
2. `createCopyOnSave()` processa arquivos modificados:
   - Remove `assetId` (não cria novo arquivo)
   - Remove `isModified`
   - Badges "A" e "M" desaparecem
3. Arquivo torna-se local com mesmo nome
4. Asset original permanece intacto

## Integração com MediaUploadDialog

O `MediaUploadDialog` é configurado para aceitar arquivos de código:

```typescript
<MediaUploadDialog
  open={showAssetDialog}
  onOpenChange={setShowAssetDialog}
  onMediaSelected={handleAssetSelected}
  title="Add Code File from Assets"
  acOtimização de Armazenamento

**Arquivos não modificados:**
- Armazenam apenas `"asset://id"` (~20 bytes)
- Conteúdo real não é duplicado
- Economia significativa para arquivos grandes

**Text Storage vs Base64:**
- Arquivos de código: armazenados como texto plano em assets
- Vantagens: Menor overhead, fácil debug, inspeção direta no IndexedDB
- Imagens/binários: continuam em base64 como antes

**Resolução de Conteúdo:**
- Lazy loading: busca do asset apenas quando necessário
- Cache em `resolvedContents` para evitar re-buscas
- Resolução automática para: visualização, edição, execução
- `resolveFileContent()` detecta automaticamente tipo de storage

### Gestão de Pastas

A lógica de pastas permanece inalterada. Apenas arquivos individuais podem ser adicionados de assets, não pastas inteiras.

### Sincronização com Monaco

- Arquivos sincronizados com conteúdos resolvidos
- Monaco FS sempre recebe conteúdo real, não referências
- Arquivos de assets mantém referência ao `assetId` original
- Ao converter para local, apenas `assetId` é removido (mesmo ID de arquivo)
### Performance

- Resolução assíncrona não bloqueia UI
- Cache de conteúdos resolvidos evita re-buscas
- useEffect otimizado para resolver apenas quando necessário
## Considerações Técnicas

### Gestão de Pastas

A lógica de pastas permanece inalterada. Apenas arquivos individuais podem ser adicionados de assets, não pastas inteiras.

### Sincronização com Monaco

Quando um arquivo é adicionado ou modificado, o sistema de arquivos virtual do Monaco é sincronizado automaticamente através de `updateMonacoFile`.

### IDs Únicos

- Cada arquivo tem um ID único gerado por `Date.now().toString()`
- Arquivos de assets mantém referência ao `assetId` original
- Cópias criadas obtêm novos IDs mas não mantêm `assetId`

## Futuras Melhorias

Possíveis melhorias para o futuro:

1. Permitir reverter arquivo modificado ao estado original do asset
2. Mostrar diff visual comparando com versão original do asset
3. Opção de "salvar no asset" (atualizar asset original)
4. Suporte para adicionar pastas inteiras de assets
5. Pre-loading inteligente de assets frequentemente usados
6. Compressão de referências para múltiplos assets
