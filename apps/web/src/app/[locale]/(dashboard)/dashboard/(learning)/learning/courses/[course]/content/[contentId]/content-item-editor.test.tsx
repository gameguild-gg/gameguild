import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ContentItemEditor } from './content-item-editor';
import { updateContent } from '@/lib/learning/actions';
import type { ContentItemDetail } from '@/lib/learning/types';

const routerMocks = vi.hoisted(() => ({
  back: vi.fn(),
  refresh: vi.fn(),
}));

Object.defineProperties(HTMLElement.prototype, {
  hasPointerCapture: { value: vi.fn(() => false) },
  setPointerCapture: { value: vi.fn() },
  releasePointerCapture: { value: vi.fn() },
  scrollIntoView: { value: vi.fn() },
});

global.ResizeObserver = class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
};

vi.mock('next/navigation', () => ({
  useRouter: () => routerMocks,
}));

vi.mock('@/lib/learning/actions', () => ({
  updateContent: vi.fn(),
}));

const item = {
  id: 'content-1',
  parentId: 'module-1',
  order: 1,
  type: 'Questionnaire',
  title: 'Intro quiz',
  description: 'First knowledge check.',
  status: 'published',
  duration: 20,
  metadata: {},
  content: '<p>Answer all questions.</p>',
  settings: { isRequired: true },
  createdAt: '2026-01-01T00:00:00.000Z',
  updatedAt: '2026-01-02T00:00:00.000Z',
} satisfies ContentItemDetail;

describe('ContentItemEditor', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(updateContent).mockResolvedValue({ success: true, data: { id: 'content-1' } });
  });

  it('renders lesson-publication copy and normalizes questionnaire to quiz', () => {
    render(<ContentItemEditor courseId="course-1" item={item} courseTitle="Advanced Game AI" />);

    expect(screen.getByRole('heading', { name: 'Intro quiz' })).toBeInTheDocument();
    expect(screen.getByText('Advanced Game AI')).toBeInTheDocument();
    expect(screen.getByText('Quiz')).toBeInTheDocument();
    expect(screen.getByText('Lesson content')).toBeInTheDocument();
    expect(screen.getByText('Lesson publication')).toBeInTheDocument();
    expect(screen.getByText(/Course marketing copy belongs in Listing/i)).toBeInTheDocument();
    expect(screen.getByText(/Public course landing-page visibility is managed in Listing/i)).toBeInTheDocument();
  });

  it('validates title before updating lesson content', async () => {
    const user = userEvent.setup();
    render(<ContentItemEditor courseId="course-1" item={item} courseTitle="Advanced Game AI" />);

    await user.clear(screen.getByLabelText(/^title$/i));
    await user.click(screen.getByRole('button', { name: /save changes/i }));

    expect(await screen.findByText('Title is required.')).toBeInTheDocument();
    expect(updateContent).not.toHaveBeenCalled();
  });

  it('saves edited lesson content and refreshes the dashboard route', async () => {
    const user = userEvent.setup();
    render(<ContentItemEditor courseId="course-1" item={item} courseTitle="Advanced Game AI" />);

    await user.clear(screen.getByLabelText(/^title$/i));
    await user.type(screen.getByLabelText(/^title$/i), 'Updated quiz');
    fireEvent.change(screen.getByLabelText(/description/i), { target: { value: 'Updated description.' } });
    fireEvent.change(screen.getByLabelText(/^body$/i), { target: { value: '<p>Updated body.</p>' } });
    fireEvent.change(screen.getByLabelText(/estimated minutes/i), { target: { value: '35' } });

    await user.click(screen.getByRole('button', { name: /save changes/i }));

    await waitFor(() => {
      expect(updateContent).toHaveBeenCalledWith({
        courseId: 'course-1',
        contentId: 'content-1',
        title: 'Updated quiz',
        description: 'Updated description.',
        body: '<p>Updated body.</p>',
        visibility: 'Public',
        isRequired: true,
        estimatedMinutes: 35,
      });
    });
    expect(routerMocks.refresh).toHaveBeenCalled();
    expect(screen.getByText('Saved successfully.')).toBeInTheDocument();
  });

  it('shows update errors and routes cancel/back through the dashboard router', async () => {
    const user = userEvent.setup();
    vi.mocked(updateContent).mockResolvedValueOnce({ success: false, error: 'Bad Request' });

    render(<ContentItemEditor courseId="course-1" item={item} courseTitle="Advanced Game AI" />);

    await user.click(screen.getByRole('button', { name: /save changes/i }));
    expect(await screen.findByText('Bad Request')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /^cancel$/i }));
    expect(routerMocks.back).toHaveBeenCalled();
  });
});
