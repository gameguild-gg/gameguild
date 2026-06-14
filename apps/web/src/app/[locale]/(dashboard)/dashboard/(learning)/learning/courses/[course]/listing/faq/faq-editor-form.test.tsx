import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { FaqEditorForm } from './faq-editor-form';

const updateCourseFaqMock = vi.fn();

vi.mock('@/lib/learning/actions', () => ({
  updateCourseFaq: (...args: unknown[]) => updateCourseFaqMock(...args),
}));

describe('FaqEditorForm', () => {
  beforeEach(() => {
    updateCourseFaqMock.mockReset();
    updateCourseFaqMock.mockResolvedValue({ success: true, data: null });
  });

  it('submits edited FAQ items to the course metadata action', async () => {
    render(
      <FaqEditorForm
        courseId="course-1"
        items={[
          {
            id: 'course-1-faq-1',
            courseId: 'course-1',
            question: 'Who is this course for?',
            answer: 'Intermediate game developers.',
            category: 'Course details',
            order: 1,
            createdAt: '2026-01-01T00:00:00.000Z',
            updatedAt: '2026-01-02T00:00:00.000Z',
          },
        ]}
      />,
    );

    fireEvent.change(screen.getByLabelText(/^question$/i), {
      target: { value: 'What should students know first?' },
    });
    fireEvent.change(screen.getByLabelText(/^answer$/i), {
      target: { value: 'Students should be comfortable with Unity scenes and C# scripts.' },
    });
    fireEvent.change(screen.getByLabelText(/^category$/i), { target: { value: 'Prerequisites' } });

    fireEvent.click(screen.getByRole('button', { name: /save faq/i }));

    await waitFor(() => {
      expect(updateCourseFaqMock).toHaveBeenCalledWith('course-1', [
        expect.objectContaining({
          id: 'course-1-faq-1',
          question: 'What should students know first?',
          answer: 'Students should be comfortable with Unity scenes and C# scripts.',
          category: 'Prerequisites',
        }),
      ]);
    });
    expect(screen.getByText('FAQ updated successfully.')).toBeInTheDocument();
  });
});
