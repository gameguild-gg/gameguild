import { AchievementManagementContent } from '@/components/achievements/management/achievement-management-content';
import { DashboardPage, DashboardPageContent, DashboardPageHeader, DashboardPageTitle, DashboardPageDescription } from '@/components/dashboard';
import { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Achievements | Dashboard',
  description: 'Manage achievements and view achievement statistics.',
};

export default function AchievementsPage() {
  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Achievements</DashboardPageTitle>
        <DashboardPageDescription>Manage and monitor achievement system performance.</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <AchievementManagementContent />
      </DashboardPageContent>
    </DashboardPage>
  );
}
