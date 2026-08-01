import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { LearnerDashboard } from './learner-dashboard';

const course = {
    id: 'course-1', title: 'Advanced Game AI', slug: 'advanced-game-ai', description: 'Build game intelligence.',
    thumbnail: null, modules: [], overallProgress: 42, totalItems: 12, completedItems: 5,
    currentItem: { id: 'lesson-6', title: 'Behavior trees', type: 'lesson' as const, status: 'available' as const, order: 6, isRequired: true },
    remainingMinutes: 180,
};

describe('LearnerDashboard', () => {
    it('shows enrolled courses, progress, and a deterministic continue action', () => {
        render(<LearnerDashboard learnerName="Ada" courses={[course]} />);
        expect(screen.getByRole('heading', { name: 'Welcome back, Ada' })).toBeInTheDocument();
        expect(screen.getAllByText('Advanced Game AI')).toHaveLength(2);
        expect(screen.getAllByText('42%')).toHaveLength(2);
        expect(screen.getAllByText('Behavior trees')).toHaveLength(2);
        expect(screen.getByRole('link', { name: 'Continue learning' })).toHaveAttribute('href', '/courses/advanced-game-ai/content');
    });

    it('provides a useful catalog action when the learner has no courses', () => {
        render(<LearnerDashboard learnerName="Ada" courses={[]} />);
        expect(screen.getByRole('heading', { name: 'Your learning space is ready' })).toBeInTheDocument();
        expect(screen.getByRole('link', { name: 'Explore the catalog' })).toHaveAttribute('href', '/catalog');
    });
});