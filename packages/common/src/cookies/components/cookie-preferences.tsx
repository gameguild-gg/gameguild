'use client';

import React, {FunctionComponent} from 'react';
import {Button} from '@game-guild/ui/components/button';

type CookiePreferencesProps = {
    // TODO: Add props here.
};

export const CookiePreferences: FunctionComponent<CookiePreferencesProps> = ({}: CookiePreferencesProps): React.JSX.Element => {
    const submit: VoidFunction = (): void => {
    };
    const reset: VoidFunction = (): void => {
    };

    return (
        <form>
            <Button type={'reset'} onClick={reset}>
                Reset to Default
            </Button>
            <Button type={'submit'} onClick={submit}>
                Save Preferences
            </Button>
        </form>
    );
};
