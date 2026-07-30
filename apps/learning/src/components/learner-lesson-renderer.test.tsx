import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { LearnerLessonRenderer } from './learner-lesson-renderer';

vi.mock('@/lib/lesson-interaction-actions', () => ({ recordLessonEvent: vi.fn().mockResolvedValue({ success: true }) }));

describe('LearnerLessonRenderer', () => {
    it('renders Markdown lessons', () => {
        render(<LearnerLessonRenderer courseId="course-1" enrollmentId="enrollment-1" itemId="lesson-1" format="Markdown" content={{ markdown: '# Pathfinding\nBuild a nav mesh.' }} />);
        expect(screen.getByRole('heading', { name: 'Pathfinding' })).toBeInTheDocument();
        expect(screen.getByText('Build a nav mesh.')).toBeInTheDocument();
    });

    it('renders persisted Lexical content without exposing JSON', () => {
        render(<LearnerLessonRenderer courseId="course-1" enrollmentId="enrollment-1" itemId="lesson-1" format="Lexical" content={{ root: { type: 'root', children: [{ type: 'heading', tag: 'h2', children: [{ type: 'text', text: 'Behavior trees' }] }, { type: 'paragraph', children: [{ type: 'text', text: 'Choose the next action.' }] }] } }} />);
        expect(screen.getByRole('heading', { name: 'Behavior trees' })).toBeInTheDocument();
        expect(screen.getByText('Choose the next action.')).toBeInTheDocument();
        expect(screen.queryByText(/"root"/)).not.toBeInTheDocument();
    });

    it('renders Reveal slides with explicit navigation', () => {
        render(<LearnerLessonRenderer courseId="course-1" enrollmentId="enrollment-1" itemId="lesson-1" format="RevealJs" content={{ markdown: '# First slide\n---\n# Second slide' }} />);
        expect(screen.getByRole('heading', { name: 'First slide' })).toBeInTheDocument();
        fireEvent.click(screen.getByRole('button', { name: 'Next slide' }));
        expect(screen.getByRole('heading', { name: 'Second slide' })).toBeInTheDocument();
        expect(screen.getByText('2 / 2')).toBeInTheDocument();
    });

    it('renders a tracked video lesson', () => {
        render(<LearnerLessonRenderer courseId="course-1" enrollmentId="enrollment-1" itemId="lesson-1" format="Video" content={{ videoUrl: 'https://cdn.example.com/lesson.mp4' }} />);
        const video = screen.getByLabelText('Video lesson');
        expect(video).toHaveAttribute('src', 'https://cdn.example.com/lesson.mp4');
    });
});