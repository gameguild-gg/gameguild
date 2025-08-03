// import React, { Suspense } from 'react';
// import { getUserStatistics } from '@/lib/users/users.actions';
// import { DashboardOverviewHeader } from './dashboard-overview-header';
// import { DashboardKeyMetrics } from './dashboard-key-metrics';
// import { DashboardEnhancedQuickActions } from './dashboard-enhanced-quick-actions';
// import { DashboardPlatformHealth } from './dashboard-platform-health';
// import { DashboardRecentActivitySummary } from './dashboard-recent-activity-summary';
//
// interface DashboardEnhancedContentProps {
//   searchParams: { [key: string]: string | string[] | undefined };
// }
//
// export async function DashboardEnhancedContent({ searchParams }: DashboardEnhancedContentProps) {
//   // Extract date filters
//   const fromDate = typeof searchParams.fromDate === 'string' ? searchParams.fromDate : undefined;
//   const toDate = typeof searchParams.toDate === 'string' ? searchParams.toDate : undefined;
//
//   // Fetch user statistics - you would fetch all statistics in parallel in a real app
//   const userStatsResult = await getUserStatistics(fromDate, toDate);
//
//   if (!userStatsResult.success) {
//     return (
//       <div className="space-y-6">
//         <DashboardOverviewHeader />
//         <div className="text-center py-12">
//           <p className="text-red-600">Failed to load dashboard data: {userStatsResult.error}</p>
//         </div>
//       </div>
//     );
//   }
//
//   const userStats = userStatsResult.data;
//
//   // Mock data for other statistics - in a real app you would fetch these
//   const mockData = {
//     totalRevenue: 125000,
//     monthlyRevenue: 15000,
//     totalPrograms: 45,
//     publishedPrograms: 38,
//     draftPrograms: 7,
//     activeSubscriptions: 230,
//     activeSubscriptionPercentage: 78.5,
//     totalProducts: 12,
//     activeProducts: 9,
//     inactiveProducts: 3,
//     totalAchievements: 156,
//     userAchievements: 1240,
//     totalEnrollments: 890,
//     averageRating: 4.7,
//     programsCreatedThisWeek: 3,
//     newSubscriptionsThisMonth: 42,
//     recentAchievements: 28,
//   };
//
//   const weeklyGrowth = userStats.usersCreatedThisWeek + mockData.programsCreatedThisWeek;
//
//   return (
//     <div className="space-y-6">
//       <DashboardOverviewHeader />
//
//       <DashboardKeyMetrics
//         totalRevenue={mockData.totalRevenue}
//         totalUsers={userStats.totalUsers}
//         totalPrograms={mockData.totalPrograms}
//         activeSubscriptions={mockData.activeSubscriptions}
//         totalProducts={mockData.totalProducts}
//         totalAchievements={mockData.totalAchievements}
//         totalEnrollments={mockData.totalEnrollments}
//         weeklyGrowth={weeklyGrowth}
//         monthlyRevenue={mockData.monthlyRevenue}
//         usersCreatedThisMonth={userStats.usersCreatedThisMonth}
//         publishedPrograms={mockData.publishedPrograms}
//         activeProducts={mockData.activeProducts}
//         userAchievements={mockData.userAchievements}
//         averageRating={mockData.averageRating}
//         activeSubscriptionPercentage={mockData.activeSubscriptionPercentage}
//       />
//
//       <DashboardEnhancedQuickActions />
//
//       {/* Status Overview */}
//       <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
//         <DashboardPlatformHealth
//           activeUsers={userStats.activeUsers}
//           totalUsers={userStats.totalUsers}
//           publishedPrograms={mockData.publishedPrograms}
//           draftPrograms={mockData.draftPrograms}
//           activeProducts={mockData.activeProducts}
//           inactiveProducts={mockData.inactiveProducts}
//         />
//
//         <DashboardRecentActivitySummary
//           usersCreatedToday={userStats.usersCreatedToday}
//           programsCreatedThisWeek={mockData.programsCreatedThisWeek}
//           newSubscriptionsThisMonth={mockData.newSubscriptionsThisMonth}
//           recentAchievements={mockData.recentAchievements}
//         />
//       </div>
//     </div>
//   );
// }
