// import React, { Suspense } from 'react';
// import { getUserStatistics } from '@/lib/users/users.actions';
// import { RecentActivityLoading, ServerRecentActivity } from '@/components/dashboard/analytics/server-recent-activity';
// import { DashboardFilters } from '@/components/dashboard/dashboard-filters';
// import { SystemStatus } from '@/components/dashboard/analytics/system-status';
// import { DashboardHeroSection } from './dashboard-hero-section';
// import { DashboardQuickActions } from './dashboard-quick-actions';
//
// interface DashboardOverviewContentProps {
//   searchParams: { [key: string]: string | string[] | undefined };
// }
//
// export async function DashboardOverviewContent({ searchParams }: DashboardOverviewContentProps) {
//   // Extract search parameters for filtering
//   const fromDate = typeof searchParams.fromDate === 'string' ? searchParams.fromDate : undefined;
//   const toDate = typeof searchParams.toDate === 'string' ? searchParams.toDate : undefined;
//   const includeDeleted = searchParams.includeDeleted === 'true';
//
//   // Fetch dashboard data using server action
//   const dashboardResult = await getUserStatistics(fromDate, toDate, includeDeleted);
//
//   if (!dashboardResult.success) {
//     return (
//       <div className="space-y-8">
//         <DashboardHeroSection error={dashboardResult.error || 'Failed to load dashboard data'} />
//       </div>
//     );
//   }
//
//   const { data: userStatistics } = dashboardResult;
//
//   return (
//     <div className="space-y-8">
//       {/* Hero Section with gradient background */}
//       <DashboardHeroSection userStatistics={userStatistics} />
//
//       {/* Main Content Grid */}
//       <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
//         {/* Recent Activity */}
//         <div className="lg:col-span-2">
//           <Suspense fallback={<RecentActivityLoading />}>
//             <ServerRecentActivity limit={15} />
//           </Suspense>
//         </div>
//
//         {/* Sidebar with Filters, Quick Actions & System Status */}
//         <div className="space-y-6">
//           {/* Dashboard Filters */}
//           <DashboardFilters />
//
//           {/* Quick Actions */}
//           <DashboardQuickActions />
//
//           {/* System Status with Server Data */}
//           <SystemStatus
//             systemStatus={{
//               apiStatus: 'online',
//               databaseStatus: 'connected',
//               paymentGatewayStatus: 'active',
//               emailServiceStatus: 'active',
//             }}
//           />
//         </div>
//       </div>
//     </div>
//   );
// }
