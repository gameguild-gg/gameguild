import React from 'react';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';

export default async function Page(): Promise<React.JSX.Element> {
  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Project Feedbacks</DashboardPageTitle>
        <DashboardPageDescription>View and manage feedback submitted to this project.</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <div className="text-center text-muted-foreground py-10">No feedbacks yet.</div>
      </DashboardPageContent>
    </DashboardPage>
  );
}
