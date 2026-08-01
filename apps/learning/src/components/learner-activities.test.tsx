import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { LearnerActivities } from './learner-activities';

const course = {
    id: 'course-1', title: 'Game Production', slug: 'game-production', description: '', thumbnail: null,
    overallProgress: 40, totalItems: 3, completedItems: 1, remainingMinutes: 60, enrollmentId: 'enrollment-1',
    modules: [{ id: 'module-1', title: 'Production', description: '', order: 0, progress: 33, items: [
        { id: 'reflection-1', title: 'Production reflection', type: 'activity', contentType: 'Reflection', status: 'available', order: 1, isRequired: true },
        { id: 'survey-1', title: 'Module survey', type: 'activity', contentType: 'Survey', status: 'completed', order: 2, isRequired: false },
    ] }],
} as const;

const context = {
    enrollmentId: 'enrollment-1', cohort: null, calendar: [], discussions: [], certificates: [],
    assessments: [
        { id: 'quiz-1', courseId: 'course-1', title: 'Knowledge check', type: 'Quiz', maxScore: 10, passingScore: 7, isAvailable: true, dueAt: '2026-08-01T12:00:00Z', submissionModalities: 'StructuredAnswer' },
        { id: 'project-1', courseId: 'course-1', title: 'Playable build', type: 'Project', maxScore: 100, passingScore: 70, isAvailable: true, submissionModalities: 'Project' },
    ],
    submissions: [{ id: 'submission-1', assessmentId: 'quiz-1', enrollmentId: 'enrollment-1', status: 'Graded', score: 9, passed: true }],
} as const;

describe('LearnerActivities', () => {
    it('lists every graded and participatory activity with persisted status', () => {
        render(<LearnerActivities course={course} context={context} />);
        expect(screen.getByRole('heading', { name: 'Knowledge check' })).toBeInTheDocument();
        expect(screen.getByText('9 / 10')).toBeInTheDocument();
        expect(screen.getByRole('heading', { name: 'Playable build' })).toBeInTheDocument();
        expect(screen.getByRole('heading', { name: 'Production reflection' })).toBeInTheDocument();
        expect(screen.getByRole('heading', { name: 'Module survey' })).toBeInTheDocument();
        expect(screen.getByText('Completed')).toBeInTheDocument();
    });
});