'use client';

import { Button } from '@gameguild/ui/components/button';
import React, { PropsWithChildren, useEffect } from 'react';

interface ErrorProps {
  error: Error & { digest?: string };
  reset: () => void;
}

export const Error = ({ error, reset, children }: PropsWithChildren<ErrorProps>): React.JSX.Element => {
  useEffect(() => {
    //
    // TODO get the path of the error.
    //
    // console.error(error);
  }, [error]);

  const retry: VoidFunction = (): void => {
    reset();
  };

  const reload: VoidFunction = (): void => {
    window.location.reload();
  };

  const reportError: VoidFunction = (): void => {};

  return (
    <div className="flex flex-col flex-1 relative items-center justify-center">
      {children && <>{children}</>}
      <div>
        <div>
          <div>
            <h2 className="text-2xl font-bold">Something went wrong!</h2>
            <p className="text-lg">We apologize for the inconvenience. An unexpected error has occurred.</p>
          </div>
          {/* TODO: Add error message here if is development environment*/}
          <div></div>
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
