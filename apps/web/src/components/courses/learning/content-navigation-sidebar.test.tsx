import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

import { ContentNavigationSidebar, type CourseModule } from './content-navigation-sidebar';

const modules: CourseModule[] = [
  {
    id: 'module-1',
    title: 'Getting Started',
    description: 'First steps',
    order: 1,
    progress: 50,
    items: [
      {
        id: 'lesson-1',
        title: 'Welcome',
        type: 'lesson',
        status: 'completed',
        order: 1,
        progress: 100,
      },
      {
        id: 'lesson-2',
        title: 'Install the tools',
        type: 'lesson',
        status: 'available',
        order: 2,
      },
    ],
  },
  {
    id: 'module-2',
    title: 'Locked Module',
    order: 2,
    isLocked: true,
    progress: 0,
    items: [
      {
        id: 'lesson-3',
        title: 'Advanced topic',
        type: 'lesson',
        status: 'locked',
        order: 1,
      },
    ],
  },
];

describe('ContentNavigationSidebar', () => {
  it('renders modules with progress and current content state', () => {
    render(<ContentNavigationSidebar modules={modules} currentContentId="lesson-2" />);

    expect(screen.getByRole('heading', { name: 'Course content' })).toBeInTheDocument();
    expect(screen.getByText('Getting Started')).toBeInTheDocument();
    expect(screen.getByText('50%')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Install the tools/ })).toHaveAttribute('aria-current', 'step');
    expect(screen.getByText('Completed')).toBeInTheDocument();
    expect(screen.getAllByText('Locked').length).toBeGreaterThan(0);
  });

  it('selects available content and ignores locked content', () => {
    const onContentSelect = vi.fn();
    render(<ContentNavigationSidebar modules={modules} currentContentId="lesson-1" onContentSelect={onContentSelect} />);

    fireEvent.click(screen.getByRole('button', { name: /Install the tools/ }));
    fireEvent.click(screen.getByRole('button', { name: /Advanced topic/ }));

    expect(onContentSelect).toHaveBeenCalledTimes(1);
    expect(onContentSelect).toHaveBeenCalledWith('lesson-2');
  });
});
