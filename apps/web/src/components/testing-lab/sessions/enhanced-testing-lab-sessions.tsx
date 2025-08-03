'use client';

import React, { useMemo, useState } from 'react';
import { Edit, Eye, Play, Trash2, Users } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { EnhancedTestingLabFilters, TestingLabSession } from '../filters/enhanced-testing-lab-filter-controls';
import { ActionConfig, CardConfig, ColumnConfig, GenericCardView, GenericRowView, GenericTableView } from '../../common/data-display/generic-data-views';
import { useEnhancedFilterContext } from '../../common/filters/enhanced-filter-context';

// Mock data for demonstration
const mockSessions: TestingLabSession[] = [
  {
    id: '1',
    title: 'React Hooks Testing Lab',
    description: 'Learn and test React hooks with hands-on exercises',
    status: 'open',
    type: 'student-testing',
    category: 'frontend',
    difficulty: 'intermediate',
    participantCount: 5,
    maxParticipants: 20,
    startDate: '2024-01-15',
    endDate: '2024-01-30',
    instructor: 'Jane Smith',
    tags: ['react', 'javascript', 'testing'],
    technologies: ['react', 'javascript'],
  },
  {
    id: '2',
    title: 'API Development Best Practices',
    description: 'Backend API development with Node.js and Express',
    status: 'in-progress',
    type: 'peer-review',
    category: 'backend',
    difficulty: 'advanced',
    participantCount: 15,
    maxParticipants: 15,
    startDate: '2024-01-10',
    endDate: '2024-01-25',
    instructor: 'John Doe',
    tags: ['nodejs', 'api', 'database'],
    technologies: ['express', 'postgresql'],
  },
  {
    id: '3',
    title: 'Full Stack Mobile App Testing',
    description: 'End-to-end testing for mobile applications',
    status: 'open',
    type: 'faculty-review',
    category: 'fullstack',
    difficulty: 'beginner',
    participantCount: 3,
    maxParticipants: 25,
    startDate: '2024-02-01',
    endDate: '2024-02-28',
    instructor: 'Alice Johnson',
    tags: ['mobile', 'testing', 'react'],
    technologies: ['react', 'react-native'],
  },
];

// Component that handles the filtered data display
function SessionsDataDisplay() {
  const { state, filterItems } = useEnhancedFilterContext<TestingLabSession>();
  const [sortKey, setSortKey] = useState<keyof TestingLabSession>('title');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');

  const filteredSessions = useMemo(() => {
    return filterItems(mockSessions);
  }, [filterItems]);

  const handleSort = (key: keyof TestingLabSession) => {
    if (sortKey === key) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
    } else {
      setSortKey(key);
      setSortDirection('asc');
    }
  };

  // Action configurations
  const actions: ActionConfig<TestingLabSession>[] = [
    {
      key: 'view',
      label: 'View',
      icon: <Eye className="h-3 w-3" />,
      variant: 'outline',
      onClick: (session) => console.log('View session:', session.id),
    },
    {
      key: 'join',
      label: 'Join',
      icon: <Play className="h-3 w-3" />,
      variant: 'default',
      onClick: (session) => console.log('Join session:', session.id),
      disabled: (session) => session.status === 'full' || session.status === 'completed',
      hidden: (session) => session.status === 'completed',
    },
    {
      key: 'edit',
      label: 'Edit',
      icon: <Edit className="h-3 w-3" />,
      variant: 'secondary',
      onClick: (session) => console.log('Edit session:', session.id),
    },
    {
      key: 'delete',
      label: 'Delete',
      icon: <Trash2 className="h-3 w-3" />,
      variant: 'destructive',
      onClick: (session) => console.log('Delete session:', session.id),
    },
  ];

  // Card configuration
  const cardConfig: CardConfig<TestingLabSession> = {
    titleKey: 'title',
    descriptionKey: 'description',
    badgeKey: 'status',
    metaFields: [
      {
        key: 'difficulty',
        label: 'Difficulty',
        render: (value) => <Badge variant={value === 'beginner' ? 'secondary' : value === 'intermediate' ? 'default' : 'destructive'}>{String(value)}</Badge>,
      },
      {
        key: 'participantCount',
        label: 'Participants',
        render: (value, item) => `${value}/${item.maxParticipants}`,
      },
      {
        key: 'instructor',
        label: 'Instructor',
      },
      {
        key: 'category',
        label: 'Category',
        render: (value) => <Badge variant="outline">{String(value)}</Badge>,
      },
    ],
    actions,
  };

  // Table column configurations
  const columns: ColumnConfig<TestingLabSession>[] = [
    {
      key: 'title',
      label: 'Title',
      sortable: true,
      width: '200px',
    },
    {
      key: 'status',
      label: 'Status',
      sortable: true,
      render: (value) => <Badge variant={value === 'open' ? 'default' : value === 'in-progress' ? 'secondary' : value === 'full' ? 'destructive' : 'outline'}>{String(value)}</Badge>,
    },
    {
      key: 'category',
      label: 'Category',
      sortable: true,
      render: (value) => <Badge variant="outline">{String(value)}</Badge>,
    },
    {
      key: 'difficulty',
      label: 'Difficulty',
      sortable: true,
      render: (value) => <Badge variant={value === 'beginner' ? 'secondary' : value === 'intermediate' ? 'default' : 'destructive'}>{String(value)}</Badge>,
    },
    {
      key: 'participantCount',
      label: 'Participants',
      sortable: true,
      render: (value, item) => (
        <div className="flex items-center gap-1">
          <Users className="h-3 w-3" />
          <span>
            {value}/{item.maxParticipants}
          </span>
        </div>
      ),
    },
    {
      key: 'instructor',
      label: 'Instructor',
      sortable: true,
    },
    {
      key: 'startDate',
      label: 'Start Date',
      sortable: true,
      render: (value) => new Date(String(value)).toLocaleDateString(),
    },
  ];

  const handleItemClick = (session: TestingLabSession) => {
    console.log('Session clicked:', session.id);
  };

  // Render based on view mode
  switch (state.viewMode) {
    case 'cards':
      return <GenericCardView items={filteredSessions} config={cardConfig} onItemClick={handleItemClick} emptyMessage="No testing lab sessions found matching your filters." />;

    case 'table':
      return (
        <GenericTableView
          items={filteredSessions}
          columns={columns}
          actions={actions}
          onItemClick={handleItemClick}
          sortKey={sortKey}
          sortDirection={sortDirection}
          onSort={handleSort}
          emptyMessage="No testing lab sessions found matching your filters."
        />
      );

    case 'row':
      return <GenericRowView items={filteredSessions} config={cardConfig} onItemClick={handleItemClick} emptyMessage="No testing lab sessions found matching your filters." />;

    default:
      return <GenericCardView items={filteredSessions} config={cardConfig} onItemClick={handleItemClick} emptyMessage="No testing lab sessions found matching your filters." />;
  }
}

// Main Testing Lab Sessions Component
export function EnhancedTestingLabSessions() {
  return (
    <div className="container mx-auto py-6 space-y-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-bold tracking-tight">Testing Lab Sessions</h1>
        <p className="text-muted-foreground">Discover and join testing lab sessions to improve your skills and collaborate with peers.</p>
      </div>

      <EnhancedTestingLabFilters>
        <SessionsDataDisplay />
      </EnhancedTestingLabFilters>
    </div>
  );
}
