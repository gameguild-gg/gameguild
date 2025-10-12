'use client';

import React, { FunctionComponent } from 'react';
import { Button } from '@gameguild/ui/components/button';

type CookieConsentProps = {
  // TODO: Add props here.
};

export const CookieConsent: FunctionComponent<CookieConsentProps> = ({}: CookieConsentProps): React.JSX.Element => {
  const accept: VoidFunction = (): void => {};
  const customize: VoidFunction = (): void => {};

  return (
    <div>
      <p>We use cookies to improve your experience on our site. By using our site, you consent to our use of cookies.</p>
      <Button type={'button'} onClick={accept}>
        Accept All
      </Button>
      <Button type={'button'} onClick={customize}>
        Customize
      </Button>
    </div>
  );
};
