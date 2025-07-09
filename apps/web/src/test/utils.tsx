import { render, type RenderOptions } from '@testing-library/react';
import { ReactElement } from 'react';
import { CourseProvider } from '@/contexts/course-context';
import { CourseData } from '@/types/courses';

// Mock course data for testing
export const mockCourseData: CourseData = {
  courses: [
    {
      id: '1',
      title: 'Test Course',
      description: 'A test course for testing purposes',
      area: 'programming',
      level: 1,
      tools: ['Unity', 'C#'],
      image: '/test-image.jpg',
      slug: 'test-course',
      progress: 50,
    },
    {
      id: '2',
      title: 'Advanced Test Course',
      description: 'An advanced test course',
      area: 'art',
      level: 3,
      tools: ['Blender', 'Photoshop'],
      image: '/test-image-2.jpg',
      slug: 'advanced-test-course',
      progress: 75,
    },
  ],
  toolsByArea: {
    programming: ['Unity', 'C#', 'JavaScript'],
    art: ['Blender', 'Photoshop', 'Maya'],
    design: ['Figma', 'Sketch'],
    audio: ['Audacity', 'Pro Tools'],
  },
};

// Custom render with providers
interface AllTheProvidersProps {
  children: React.ReactNode;
  courseData?: CourseData;
}

const AllTheProviders = ({ children, courseData = mockCourseData }: AllTheProvidersProps) => {
  return <CourseProvider initialData={courseData}>{children}</CourseProvider>;
};

const customRender = (ui: ReactElement, options?: Omit<RenderOptions, 'wrapper'> & { courseData?: CourseData }) => {
  const { courseData, ...renderOptions } = options ?? {};
  
  return render(ui, {
    wrapper: ({ children }) => <AllTheProviders courseData={courseData}>{children}</AllTheProviders>,
    ...renderOptions,
  });
};

export * from '@testing-library/react';
export { customRender as render };
