import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { CourseLearnerOverview } from './course-learner-overview';

const course = {
    id: 'course-1', title: 'Advanced Game AI', slug: 'advanced-game-ai', description: 'Build game intelligence.',
    thumbnail: null, modules: [], overallProgress: 42, totalItems: 12, completedItems: 5,
    currentItem: { id: 'lesson-6', title: 'Behavior trees', type: 'lesson' as const, status: 'available' as const, order: 6, isRequired: true },
    remainingMinutes: 180, enrollmentId: 'enrollment-1',
};

const context = {
    enrollmentId: 'enrollment-1',
    cohort: { id: 'cohort-1', name: 'Evening cohort', startDate: '2026-08-03T00:00:00Z', endDate: '2026-10-03T00:00:00Z', instructorId: 'teacher-1' },
    calendar: [{ itemId: 'event-1', title: 'Live kickoff', type: 'LiveSession' as const, startsAt: '2026-08-03T22:00:00Z', endsAt: '2026-08-03T23:00:00Z', cohortId: 'cohort-1', cohortName: 'Evening cohort', status: 'Published' as const }],
    assessments: [{ id: 'assessment-1', courseId: 'course-1', title: 'AI behavior quiz', type: 'Quiz' as const, dueAt: '2026-08-05T23:59:00Z', maxScore: 10 }],
    submissions: [], discussions: [], certificates: [],
};

describe('CourseLearnerOverview', () => {
    it('shows cohort, next event, deadline, progress, and learner actions', () => {
        render(<CourseLearnerOverview course={course} context={context} />);
        expect(screen.getByRole('heading', { name: 'Advanced Game AI' })).toBeInTheDocument();
        expect(screen.getByText('Evening cohort')).toBeInTheDocument();
        expect(screen.getByText('Live kickoff')).toBeInTheDocument();
        expect(screen.getByText('AI behavior quiz')).toBeInTheDocument();
        expect(screen.getByText('42%')).toBeInTheDocument();
        expect(screen.getByRole('link', { name: 'Continue learning' })).toHaveAttribute('href', '/courses/advanced-game-ai/content');
        expect(screen.getByRole('link', { name: 'View assignments' })).toHaveAttribute('href', '/courses/advanced-game-ai/assignments');
        expect(screen.getByRole('link', { name: 'Open community' })).toHaveAttribute('href', '/courses/advanced-game-ai/community');
    });
});