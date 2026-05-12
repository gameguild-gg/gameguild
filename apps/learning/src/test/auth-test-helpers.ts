import type { ReactElement } from 'react';
import { render, type RenderOptions } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi } from 'vitest';

export interface MockUseAuthReturn {
    signIn: ReturnType<typeof vi.fn>;
    signUp: ReturnType<typeof vi.fn>;
    signOut: ReturnType<typeof vi.fn>;
    isLoading: boolean;
    error: Error | null;
    clearError: ReturnType<typeof vi.fn>;
}

export function createMockUseAuth(
    overrides: Partial<MockUseAuthReturn> = {}
): MockUseAuthReturn {
    return {
        signIn: vi.fn().mockResolvedValue(undefined),
        signUp: vi.fn().mockResolvedValue(undefined),
        signOut: vi.fn().mockResolvedValue(undefined),
        isLoading: false,
        error: null,
        clearError: vi.fn(),
        ...overrides,
    };
}

export function renderWithUser(
    ui: ReactElement,
    options?: Omit<RenderOptions, 'wrapper'>
) {
    return {
        user: userEvent.setup(),
        ...render(ui, options),
    };
}
