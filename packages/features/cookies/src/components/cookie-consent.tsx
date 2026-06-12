'use client';

import React, {FunctionComponent} from 'react';
import {Button} from '@game-guild/ui/components/button';

type CookieConsentProps = {
    onAcceptAll?: () => void;
    onCustomize?: () => void;
    policyHref?: string;
};

export const CookieConsent: FunctionComponent<CookieConsentProps> = ({onAcceptAll, onCustomize, policyHref = '/privacy'}: CookieConsentProps): React.JSX.Element => {
    const accept: VoidFunction = (): void => {
        onAcceptAll?.();
    };

    const customize: VoidFunction = (): void => {
        onCustomize?.();
    };

    return (
        <div className="space-y-4 rounded-lg border p-4">
            <p>
                We use necessary cookies to run the site and optional cookies to improve product analytics and content.
                Review the <a className="underline" href={policyHref}>cookie policy</a> for details.
            </p>
            <Button type={'button'} onClick={accept}>
                Accept All
            </Button>
            <Button type={'button'} onClick={customize}>
                Customize
            </Button>
        </div>
    );
};
