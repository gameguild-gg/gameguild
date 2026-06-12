'use client';

import {Button} from '@game-guild/ui/components/button';
import React from 'react';

export interface NotFoundProps {
    homeHref?: string;
    supportHref?: string;
}

export const NotFound = ({homeHref = '/', supportHref = 'mailto:support@gameguild.gg'}: NotFoundProps): React.JSX.Element => {
    const goHome: VoidFunction = (): void => {
        window.location.assign(homeHref);
    };

    const goBack: VoidFunction = (): void => {
        if (window.history.length > 1) {
            window.history.back();
            return;
        }

        window.location.assign(homeHref);
    };

    const contactSupport: VoidFunction = (): void => {
        window.location.assign(supportHref);
    };

    return (
        <div>
            <div>
                <h2 className="text-2xl font-bold">Not Found</h2>
                <p className="text-lg">The page you are looking for does not exist or has been moved.</p>
            </div>
            <div>
                <p>Check the address, use site navigation, or return to the homepage.</p>
            </div>
            <div>
                <div>
                    <Button onClick={goHome}>Go Home</Button>
                    <Button onClick={goBack}>Go Back</Button>
                </div>
                <div>
                    <p>If you believe this is an error, please contact support or try again later.</p>
                    <Button onClick={contactSupport}>Contact Support</Button>
                    <p>Thank you for your patience!</p>
                </div>
            </div>
        </div>
    );
};
