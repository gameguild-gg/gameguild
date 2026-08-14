import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

vi.mock('@/lib/launch-pad/actions', () => ({
  submitLaunchPadApplicationForm: vi.fn(),
}));

import { LaunchPadApplicationForm } from './launch-pad-application-form';

describe('LaunchPadApplicationForm', () => {
  it('preselects the Project carried from its Distribution workspace', () => {
    render(
      <LaunchPadApplicationForm
        eventId="event-1"
        initialProjectId="project-2"
        versions={[
          { id: 'version-1', projectId: 'project-1', projectTitle: 'Asterion', versionNumber: '1.0.0', status: 'published' },
          { id: 'version-2', projectId: 'project-2', projectTitle: 'Wayfinder', versionNumber: '2.0.0', status: 'testing' },
        ]}
      />,
    );

    expect(screen.getByRole('combobox', { name: /project version/i })).toHaveValue('version-2');
  });
});
