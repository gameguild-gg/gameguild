import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ProjectCarouselEditorForm } from './project-carousel-editor-form';

const updateCourseLandingProjectsMock = vi.fn();

vi.mock('@/lib/learning/actions', () => ({
  updateCourseLandingProjects: (...args: unknown[]) => updateCourseLandingProjectsMock(...args),
}));

describe('ProjectCarouselEditorForm', () => {
  beforeEach(() => {
    updateCourseLandingProjectsMock.mockReset();
    updateCourseLandingProjectsMock.mockResolvedValue({ success: true, data: null });
  });

  it('submits edited project carousel items to the course metadata action', async () => {
    const user = userEvent.setup();

    render(
      <ProjectCarouselEditorForm
        courseId="course-1"
        items={[
          {
            id: 'course-1-project-1',
            courseId: 'course-1',
            title: 'Boss behavior sandbox',
            summary: 'Students build a readable boss encounter with inspectable AI states.',
            image: 'https://example.com/boss-sandbox.jpg',
            skills: ['State debugging', 'Combat pacing'],
            deliverable: 'A playable boss encounter with annotated decision logic.',
            moduleLabel: 'Project A',
            order: 1,
            createdAt: '2026-01-01T00:00:00.000Z',
            updatedAt: '2026-01-02T00:00:00.000Z',
          },
        ]}
      />,
    );

    await user.clear(screen.getByLabelText('Project title 1'));
    await user.type(screen.getByLabelText('Project title 1'), 'Arena puzzle prototype');
    await user.clear(screen.getByLabelText('Skills 1'));
    await user.type(screen.getByLabelText('Skills 1'), 'Spatial puzzle, Playtest notes');

    await user.click(screen.getByRole('button', { name: /save project carousel/i }));

    await waitFor(() => {
      expect(updateCourseLandingProjectsMock).toHaveBeenCalledWith('course-1', [
        expect.objectContaining({
          title: 'Arena puzzle prototype',
          skills: 'Spatial puzzle, Playtest notes',
          deliverable: 'A playable boss encounter with annotated decision logic.',
        }),
      ]);
    });
    expect(screen.getByText('Project carousel updated successfully.')).toBeInTheDocument();
  }, 15_000);

  it('supports empty project setup, add/remove flows, and API errors', async () => {
    const user = userEvent.setup();
    updateCourseLandingProjectsMock.mockResolvedValueOnce({ success: false, error: 'Project carousel is invalid.' });

    render(<ProjectCarouselEditorForm courseId="course-1" items={[]} />);

    expect(screen.getByText('Project 1')).toBeInTheDocument();
    expect(screen.getByLabelText('Module label 1')).toHaveValue('Project 01');

    await user.click(screen.getByRole('button', { name: /add project/i }));
    expect(screen.getByText('Project 2')).toBeInTheDocument();
    expect(screen.getByLabelText('Module label 2')).toHaveValue('Project 02');

    await user.click(screen.getByRole('button', { name: /remove project 2/i }));
    expect(screen.queryByText('Project 2')).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /remove project 1/i }));
    expect(screen.getByText('Project 1')).toBeInTheDocument();
    expect(screen.getByLabelText('Project title 1')).toHaveValue('');

    await user.type(screen.getByLabelText('Project title 1'), 'Portfolio milestone');
    await user.type(screen.getByLabelText('Summary 1'), 'A portfolio-ready production milestone.');
    await user.type(screen.getByLabelText('Deliverable 1'), 'A playable build with a short retrospective.');
    await user.click(screen.getByRole('button', { name: /save project carousel/i }));

    await waitFor(() => {
      expect(updateCourseLandingProjectsMock).toHaveBeenCalledWith('course-1', [
        expect.objectContaining({
          title: 'Portfolio milestone',
          moduleLabel: 'Project 01',
        }),
      ]);
    });
    expect(screen.getByText('Project carousel is invalid.')).toBeInTheDocument();
  }, 15_000);

  it('rejects partially completed project slides before calling the API', async () => {
    const user = userEvent.setup();
    render(<ProjectCarouselEditorForm courseId="course-1" items={[]} />);

    await user.type(screen.getByLabelText('Project title 1'), 'Incomplete milestone');
    await user.click(screen.getByRole('button', { name: /save project carousel/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Complete the title, summary, and deliverable for Project 1.');
    expect(updateCourseLandingProjectsMock).not.toHaveBeenCalled();
  });
  it('allows consecutive saves after editing and removing a project', async () => {
    render(<ProjectCarouselEditorForm courseId="course-1" items={[]} />);

    fireEvent.change(screen.getByLabelText('Project title 1'), { target: { value: 'First milestone' } });
    fireEvent.change(screen.getByLabelText('Summary 1'), { target: { value: 'First milestone summary.' } });
    fireEvent.change(screen.getByLabelText('Deliverable 1'), { target: { value: 'First milestone deliverable.' } });
    fireEvent.click(screen.getByRole('button', { name: /save project carousel/i }));
    await waitFor(() => expect(screen.getByRole('button', { name: /save project carousel/i })).toBeEnabled());

    fireEvent.click(screen.getByRole('button', { name: /add project/i }));
    fireEvent.change(screen.getByLabelText('Project title 2'), { target: { value: 'Temporary milestone' } });
    fireEvent.change(screen.getByLabelText('Summary 2'), { target: { value: 'Temporary milestone summary.' } });
    fireEvent.change(screen.getByLabelText('Deliverable 2'), { target: { value: 'Temporary milestone deliverable.' } });
    fireEvent.click(screen.getByRole('button', { name: /save project carousel/i }));
    await waitFor(() => expect(screen.getByRole('button', { name: /save project carousel/i })).toBeEnabled());

    fireEvent.click(screen.getByRole('button', { name: /remove project 2/i }));
    fireEvent.click(screen.getByRole('button', { name: /save project carousel/i }));

    await waitFor(() => expect(updateCourseLandingProjectsMock).toHaveBeenCalledTimes(3));
    expect(updateCourseLandingProjectsMock).toHaveBeenLastCalledWith('course-1', [expect.objectContaining({ title: 'First milestone' })]);
    expect(screen.getAllByLabelText(/Project title/)).toHaveLength(1);
    expect(screen.getByRole('button', { name: /save project carousel/i })).toBeEnabled();
  });
});
