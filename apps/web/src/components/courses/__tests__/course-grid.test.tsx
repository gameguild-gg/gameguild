import { describe, it, expect, jest, beforeEach } from '@jest/globals';
import { fireEvent, screen } from '@testing-library/react';
import { CourseGrid } from '@/components/courses/course-grid';
import { render, mockCourseData } from '@/test/utils';

// Mock the useRouter hook
const mockPush = jest.fn();
jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: mockPush,
  }),
}));

// Mock the course context hook
const mockUseFilteredCourses = jest.fn();
jest.mock('@/hooks/use-courses', () => ({
  useFilteredCourses: () => mockUseFilteredCourses(),
}));

describe('CourseGrid', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    // Set default mock return value
    mockUseFilteredCourses.mockReturnValue(mockCourseData.courses);
  });

  it('renders course cards', () => {
    render(<CourseGrid />);

    expect(screen.getByText('Test Course')).toBeInTheDocument();
    expect(screen.getByText('Advanced Test Course')).toBeInTheDocument();
  });

  it('navigates to course detail when card is clicked', () => {
    render(<CourseGrid />);

    // Find the clickable card using a more specific selector
    const firstCourseCard = screen.getByText('Test Course').closest('[data-slot="card"]');
    expect(firstCourseCard).toBeInTheDocument();

    if (firstCourseCard) {
      fireEvent.click(firstCourseCard);
      expect(mockPush).toHaveBeenCalledWith('/course/test-course');
    }
  });

  it('shows empty state when no courses are available', () => {
    // Mock empty courses for this test
    mockUseFilteredCourses.mockReturnValue([]);

    render(<CourseGrid />);

    // Should show empty state component
    expect(screen.getByText('No courses found')).toBeInTheDocument();
  });

  it('displays course information correctly', () => {
    render(<CourseGrid />);

    // Check first course
    expect(screen.getByText('Test Course')).toBeInTheDocument();
    expect(screen.getByText('A test course for testing purposes')).toBeInTheDocument();

    // Check second course
    expect(screen.getByText('Advanced Test Course')).toBeInTheDocument();
    expect(screen.getByText('An advanced test course')).toBeInTheDocument();
  });

  it('uses course slugs for navigation', () => {
    render(<CourseGrid />);

    // Find course cards by their clickable elements
    const firstCourseCard = screen.getByText('Test Course').closest('[data-slot="card"]');
    const secondCourseCard = screen.getByText('Advanced Test Course').closest('[data-slot="card"]');

    // Click on first course
    if (firstCourseCard) {
      fireEvent.click(firstCourseCard);
      expect(mockPush).toHaveBeenCalledWith('/course/test-course');
    }

    jest.clearAllMocks(); // Clear previous calls

    // Click on second course
    if (secondCourseCard) {
      fireEvent.click(secondCourseCard);
      expect(mockPush).toHaveBeenCalledWith('/course/advanced-test-course');
    }
  });
});
