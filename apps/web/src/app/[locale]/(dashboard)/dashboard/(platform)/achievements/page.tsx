import { AchievementManagementContent } from '@/components/achievements/management/achievement-management-content';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { getAchievementsAction } from '@/lib/admin/achievements/achievements.actions';
import type { AchievementDto } from '@/lib/core/api/generated/types.gen';
import { Loader2 } from 'lucide-react';
import { Metadata } from 'next';
import { Suspense } from 'react';

export const metadata: Metadata = {
  title: 'Achievements | Dashboard',
  description: 'Manage achievements and view achievement statistics.',
};

export default async function AchievementsPage() {
  // Load achievements data
  let achievements: AchievementDto[] = [];
  try {
    const result = await getAchievementsAction();
    if (result.success && result.data) {
      achievements = result.data;
    }
  } catch (error) {
    console.error('Failed to load achievements:', error);
  }

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Achievement Management</DashboardPageTitle>
        <DashboardPageDescription>Manage and monitor achievement system performance</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <Suspense
          fallback={
            <div className="flex items-center justify-center p-4">
              <Loader2 className="h-4 w-4 animate-spin" />
            </div>
          }
        >
          <AchievementManagementContent achievements={achievements} />
        </Suspense>
      </DashboardPageContent>
    </DashboardPage>
  );
}
