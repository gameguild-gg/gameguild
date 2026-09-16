import type {
  TestingLabQuestionnaireSchema,
  TestingLabTestingEventTemplateProjection,
} from '@game-guild/client';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  refresh: vi.fn(),
  save: vi.fn(),
  archive: vi.fn(),
}));

vi.mock('@/i18n/navigation', () => ({
  useRouter: () => ({ refresh: mocks.refresh }),
}));

vi.mock('@/lib/testing-lab/events-actions', () => ({
  saveTestingEventTemplate: mocks.save,
  setTestingEventTemplateArchived: mocks.archive,
}));

vi.mock('./questionnaire-builder', () => ({
  QuestionnaireBuilder: ({
    value,
    onChange,
  }: {
    value: TestingLabQuestionnaireSchema;
    onChange: (value: TestingLabQuestionnaireSchema) => void;
  }) => (
    <button
      type="button"
      onClick={() =>
        onChange({
          ...value,
          questions: [
            {
              id: `${value.title}-question`,
              prompt: 'Question',
              type: 'FreeText',
              options: [],
            },
          ],
        })
      }
    >
      Update {value.title}
    </button>
  ),
}));

import { TestingEventTemplateManagement } from './testing-event-template-management';

const activeTemplate = {
  id: 'template-active',
  name: 'Community playtests',
  description: 'Recurring community calendar',
  currentRevisionNumber: 4,
  isArchived: false,
  currentRevision: {
    generalRules: 'Be respectful.',
    candidateInstructions: 'Bring a build.',
    testerInstructions: 'Complete every task.',
    defaultMode: 'Hybrid',
    defaultApprovalMode: 'Committee',
    defaultRequiresFeedback: false,
    projectApplicationSchema: {
      title: 'Project application',
      questions: [],
    },
    testerRegistrationSchema: {
      title: 'Tester registration',
      questions: [],
    },
  },
} as TestingLabTestingEventTemplateProjection;

const archivedTemplate = {
  id: 'template-archived',
  name: '',
  isArchived: true,
  currentRevision: null,
} as TestingLabTestingEventTemplateProjection;

async function fillRequiredFields(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText('Calendar name'), 'Launch calendar');
  await user.type(screen.getByLabelText('Description'), 'Launch events');
  await user.type(screen.getByLabelText('General rules'), 'Respect everyone.');
  await user.type(screen.getByLabelText('Candidate instructions'), 'Upload a build.');
  await user.type(screen.getByLabelText('Tester instructions'), 'Run every task.');
}

describe('TestingEventTemplateManagement extended behavior', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.save.mockResolvedValue({ success: true, message: 'Calendar saved.' });
    mocks.archive.mockResolvedValue({ success: true, message: 'Calendar archived.' });
  });

  it('creates a calendar with the current questionnaire schemas and refreshes', async () => {
    const user = userEvent.setup();
    render(<TestingEventTemplateManagement templates={[]} />);
    await fillRequiredFields(user);
    await user.click(screen.getByRole('button', { name: 'Update Project application' }));
    await user.click(screen.getByRole('button', { name: 'Update Tester registration' }));
    await user.click(screen.getByRole('button', { name: 'Create calendar' }));

    await waitFor(() => expect(mocks.save).toHaveBeenCalledOnce());
    const formData = mocks.save.mock.calls[0]![0] as FormData;
    expect(formData.get('name')).toBe('Launch calendar');
    expect(formData.get('description')).toBe('Launch events');
    expect(JSON.parse(String(formData.get('projectApplicationSchemaJson')))).toMatchObject({
      title: 'Project application',
      questions: [{ id: 'Project application-question' }],
    });
    expect(JSON.parse(String(formData.get('testerRegistrationSchemaJson')))).toMatchObject({
      title: 'Tester registration',
      questions: [{ id: 'Tester registration-question' }],
    });
    expect(await screen.findByText('Calendar saved.')).toBeInTheDocument();
    expect(mocks.refresh).toHaveBeenCalledOnce();
  });

  it.each([
    [{ success: false, error: 'Calendar validation failed.' }, 'Calendar validation failed.'],
    [new Error('Calendar service offline'), 'Calendar service offline'],
    ['failure', 'The Testing Lab operation failed.'],
  ])('renders save failures without refreshing', async (failure, message) => {
    const user = userEvent.setup();
    if (failure instanceof Error || typeof failure === 'string') {
      mocks.save.mockRejectedValueOnce(failure);
    } else {
      mocks.save.mockResolvedValueOnce(failure);
    }
    render(<TestingEventTemplateManagement templates={[]} />);
    await fillRequiredFields(user);
    await user.click(screen.getByRole('button', { name: 'Create calendar' }));
    expect(await screen.findByText(message)).toBeInTheDocument();
    expect(mocks.refresh).not.toHaveBeenCalled();
  });

  it('archives the selected active calendar and switches back to a new calendar', async () => {
    const user = userEvent.setup();
    render(
      <TestingEventTemplateManagement
        templates={[activeTemplate, archivedTemplate]}
      />,
    );

    expect(screen.getByLabelText('Calendar name')).toHaveValue('Community playtests');
    expect(screen.getByLabelText('Description')).toHaveValue('Recurring community calendar');
    expect(screen.getByRole('checkbox', { name: /Require developer feedback/ })).not.toBeChecked();
    await user.click(screen.getByRole('button', { name: 'Archive' }));
    await waitFor(() => expect(mocks.archive).toHaveBeenCalledOnce());
    expect(Object.fromEntries((mocks.archive.mock.calls[0]![0] as FormData).entries())).toEqual({
      templateId: 'template-active',
    });
    expect(await screen.findByText('Calendar archived.')).toBeInTheDocument();
    expect(mocks.refresh).toHaveBeenCalledOnce();

    await user.click(screen.getByRole('button', { name: 'New calendar' }));
    expect(screen.getByRole('heading', { name: 'Create an event calendar' })).toBeInTheDocument();
    expect(screen.getByLabelText('Calendar name')).toHaveValue('');
  });

  it('restores an archived calendar and prevents revisions until restoration', async () => {
    const user = userEvent.setup();
    mocks.archive.mockResolvedValueOnce({ success: true, message: 'Calendar restored.' });
    render(<TestingEventTemplateManagement templates={[archivedTemplate]} />);

    expect(screen.queryByText('No calendars yet. Create one to group events and reuse defaults.')).not.toBeInTheDocument();
    expect(screen.getByText('Untitled calendar')).toBeInTheDocument();
    expect(screen.getByText('Revision 1')).toBeInTheDocument();
    expect(screen.getByText('Archived')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /Untitled calendar/ }));
    expect(screen.getByRole('button', { name: 'Save new revision' })).toBeDisabled();
    expect(screen.getByText(/Restore this calendar before/)).toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: /Require developer feedback/ })).toBeChecked();
    await user.click(screen.getByRole('button', { name: 'Restore' }));

    await waitFor(() => expect(mocks.archive).toHaveBeenCalledOnce());
    expect(Object.fromEntries((mocks.archive.mock.calls[0]![0] as FormData).entries())).toEqual({
      templateId: 'template-archived',
      restore: 'true',
    });
    expect(await screen.findByText('Calendar restored.')).toBeInTheDocument();
  });

  it.each([
    [{ success: false, error: 'Archive denied.' }, 'Archive denied.'],
    [new Error('Archive service offline'), 'Archive service offline'],
    ['failure', 'The Testing Lab operation failed.'],
  ])('renders archive failures without refreshing', async (failure, message) => {
    const user = userEvent.setup();
    if (failure instanceof Error || typeof failure === 'string') {
      mocks.archive.mockRejectedValueOnce(failure);
    } else {
      mocks.archive.mockResolvedValueOnce(failure);
    }
    render(<TestingEventTemplateManagement templates={[activeTemplate]} />);
    await user.click(screen.getByRole('button', { name: 'Archive' }));
    expect(await screen.findByText(message)).toBeInTheDocument();
    expect(mocks.refresh).not.toHaveBeenCalled();
  });

  it('does not archive a malformed calendar projection without an identifier', async () => {
    const user = userEvent.setup();
    const malformed = {
      ...activeTemplate,
      id: null,
      name: 'Malformed calendar',
    } as unknown as TestingLabTestingEventTemplateProjection;
    render(<TestingEventTemplateManagement templates={[malformed]} />);

    await user.click(screen.getByRole('button', { name: /Malformed calendar/ }));
    await user.click(screen.getByRole('button', { name: 'Archive' }));
    expect(mocks.archive).not.toHaveBeenCalled();
  });
});
