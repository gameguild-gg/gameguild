# Testing Lab Component Refactoring

This refactoring improves the testing-lab components to be more reusable, SSR-friendly, and maintainable using modern
Next.js 15+, React 19+, and Tailwind CSS 4 patterns.

## 🎯 Key Improvements

### 1. **Reusable Filter Components**

- **`SearchBar`**: Generic search component that can be used across the application
- **`ViewModeToggle`**: Reusable view mode switcher (cards/row/table)
- **`MultiSelectFilter`**: Generic multi-select dropdown with search functionality
- **`PeriodSelector`**: Existing component moved to common filters

### 2. **Filter Context & State Management**

- **`FilterProvider`**: Centralized filter state management with useReducer
- **`TestingLabFilterProvider`**: Testing-lab specific filter context
- Type-safe actions and state management
- Convenient helper methods for common operations

### 3. **SSR Optimization**

- **`TestingLabOverview`**: Server component for data fetching
- **`TestingLabOverviewClient`**: Client component for interactivity
- Clear separation of server and client responsibilities
- Better performance and SEO

### 4. **Component Architecture**

```
src/components/
├── common/
│   └── filters/
│       ├── filter-context.tsx          # Base filter state & context
│       ├── search-bar.tsx              # Reusable search component
│       ├── view-mode-toggle.tsx        # Reusable view mode switcher
│       ├── multi-select-filter.tsx     # Generic multi-select dropdown
│       └── index.ts                    # Exports
├── testing-lab/
│   ├── filters/
│   │   ├── testing-lab-filter-context.tsx    # Testing-lab specific context
│   │   ├── testing-lab-filter-controls.tsx   # Filter controls layout
│   │   ├── testing-lab-active-filters.tsx    # Active filter display
│   │   └── index.ts                           # Exports
│   ├── testing-lab-overview-ssr.tsx          # Server component
│   ├── testing-lab-overview-client.tsx       # Client component
│   ├── testing-lab-sessions.tsx              # Refactored sessions component
│   └── ...
```

## 🚀 Usage Examples

### Using Common Filter Components

```tsx
import { SearchBar, ViewModeToggle, MultiSelectFilter } from '@/components/common/filters';

// Search bar
<SearchBar
  searchTerm={searchTerm}
  onSearchChange={setSearchTerm}
  placeholder="Search items..."
/>

// View mode toggle
<ViewModeToggle
  viewMode={viewMode}
  onViewModeChange={setViewMode}
/>

// Multi-select filter
<MultiSelectFilter
  options={[
    { value: 'active', label: 'Active', count: 12 },
    { value: 'pending', label: 'Pending', count: 5 },
  ]}
  selectedValues={selectedStatuses}
  onToggle={toggleStatus}
  placeholder="Select status"
/>
```

### Using Filter Context

```tsx
import { FilterProvider, useFilterContext } from '@/components/common/filters';

function MyComponent() {
  const { state, setSearchTerm, toggleStatus, clearFilters } = useFilterContext();

  return (
    <div>
      <SearchBar searchTerm={state.searchTerm} onSearchChange={setSearchTerm} />
      <button onClick={clearFilters}>Clear All</button>
    </div>
  );
}

function App() {
  return (
    <FilterProvider initialState={{ viewMode: 'cards' }}>
      <MyComponent />
    </FilterProvider>
  );
}
```

### Testing Lab Specific Usage

```tsx
import { TestingLabFilterProvider, useTestingLabFilters, TestingLabFilterControls, TestingLabActiveFilters } from '@/components/testing-lab/filters';

function SessionsPage({ sessions }) {
  return (
    <TestingLabFilterProvider sessions={sessions}>
      <TestingLabFilterControls />
      <TestingLabActiveFilters totalCount={sessions.length} />
      <SessionsContent />
    </TestingLabFilterProvider>
  );
}

function SessionsContent() {
  const { filteredSessions, state } = useTestingLabFilters();

  return <SessionContent sessions={filteredSessions} viewMode={state.viewMode} />;
}
```

## 🛠 Filter State Management

### Base Filter State

```typescript
interface BaseFilterState {
  searchTerm: string;
  selectedStatuses: string[];
  selectedTypes: string[];
  selectedPeriod: string;
  viewMode: 'cards' | 'row' | 'table';
}
```

### Available Actions

- `SET_SEARCH_TERM`: Update search term
- `TOGGLE_STATUS`: Toggle status filter
- `TOGGLE_TYPE`: Toggle type filter
- `SET_PERIOD`: Set time period
- `SET_VIEW_MODE`: Change view mode
- `CLEAR_SEARCH`: Clear search term
- `CLEAR_FILTERS`: Clear all filters
- `RESET_STATE`: Reset to initial state

### Helper Methods

```typescript
const { state, setSearchTerm, toggleStatus, toggleType, setPeriod, setViewMode, clearSearch, clearFilters, hasActiveFilters } = useFilterContext();
```

## 📱 Responsive Design

### View Mode Behavior

- **Desktop (xl+)**: All view modes available
- **Tablet/Mobile (lg-)**: Auto-switches to row view
- **View toggle**: Hidden on small screens

### Layout Patterns

- **Desktop**: Single row layout with search, filters, and controls
- **Tablet/Mobile**: Two-row layout for better mobile experience

## ♿ Accessibility

- Proper ARIA labels and roles
- Keyboard navigation support
- Focus management
- Screen reader friendly
- High contrast mode support

## 🎨 Styling Features

- **Tailwind CSS 4**: Modern utility classes
- **Backdrop blur**: Glassmorphism effects
- **Gradient backgrounds**: Visual appeal
- **Smooth transitions**: 200ms duration
- **Responsive breakpoints**: Mobile-first approach

## 🔧 Migration Guide

### From Old Components

```tsx
// OLD: Direct state management
const [searchTerm, setSearchTerm] = useState('');
const [viewMode, setViewMode] = useState('cards');

// NEW: Context-based management
const { state, setSearchTerm, setViewMode } = useTestingLabFilters();
```

### Filter Components

```tsx
// OLD: Custom session search bar
<SessionSearchBar searchTerm={searchTerm} onSearchChange={setSearchTerm} />

// NEW: Reusable search bar
<SearchBar searchTerm={searchTerm} onSearchChange={setSearchTerm} />
```

### View Toggle

```tsx
// OLD: Session-specific view toggle
<SessionViewToggle viewMode={viewMode} onViewModeChange={setViewMode} />

// NEW: Reusable view toggle
<ViewModeToggle viewMode={viewMode} onViewModeChange={setViewMode} />
```

## 🚀 Benefits

1. **Reusability**: Components can be used across different parts of the application
2. **Type Safety**: Full TypeScript support with proper interfaces
3. **Performance**: Better SSR support and client-side optimization
4. **Maintainability**: Clear separation of concerns and modular architecture
5. **Accessibility**: Built-in accessibility features
6. **Consistency**: Standardized filter patterns across the app
7. **Testability**: Easy to unit test individual components and contexts

## 🧪 Testing

### Unit Tests

```tsx
import { render, screen } from '@testing-library/react';
import { FilterProvider } from '@/components/common/filters';
import { SearchBar } from '@/components/common/filters';

test('SearchBar updates search term', () => {
  const handleChange = jest.fn();
  render(
    <FilterProvider>
      <SearchBar searchTerm="" onSearchChange={handleChange} />
    </FilterProvider>,
  );

  // Test implementation
});
```

### Integration Tests

```tsx
test('Filter context manages state correctly', () => {
  // Test filter state management
});
```

## 🔮 Future Enhancements

1. **URL State Sync**: Sync filter state with URL parameters
2. **Saved Filters**: Allow users to save and restore filter presets
3. **Advanced Filters**: Date ranges, custom filters, etc.
4. **Filter Analytics**: Track filter usage for UX improvements
5. **Real-time Updates**: WebSocket integration for live filter updates

## 📚 Related Documentation

- [Next.js 15 App Router](https://nextjs.org/docs/app)
- [React 19 Features](https://react.dev/blog/2024/04/25/react-19)
- [Tailwind CSS 4](https://tailwindcss.com/blog/tailwindcss-v4-alpha)
- [shadcn/ui Components](https://ui.shadcn.com/)
