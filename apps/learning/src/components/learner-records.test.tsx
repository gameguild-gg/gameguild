import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { LearnerCalendar, LearnerCertificates, LearnerGradebook } from './learner-records';

const records = [{
    course: { id: 'course-1', title: 'Game Production', slug: 'game-production', description: '', thumbnail: null, modules: [], overallProgress: 50, totalItems: 2, completedItems: 1, remainingMinutes: 30, enrollmentId: 'enrollment-1' },
    context: {
        enrollmentId: 'enrollment-1', cohort: { id: 'cohort-1', name: 'Evening cohort' }, discussions: [], certificates: [],
        calendar: [{ itemId: 'class-1', title: 'Live critique', startsAt: '2026-08-03T22:00:00Z', endsAt: '2026-08-03T23:00:00Z', itemType: 'LiveSession' }],
        assessments: [{ id: 'assessment-1', title: 'Playable build', maxScore: 100, dueAt: '2026-08-05T22:00:00Z', assessmentGroupName: 'Final project' }],
        submissions: [{ id: 'submission-1', assessmentId: 'assessment-1', status: 'Graded', score: 88, passed: true, feedback: 'Strong iteration and testing evidence.' }],
    },
}] as never;

describe('learner record views', () => {
    it('renders cohort events and assessment deadlines in one calendar', () => {
        render(<LearnerCalendar records={records} />);
        expect(screen.getByText('Live critique')).toBeInTheDocument();
        expect(screen.getByText('Playable build')).toBeInTheDocument();
        expect(screen.getByText(/Evening cohort/)).toBeInTheDocument();
    });

    it('renders grades and instructor feedback from submissions', () => {
        render(<LearnerGradebook records={records} />);
        expect(screen.getByText('88 / 100')).toBeInTheDocument();
        expect(screen.getByText('Strong iteration and testing evidence.')).toBeInTheDocument();
        expect(screen.getByText('Final project')).toBeInTheDocument();
    });

    it('renders issued credentials and an honest empty state', () => {
        const { rerender } = render(<LearnerCertificates certificates={[]} />);
        expect(screen.getByText('No certificates issued yet')).toBeInTheDocument();
        rerender(<LearnerCertificates certificates={[{ id: 'certificate-1', courseId: 'course-1', courseName: 'Game Production', certificateNumber: 'GG-2026-001', issuedAt: '2026-08-10T12:00:00Z', status: 'Active' }]} />);
        expect(screen.getByText('GG-2026-001')).toBeInTheDocument();
        expect(screen.getByText('Game Production')).toBeInTheDocument();
    });
});