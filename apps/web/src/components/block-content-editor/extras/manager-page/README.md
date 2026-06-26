# Manager Page System

Este módulo fornece um sistema unificado de gerenciamento de conteúdo para projetos e assets, com estilo de card padronizado e layouts responsivos.

## Componentes

### ManagerCardComponent
Componente de card unificado que renderiza tanto projetos quanto assets com estilo consistente.

**Features:**
- Suporte para grid e list views
- Estilo responsivo com breakpoints progressivos
- Hover effects consistentes (borders, shadows)
- Typography adaptativa
- Ações primárias e secundárias via dropdown
- Badges coloridos para mimeType (assets)
- Ícones de storage type (projects)

### GridView
Container para exibição em grid com suporte de 5 a 12 colunas.

**Breakpoints progressivos:**
- 5 cols: `1 → 2 → 3 → 4 → 5`
- 6 cols: `2 → 3 → 4 → 5 → 6`
- 7 cols: `2 → 3 → 4 → 5 → 7`
- 9 cols: `3 → 4 → 5 → 7 → 9`
- 12 cols: `3 → 6 → 8 → 10 → 12`

### ListView
Container para exibição em lista com suporte de 1 ou 2 colunas.

### ManagerLayout
Layout principal da página com sidebar, header e área de conteúdo.

**Features:**
- Navegação entre contexts (Projects/Assets)
- Controles de view mode (List/Grid)
- Seletor de colunas dinâmico
- Seções para filtros e paginação
- Botão de criação contextual

### ManagerFilters
Sistema de filtros unificado com suporte para:

**Projects:**
- Busca por nome
- Tags (all/any mode)
- Storage type
- Sort order

**Assets:**
- Busca por nome
- MIME type
- Project filter
- Usage (used/unused)
- Sort order

## Tipos

### ManagerCard
Union type que representa tanto ProjectCard quanto AssetCard:

```typescript
type ManagerCard = ProjectCard | AssetCard

interface ProjectCard extends BaseCard {
  type: 'project'
  tags: string[]
  size: number
  data: string
  storageType?: 'local' | 'gameguild-cloud' | 'google-drive'
}

interface AssetCard extends BaseCard {
  type: 'asset'
  mimeType: string
  size: number
  projects?: string[]
  thumbnailUrl?: string
}
```

### CardAction
Define ações disponíveis nos cards:

```typescript
interface CardAction {
  label: string
  icon?: React.ReactNode
  onClick: (card: ManagerCard) => void
  variant?: 'default' | 'destructive'
}
```

### FilterConfig
Configuração unificada de filtros:

```typescript
interface FilterConfig {
  searchTerm: string
  tags?: string[]
  tagFilterMode?: 'all' | 'any'
  storageType?: 'all' | 'local' | 'gameguild-cloud' | 'google-drive'
  mimeType?: string
  projectFilter?: string
  usageFilter?: 'all' | 'used' | 'unused'
  dateFrom?: string
  dateTo?: string
  sortOrder?: 'newest' | 'oldest' | 'name' | 'name-desc'
}
```

## Estilo Padronizado

### Cards
- **Border:** `border-gray-200 dark:border-gray-700`
- **Hover Border:** `border-gray-300 dark:border-gray-600`
- **Shadow:** `shadow-sm hover:shadow-lg`
- **Transition:** `transition-all duration-200`
- **Heights:** Flexíveis com `min-h` em vez de `h` fixo

### Typography
- **Title (Grid):** `text-sm sm:text-base` (projetos), `text-xs sm:text-sm` (assets)
- **Badges:** `text-[9px] sm:text-xs` (assets), `text-[10px] sm:text-xs` (projetos)
- **Secondary Text:** `text-[10px] sm:text-xs`

### Spacing
- **Card Padding (Grid):** `p-3 sm:p-4` (projetos), `p-2 sm:p-3` (assets)
- **Card Padding (List):** `p-4`
- **Gap:** `gap-4` (grid), `gap-3` (list)

## Uso

```tsx
import { 
  ManagerLayout, 
  ManagerFilters, 
  GridView, 
  ListView,
  type ManagerCard,
  type CardAction 
} from '@/components/block-content-editor/extras/asset-manager/manager-page'

// Define actions
const primaryActions: CardAction[] = [
  { label: 'Abrir', onClick: (card) => handleOpen(card) },
  { label: 'Editar', onClick: (card) => handleEdit(card) },
]

const secondaryActions: CardAction[] = [
  { label: 'Excluir', onClick: (card) => handleDelete(card), variant: 'destructive' },
]

// Render
<ManagerLayout
  activeContext="projects"
  viewMode="grid"
  gridColumns={5}
  listColumns={1}
  onContextChange={setContext}
  onViewModeChange={setViewMode}
  onGridColumnsChange={setGridColumns}
  onListColumnsChange={setListColumns}
  filterSection={
    <ManagerFilters
      filters={filters}
      onFilterChange={handleFilterChange}
      contextType="projects"
      itemsPerPage={24}
      onItemsPerPageChange={setItemsPerPage}
    />
  }
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
```

## Design Principles

1. **Consistência:** Mesmo estilo visual para projetos e assets
2. **Responsividade:** Design adaptativo de mobile a ultra-wide
3. **Flexibilidade:** Suporte para 1-12 colunas com transições suaves
4. **Legibilidade:** Typography que escala apropriadamente
5. **Performance:** Componentes otimizados e memoizados
6. **Acessibilidade:** Keyboard navigation e ARIA labels

## Migração

Para migrar código existente:

1. Converta dados para tipo `ManagerCard`
2. Defina `CardAction[]` arrays
3. Use `ManagerLayout` como wrapper principal
4. Substitua listas customizadas por `GridView`/`ListView`
5. Use `ManagerFilters` em vez de filtros separados

Não exclua `project-dialog` - ele permanece útil para seu contexto de dialog.
