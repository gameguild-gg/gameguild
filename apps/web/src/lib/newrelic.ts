// New Relic Browser Agent setup
export const initNewRelic = () => {
    if (typeof window !== 'undefined') {
        try {
            // Check if New Relic is already loaded
            if (window.newrelic) {
                // Set custom attributes
                window.newrelic.setCustomAttribute('app_version', process.env.NEXT_PUBLIC_APP_VERSION || '1.0.0');
                window.newrelic.setCustomAttribute('environment', process.env.NODE_ENV || 'production');
                window.newrelic.setCustomAttribute('user_agent', navigator.userAgent);

                // Track initial page view
                window.newrelic.setPageViewName(window.location.pathname);

                console.log('New Relic initialized successfully');
                return;
            }

            // Check if we have the required account ID
            const accountId = process.env.NEXT_PUBLIC_NEW_RELIC_ACCOUNT_ID;
            console.log('New Relic Debug:', {
                accountId,
                nodeEnv: process.env.NODE_ENV,
                hasAccountId: !!accountId && accountId !== 'your-account-id-here'
            });

            if (!accountId || accountId === 'your-account-id-here') {
                console.warn('New Relic Browser Agent: Account ID not configured. Skipping client-side monitoring.');
                return;
            }

            // Load New Relic Browser Agent script if not already loaded
            const script = document.createElement('script');
            script.src = `https://js-agent.newrelic.com/nr-spa-${accountId}.min.js`;
            script.async = true;

            script.onload = () => {
                if (window.newrelic) {
                    window.newrelic.setCustomAttribute('app_version', process.env.NEXT_PUBLIC_APP_VERSION || '1.0.0');
                    window.newrelic.setCustomAttribute('environment', process.env.NODE_ENV || 'production');
                    window.newrelic.setCustomAttribute('user_agent', navigator.userAgent);
                    window.newrelic.setPageViewName(window.location.pathname);
                    console.log('New Relic Browser Agent loaded successfully');
                }
            };

            script.onerror = () => {
                console.warn('Failed to load New Relic Browser Agent. Please check your account ID configuration.');
            };

            document.head.appendChild(script);
        } catch (error) {
            console.warn('Failed to initialize New Relic:', error);
        }
    }
};

// Custom error tracking
export const trackError = (error: Error, context?: Record<string, any>) => {
    if (typeof window !== 'undefined' && window.newrelic) {
        try {
            window.newrelic.noticeError(error, context);
        } catch (e) {
            console.warn('Failed to track error with New Relic:', e);
        }
    }
};

// Custom event tracking
export const trackEvent = (eventName: string, attributes?: Record<string, any>) => {
    if (typeof window !== 'undefined' && window.newrelic) {
        try {
            window.newrelic.addToTrace({
                name: eventName,
                attributes,
            });
        } catch (e) {
            console.warn('Failed to track event with New Relic:', e);
        }
    }
};

// Add New Relic types to window
declare global {
    interface Window {
        newrelic?: {
            setCustomAttribute: (key: string, value: string | number | boolean) => void;
            noticeError: (error: Error, customAttributes?: Record<string, any>) => void;
            addToTrace: (payload: { name: string; attributes?: Record<string, any> }) => void;
            setPageViewName: (name: string) => void;
        };
    }
} 