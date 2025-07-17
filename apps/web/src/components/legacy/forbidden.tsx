import React from 'react';
import Link from 'next/link';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { ArrowLeft, Home, LogIn, Shield } from 'lucide-react';

interface ForbiddenProps {
  title?: string;
  message?: string;
  showLoginButton?: boolean;
}

export default function Forbidden({
  title = 'Access Forbidden',
  message = "You don't have permission to access this resource.",
  showLoginButton = true,
}: ForbiddenProps) {
  return (
    <div className="min-h-screen bg-background flex items-center justify-center p-4">
      <Card className="w-full max-w-2xl">
        <CardHeader className="text-center">
          <div className="mx-auto w-16 h-16 bg-destructive/10 rounded-full flex items-center justify-center mb-4">
            <Shield className="h-8 w-8 text-destructive" />
          </div>
          <CardTitle className="text-2xl">{title}</CardTitle>
          <p className="text-muted-foreground">{message}</p>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="flex flex-col sm:flex-row gap-3 justify-center">
            <Button asChild>
              <Link href="/">
                <Home className="h-4 w-4 mr-2" />
                Go Home
              </Link>
            </Button>
            <Button variant="outline" onClick={() => window.history.back()}>
              <ArrowLeft className="h-4 w-4 mr-2" />
              Go Back
            </Button>
            {showLoginButton && (
              <Button variant="outline" asChild>
                <Link href="/auth/signin">
                  <LogIn className="h-4 w-4 mr-2" />
                  Sign In
                </Link>
              </Button>
            )}
          </div>

          <div className="text-center text-sm text-muted-foreground">
            <p>If you believe this is an error, please contact support.</p>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
