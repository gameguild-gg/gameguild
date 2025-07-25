import React from 'react';
import {
  DashboardPage,
  DashboardPageContent,
  DashboardPageDescription,
  DashboardPageHeader,
  DashboardPageTitle,
} from '@/components/dashboard/common/ui/dashboard-page';

export default async function Page(): Promise<React.JSX.Element> {
  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Sales & Pricing</DashboardPageTitle>
        <DashboardPageDescription>Configure course pricing, products, and sales showcase settings</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent></DashboardPageContent>
    </DashboardPage>
  );
}
