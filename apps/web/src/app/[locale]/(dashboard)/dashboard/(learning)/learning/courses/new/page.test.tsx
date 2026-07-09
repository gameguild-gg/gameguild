import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import { beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import CreateCoursePage from './page';
import { createCourse, updateCourse } from '@/lib/learning/actions';

const pushMock = vi.fn();

Object.defineProperties(HTMLElement.prototype, {
  hasPointerCapture: { value: vi.fn(() => false) },
  setPointerCapture: { value: vi.fn() },
  releasePointerCapture: { value: vi.fn() },
  scrollIntoView: { value: vi.fn() },
});

beforeAll(() => {
  global.ResizeObserver = class ResizeObserver {
    observe() {}
    unobserve() {}
    disconnect() {}
  };
});

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...props }: { href: string; children: ReactNode }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
  useRouter: () => ({
    push: pushMock,
  }),
}));

vi.mock('@/lib/learning/actions', () => ({
  createCourse: vi.fn(),
  updateCourse: vi.fn(),
}));

describe('CreateCoursePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(createCourse).mockResolvedValue({
      success: true,
      data: {
        id: 'course-1',
        slug: 'boss-ai',
        routeParam: 'boss-ai-by-gameguild',
      },
    });
    vi.mocked(updateCourse).mockResolvedValue({ success: true, data: null });
  });

  it('creates a course through the three-step professor wizard', async () => {
    render(<CreateCoursePage params={Promise.resolve({ locale: 'en-US' })} />);

    expect(screen.getByText('Step 1 of 3: Basics')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /next/i })).toBeDisabled();

    fireEvent.change(screen.getByLabelText(/title/i), { target: { value: 'Boss AI' } });
    fireEvent.change(screen.getByLabelText(/description/i), { target: { value: 'Build production-ready game AI systems.' } });
    expect(screen.getByLabelText(/url slug/i)).toHaveValue('boss-ai');

    fireEvent.click(screen.getByRole('button', { name: /next/i }));
    expect(screen.getByText('Step 2 of 3: Details')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText(/estimated hours/i), { target: { value: '18' } });
    fireEvent.change(screen.getByLabelText(/thumbnail url/i), { target: { value: 'https://cdn.example.com/boss-ai.jpg' } });
    fireEvent.click(screen.getByRole('button', { name: /next/i }));

    expect(screen.getByText('Step 3 of 3: Settings')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText(/max enrollments/i), { target: { value: '0' } });
    fireEvent.change(screen.getByLabelText(/skills required/i), { target: { value: 'C# basics' } });
    fireEvent.change(screen.getByLabelText(/skills provided/i), { target: { value: 'AI behavior trees' } });
    fireEvent.click(screen.getByRole('button', { name: /create course/i }));

    await waitFor(() => {
      expect(createCourse).toHaveBeenCalledWith({
        title: 'Boss AI',
        description: 'Build production-ready game AI systems.',
        slug: 'boss-ai',
      });
    });
    expect(updateCourse).toHaveBeenCalledWith(expect.objectContaining({
      courseId: 'course-1',
      estimatedHours: 18,
      thumbnail: 'https://cdn.example.com/boss-ai.jpg',
      maxEnrollments: null,
      skillsRequired: 'C# basics',
      skillsProvided: 'AI behavior trees',
    }));
    expect(pushMock).toHaveBeenCalledWith('/dashboard/learning/courses/boss-ai-by-gameguild');
  });

  it('shows create errors and keeps the professor on the final step', async () => {
    vi.mocked(createCourse).mockResolvedValueOnce({ success: false, error: 'Slug already exists.' });

    render(<CreateCoursePage params={Promise.resolve({ locale: 'en-US' })} />);

    fireEvent.change(screen.getByLabelText(/title/i), { target: { value: 'Boss AI' } });
    fireEvent.change(screen.getByLabelText(/description/i), { target: { value: 'Build production-ready game AI systems.' } });
    fireEvent.click(screen.getByRole('button', { name: /next/i }));
    fireEvent.click(screen.getByRole('button', { name: /next/i }));
    fireEvent.click(screen.getByRole('button', { name: /create course/i }));

    expect(await screen.findByText('Slug already exists.')).toBeInTheDocument();
    expect(pushMock).not.toHaveBeenCalled();
  });

  it('supports custom slugs, back navigation, finite enrollment caps, and update warnings', async () => {
    const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    vi.mocked(createCourse).mockResolvedValueOnce({
      success: true,
      data: {
        id: 'course-2',
        slug: 'manual-slug-custom',
      },
    });
    vi.mocked(updateCourse).mockResolvedValueOnce({ success: false, error: 'Extended course data failed.' });

    render(<CreateCoursePage params={Promise.resolve({ locale: 'en-US' })} />);

    fireEvent.change(screen.getByLabelText(/title/i), { target: { value: 'Boss AI!' } });
    fireEvent.change(screen.getByLabelText(/description/i), { target: { value: 'Build production-ready game AI systems.' } });
    fireEvent.change(screen.getByLabelText(/url slug/i), { target: { value: 'Manual Slug Custom!' } });
    expect(screen.getByLabelText(/url slug/i)).toHaveValue('manual-slug-custom');

    fireEvent.change(screen.getByLabelText(/title/i), { target: { value: 'Changed Boss AI' } });
    expect(screen.getByLabelText(/url slug/i)).toHaveValue('manual-slug-custom');

    fireEvent.click(screen.getByRole('button', { name: /next/i }));
    expect(screen.getByText('Step 2 of 3: Details')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /^back$/i }));
    expect(screen.getByText('Step 1 of 3: Basics')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /next/i }));
    fireEvent.change(screen.getByLabelText(/video showcase url/i), { target: { value: 'https://video.example/trailer' } });
    fireEvent.click(screen.getByRole('button', { name: /next/i }));

    fireEvent.change(screen.getByLabelText(/max enrollments/i), { target: { value: '12' } });
    fireEvent.click(screen.getByRole('button', { name: /create course/i }));

    await waitFor(() => {
      expect(updateCourse).toHaveBeenCalledWith(expect.objectContaining({
        courseId: 'course-2',
        videoShowcaseUrl: 'https://video.example/trailer',
        maxEnrollments: 12,
        thumbnail: undefined,
        skillsRequired: undefined,
        skillsProvided: undefined,
      }));
    });
    expect(warnSpy).toHaveBeenCalledWith('[CreateCourse] Update failed after creation:', 'Extended course data failed.');
    expect(pushMock).toHaveBeenCalledWith('/dashboard/learning/courses/manual-slug-custom-by-gameguild');

    warnSpy.mockRestore();
  });
});
