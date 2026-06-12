'use client';

import React, {FunctionComponent} from 'react';
import {Button} from '@game-guild/ui/components/button';

type CookiePreferencesProps = {
    value?: {
        necessary: boolean;
        analytics: boolean;
        marketing: boolean;
    };
    onChange?: (value: {necessary: boolean; analytics: boolean; marketing: boolean}) => void;
    onSave?: (value: {necessary: boolean; analytics: boolean; marketing: boolean}) => void;
};

const defaultPreferences = {
    necessary: true,
    analytics: false,
    marketing: false,
};

export const CookiePreferences: FunctionComponent<CookiePreferencesProps> = ({value = defaultPreferences, onChange, onSave}: CookiePreferencesProps): React.JSX.Element => {
    const [preferences, setPreferences] = React.useState(value);

    const setPreference = (key: 'analytics' | 'marketing', checked: boolean): void => {
        const next = {...preferences, [key]: checked};
        setPreferences(next);
        onChange?.(next);
    };

    const submit = (event: React.FormEvent<HTMLFormElement>): void => {
        event.preventDefault();
        onSave?.(preferences);
    };

    const reset: VoidFunction = (): void => {
        setPreferences(defaultPreferences);
        onChange?.(defaultPreferences);
    };

    return (
        <form onSubmit={submit} className="space-y-4">
            <label className="flex items-center justify-between gap-4">
                <span>Necessary cookies</span>
                <input type="checkbox" checked={preferences.necessary} disabled readOnly />
            </label>
            <label className="flex items-center justify-between gap-4">
                <span>Analytics cookies</span>
                <input type="checkbox" checked={preferences.analytics} onChange={(event) => setPreference('analytics', event.target.checked)} />
            </label>
            <label className="flex items-center justify-between gap-4">
                <span>Marketing cookies</span>
                <input type="checkbox" checked={preferences.marketing} onChange={(event) => setPreference('marketing', event.target.checked)} />
            </label>
            <Button type={'reset'} onClick={reset}>
                Reset to Default
            </Button>
            <Button type={'submit'}>
                Save Preferences
            </Button>
        </form>
    );
};
