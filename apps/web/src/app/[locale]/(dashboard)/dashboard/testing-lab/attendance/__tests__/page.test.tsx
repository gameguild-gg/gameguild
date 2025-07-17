import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useSession } from 'next-auth/react';
import { testingLabApi } from '@/lib/api/testing-lab/testing-lab-api';
import AttendanceReportsPage from '../page';

// Mock dependencies
jest.mock('next-auth/react');
jest.mock('@/lib/api/testing-lab/testing-lab-api');

const mockUseSession = useSession as jest.MockedFunction<typeof useSession>;
const mockTestingLabApi = testingLabApi as jest.Mocked<typeof testingLabApi>;

describe('AttendanceReportsPage', () => {
  const mockProfessorSession = {
    user: {
      email: 'prof.smith@champlain.edu',
      name: 'Professor Smith',
    },
  };

  const mockStudentSession = {
    user: {
      email: 'john.student@mymail.champlain.edu',
      name: 'John Student',
    },
  };

  const mockAttendanceData = [
    {
      studentId: 's1',
      studentName: 'John Student',
      studentEmail: 'john.student@mymail.champlain.edu',
      totalSessions: 5,
      attendedSessions: 4,
      attendancePercentage: 80,
      currentBlock: 'Block A',
      lastAttendance: '2024-01-15T10:00:00Z',
      missedSessions: [
        {
          sessionId: 'session-3',
          sessionDate: '2024-01-10T14:00:00Z',
          reason: 'Unexcused',
        },
      ],
    },
    {
      studentId: 's2',
      studentName: 'Jane Doe',
      studentEmail: 'jane.doe@mymail.champlain.edu',
      totalSessions: 5,
      attendedSessions: 5,
      attendancePercentage: 100,
      currentBlock: 'Block B',
      lastAttendance: '2024-01-15T15:00:00Z',
      missedSessions: [],
    },
  ];

  beforeEach(() => {
    jest.clearAllMocks();
    mockTestingLabApi.getStudentAttendanceReport.mockResolvedValue(mockAttendanceData);
  });

  describe('Access Control', () => {
    it('should show access denied for students', () => {
      mockUseSession.mockReturnValue({
        data: mockStudentSession,
        status: 'authenticated',
        update: jest.fn(),
      });

      render(<AttendanceReportsPage />);

      expect(screen.getByText(/access denied/i)).toBeInTheDocument();
      expect(screen.getByText(/professors only/i)).toBeInTheDocument();
    });

    it('should show content for professors', () => {
      mockUseSession.mockReturnValue({
        data: mockProfessorSession,
        status: 'authenticated',
        update: jest.fn(),
      });

      render(<AttendanceReportsPage />);

      expect(screen.queryByText(/access denied/i)).not.toBeInTheDocument();
      expect(screen.getByText(/attendance reports/i)).toBeInTheDocument();
    });

    it('should show loading for unauthenticated users', () => {
      mockUseSession.mockReturnValue({
        data: null,
        status: 'loading',
        update: jest.fn(),
      });

      render(<AttendanceReportsPage />);

      expect(screen.getByText(/loading/i)).toBeInTheDocument();
    });
  });

  describe('Attendance Data Display', () => {
    beforeEach(() => {
      mockUseSession.mockReturnValue({
        data: mockProfessorSession,
        status: 'authenticated',
        update: jest.fn(),
      });
    });

    it('should display student attendance summary', async () => {
      render(<AttendanceReportsPage />);

      await waitFor(() => {
        expect(screen.getByText('John Student')).toBeInTheDocument();
        expect(screen.getByText('Jane Doe')).toBeInTheDocument();
      });

      expect(screen.getByText('80%')).toBeInTheDocument();
      expect(screen.getByText('100%')).toBeInTheDocument();
      expect(screen.getByText('Block A')).toBeInTheDocument();
      expect(screen.getByText('Block B')).toBeInTheDocument();
    });

    it('should show attendance statistics', async () => {
      render(<AttendanceReportsPage />);

      await waitFor(() => {
        expect(screen.getByText(/total students: 2/i)).toBeInTheDocument();
        expect(screen.getByText(/average attendance: 90%/i)).toBeInTheDocument();
      });
    });

    it('should display missed sessions for students', async () => {
      const user = userEvent.setup();
      render(<AttendanceReportsPage />);

      await waitFor(() => {
        expect(screen.getByText('John Student')).toBeInTheDocument();
      });

      // Click to expand missed sessions
      const expandButton = screen.getByText(/view missed sessions/i);
      await user.click(expandButton);

      expect(screen.getByText(/session on/i)).toBeInTheDocument();
      expect(screen.getByText(/unexcused/i)).toBeInTheDocument();
    });
  });

  describe('Block Management', () => {
    beforeEach(() => {
      mockUseSession.mockReturnValue({
        data: mockProfessorSession,
        status: 'authenticated',
        update: jest.fn(),
      });
    });

    it('should show block filter options', async () => {
      render(<AttendanceReportsPage />);

      await waitFor(() => {
        expect(screen.getByText(/filter by block/i)).toBeInTheDocument();
      });
    });

    it('should filter students by block', async () => {
      const user = userEvent.setup();
      render(<AttendanceReportsPage />);

      await waitFor(() => {
        expect(screen.getByText('John Student')).toBeInTheDocument();
        expect(screen.getByText('Jane Doe')).toBeInTheDocument();
      });

      // Select Block A filter
      const blockFilter = screen.getByRole('combobox', { name: /block/i });
      await user.click(blockFilter);
      await user.click(screen.getByText('Block A'));

      await waitFor(() => {
        expect(screen.getByText('John Student')).toBeInTheDocument();
        expect(screen.queryByText('Jane Doe')).not.toBeInTheDocument();
      });
    });
  });

  describe('Attendance Updates', () => {
    beforeEach(() => {
      mockUseSession.mockReturnValue({
        data: mockProfessorSession,
        status: 'authenticated',
        update: jest.fn(),
      });
      mockTestingLabApi.updateAttendance.mockResolvedValue({ success: true });
    });

    it('should allow marking students present', async () => {
      const user = userEvent.setup();
      render(<AttendanceReportsPage />);

      await waitFor(() => {
        expect(screen.getByText('John Student')).toBeInTheDocument();
      });

      const markPresentButton = screen.getByText(/mark present/i);
      await user.click(markPresentButton);

      expect(mockTestingLabApi.updateAttendance).toHaveBeenCalledWith({
        studentId: 's1',
        sessionDate: expect.any(String),
        isPresent: true,
        notes: '',
      });
    });

    it('should allow marking students absent with reason', async () => {
      const user = userEvent.setup();
      render(<AttendanceReportsPage />);

      await waitFor(() => {
        expect(screen.getByText('John Student')).toBeInTheDocument();
      });

      const markAbsentButton = screen.getByText(/mark absent/i);
      await user.click(markAbsentButton);

      // Add absence reason
      const reasonInput = screen.getByLabelText(/reason/i);
      await user.type(reasonInput, 'Sick leave');

      const confirmButton = screen.getByText(/confirm/i);
      await user.click(confirmButton);

      expect(mockTestingLabApi.updateAttendance).toHaveBeenCalledWith({
        studentId: 's1',
        sessionDate: expect.any(String),
        isPresent: false,
        notes: 'Sick leave',
      });
    });
  });

  describe('Export Functionality', () => {
    beforeEach(() => {
      mockUseSession.mockReturnValue({
        data: mockProfessorSession,
        status: 'authenticated',
        update: jest.fn(),
      });

      // Mock CSV download
      global.URL.createObjectURL = jest.fn(() => 'mock-url');
      global.URL.revokeObjectURL = jest.fn();

      // Mock link click
      const mockLink = {
        click: jest.fn(),
        href: '',
        download: '',
        style: { display: '' },
      };
      jest.spyOn(document, 'createElement').mockReturnValue(mockLink as any);
      jest.spyOn(document.body, 'appendChild').mockImplementation();
      jest.spyOn(document.body, 'removeChild').mockImplementation();
    });

    it('should export attendance data to CSV', async () => {
      const user = userEvent.setup();
      render(<AttendanceReportsPage />);

      await waitFor(() => {
        expect(screen.getByText(/export csv/i)).toBeInTheDocument();
      });

      const exportButton = screen.getByText(/export csv/i);
      await user.click(exportButton);

      expect(global.URL.createObjectURL).toHaveBeenCalled();
      expect(document.createElement).toHaveBeenCalledWith('a');
    });

    it('should include all attendance data in CSV export', async () => {
      const user = userEvent.setup();
      render(<AttendanceReportsPage />);

      await waitFor(() => {
        expect(screen.getByText(/export csv/i)).toBeInTheDocument();
      });

      const exportButton = screen.getByText(/export csv/i);
      await user.click(exportButton);

      // Verify CSV content creation
      const createObjectURLCall = (global.URL.createObjectURL as jest.Mock).mock.calls[0];
      const blob = createObjectURLCall[0];
      expect(blob.type).toBe('text/csv');
    });
  });

  describe('Real-time Updates', () => {
    beforeEach(() => {
      mockUseSession.mockReturnValue({
        data: mockProfessorSession,
        status: 'authenticated',
        update: jest.fn(),
      });
    });

    it('should refresh data when manually triggered', async () => {
      const user = userEvent.setup();
      render(<AttendanceReportsPage />);

      await waitFor(() => {
        expect(mockTestingLabApi.getStudentAttendanceReport).toHaveBeenCalledTimes(1);
      });

      const refreshButton = screen.getByText(/refresh/i);
      await user.click(refreshButton);

      expect(mockTestingLabApi.getStudentAttendanceReport).toHaveBeenCalledTimes(2);
    });
  });

  describe('Error Handling', () => {
    beforeEach(() => {
      mockUseSession.mockReturnValue({
        data: mockProfessorSession,
        status: 'authenticated',
        update: jest.fn(),
      });
    });

    it('should handle API errors gracefully', async () => {
      mockTestingLabApi.getStudentAttendanceReport.mockRejectedValue(new Error('Failed to fetch attendance data'));

      render(<AttendanceReportsPage />);

      await waitFor(() => {
        expect(screen.getByText(/failed to load attendance data/i)).toBeInTheDocument();
      });
    });

    it('should handle attendance update errors', async () => {
      const user = userEvent.setup();
      mockTestingLabApi.updateAttendance.mockRejectedValue(new Error('Failed to update attendance'));

      render(<AttendanceReportsPage />);

      await waitFor(() => {
        expect(screen.getByText('John Student')).toBeInTheDocument();
      });

      const markPresentButton = screen.getByText(/mark present/i);
      await user.click(markPresentButton);

      await waitFor(() => {
        expect(screen.getByText(/failed to update attendance/i)).toBeInTheDocument();
      });
    });
  });

  describe('Grading Integration', () => {
    beforeEach(() => {
      mockUseSession.mockReturnValue({
        data: mockProfessorSession,
        status: 'authenticated',
        update: jest.fn(),
      });
    });

    it('should show attendance grade calculation', async () => {
      render(<AttendanceReportsPage />);

      await waitFor(() => {
        expect(screen.getByText(/grade calculation/i)).toBeInTheDocument();
      });

      // Should show attendance percentage as potential grade
      expect(screen.getByText(/80% → B-/i)).toBeInTheDocument();
      expect(screen.getByText(/100% → A/i)).toBeInTheDocument();
    });

    it('should highlight students at risk', async () => {
      const attendanceDataWithRisk = [
        {
          ...mockAttendanceData[0],
          attendancePercentage: 60, // Below threshold
        },
        ...mockAttendanceData.slice(1),
      ];

      mockTestingLabApi.getStudentAttendanceReport.mockResolvedValue(attendanceDataWithRisk);

      render(<AttendanceReportsPage />);

      await waitFor(() => {
        expect(screen.getByText(/at risk/i)).toBeInTheDocument();
      });
    });
  });
});
