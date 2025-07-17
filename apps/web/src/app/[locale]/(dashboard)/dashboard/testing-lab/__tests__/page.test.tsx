import React from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { useSession } from 'next-auth/react';
import { testingLabApi } from '@/lib/api/testing-lab/testing-lab-api';
import TestingLabPage from '../page';

// Mock dependencies
jest.mock('next-auth/react');
jest.mock('@/lib/api/testing-lab/testing-lab-api');
jest.mock('next/link', () => {
  return ({ children, href }: { children: React.ReactNode; href: string }) => <a href={href}>{children}</a>;
});

const mockUseSession = useSession as jest.MockedFunction<typeof useSession>;
const mockTestingLabApi = testingLabApi as jest.Mocked<typeof testingLabApi>;

describe('TestingLabPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  const mockStudentSession = {
    user: {
      email: 'john.student@mymail.champlain.edu',
      name: 'John Student',
    },
  };

  const mockProfessorSession = {
    user: {
      email: 'jane.prof@champlain.edu',
      name: 'Jane Professor',
    },
  };

  const mockTestingRequests = [
    {
      id: '1',
      title: 'Alpha Build Testing - Team 01',
      description: 'Testing the core gameplay mechanics for our RPG game',
      projectVersionId: 'pv1',
      downloadUrl: 'https://drive.google.com/file/d/example123/view',
      instructionsType: 'inline' as const,
      instructionsContent: 'Test the game thoroughly...',
      feedbackFormContent: 'Please rate the game...',
      maxTesters: 8,
      currentTesterCount: 3,
      startDate: '2024-01-15',
      endDate: '2024-01-22',
      status: 'open' as const,
      createdBy: {
        id: 'u1',
        name: 'John Developer',
        email: 'john.dev@mymail.champlain.edu',
      },
      projectVersion: {
        id: 'pv1',
        versionNumber: 'v0.1.0-alpha',
        project: {
          id: 'p1',
          title: 'fa23-capstone-2023-24-t01',
        },
      },
    },
  ];

  const mockTestingSessions = [
    {
      id: 's1',
      sessionName: 'Block 1 Testing Session',
      sessionDate: '2024-01-18',
      startTime: '10:00',
      endTime: '12:00',
      maxTesters: 16,
      registeredTesterCount: 12,
      status: 'scheduled' as const,
      location: {
        id: 'l1',
        name: 'Room A101',
        capacity: 16,
      },
    },
  ];

  describe('Role-based Authentication', () => {
    it('should identify students by @mymail.champlain.edu domain', async () => {
      mockUseSession.mockReturnValue({
        data: mockStudentSession,
        status: 'authenticated',
        update: jest.fn(),
      });

      mockTestingLabApi.getAvailableTestingRequests.mockResolvedValue(mockTestingRequests);
      mockTestingLabApi.getTestingSessions.mockResolvedValue(mockTestingSessions);

      render(<TestingLabPage />);

      await waitFor(() => {
        expect(screen.getByText('Submit Version')).toBeInTheDocument();
        expect(screen.queryByText('Create Session')).not.toBeInTheDocument();
        expect(screen.queryByText('Attendance Reports')).not.toBeInTheDocument();
      });
    });

    it('should identify professors by @champlain.edu domain', async () => {
      mockUseSession.mockReturnValue({
        data: mockProfessorSession,
        status: 'authenticated',
        update: jest.fn(),
      });

      mockTestingLabApi.getAvailableTestingRequests.mockResolvedValue(mockTestingRequests);
      mockTestingLabApi.getTestingSessions.mockResolvedValue(mockTestingSessions);

      render(<TestingLabPage />);

      await waitFor(() => {
        expect(screen.getByText('Submit Version')).toBeInTheDocument();
        expect(screen.getByText('Create Session')).toBeInTheDocument();
        expect(screen.getByText('Attendance Reports')).toBeInTheDocument();
      });
    });
  });

  describe('Data Loading', () => {
    beforeEach(() => {
      mockUseSession.mockReturnValue({
        data: mockStudentSession,
        status: 'authenticated',
        update: jest.fn(),
      });
    });

    it('should display loading state initially', () => {
      mockTestingLabApi.getAvailableTestingRequests.mockImplementation(
        () => new Promise(() => {}), // Never resolves
      );
      mockTestingLabApi.getTestingSessions.mockImplementation(
        () => new Promise(() => {}), // Never resolves
      );

      render(<TestingLabPage />);

      expect(screen.getByText('Loading Testing Lab...')).toBeInTheDocument();
    });

    it('should display testing requests when data loads successfully', async () => {
      mockTestingLabApi.getAvailableTestingRequests.mockResolvedValue(mockTestingRequests);
      mockTestingLabApi.getTestingSessions.mockResolvedValue(mockTestingSessions);

      render(<TestingLabPage />);

      await waitFor(() => {
        expect(screen.getByText('Alpha Build Testing - Team 01')).toBeInTheDocument();
        expect(screen.getByText('fa23-capstone-2023-24-t01 • v0.1.0-alpha')).toBeInTheDocument();
        expect(screen.getByText('3/8 testers')).toBeInTheDocument();
      });
    });

    it('should fallback to mock data when API fails', async () => {
      mockTestingLabApi.getAvailableTestingRequests.mockRejectedValue(new Error('API Error'));
      mockTestingLabApi.getTestingSessions.mockRejectedValue(new Error('API Error'));

      render(<TestingLabPage />);

      await waitFor(() => {
        expect(screen.getByText('Alpha Build Testing - Team 01')).toBeInTheDocument();
      });
    });
  });

  describe('Testing Requests Tab', () => {
    beforeEach(() => {
      mockUseSession.mockReturnValue({
        data: mockStudentSession,
        status: 'authenticated',
        update: jest.fn(),
      });
      mockTestingLabApi.getAvailableTestingRequests.mockResolvedValue(mockTestingRequests);
      mockTestingLabApi.getTestingSessions.mockResolvedValue(mockTestingSessions);
    });

    it('should display testing request details correctly', async () => {
      render(<TestingLabPage />);

      await waitFor(() => {
        expect(screen.getByText('Alpha Build Testing - Team 01')).toBeInTheDocument();
        expect(screen.getByText('Testing the core gameplay mechanics for our RPG game')).toBeInTheDocument();
        expect(screen.getByText('John Developer')).toBeInTheDocument();
      });
    });

    it('should show correct status badge', async () => {
      render(<TestingLabPage />);

      await waitFor(() => {
        const statusBadge = screen.getByText('open');
        expect(statusBadge).toBeInTheDocument();
        expect(statusBadge.closest('*')).toHaveClass('bg-gray-100', 'text-gray-800');
      });
    });

    it('should display empty state when no requests available', async () => {
      mockTestingLabApi.getAvailableTestingRequests.mockResolvedValue([]);

      render(<TestingLabPage />);

      await waitFor(() => {
        expect(screen.getByText('No Testing Requests')).toBeInTheDocument();
        expect(screen.getByText('No active testing requests at the moment.')).toBeInTheDocument();
      });
    });
  });

  describe('Testing Sessions Tab', () => {
    beforeEach(() => {
      mockUseSession.mockReturnValue({
        data: mockStudentSession,
        status: 'authenticated',
        update: jest.fn(),
      });
      mockTestingLabApi.getAvailableTestingRequests.mockResolvedValue(mockTestingRequests);
      mockTestingLabApi.getTestingSessions.mockResolvedValue(mockTestingSessions);
    });

    it('should switch to sessions tab when clicked', async () => {
      render(<TestingLabPage />);

      await waitFor(() => {
        const sessionsTab = screen.getByText('Testing Sessions');
        fireEvent.click(sessionsTab);
      });

      expect(screen.getByText('Block 1 Testing Session')).toBeInTheDocument();
      expect(screen.getByText('Room A101 - 16 capacity')).toBeInTheDocument();
    });

    it('should display session attendance information', async () => {
      render(<TestingLabPage />);

      await waitFor(() => {
        const sessionsTab = screen.getByText('Testing Sessions');
        fireEvent.click(sessionsTab);
      });

      await waitFor(() => {
        expect(screen.getByText('12/16 registered')).toBeInTheDocument();
        expect(screen.getByText('10:00 - 12:00')).toBeInTheDocument();
      });
    });
  });

  describe('Professor-only Features', () => {
    beforeEach(() => {
      mockUseSession.mockReturnValue({
        data: mockProfessorSession,
        status: 'authenticated',
        update: jest.fn(),
      });
      mockTestingLabApi.getAvailableTestingRequests.mockResolvedValue(mockTestingRequests);
      mockTestingLabApi.getTestingSessions.mockResolvedValue(mockTestingSessions);
    });

    it('should show attendance reports tab for professors', async () => {
      render(<TestingLabPage />);

      await waitFor(() => {
        expect(screen.getByText('Attendance Reports')).toBeInTheDocument();
      });
    });

    it('should display attendance tracking information', async () => {
      render(<TestingLabPage />);

      await waitFor(() => {
        const attendanceTab = screen.getByText('Attendance Reports');
        fireEvent.click(attendanceTab);
      });

      await waitFor(() => {
        expect(screen.getByText('Attendance Tracking')).toBeInTheDocument();
        expect(screen.getByText('Track student attendance and testing participation for grading purposes')).toBeInTheDocument();
      });
    });
  });

  describe('Navigation Links', () => {
    beforeEach(() => {
      mockUseSession.mockReturnValue({
        data: mockStudentSession,
        status: 'authenticated',
        update: jest.fn(),
      });
      mockTestingLabApi.getAvailableTestingRequests.mockResolvedValue(mockTestingRequests);
      mockTestingLabApi.getTestingSessions.mockResolvedValue(mockTestingSessions);
    });

    it('should have correct navigation links for testing requests', async () => {
      render(<TestingLabPage />);

      await waitFor(() => {
        const testNowLink = screen.getByText('Test Now');
        const viewDetailsLink = screen.getByText('View Details');

        expect(testNowLink.closest('a')).toHaveAttribute('href', '/dashboard/testing-lab/requests/1');
        expect(viewDetailsLink.closest('a')).toHaveAttribute('href', '/dashboard/testing-lab/requests/1/details');
      });
    });

    it('should have submit version link', async () => {
      render(<TestingLabPage />);

      await waitFor(() => {
        const submitLink = screen.getByText('Submit Version');
        expect(submitLink.closest('a')).toHaveAttribute('href', '/dashboard/testing-lab/submit');
      });
    });
  });
});
