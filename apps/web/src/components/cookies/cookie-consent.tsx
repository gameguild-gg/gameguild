'use client';

import React, { useEffect, useState } from 'react';
import { CookieIcon, Settings, X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useCookies } from '@/hooks/use-cookies';
import { CookiePreferences } from './cookie-preferences';

type CookieConsentProps = {
  className?: string;
  showPreferencesButton?: boolean;
  compactMode?: boolean;
};

export const CookieConsent = ({ className = '', showPreferencesButton = true, compactMode = false }: Readonly<CookieConsentProps>): React.JSX.Element | null => {
  const { hasConsented, acceptAll, acceptEssential, isLoading } = useCookies();
  const [visible, setVisible] = useState(false);
  const [showPreferences, setShowPreferences] = useState(false);

  useEffect(() => {
    // Only show consent banner if user hasn't consented and we're not loading
    if (!isLoading && !hasConsented) {
      setVisible(true);
    } else {
      setVisible(false);
    }
  }, [hasConsented, isLoading]);

  const handleAcceptAll = () => {
    acceptAll();
    setVisible(false);
  };

  const handleAcceptEssential = () => {
    acceptEssential();
    setVisible(false);
  };

  const handleShowPreferences = () => {
    setShowPreferences(true);
  };

  const handleClosePreferences = () => {
    setShowPreferences(false);
    // Check if user has consented after closing preferences
    if (hasConsented) {
      setVisible(false);
    }
  };

  // Don't render anything while loading or if consent has been given
  if (isLoading || !visible) {
    return null;
  }

  // Show preferences modal
  if (showPreferences) {
    return (
      <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
        <div className="relative max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-lg bg-white shadow-xl">
          <button onClick={handleClosePreferences} className="absolute right-4 top-4 z-10 rounded-full p-2 hover:bg-gray-100" aria-label="Close preferences">
            <X className="h-4 w-4" />
          </button>
          <CookiePreferences onSave={handleClosePreferences} />
        </div>
      </div>
    );
  }

  // Compact mode for smaller screens or embedded contexts
  if (compactMode) {
    return (
      <aside className={`fixed inset-x-0 bottom-0 z-50 bg-gray-950 text-white p-4 ${className}`}>
        <div className="flex flex-col gap-3">
          <div className="flex items-start gap-3">
            <CookieIcon className="h-6 w-6 flex-shrink-0 mt-0.5" />
            <div className="flex-1 text-sm">
              <p>We use cookies to enhance your experience. Choose your preferences or accept all.</p>
            </div>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button onClick={handleAcceptAll} size="sm" className="bg-white text-gray-950 hover:bg-gray-100">
              Accept All
            </Button>
            <Button onClick={handleAcceptEssential} variant="outline" size="sm" className="border-white text-white hover:bg-white hover:text-gray-950">
              Essential Only
            </Button>
            {showPreferencesButton && (
              <Button onClick={handleShowPreferences} variant="ghost" size="sm" className="text-white hover:bg-gray-800">
                <Settings className="h-4 w-4 mr-1" />
                Preferences
              </Button>
            )}
          </div>
        </div>
      </aside>
    );
  }

  // Full banner mode
  return (
    <aside className={`fixed inset-x-0 bottom-0 z-50 bg-gray-950 text-white p-6 ${className}`}>
      <div className="mx-auto max-w-6xl">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div className="flex items-start gap-4 lg:flex-1">
            <CookieIcon className="h-8 w-8 flex-shrink-0" />
            <div className="space-y-1">
              <h4 className="font-semibold text-lg">We value your privacy</h4>
              <p className="text-sm text-gray-300 max-w-2xl">
                We use cookies to provide the best experience on our website. These cookies help us understand how you use our site, remember your preferences, and show you relevant content. You can choose which types of cookies to allow.
              </p>
            </div>
          </div>

          <div className="flex flex-col gap-3 sm:flex-row lg:flex-shrink-0">
            <Button onClick={handleAcceptAll} className="bg-white text-gray-950 hover:bg-gray-100 font-medium">
              Accept All Cookies
            </Button>
            <Button onClick={handleAcceptEssential} variant="outline" className="border-white text-white hover:bg-white hover:text-gray-950">
              Essential Only
            </Button>
            {showPreferencesButton && (
              <Button onClick={handleShowPreferences} variant="ghost" className="text-white hover:bg-gray-800">
                <Settings className="h-4 w-4 mr-2" />
                Customize
              </Button>
            )}
          </div>
        </div>
      </div>
    </aside>
  );
};
