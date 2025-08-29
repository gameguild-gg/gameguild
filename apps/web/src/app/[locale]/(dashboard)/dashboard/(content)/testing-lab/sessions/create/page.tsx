import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Calendar } from 'lucide-react';
import { Metadata } from 'next';
import Link from 'next/link';
import React from 'react';

export const metadata: Metadata = {
  title: 'Create Testing Session | Game Guild Dashboard',
  description: 'Create a new testing session to coordinate game testing activities.',
};

export default async function CreateSessionPage(): Promise<React.JSX.Element> {
  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Create Testing Session</DashboardPageTitle>
        <DashboardPageDescription>Create a new testing session to coordinate game testing activities</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <Card className="max-w-md mx-auto">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Calendar className="h-5 w-5" />
              Session Creation Simplified
            </CardTitle>
          </CardHeader>
          <CardContent className="text-center space-y-4">
            <p className="text-muted-foreground">
              Session creation has been simplified. Please use the sample sessions available in the main sessions list.
            </p>
            <Button asChild>
              <Link href="/dashboard/testing-lab/sessions">
                View Testing Sessions
              </Link>
            </Button>
          </CardContent>
        </Card>
      </DashboardPageContent>
    </DashboardPage>
  );
}
