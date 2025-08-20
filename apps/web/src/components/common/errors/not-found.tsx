'use client';

import { Button } from '@/components/ui/button';
import { ArrowLeft, Home, Mail, Bug } from 'lucide-react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { useEffect } from 'react';
// Note: Using a simple header since this is a client component
import Footer from '@/components/common/footer/default-footer';

export function NotFound() {
  const router = useRouter();

  useEffect(() => {
    // Track 404 page view with Google Analytics
    if (typeof window !== 'undefined' && window.gtag) {
      window.gtag('event', 'page_view', {
        page_title: '404 - Page Not Found',
        page_location: window.location.href,
        event_category: 'Error',
        event_label: '404_not_found',
        custom_map: {
          custom_parameter_1: 'not_found_page'
        }
      });
    }
  }, []);

  const handleReportIssue = () => {
    // Create a temporary link element with GitHub issue data attributes
    const link = document.createElement('a');
    link.setAttribute('data-github-issue', 'true');
    link.setAttribute('data-route', window.location.pathname);
    link.style.display = 'none';
    document.body.appendChild(link);
    
    // Trigger a click event on the link to activate the GitHub issue handler
    const clickEvent = new MouseEvent('click', {
      bubbles: true,
      cancelable: true,
      view: window
    });
    link.dispatchEvent(clickEvent);
    
    // Clean up the temporary link
    document.body.removeChild(link);
  };

  return (
    <div className="min-h-screen flex flex-col">
      {/* Simple header for 404 page */}
      <header className="w-full bg-white/10 dark:bg-slate-900/20 backdrop-blur-md border-b border-white/10 dark:border-slate-700/30 text-slate-900 dark:text-white sticky top-0 z-50 shadow-lg">
        <div className="container mx-auto px-4 flex justify-between items-center py-4">
          <Link href="/" className="text-xl font-bold">
            Game Guild
          </Link>
          <Link href="/" className="text-sm hover:underline">
            Back to Home
          </Link>
        </div>
      </header>
      <main className="flex-1 flex items-center justify-center bg-gradient-to-br from-background to-muted/20">
        <div className="max-w-md w-full mx-auto text-center space-y-8 p-6">
          <div className="space-y-4">
            <h1 className="text-9xl font-bold text-muted-foreground/30">404</h1>
            <h2 className="text-3xl font-bold tracking-tight">Page Not Found</h2>
            <p className="text-muted-foreground text-lg">
              The page you&apos;re looking for doesn&apos;t exist or has been moved.
            </p>
          </div>

          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Button
              onClick={() => router.push('/')}
              className="flex items-center gap-2"
            >
              <Home className="h-4 w-4" />
              Go Home
            </Button>
            
            <Button
              variant="outline"
              onClick={() => router.back()}
              className="flex items-center gap-2"
            >
              <ArrowLeft className="h-4 w-4" />
              Go Back
            </Button>
          </div>

          <div className="pt-8 border-t border-border/50 space-y-6">
            <div className="space-y-4">
              <p className="text-sm text-muted-foreground">
                Think this is a mistake? Help us improve by reporting this issue.
              </p>
              
              {/* Featured GitHub Issue Button */}
              <div className="relative">
                <Button
                  onClick={handleReportIssue}
                  className="relative bg-gradient-to-r from-orange-500 to-red-500 hover:from-orange-600 hover:to-red-600 text-white font-semibold px-6 py-3 rounded-lg shadow-lg hover:shadow-xl transition-all duration-300 transform hover:scale-105 flex items-center gap-3 mx-auto animate-pulse"
                  size="lg"
                >
                  <Bug className="h-5 w-5" />
                  Report Issue on GitHub
                  <div className="absolute -top-1 -right-1 w-3 h-3 bg-yellow-400 rounded-full animate-ping"></div>
                </Button>
                <div className="absolute inset-0 bg-gradient-to-r from-orange-500/20 to-red-500/20 rounded-lg blur-xl -z-10 animate-pulse"></div>
              </div>
            </div>
            
            {/* Secondary Support Option */}
            <div className="flex justify-center">
              <Link href="mailto:support@gameguild.com">
                <Button variant="ghost" size="sm" className="flex items-center gap-2 text-muted-foreground hover:text-foreground">
                  <Mail className="h-4 w-4" />
                  Contact Support
                </Button>
              </Link>
            </div>
          </div>
        </div>
      </main>
      <Footer />
    </div>
  );
}
