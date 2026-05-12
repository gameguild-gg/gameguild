# Project Export/Import Utilities

## Visão Geral

Esta documentação descreve as utilidades `ProjectExporter` e `ProjectImporter` que foram criadas para componentizar e padronizar as funções de exportação e importação de projetos no Block Content Editor. Estas utilidades são de uso comum entre diferentes modos de armazenamento (Google Drive e máquina local).

## Estrutura dos Arquivos

```
src/lib/project/
├── project-exporter.ts     # Utilitário de exportação
├── project-importer.ts     # Utilitário de importação
├── usage-examples.ts       # Exemplos de uso
└── README.md              # Esta documentação
```

## Estrutura Padrão de Pastas

### Formato Padrão
```
projeto-{id}/
├── index.json              # Metadados do projeto
└── data.block-content-editor         # Dados do editor Lexical
```

## ProjectExporter

### Funcionalidades
- ✅ Criação de estrutura de pastas padronizada para Google Drive
- ✅ Geração de arquivos ZIP para download local
- ✅ Separação clara entre metadados e dados
- ✅ Suporte a hash para integridade dos dados

### Métodos Principais

#### `prepareForExport(projectData, hash)`
Prepara os dados do projeto para exportação com estrutura padronizada.

```typescript
const folderData = ProjectExporter.prepareForExport(projectData, hash)
// Retorna: { metadata, data, folderName }
```

#### `createZipFile(projectData, hash)`
Cria arquivo ZIP para download local.

```typescript
const zipBlob = await ProjectExporter.createZipFile(projectData, hash)
// Retorna: Blob para download
```

#### `createMetadata(projectData, hash)`
Gera metadados padronizados do projeto.

```typescript
const metadata = ProjectExporter.createMetadata(projectData, hash)
// Retorna: ProjectMetadata
```

## ProjectImporter

### Funcionalidades
- ✅ Importação de arquivos ZIP
- ✅ Importação de arquivos .block-content-editor individuais
- ✅ Suporte ao formato de pastas (projeto-*)
- ✅ Validação de dados importados
- ✅ Conversão para formato padrão do sistema

### Métodos Principais

#### `importFromFile(file)`
Importa projeto de arquivo (ZIP, .block-content-editor, .lexical).

```typescript
const importedData = await ProjectImporter.importFromFile(file)
// Retorna: ImportedProjectData
```

#### `importFromFolderStructure(folderData)`
Importa projeto da estrutura de pastas do Google Drive.

```typescript
const importedData = await ProjectImporter.importFromFolderStructure({
  indexContent: "...",
  dataContent: "...",
  folderName: "projeto-123"
})
```

#### `convertToProjectData(importedData, newId, storageType)`
Converte dados importados para formato padrão do sistema.

```typescript
const projectData = ProjectImporter.convertToProjectData(
  importedData,
  'new-id',
  'google-drive'
)
```

#### `validateImportedData(importedData)`
Valida se os dados importados estão corretos.

```typescript
const isValid = ProjectImporter.validateImportedData(importedData)
```

## Tipos e Interfaces

### ProjectData
```typescript
interface ProjectData {
  id: string
  name: string
  data: string                    // JSON string do Lexical
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
  hash?: string
  storageType?: "local" | "gameguild-cloud" | "google-drive"
}
```

### ProjectMetadata
```typescript
interface ProjectMetadata {
  id: string
  name: string
  tags: string[]
  size: number
  hash: string
  createdAt: string
  updatedAt: string
  storageType: string
  version: string
  exportedAt: string
}
```

### ImportedProjectData
```typescript
interface ImportedProjectData {
  id: string
  name: string
  data: string
  tags: string[]
  metadata: ProjectMetadata | null
  isNewFormat: boolean
  originalFilename?: string
}
```

## Exemplos de Uso

### Exportação para Google Drive
```typescript
import { ProjectExporter } from './project-exporter'

// Preparar dados para Google Drive
const folderData = ProjectExporter.prepareForExport(projectData, hash)

// Usar GoogleDriveService para salvar
await googleDriveService.createFolder(folderData.folderName)
await googleDriveService.saveFile('index.json', folderData.metadata)
await googleDriveService.saveFile(`${projectData.id}.block-content-editor`, folderData.data)
```

### Exportação para Download Local
```typescript
import { ProjectExporter } from './project-exporter'

// Criar ZIP para download
const zipBlob = await ProjectExporter.createZipFile(projectData, hash)

// Fazer download
const url = URL.createObjectURL(zipBlob)
const link = document.createElement('a')
link.href = url
link.download = `${projectData.name}.zip`
link.click()
URL.revokeObjectURL(url)
```

### Importação de Arquivo
```typescript
import { ProjectImporter } from './project-importer'

// Importar de file input
const importedData = await ProjectImporter.importFromFile(file)

// Validar dados
if (ProjectImporter.validateImportedData(importedData)) {
  // Converter para formato do sistema
  const projectData = ProjectImporter.convertToProjectData(
    importedData,
    generateNewId(),
    'local'
  )
  
  // Salvar no sistema
  await saveProject(projectData)
}
```

### Importação do Google Drive
```typescript
import { ProjectImporter } from './project-importer'

// Baixar dados do Google Drive
const indexContent = await googleDriveService.downloadFile('index.json')
const dataContent = await googleDriveService.downloadFile(`${id}.block-content-editor`)

// Importar da estrutura de pastas
const importedData = await ProjectImporter.importFromFolderStructure({
  indexContent,
  dataContent,
  folderName: `projeto-${id}`
})
```

## Integração com Serviços Existentes

### GoogleDriveService
```typescript
class GoogleDriveService {
  async saveProject(projectData: ProjectData) {
    // ✅ Use ProjectExporter em vez de lógica inline
    const folderData = await ProjectExporter.prepareForExport(projectData, hash)
    
    // Criar pasta e salvar arquivos
    const folderId = await this.createFolder(folderData.folderName)
    await this.saveFileToFolder(folderId, 'index.json', folderData.metadata)
    await this.saveFileToFolder(folderId, `${projectData.id}.block-content-editor`, folderData.data)
  }
  
  async loadProject(folderId: string) {
    // Baixar arquivos
    const indexContent = await this.downloadFile(folderId, 'index.json')
    const dataContent = await this.downloadFile(folderId, `${id}.block-content-editor`)
    
    // ✅ Use ProjectImporter em vez de lógica inline
    return await ProjectImporter.importFromFolderStructure({
      indexContent,
      dataContent,
      folderName: `projeto-${id}`
    })
  }
}
```

### Local File Service
```typescript
class LocalFileService {
  async exportProject(projectData: ProjectData) {
    // ✅ Use ProjectExporter para exportação consistente
    const zipBlob = await ProjectExporter.createZipFile(projectData, hash)
    this.downloadBlob(zipBlob, `${projectData.name}.zip`)
  }
  
  async importProject(file: File) {
    // ✅ Use ProjectImporter para importação consistente
    const importedData = await ProjectImporter.importFromFile(file)
    const projectData = ProjectImporter.convertToProjectData(importedData, generateId(), 'local')
    return await this.saveToIndexedDB(projectData)
  }
}
```

## Extensões Suportadas

- `.zip` - Arquivo compactado com projeto (estrutura projeto-*)
- `.block-content-editor` - Arquivo de projeto Block Content Editor

## Vantagens da Componentização

1. **Consistência**: Mesmo formato entre Google Drive e arquivos locais
2. **Manutenibilidade**: Lógica centralizada em utilitários especializados
3. **Reutilização**: Código compartilhado entre diferentes serviços
4. **Testabilidade**: Funções puras e isoladas facilitam testes
5. **Simplicidade**: Formato único e padrão
6. **Validação**: Verificação automática de integridade dos dados

## Próximos Passos

1. ✅ Implementar ProjectExporter
2. ✅ Implementar ProjectImporter
3. ✅ Corrigir erros de TypeScript
4. ✅ Criar documentação e exemplos
5. 🔄 Migrar GoogleDriveService para usar as novas utilidades
6. 🔄 Migrar serviços de arquivos locais para usar as novas utilidades
7. 🔄 Adicionar testes unitários
8. 🔄 Atualizar componentes de UI para usar as novas APIs

## Observações Técnicas

- Todos os hashes são calculados usando o conteúdo dos dados do projeto
- Os metadados incluem timestamp de exportação para rastreamento
- A validação de JSON é feita em todas as importações
- Estrutura de pastas projeto-* garante organização clara
- Tipo safety garantido através do TypeScript
