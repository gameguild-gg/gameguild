# Enhanced Course Provider

A modern, comprehensive course management system built with React's useReducer pattern. This provider offers advanced filtering, search capabilities, optimistic updates, persistent state management, and more.

## Features

### 🎯 Core Features
- **Modern React Pattern**: Built with useReducer for predictable state management
- **TypeScript Support**: Fully typed with comprehensive interfaces
- **Advanced Filtering**: Multi-dimensional filtering by area, level, status, instructor, tags, and tools
- **Search**: Real-time search across course titles, descriptions, and metadata
- **Pagination**: Configurable pagination with multiple view modes (grid, list, table)
- **Selection Management**: Multi-select capabilities with bulk operations
- **Optimistic Updates**: Immediate UI feedback with rollback capabilities
- **Persistent State**: localStorage integration for filter and view preferences

### 🔄 Sync & Performance
- **Real-time Sync**: Configurable sync intervals and status tracking
- **Debounced Search**: Optimized search with configurable debounce timing
- **Error Handling**: Comprehensive error states and recovery
- **Loading States**: Granular loading indicators
- **Background Sync**: Non-blocking sync operations

### 🛠️ Developer Experience
- **Easy Setup**: Simple provider configuration with sensible defaults
- **Hook-based API**: Clean, composable hooks for different use cases
- **Extensible**: Easy to extend with additional features
- **Well-documented**: Comprehensive type definitions and JSDoc comments

## Quick Start

### Basic Setup

```tsx
import { EnhancedCourseProvider, useEnhancedCourses } from '@/lib/courses';

function App() {
  return (
    <EnhancedCourseProvider>
      <CourseList />
    </EnhancedCourseProvider>
  );
}

function CourseList() {
  const { state, setCourses, setSearch, setAreaFilter } = useEnhancedCourses();
  
  return (
    <div>
      <input 
        type="text" 
        placeholder="Search courses..."
        onChange={(e) => setSearch(e.target.value)}
      />
      <select onChange={(e) => setAreaFilter(e.target.value)}>
        <option value="all">All Areas</option>
        <option value="programming">Programming</option>
        <option value="art">Art</option>
        <option value="design">Design</option>
        <option value="audio">Audio</option>
      </select>
      
      {state.filteredCourses.map(course => (
        <div key={course.id}>
          <h3>{course.title}</h3>
          <p>{course.description}</p>
        </div>
      ))}
    </div>
  );
}
```

### Advanced Configuration

```tsx
import { EnhancedCourseProvider, createBasicCourseConfig } from '@/lib/courses';

const config = createBasicCourseConfig({
  persistFilters: true,
  debounceSearch: 500,
  defaultPageSize: 20,
  autoSync: true,
  syncInterval: 60000,
});

function App() {
  return (
    <EnhancedCourseProvider 
      initialState={{ config }}
      onStateChange={(state) => console.log('State changed:', state)}
    >
      <CourseApp />
    </EnhancedCourseProvider>
  );
}
```

## API Reference

### EnhancedCourseProvider Props

```tsx
interface EnhancedCourseProviderProps {
  children: ReactNode;
  initialState?: Partial<EnhancedCourseState>;
  onStateChange?: (state: EnhancedCourseState) => void;
}
```

### useEnhancedCourses Hook

The main hook provides access to all course management functionality:

#### State
- `state`: Complete course state including courses, filters, pagination, and UI state

#### Basic Operations
- `setCourses(courses, totalCount?)`: Set the course list
- `addCourse(course)`: Add a new course
- `updateCourse(course)`: Update an existing course
- `deleteCourse(courseId)`: Delete a course

#### Filtering
- `setSearch(query)`: Set search query
- `setAreaFilter(area)`: Filter by course area
- `setLevelFilter(level)`: Filter by difficulty level
- `setStatusFilter(status)`: Filter by course status
- `setInstructorFilter(instructor)`: Filter by instructor
- `setTagFilter(tag)`: Filter by tags
- `setSort(sortBy, sortOrder)`: Set sorting
- `resetFilters()`: Clear all filters

#### Selection
- `setSelectedCourses(ids)`: Set selected course IDs
- `toggleCourseSelection(id)`: Toggle course selection
- `selectAllCourses()`: Select all filtered courses
- `clearSelection()`: Clear selection
- `getSelectedCoursesData()`: Get selected course objects
- `hasSelection()`: Check if any courses are selected
- `isAllSelected()`: Check if all filtered courses are selected

#### Pagination
- `setPage(page)`: Set current page
- `setPageSize(size)`: Set page size
- `setPagination(config)`: Set pagination config

#### View Management
- `setViewMode(mode)`: Set view mode (grid, list, table)
- `toggleViewMode()`: Cycle through view modes

#### Loading & Errors
- `setLoading(loading)`: Set loading state
- `setError(error)`: Set error message
- `clearError()`: Clear error

#### Sync Operations
- `setSyncStatus(status)`: Set sync status
- `setLastSyncTime(time)`: Set last sync time

#### Optimistic Updates
- `addOptimisticUpdate(id, course)`: Add optimistic update
- `removeOptimisticUpdate(id)`: Remove optimistic update
- `clearOptimisticUpdates()`: Clear all optimistic updates

#### Configuration
- `updateConfig(config)`: Update provider configuration
- `resetState()`: Reset to initial state

#### Persistence
- `persistState()`: Manually save state to localStorage
- `clearPersistedState()`: Clear saved state

## Course Object Structure

```tsx
interface EnhancedCourse {
  id: string;
  title: string;
  description: string;
  area: CourseArea; // 'programming' | 'art' | 'design' | 'audio'
  level: CourseLevel; // 1 | 2 | 3 | 4
  difficulty: number; // 1-10 scale
  status: CourseStatus; // 'draft' | 'published' | 'archived'
  tools: string[];
  tags?: string[];
  instructors?: string[];
  price?: number;
  currency?: string;
  duration?: number; // in minutes
  enrollmentCount?: number;
  isPublic: boolean;
  isFeatured: boolean;
  createdAt?: string;
  updatedAt?: string;
  content?: CourseContent;
  analytics?: CourseAnalytics;
}
```

## Migration from Legacy Provider

If you're migrating from the legacy CourseProvider, here's a comparison:

### Legacy (Deprecated)
```tsx
import { CourseProvider, useCourseContext } from '@/lib/courses';

// Old way
const { courses, filters, setFilters } = useCourseContext();
```

### Enhanced (Recommended)
```tsx
import { EnhancedCourseProvider, useEnhancedCourses } from '@/lib/courses';

// New way
const { state, setSearch, setAreaFilter } = useEnhancedCourses();
```

## Configuration Options

```tsx
interface CourseConfig {
  persistFilters?: boolean; // Save filters to localStorage
  persistViewMode?: boolean; // Save view mode to localStorage
  autoSync?: boolean; // Enable automatic syncing
  syncInterval?: number; // Sync interval in milliseconds
  optimisticUpdates?: boolean; // Enable optimistic updates
  debounceSearch?: number; // Search debounce in milliseconds
  defaultPageSize?: number; // Default pagination size
  maxPageSize?: number; // Maximum pagination size
  enableSelection?: boolean; // Enable course selection
  enableBulkOperations?: boolean; // Enable bulk operations
  enableOptimisticUpdates?: boolean; // Enable optimistic updates
  enablePersistence?: boolean; // Enable state persistence
}
```

## Examples

### Course Dashboard with Filters

```tsx
function CourseDashboard() {
  const {
    state,
    setSearch,
    setAreaFilter,
    setLevelFilter,
    setViewMode,
    toggleCourseSelection,
    selectAllCourses,
    clearSelection,
    hasSelection,
    isAllSelected,
  } = useEnhancedCourses();

  return (
    <div>
      {/* Search and Filters */}
      <div className="filters">
        <input
          type="text"
          placeholder="Search courses..."
          value={state.filters.search}
          onChange={(e) => setSearch(e.target.value)}
        />
        
        <select 
          value={state.filters.area} 
          onChange={(e) => setAreaFilter(e.target.value)}
        >
          <option value="all">All Areas</option>
          <option value="programming">Programming</option>
          <option value="art">Art</option>
          <option value="design">Design</option>
          <option value="audio">Audio</option>
        </select>

        <select 
          value={state.filters.level} 
          onChange={(e) => setLevelFilter(e.target.value)}
        >
          <option value="all">All Levels</option>
          <option value="Beginner">Beginner</option>
          <option value="Intermediate">Intermediate</option>
          <option value="Advanced">Advanced</option>
          <option value="Arcane">Arcane</option>
        </select>
      </div>

      {/* View Controls */}
      <div className="controls">
        <button onClick={() => setViewMode('grid')}>Grid</button>
        <button onClick={() => setViewMode('list')}>List</button>
        <button onClick={() => setViewMode('table')}>Table</button>
        
        {hasSelection() && (
          <div className="selection-controls">
            <span>{state.selectedCourses.length} selected</span>
            <button onClick={clearSelection}>Clear</button>
            <button onClick={selectAllCourses}>
              {isAllSelected() ? 'Deselect All' : 'Select All'}
            </button>
          </div>
        )}
      </div>

      {/* Course List */}
      <div className={`courses ${state.viewMode}`}>
        {state.filteredCourses.map(course => (
          <div
            key={course.id}
            className={`course ${state.selectedCourses.includes(course.id) ? 'selected' : ''}`}
            onClick={() => toggleCourseSelection(course.id)}
          >
            <h3>{course.title}</h3>
            <p>{course.description}</p>
            <div className="meta">
              <span className="area">{course.area}</span>
              <span className="level">Level {course.level}</span>
              <span className="status">{course.status}</span>
            </div>
          </div>
        ))}
      </div>

      {/* Pagination */}
      <div className="pagination">
        <span>
          {state.filteredCourses.length} of {state.totalCount} courses
        </span>
      </div>
    </div>
  );
}
```

## Best Practices

1. **Use the Enhanced Provider**: The new `EnhancedCourseProvider` offers better performance and more features than the legacy provider.

2. **Configure Appropriately**: Set up the provider configuration based on your needs. Enable persistence for better UX, configure debouncing for search optimization.

3. **Handle Loading States**: Always handle loading and error states in your UI for better user experience.

4. **Leverage Optimistic Updates**: Use optimistic updates for immediate feedback on user actions.

5. **Implement Proper Error Handling**: Use the error handling capabilities to provide meaningful feedback to users.

6. **Use Selection Wisely**: The selection system is powerful for bulk operations, but don't overuse it where it's not needed.

## Performance Considerations

- The provider uses React's `useReducer` for efficient state management
- Filters are applied efficiently with memoized calculations
- Search is debounced to prevent excessive filtering
- Optimistic updates provide immediate feedback without blocking the UI
- State persistence only saves essential data to localStorage

## TypeScript Support

This library is built with TypeScript and provides comprehensive type definitions. All interfaces are exported for use in your applications.

## Troubleshooting

### Common Issues

1. **Context Not Found Error**: Ensure your component is wrapped in `EnhancedCourseProvider`
2. **Filters Not Persisting**: Check that `persistFilters` is enabled in configuration
3. **Search Not Working**: Verify that the search term matches course properties
4. **Types Not Available**: Make sure you're importing types from the correct path

### Debug Mode

Enable debug logging by setting the configuration:

```tsx
const config = {
  debug: true, // This will log state changes to console
};
```

## Contributing

When contributing to this provider:

1. Maintain TypeScript strict mode compliance
2. Add comprehensive tests for new features
3. Update this README with any new functionality
4. Follow the established patterns for action creators and reducers

## Future Roadmap

- [ ] Virtual scrolling for large course lists
- [ ] Advanced search with operators (AND, OR, NOT)
- [ ] Undo/redo functionality
- [ ] Real-time collaborative filtering
- [ ] Performance analytics and monitoring
- [ ] Advanced caching strategies
