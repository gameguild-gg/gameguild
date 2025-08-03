import {
  DashboardPage,
  DashboardPageContent,
  DashboardPageDescription,
  DashboardPageHeader,
  DashboardPageTitle,
} from '@/components/dashboard/common/ui/dashboard-page';
import React from 'react';

export default function TestingLabPage() {
  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Courses</DashboardPageTitle>
        <DashboardPageDescription></DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent></DashboardPageContent>
    </DashboardPage>
  );
}
