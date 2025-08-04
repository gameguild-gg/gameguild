'use client';

import React, { useEffect, useState } from 'react';
import { CookieIcon, Shield, BarChart3, Target, Wrench } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { useCookies } from '@/hooks/use-cookies';
import { type CookieCategory } from '@/lib/cookies';

type CookiePreferencesProps = {
  onSave?: () => void;
  className?: string;
  showTitle?: boolean;
};

const cookieCategories = [
  {
    id: 'essential' as CookieCategory,
    title: 'Essential Cookies',
    description: 'These cookies are necessary for the website to function and cannot be switched off.',
    icon: Shield,
    required: true,
  },
  {
    id: 'functional' as CookieCategory,
    title: 'Functional Cookies',
    description: 'These cookies enable enhanced functionality and personalization, such as remembering your preferences.',
    icon: Wrench,
    required: false,
  },
  {
    id: 'analytics' as CookieCategory,
    title: 'Analytics Cookies',
    description: 'These cookies allow us to count visits and traffic sources to measure and improve site performance.',
    icon: BarChart3,
    required: false,
  },
  {
    id: 'marketing' as CookieCategory,
    title: 'Marketing Cookies',
    description: 'These cookies help us show you relevant ads and measure the effectiveness of our marketing campaigns.',
    icon: Target,
    required: false,
  },
] as const;

export const CookiePreferences = ({ onSave, className = '', showTitle = true }: Readonly<CookiePreferencesProps>): React.JSX.Element => {
  const { preferences, updatePreference, savePreferences, reset } = useCookies();
  const [localPreferences, setLocalPreferences] = useState(preferences);

  // Update local state when preferences change
  useEffect(() => {
    setLocalPreferences(preferences);
  }, [preferences]);

  const handleToggle = (category: CookieCategory, enabled: boolean) => {
    if (category === 'essential') {
      // Essential cookies cannot be disabled
      return;
    }

    setLocalPreferences((prev) => ({
      ...prev,
      [category]: enabled,
    }));
  };

  const handleSave = () => {
    // Update the hook's state with local preferences
    Object.entries(localPreferences).forEach(([key, value]) => {
      if (key !== 'essential' && key !== 'consentDate' && key !== 'version') {
        updatePreference(key as CookieCategory, value as boolean);
      }
    });

    // Save to localStorage
    savePreferences();

    // Call the onSave callback if provided
    onSave?.();
  };

  const handleReset = () => {
    reset();
    setLocalPreferences(preferences);
  };

  const handleAcceptAll = () => {
    const allAccepted = {
      ...localPreferences,
      analytics: true,
      marketing: true,
      functional: true,
    };
    setLocalPreferences(allAccepted);

    // Update preferences and save
    updatePreference('analytics', true);
    updatePreference('marketing', true);
    updatePreference('functional', true);
    savePreferences();

    onSave?.();
  };

  const handleRejectAll = () => {
    const onlyEssential = {
      ...localPreferences,
      analytics: false,
      marketing: false,
      functional: false,
    };
    setLocalPreferences(onlyEssential);

    // Update preferences and save
    updatePreference('analytics', false);
    updatePreference('marketing', false);
    updatePreference('functional', false);
    savePreferences();

    onSave?.();
  };

  return (
    <Card className={`w-full max-w-2xl ${className}`}>
      {showTitle && (
        <CardHeader className="border-b pb-4">
          <div className="flex items-center gap-2">
            <CookieIcon className="h-6 w-6" />
            <CardTitle>Cookie Preferences</CardTitle>
          </div>
          <CardDescription>Manage your cookie settings. You can enable or disable different types of cookies below. Changes will take effect immediately upon saving.</CardDescription>
        </CardHeader>
      )}

      <CardContent className="space-y-6 pt-6">
        {cookieCategories.map((category) => {
          const Icon = category.icon;
          const isEnabled = category.required || localPreferences[category.id];

          return (
            <div key={category.id} className="flex items-start justify-between gap-4">
              <div className="flex items-start gap-3 flex-1">
                <Icon className="h-5 w-5 mt-1 text-gray-600" />
                <div className="space-y-1">
                  <Label htmlFor={category.id} className="text-base font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70">
                    {category.title}
                    {category.required && <span className="ml-1 text-xs text-gray-500">(Required)</span>}
                  </Label>
                  <p className="text-sm text-gray-600 leading-relaxed">{category.description}</p>
                </div>
              </div>
              <Switch id={category.id} checked={isEnabled} onCheckedChange={(checked) => handleToggle(category.id, checked)} disabled={category.required} aria-label={`Toggle ${category.title}`} />
            </div>
          );
        })}

        <div className="pt-4 border-t">
          <p className="text-xs text-gray-500 leading-relaxed">Essential cookies are always enabled as they are necessary for the website to function properly. You can change your preferences at any time by accessing this panel again.</p>
        </div>
      </CardContent>

      <CardFooter className="flex flex-col gap-3 sm:flex-row sm:justify-between">
        <div className="flex gap-2">
          <Button variant="outline" onClick={handleReset} className="text-sm">
            Reset to Default
          </Button>
          <Button variant="outline" onClick={handleRejectAll} className="text-sm">
            Reject All
          </Button>
        </div>

        <div className="flex gap-2">
          <Button variant="secondary" onClick={handleAcceptAll} className="text-sm">
            Accept All
          </Button>
          <Button onClick={handleSave} className="text-sm font-medium">
            Save Preferences
          </Button>
        </div>
      </CardFooter>
    </Card>
  );
};
