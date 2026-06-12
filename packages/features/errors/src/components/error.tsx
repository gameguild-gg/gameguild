'use client';

import {Button} from '@game-guild/ui/components/button';
import React, {PropsWithChildren, useEffect} from 'react';

interface ErrorProps {
    error: Error & { digest?: string };
    reset: () => void;
    homeHref?: string;
    supportHref?: string;
    telemetryEndpoint?: string;
}

export const Error = ({error, reset, children, homeHref = '/', supportHref = 'mailto:support@gameguild.gg', telemetryEndpoint}: PropsWithChildren<ErrorProps>): React.JSX.Element => {
    useEffect(() => {
        if (!telemetryEndpoint) {
            console.error(error);
            return;
        }

        void fetch(telemetryEndpoint, {
            method: 'POST',
            headers: {'Content-Type': 'application/json'},
            body: JSON.stringify({
                message: error.message,
                stack: error.stack,
                digest: error.digest,
                path: window.location.pathname,
                timestamp: new Date().toISOString(),
            }),
            keepalive: true,
        }).catch(() => console.error(error));
    }, [error, telemetryEndpoint]);

    const retry: VoidFunction = (): void => {
        reset();
    };

    const reload: VoidFunction = (): void => {
        window.location.assign(homeHref);
    };

    const reportError: VoidFunction = (): void => {
        window.location.assign(supportHref);
    };

    return (
        <div className="flex flex-col flex-1 relative items-center justify-center">
            {children && <>{children}</>}
            <div>
                <div>
                    <div>
                        <h2 className="text-2xl font-bold">Something went wrong!</h2>
                        <p className="text-lg">We apologize for the inconvenience. An unexpected error has occurred.</p>
                    </div>
                    {process.env.NODE_ENV === 'development' && (
                        <pre className="my-4 max-w-2xl overflow-auto rounded-md bg-black/80 p-4 text-left text-xs text-red-100">
                            {error.stack || error.message}
                        </pre>
                    )}
                    <div className="flex flex-row gap-2">
                        <div>
                            <Button onClick={retry}>Try Again</Button>
                        </div>
                        <div>
                            <Button onClick={reload}>Go Home </Button>
                            <Button onClick={reload}>Go Back</Button>
                        </div>
                        <div>
                            <Button onClick={reportError}>Contact Support</Button>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};
