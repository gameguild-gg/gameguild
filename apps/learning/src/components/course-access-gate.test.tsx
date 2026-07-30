import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mockEnrollInCourse = vi.fn();
const mockRefresh = vi.fn();
vi.mock('@/lib/learner-actions', () => ({ enrollInCourse: (...args: unknown[]) => mockEnrollInCourse(...args) }));
vi.mock('next/navigation', () => ({ useRouter: () => ({ refresh: mockRefresh }) }));
const { CourseAccessGate } = await import('./course-access-gate');

const course = {
    id: 'course-1', slug: 'intro-to-rpg', title: 'Intro to RPG', description: 'Build role-playing systems.',
    thumbnail: null, category: 'Game Development', difficulty: 'Beginner', estimatedHours: 12,
    currentEnrollments: 10, averageRating: 4.8, isEnrollmentOpen: true,
};

describe('CourseAccessGate', () => {
    beforeEach(() => { vi.clearAllMocks(); mockEnrollInCourse.mockResolvedValue({ success: true }); });

    it('enrolls the current learner in a free course and refreshes access', async () => {
        render(<CourseAccessGate access={{ kind: 'enrollment-required', course }} />);
        fireEvent.click(screen.getByRole('button', { name: 'Enroll for free' }));
        await waitFor(() => expect(mockEnrollInCourse).toHaveBeenCalledWith('course-1'));
        expect(await screen.findByText('Enrollment confirmed')).toBeInTheDocument();
        expect(mockRefresh).toHaveBeenCalled();
    });

    it('sends paid courses to the public storefront checkout', () => {
        render(<CourseAccessGate access={{ kind: 'payment-required', course, price: 49, currency: 'USD' }} />);
        expect(screen.getByRole('link', { name: 'Continue to checkout' })).toHaveAttribute('href', 'http://localhost:3000/courses/intro-to-rpg');
        expect(screen.getByText('$49.00')).toBeInTheDocument();
    });

    it('explains when enrollment is closed without presenting an enrollment action', () => {
        render(<CourseAccessGate access={{ kind: 'enrollment-closed', course }} />);
        expect(screen.getByRole('heading', { name: 'Enrollment is closed' })).toBeInTheDocument();
        expect(screen.queryByRole('button', { name: 'Enroll for free' })).not.toBeInTheDocument();
    });
});