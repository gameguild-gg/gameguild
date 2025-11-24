'use client';

import {Button} from '@game-guild/ui/components/button';
import React from 'react';

export const NotFound = (): React.JSX.Element => {
    const reload: VoidFunction = (): void => {
        window.location.reload();
    };

    const reportError: VoidFunction = (): void => {
    };

    return (
        <div>
            <div>
                <h2 className="text-2xl font-bold">Not Found</h2>
                <p className="text-lg">The page you are looking for does not exist or has been moved.</p>
            </div>
            <div>
                {/* TODO: Add a search component here.*/}
                <p>Try searching for what you need or return to the homepage.</p>
            </div>
            <div>
                <div>
                    <Button onClick={reload}>Go Home </Button>
                    <Button onClick={reload}>Go Back</Button>
                </div>
                <div>
                    <p>If you believe this is an error, please contact support or try again later.</p>
                    <Button onClick={reportError}>Contact Support</Button>
                    <p>Thank you for your patience!</p>
                </div>
            </div>
        </div>
    );
};
