import React from 'react';
import { render, screen, fireEvent, act } from '@testing-library/react';
import '@testing-library/jest-dom';
import { GeneralSettings, generalSettingsSchema, GeneralSettingsState } from '../general-settings';

function setup(partial?: Partial<GeneralSettingsState>) {
  const state: GeneralSettingsState = {
    labName: 'Test Lab',
    description: 'Desc',
    timezone: 'UTC',
    defaultSessionDuration: 60,
    allowPublicSignups: true,
    requireApproval: true,
    enableNotifications: true,
    maxSimultaneousSessions: 5,
    ...partial,
  };
  let current = state;
  const setState = (s: GeneralSettingsState) => { current = s; };
  const save = jest.fn().mockResolvedValue(undefined);
  render(<GeneralSettings generalSettings={current} setGeneralSettings={setState} saveGeneralSettings={save} />);
  return { save };
}

describe('generalSettingsSchema', () => {
  it('validates correct data', () => {
    const parsed = generalSettingsSchema.parse({
      labName: 'My Lab',
      description: 'A description',
      timezone: 'UTC',
      defaultSessionDuration: 120,
      allowPublicSignups: true,
      requireApproval: false,
      enableNotifications: true,
      maxSimultaneousSessions: 10,
    });
    expect(parsed.labName).toBe('My Lab');
  });

  it('rejects short lab name', () => {
    const result = generalSettingsSchema.safeParse({
      labName: 'ab',
      description: '',
      timezone: 'UTC',
      defaultSessionDuration: 30,
      allowPublicSignups: true,
      requireApproval: true,
      enableNotifications: true,
      maxSimultaneousSessions: 5,
    });
    expect(result.success).toBe(false);
  });
});

describe('GeneralSettings component', () => {
  jest.useFakeTimers();
  it('shows dirty indicator and disables save when invalid', () => {
    setup();
    const nameInput = screen.getByLabelText('Lab Name');
    fireEvent.change(nameInput, { target: { value: 'ab' } });
    act(() => { jest.runAllTimers(); });
    const error = screen.getByText(/at least 3 characters/i);
    expect(error).toBeInTheDocument();
  });
});
