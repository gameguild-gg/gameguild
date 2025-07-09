import { describe, it, expect } from '@jest/globals';
import { screen } from '@testing-library/react';
import { Loading, CourseCardSkeleton, PageLoading } from '@/components/ui/loading';
import { render } from '@/test/utils';

describe('Loading Components', () => {
  describe('Loading', () => {
    it('renders with default props', () => {
      render(<Loading />);
      
      expect(screen.getByText('Loading...')).toBeInTheDocument();
      expect(document.querySelector('.lucide-loader-circle')).toBeInTheDocument(); // Loader icon
    });

    it('renders with custom text', () => {
      render(<Loading text="Please wait..." />);
      
      expect(screen.getByText('Please wait...')).toBeInTheDocument();
    });

    it('renders with children instead of text', () => {
      render(
        <Loading text="This should not appear">
          <span>Custom loading content</span>
        </Loading>
      );
      
      expect(screen.getByText('Custom loading content')).toBeInTheDocument();
      expect(screen.queryByText('This should not appear')).not.toBeInTheDocument();
    });

    it('applies custom className', () => {
      const { container } = render(<Loading className="custom-class" />);
      
      expect(container.firstChild).toHaveClass('custom-class');
    });

    it('renders different sizes correctly', () => {
      const { rerender } = render(<Loading size="sm" />);
      expect(document.querySelector('.h-4.w-4')).toBeInTheDocument();

      rerender(<Loading size="md" />);
      expect(document.querySelector('.h-8.w-8')).toBeInTheDocument();

      rerender(<Loading size="lg" />);
      expect(document.querySelector('.h-12.w-12')).toBeInTheDocument();
    });

    it('shows no text when both children and text are empty', () => {
      render(<Loading text="" />);
      
      expect(screen.queryByText(/loading/i)).not.toBeInTheDocument();
    });
  });

  describe('CourseCardSkeleton', () => {
    it('renders skeleton structure', () => {
      const { container } = render(<CourseCardSkeleton />);
      
      expect(container.firstChild).toHaveClass('animate-pulse');
      expect(container.querySelector('.aspect-video')).toBeInTheDocument();
      expect(container.querySelectorAll('.h-4, .h-3')).toHaveLength(3);
    });
  });

  describe('PageLoading', () => {
    it('renders full page loading component', () => {
      render(<PageLoading />);
      
      expect(screen.getByText('Loading course details...')).toBeInTheDocument();
      expect(document.querySelector('.h-12.w-12')).toBeInTheDocument(); // Large size
    });

    it('has full screen styling', () => {
      const { container } = render(<PageLoading />);
      
      expect(container.firstChild).toHaveClass('min-h-screen', 'bg-gray-950');
    });
  });
});
