# Course List Components

A comprehensive set of components for displaying and filtering courses with a modern, responsive interface similar to the testing lab sessions.

## Components

### CourseList

The main component that displays a filterable list of courses with multiple view modes.

#### Props

- `courses: Course[]` - Array of course objects to display
- `onEdit?: (course: Course) => void` - Optional callback when editing a course
- `onView?: (course: Course) => void` - Optional callback when viewing a course
- `onEnroll?: (course: Course) => void` - Optional callback when enrolling in a course
- `initialViewMode?: 'cards' | 'row' | 'table'` - Initial view mode (default: 'cards')
- `hideViewToggle?: boolean` - Whether to hide the view mode toggle (default: false)

#### Features

- **Search**: Full-text search across title, description, area, and tools
- **Status Filter**: Filter by course status (draft, published, archived)
- **Area Filter**: Filter by course area (programming, art, design, audio)
- **Level Filter**: Filter by course level (1-4: Beginner, Intermediate, Advanced, Arcane)
- **View Modes**: Cards, row, and table views
- **Responsive**: Adapts to different screen sizes
- **Clear Filters**: Easy way to reset all filters

### CourseCard

Individual course card component displaying course information in an attractive format.

#### Props

- `course: Course` - Course object to display
- `onEdit?: (course: Course) => void` - Optional callback when editing
- `onView?: (course: Course) => void` - Optional callback when viewing
- `onEnroll?: (course: Course) => void` - Optional callback when enrolling

#### Features

- **Status Indicators**: Color-coded badges for course status
- **Level Display**: Visual level indicators with appropriate colors
- **Analytics**: Shows enrollment count, duration, rating
- **Tools Display**: Shows up to 3 tools with overflow indicator
- **Action Buttons**: Edit, View, and Enroll actions based on props

### Filter Components

#### CourseFilterControls

Main filter control container that organizes all filters in a responsive layout.

#### CourseSearchBar

Search input with clear functionality and visual feedback.

#### CourseStatusFilter

Dropdown filter for course status with icons and multi-select capability.

#### CourseAreaFilter

Dropdown filter for course areas with color-coded icons.

#### CourseLevelFilter

Dropdown filter for course levels with difficulty indicators.

## Usage Examples

### Basic Usage

```tsx
import { CourseList } from '@/components/courses/common';

function CoursesPage() {
  const courses = []; // Your courses data

  return (
    <CourseList
      courses={courses}
      onView={(course) => router.push(`/courses/${course.slug}`)}
      onEnroll={(course) => handleEnrollment(course)}
    />
  );
}
```

### With All Callbacks

```tsx
import { CourseList } from '@/components/courses/common';

function CourseManagement() {
  const courses = []; // Your courses data

  return (
    <CourseList
      courses={courses}
      onEdit={(course) => router.push(`/admin/courses/${course.id}/edit`)}
      onView={(course) => router.push(`/courses/${course.slug}`)}
      onEnroll={(course) => handleEnrollment(course)}
      initialViewMode="table"
    />
  );
}
```

### Demo Component

A complete demo is available in `CourseListDemo` which includes mock data and shows all features.

## Styling

The components use Tailwind CSS with a dark theme consistent with the Game Guild design system:

- **Dark Background**: slate-950/900 color scheme
- **Glassmorphism**: Backdrop blur effects with semi-transparent backgrounds
- **Color Coding**: 
  - Blue for general actions and search
  - Purple for areas
  - Orange for levels
  - Green/Yellow/Red for status indicators
- **Smooth Animations**: Hover states and transitions

## Type Safety

All components are fully typed using TypeScript with the following key types:

- `Course` - Main course interface
- `CourseArea` - 'programming' | 'art' | 'design' | 'audio'
- `CourseLevel` - 1 | 2 | 3 | 4
- `CourseStatus` - 'draft' | 'published' | 'archived'

## Accessibility

- **Keyboard Navigation**: All interactive elements are keyboard accessible
- **Screen Reader Support**: Proper ARIA labels and semantic HTML
- **Color Contrast**: High contrast ratios for text readability
- **Focus Management**: Clear focus indicators

## Performance

- **Memoization**: Filtered results are memoized for optimal performance
- **Lazy Loading**: Components can be easily extended to support pagination
- **Optimized Rendering**: Efficient re-rendering with proper React patterns
