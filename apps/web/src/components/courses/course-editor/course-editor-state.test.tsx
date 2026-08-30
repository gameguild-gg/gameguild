import { fireEvent, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useEffect, useRef } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { CourseEditorProvider } from '../editor/context/course-editor-provider';
import { useCourseEditor } from '../editor/context/course-editor-provider';
import { createCourse, saveCourse } from '../editor/actions';
import { CourseEditorSidebar } from '../editor/ui/course-editor-sidebar';
import { SidebarProvider } from '@/components/ui/sidebar';
import { CourseEditor } from './course-editor';
import { GeneralDetailsSection } from './sections/general-details-section';
import { SalesShowcaseSection } from './sections/sales-showcase-section';
import { ContentStructureSection } from './sections/content-structure-section';

vi.mock('../editor/actions', () => ({
  createCourse: vi.fn(),
  saveCourse: vi.fn(),
}));

vi.mock('next/navigation', () => ({
  useParams: () => ({ course: 'course-123' }),
  usePathname: () => '/workspace/learning/courses/course-123',
}));

function ValidCourseState() {
  const { updateTitle, updateSummary, updateDescription, updateCategory } = useCourseEditor();
  const seeded = useRef(false);

  useEffect(() => {
    if (seeded.current) return;
    seeded.current = true;

    updateTitle('Launch Ready Gameplay');
    updateSummary('A complete production course.');
    updateDescription('Learn how to prepare and publish a game project.');
    updateCategory('GameDevelopment');
  }, [updateCategory, updateDescription, updateSummary, updateTitle]);

  return null;
}

describe('course editor state-backed sections', () => {
  beforeEach(() => {
    vi.mocked(createCourse).mockReset();
    vi.mocked(saveCourse).mockReset();
  });

  it('updates general details and auto-generates the slug until manually edited', () => {
    render(
      <CourseEditorProvider>
        <GeneralDetailsSection />
      </CourseEditorProvider>,
    );

    fireEvent.change(screen.getByLabelText(/course title/i), {
      target: { value: 'Advanced Gameplay Systems' },
    });

    expect(screen.getByLabelText(/course url slug/i)).toHaveValue('advanced-gameplay-systems');

    fireEvent.change(screen.getByLabelText(/course url slug/i), {
      target: { value: 'custom-gameplay-path' },
    });
    fireEvent.change(screen.getByLabelText(/course title/i), {
      target: { value: 'Advanced Gameplay Systems Updated' },
    });

    expect(screen.getByLabelText(/course url slug/i)).toHaveValue('custom-gameplay-path');
  });

  it('persists product and tag additions in the sales showcase section', () => {
    render(
      <CourseEditorProvider>
        <SalesShowcaseSection />
      </CourseEditorProvider>,
    );

    fireEvent.change(screen.getByPlaceholderText(/product name/i), {
      target: { value: 'Studio Cohort' },
    });
    fireEvent.change(screen.getByPlaceholderText(/price/i), {
      target: { value: '249' },
    });
    fireEvent.click(screen.getByRole('button', { name: /add product/i }));

    expect(screen.getByText('Studio Cohort')).toBeInTheDocument();
    expect(screen.getByText(/\$249 USD/)).toBeInTheDocument();

    fireEvent.change(screen.getByPlaceholderText(/add a tag/i), {
      target: { value: 'portfolio' },
    });
    fireEvent.click(screen.getByRole('button', { name: /add tag/i }));

    expect(screen.getByText('portfolio')).toBeInTheDocument();
  });

  it('adds modules and lessons through the content structure dialogs', () => {
    render(
      <CourseEditorProvider>
        <ContentStructureSection />
      </CourseEditorProvider>,
    );

    fireEvent.click(screen.getByRole('button', { name: /add first module/i }));
    fireEvent.change(screen.getByPlaceholderText(/module title/i), {
      target: { value: 'Prototype Fundamentals' },
    });
    fireEvent.click(screen.getByRole('button', { name: /^add module$/i }));

    expect(screen.getByText('Prototype Fundamentals')).toBeInTheDocument();

    const moduleCard = screen.getByText('Prototype Fundamentals').closest('.border')!;
    fireEvent.click(within(moduleCard).getByRole('button', { name: /add lesson/i }));
    fireEvent.change(screen.getByPlaceholderText(/lesson title/i), {
      target: { value: 'Build the First Loop' },
    });
    fireEvent.click(screen.getByRole('button', { name: /^add lesson$/i }));

    expect(screen.getByText('Build the First Loop')).toBeInTheDocument();
  });

  it('shows validation errors when saving an incomplete course', async () => {
    render(
      <CourseEditorProvider>
        <CourseEditor isCreating />
      </CourseEditorProvider>,
    );

    await userEvent.click(screen.getByRole('button', { name: /create course/i }));

    expect(screen.getAllByText(/course title is required/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/course summary is required/i).length).toBeGreaterThan(0);
    expect(createCourse).not.toHaveBeenCalled();
    expect(saveCourse).not.toHaveBeenCalled();
  });

  it('submits valid create state through the course editor action', async () => {
    vi.mocked(createCourse).mockResolvedValueOnce({
      id: 'course-123',
      title: 'Launch Ready Gameplay',
      slug: 'launch-ready-gameplay',
      description: 'Learn how to prepare and publish a game project.',
      level: 'Beginner',
    });

    render(
      <CourseEditorProvider>
        <ValidCourseState />
        <CourseEditor isCreating />
      </CourseEditorProvider>,
    );

    await userEvent.click(screen.getByRole('button', { name: /create course/i }));

    expect(createCourse).toHaveBeenCalledWith(
      expect.objectContaining({
        title: 'Launch Ready Gameplay',
        slug: 'launch-ready-gameplay',
        description: 'Learn how to prepare and publish a game project.',
        area: 'GameDevelopment',
      }),
    );
    expect(await screen.findByText(/course created/i)).toBeInTheDocument();
  });

  it('saves valid state from the course editor sidebar action area', async () => {
    vi.mocked(saveCourse).mockResolvedValueOnce(true);

    render(
      <CourseEditorProvider>
        <ValidCourseState />
        <SidebarProvider>
          <CourseEditorSidebar />
        </SidebarProvider>
      </CourseEditorProvider>,
    );

    await userEvent.click(screen.getByRole('button', { name: /save changes/i }));

    expect(saveCourse).toHaveBeenCalledWith(
      expect.objectContaining({
        id: 'course-123',
        title: 'Launch Ready Gameplay',
        slug: 'launch-ready-gameplay',
      }),
    );
    expect(await screen.findByText(/course saved/i)).toBeInTheDocument();
  });
});
