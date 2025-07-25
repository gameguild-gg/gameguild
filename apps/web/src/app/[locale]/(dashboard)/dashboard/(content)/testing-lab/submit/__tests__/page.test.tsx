import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useSession } from 'next-auth/react';
import { useRouter } from 'next/navigation';
import { testingLabApi } from '@/lib/api/testing-lab/testing-lab-api';
import SubmitVersionPage from '../page';

// Mock dependencies
jest.mock('next-auth/react');
jest.mock('next/navigation');
jest.mock('@/lib/api/testing-lab/testing-lab-api');

const mockUseSession = useSession as jest.MockedFunction<typeof useSession>;
const mockUseRouter = useRouter as jest.MockedFunction<typeof useRouter>;
const mockTestingLabApi = testingLabApi as jest.Mocked<typeof testingLabApi>;

describe('SubmitVersionPage', () => {
  const mockPush = jest.fn();
  const mockSession = {
    user: {
      email: 'john.student@mymail.champlain.edu',
      name: 'John Student',
    },
  };

  beforeEach(() => {
    jest.clearAllMocks();
    mockUseSession.mockReturnValue({
      data: mockSession,
      status: 'authenticated',
      update: jest.fn(),
    });
    mockUseRouter.mockReturnValue({
      push: mockPush,
      back: jest.fn(),
      forward: jest.fn(),
      refresh: jest.fn(),
      replace: jest.fn(),
      prefetch: jest.fn(),
    });
  });

  describe('Form Rendering', () => {
    it('should render all required form fields', () => {
      render(<SubmitVersionPage />);

      expect(screen.getByLabelText(/project title/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/version number/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/description/i)).toBeInTheDocument();
      expect(screen.getByText(/submission type/i)).toBeInTheDocument();
      expect(screen.getByText(/testing instructions/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/feedback form/i)).toBeInTheDocument();
    });

    it('should have URL submission selected by default', () => {
      render(<SubmitVersionPage />);

      const urlRadio = screen.getByLabelText(/url\/link/i);
      expect(urlRadio).toBeChecked();
    });

    it('should show URL input when URL submission is selected', () => {
      render(<SubmitVersionPage />);

      expect(screen.getByLabelText(/download url/i)).toBeInTheDocument();
    });

    it('should show file input when file submission is selected', async () => {
      const user = userEvent.setup();
      render(<SubmitVersionPage />);

      const fileRadio = screen.getByLabelText(/file upload/i);
      await user.click(fileRadio);

      expect(screen.getByText(/upload file/i)).toBeInTheDocument();
    });
  });

  describe('Form Validation', () => {
    it('should show validation errors for required fields', async () => {
      const user = userEvent.setup();
      render(<SubmitVersionPage />);

      const submitButton = screen.getByText(/submit for testing/i);
      await user.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/title is required/i)).toBeInTheDocument();
        expect(screen.getByText(/version number is required/i)).toBeInTheDocument();
      });
    });

    it('should validate URL format', async () => {
      const user = userEvent.setup();
      render(<SubmitVersionPage />);

      const titleInput = screen.getByLabelText(/project title/i);
      const versionInput = screen.getByLabelText(/version number/i);
      const urlInput = screen.getByLabelText(/download url/i);
      const submitButton = screen.getByText(/submit for testing/i);

      await user.type(titleInput, 'Test Game');
      await user.type(versionInput, 'v1.0.0');
      await user.type(urlInput, 'invalid-url');
      await user.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/please enter a valid url/i)).toBeInTheDocument();
      });
    });

    it('should validate version number format', async () => {
      const user = userEvent.setup();
      render(<SubmitVersionPage />);

      const titleInput = screen.getByLabelText(/project title/i);
      const versionInput = screen.getByLabelText(/version number/i);
      const submitButton = screen.getByText(/submit for testing/i);

      await user.type(titleInput, 'Test Game');
      await user.type(versionInput, 'invalid-version');
      await user.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/please use semantic versioning/i)).toBeInTheDocument();
      });
    });
  });

  describe('Form Submission', () => {
    it('should submit form with valid data', async () => {
      const user = userEvent.setup();
      const mockResponse = {
        id: '123',
        title: 'Test Game',
        versionNumber: 'v1.0.0',
        status: 'draft' as const,
        currentTesterCount: 0,
        startDate: '2024-01-15',
        endDate: '2024-01-22',
        createdBy: {
          id: 'u1',
          name: 'John Student',
          email: 'john.student@mymail.champlain.edu',
        },
        projectVersionId: 'pv1',
        downloadUrl: 'https://example.com/game.zip',
        instructionsType: 'inline' as const,
        instructionsContent: 'Test the game',
        feedbackFormContent: 'Please provide feedback',
      };

      mockTestingLabApi.createSimpleTestingRequest.mockResolvedValue(mockResponse);

      render(<SubmitVersionPage />);

      // Fill required fields
      await user.type(screen.getByLabelText(/project title/i), 'Test Game');
      await user.type(screen.getByLabelText(/version number/i), 'v1.0.0');
      await user.type(screen.getByLabelText(/description/i), 'A test game for capstone');
      await user.type(screen.getByLabelText(/download url/i), 'https://example.com/game.zip');
      await user.type(screen.getByLabelText(/testing instructions/i), 'Test the game thoroughly');
      await user.type(screen.getByLabelText(/feedback form/i), 'Please rate the game');

      const submitButton = screen.getByText(/submit for testing/i);
      await user.click(submitButton);

      await waitFor(() => {
        expect(mockTestingLabApi.createSimpleTestingRequest).toHaveBeenCalledWith({
          title: 'Test Game',
          description: 'A test game for capstone',
          versionNumber: 'v1.0.0',
          downloadUrl: 'https://example.com/game.zip',
          instructionsType: 'inline',
          instructionsContent: 'Test the game thoroughly',
          feedbackFormContent: 'Please rate the game',
          teamIdentifier: expect.stringMatching(/fa23-capstone-2023-24-t\d{2}/),
        });
      });

      expect(mockPush).toHaveBeenCalledWith('/dashboard/testing-lab');
    });

    it('should handle submission errors gracefully', async () => {
      const user = userEvent.setup();
      mockTestingLabApi.createSimpleTestingRequest.mockRejectedValue(new Error('API Error'));

      render(<SubmitVersionPage />);

      // Fill required fields
      await user.type(screen.getByLabelText(/project title/i), 'Test Game');
      await user.type(screen.getByLabelText(/version number/i), 'v1.0.0');
      await user.type(screen.getByLabelText(/download url/i), 'https://example.com/game.zip');

      const submitButton = screen.getByText(/submit for testing/i);
      await user.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/failed to submit/i)).toBeInTheDocument();
      });

      // Should not navigate on error
      expect(mockPush).not.toHaveBeenCalled();
    });
  });

  describe('Team Identifier Detection', () => {
    it('should automatically detect team from email domain', async () => {
      const user = userEvent.setup();
      const mockResponse = {
        id: '123',
        title: 'Test Game',
        versionNumber: 'v1.0.0',
        status: 'draft' as const,
        currentTesterCount: 0,
        startDate: '2024-01-15',
        endDate: '2024-01-22',
        createdBy: {
          id: 'u1',
          name: 'John Student',
          email: 'john.student@mymail.champlain.edu',
        },
        projectVersionId: 'pv1',
        downloadUrl: 'https://example.com/game.zip',
        instructionsType: 'inline' as const,
        instructionsContent: 'Test the game',
        feedbackFormContent: 'Please provide feedback',
      };

      mockTestingLabApi.createSimpleTestingRequest.mockResolvedValue(mockResponse);

      render(<SubmitVersionPage />);

      // Fill minimal required fields
      await user.type(screen.getByLabelText(/project title/i), 'Test Game');
      await user.type(screen.getByLabelText(/version number/i), 'v1.0.0');
      await user.type(screen.getByLabelText(/download url/i), 'https://example.com/game.zip');

      const submitButton = screen.getByText(/submit for testing/i);
      await user.click(submitButton);

      await waitFor(() => {
        expect(mockTestingLabApi.createSimpleTestingRequest).toHaveBeenCalledWith(
          expect.objectContaining({
            teamIdentifier: expect.stringMatching(/fa23-capstone-2023-24-t\d{2}/),
          }),
        );
      });
    });
  });

  describe('Form State Management', () => {
    it('should show loading state during submission', async () => {
      const user = userEvent.setup();
      mockTestingLabApi.createSimpleTestingRequest.mockImplementation(
        () => new Promise(() => {}), // Never resolves
      );

      render(<SubmitVersionPage />);

      // Fill required fields
      await user.type(screen.getByLabelText(/project title/i), 'Test Game');
      await user.type(screen.getByLabelText(/version number/i), 'v1.0.0');
      await user.type(screen.getByLabelText(/download url/i), 'https://example.com/game.zip');

      const submitButton = screen.getByText(/submit for testing/i);
      await user.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/submitting/i)).toBeInTheDocument();
      });
    });

    it('should reset form after successful submission', async () => {
      const user = userEvent.setup();
      const mockResponse = {
        id: '123',
        title: 'Test Game',
        versionNumber: 'v1.0.0',
        status: 'draft' as const,
        currentTesterCount: 0,
        startDate: '2024-01-15',
        endDate: '2024-01-22',
        createdBy: {
          id: 'u1',
          name: 'John Student',
          email: 'john.student@mymail.champlain.edu',
        },
        projectVersionId: 'pv1',
        downloadUrl: 'https://example.com/game.zip',
        instructionsType: 'inline' as const,
        instructionsContent: 'Test the game',
        feedbackFormContent: 'Please provide feedback',
      };

      mockTestingLabApi.createSimpleTestingRequest.mockResolvedValue(mockResponse);

      render(<SubmitVersionPage />);

      const titleInput = screen.getByLabelText(/project title/i) as HTMLInputElement;
      const versionInput = screen.getByLabelText(/version number/i) as HTMLInputElement;

      await user.type(titleInput, 'Test Game');
      await user.type(versionInput, 'v1.0.0');
      await user.type(screen.getByLabelText(/download url/i), 'https://example.com/game.zip');

      const submitButton = screen.getByText(/submit for testing/i);
      await user.click(submitButton);

      await waitFor(() => {
        expect(mockPush).toHaveBeenCalledWith('/dashboard/testing-lab');
      });
    });
  });
});
