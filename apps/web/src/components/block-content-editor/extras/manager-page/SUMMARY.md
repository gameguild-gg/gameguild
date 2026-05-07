# Manager Page - Sistema de Gerenciamento Unificado

## 📁 Estrutura Criada

```
asset-manager/manager-page/
├── types.ts              # Tipos e interfaces compartilhadas
├── card.tsx              # Componente de card unificado
├── grid-view.tsx         # Container de visualização em grid
├── list-view.tsx         # Container de visualização em lista
├── manager-layout.tsx    # Layout principal da página
├── filters.tsx           # Sistema de filtros unificado
├── index.ts              # Exports centralizados
└── README.md             # Documentação completa
```

## ✨ Características Principais

### 1. **Card Unificado** (`card.tsx`)
- Suporte para projetos e assets com mesmo estilo visual
- Modos grid e list com layouts otimizados
- Responsive design com breakpoints progressivos
- Hover effects consistentes (borders, shadows)
- Typography adaptativa (text-[10px] → text-base)
- Badges coloridos para mimeType
- Ações via dropdown menu

### 2. **Grid View** (`grid-view.tsx`)
- Suporte para 5, 6, 7, 9 e 12 colunas
- Breakpoints progressivos para transições suaves:
  - **5 cols:** 1 → 2 → 3 → 4 → 5
  - **6 cols:** 2 → 3 → 4 → 5 → 6
  - **7 cols:** 2 → 3 → 4 → 5 → 7
  - **9 cols:** 3 → 4 → 5 → 7 → 9
  - **12 cols:** 3 → 6 → 8 → 10 → 12

### 3. **List View** (`list-view.tsx`)
- 1 ou 2 colunas
- Layout horizontal otimizado
- Mais informações visíveis

### 4. **Manager Layout** (`manager-layout.tsx`)
- Sidebar com navegação entre contexts
- Header com controles de view mode
- Seletor de colunas dinâmico
- Slots para filtros e paginação
- Botão contextual de criação

### 5. **Filtros Unificados** (`filters.tsx`)
- Busca por texto
- Tags (para projetos)
- Storage type (para projetos)
- MIME type (para assets)
- Project filter (para assets)
- Usage filter (para assets)
- Ordenação flexível
- Active filters badges

## 🎨 Estilo Padronizado

### Cards
```css
/* Base */
border: border-gray-200 dark:border-gray-700
background: bg-white dark:bg-gray-800
shadow: shadow-sm

/* Hover */
border: hover:border-gray-300 dark:hover:border-gray-600
shadow: hover:shadow-lg
transition: transition-all duration-200

/* Heights */
Projects: min-h-[180px]
Assets (image): min-h-[120px]
Assets (content): min-h-[70px] sm:min-h-[80px]
```

### Typography
```css
/* Títulos */
Projects (grid): text-sm sm:text-base
Assets (grid): text-xs sm:text-sm

/* Badges */
Assets: text-[9px] sm:text-xs
Projects: text-[10px] sm:text-xs

/* Texto secundário */
text-[10px] sm:text-xs
```

### Spacing
```css
/* Padding */
Projects (grid): p-3 sm:p-4
Assets (grid): p-2 sm:p-3
List: p-4

/* Gap */
Grid: gap-4
List: gap-3
```

## 📊 Tipos Principais

```typescript
// Card Types
type ManagerCard = ProjectCard | AssetCard

interface ProjectCard {
  type: 'project'
  id: string
  name: string
  tags: string[]
  size: number
  storageType?: 'local' | 'gameguild-cloud' | 'google-drive'
  createdAt: string
  updatedAt: string
}

interface AssetCard {
  type: 'asset'
  id: string
  name: string
  mimeType: string
  size: number
  projects?: string[]
  thumbnailUrl?: string
  createdAt: string
  updatedAt: string
}

// Actions
interface CardAction {
  label: string
  icon?: React.ReactNode
  onClick: (card: ManagerCard) => void
  variant?: 'default' | 'destructive'
}

// Filters
interface FilterConfig {
  searchTerm: string
  tags?: string[]
  tagFilterMode?: 'all' | 'any'
  storageType?: 'all' | 'local' | 'gameguild-cloud' | 'google-drive'
  mimeType?: string
  projectFilter?: string
  usageFilter?: 'all' | 'used' | 'unused'
  sortOrder?: 'newest' | 'oldest' | 'name' | 'name-desc'
}
```

## 🚀 Exemplo de Uso

```tsx
import { 
  ManagerLayout, 
  ManagerFilters, 
  GridView, 
  ListView,
  type ManagerCard,
  type CardAction,
  type FilterConfig 
} from '@/components/block-content-editor/extras/asset-manager/manager-page'

function MyManagerPage() {
  const [activeContext, setActiveContext] = useState<'projects' | 'assets'>('projects')
  const [viewMode, setViewMode] = useState<'list' | 'grid'>('grid')
  const [gridColumns, setGridColumns] = useState(5)
  const [listColumns, setListColumns] = useState(1)
  const [filters, setFilters] = useState<FilterConfig>({ searchTerm: '' })

  // Convert your data to ManagerCard format
  const cards: ManagerCard[] = projects.map(p => ({
    type: 'project',
    id: p.id,
    name: p.name,
    tags: p.tags,
    size: p.size,
    createdAt: p.createdAt,
    updatedAt: p.updatedAt,
  }))

  // Define actions
  const primaryActions: CardAction[] = [
    { 
      label: 'Abrir', 
      icon: <FolderOpen className="h-4 w-4" />,
      onClick: (card) => handleOpen(card) 
    },
    { 
      label: 'Editar', 
      icon: <Edit className="h-4 w-4" />,
      onClick: (card) => handleEdit(card) 
    },
  ]

  const secondaryActions: CardAction[] = [
    { 
      label: 'Excluir', 
      icon: <Trash className="h-4 w-4" />,
      onClick: (card) => handleDelete(card),
      variant: 'destructive' 
    },
  ]

  return (
    <ManagerLayout
      activeContext={activeContext}
      viewMode={viewMode}
      gridColumns={gridColumns}
      listColumns={listColumns}
      onContextChange={setActiveContext}
      onViewModeChange={setViewMode}
      onGridColumnsChange={setGridColumns}
      onListColumnsChange={setListColumns}
      onCreateNew={() => handleCreate()}
      filterSection={
        <ManagerFilters
          filters={filters}
          onFilterChange={(f) => setFilters({ ...filters, ...f })}
          contextType={activeContext}
          itemsPerPage={24}
          onItemsPerPageChange={setItemsPerPage}
          availableTags={availableTags}
          availableProjects={availableProjects}
        />
      }
      paginationSection={<YourPaginationComponent />}
    >
      {viewMode === 'grid' ? (
        <GridView
          cards={cards}
          columns={gridColumns}
          viewMode="grid"
          primaryActions={primaryActions}
          secondaryActions={secondaryActions}
          onCardClick={handleCardClick}
        />
      ) : (
        <ListView
          cards={cards}
          columns={listColumns}
          viewMode="list"
          primaryActions={primaryActions}
          secondaryActions={secondaryActions}
          onCardClick={handleCardClick}
        />
      )}
    </ManagerLayout>
  )
}
```

## 🔄 Vantagens sobre Código Anterior

### ✅ Consolidação
- **Antes:** Código duplicado em `project-dialog` e `asset-manager`
- **Agora:** Sistema único e reutilizável

### ✅ Consistência
- **Antes:** Estilos diferentes entre projetos e assets
- **Agora:** Estilo visual unificado e padronizado

### ✅ Manutenibilidade
- **Antes:** Mudanças requerem atualizar múltiplos arquivos
- **Agora:** Mudanças em um só lugar

### ✅ Flexibilidade
- **Antes:** Layouts fixos
- **Agora:** 1-12 colunas com breakpoints progressivos

### ✅ Responsividade
- **Antes:** Design básico
- **Agora:** Typography e spacing adaptativos em 5 breakpoints

## 📝 Notas Importantes

1. **project-dialog mantido:** O código em `project-dialog` permanece útil para seu contexto de dialog e não deve ser excluído.

2. **Migração gradual:** Você pode migrar página por página, não é necessário mudar tudo de uma vez.

3. **Extensibilidade:** O sistema é fácil de extender - adicione novos tipos de card implementando a interface base.

4. **Performance:** Componentes otimizados e preparados para memoização.

## 🎯 Princípios de Design

1. **Consistência visual** em todos os contextos
2. **Responsividade** de mobile a ultra-wide
3. **Flexibilidade** de layout (1-12 colunas)
4. **Legibilidade** com typography adaptativa
5. **Performance** com componentes otimizados
6. **Acessibilidade** com keyboard navigation

## 📦 Arquivos Gerados

- ✅ `types.ts` - Tipos e interfaces
- ✅ `card.tsx` - Componente de card (450+ linhas)
- ✅ `grid-view.tsx` - Grid container
- ✅ `list-view.tsx` - List container
- ✅ `manager-layout.tsx` - Layout principal (180+ linhas)
- ✅ `filters.tsx` - Sistema de filtros (300+ linhas)
- ✅ `index.ts` - Exports
- ✅ `README.md` - Documentação completa

**Total:** ~1200 linhas de código TypeScript/React otimizado e documentado.
