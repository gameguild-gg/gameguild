// import React from 'react';
// import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
// import { Badge } from '@/components/ui/badge';
//
// interface RecentActivitySummaryProps {
//   usersCreatedToday: number;
//   programsCreatedThisWeek: number;
//   newSubscriptionsThisMonth: number;
//   recentAchievements: number;
// }
//
// export function DashboardRecentActivitySummary({
//   usersCreatedToday,
//   programsCreatedThisWeek,
//   newSubscriptionsThisMonth,
//   recentAchievements,
// }: RecentActivitySummaryProps) {
//   return (
//     <Card>
//       <CardHeader>
//         <CardTitle>Recent Activity</CardTitle>
//       </CardHeader>
//       <CardContent>
//         <div className="space-y-4">
//           <div className="flex items-center justify-between">
//             <span className="text-sm">New users today</span>
//             <Badge variant="secondary">{usersCreatedToday}</Badge>
//           </div>
//
//           <div className="flex items-center justify-between">
//             <span className="text-sm">New programs this week</span>
//             <Badge variant="secondary">{programsCreatedThisWeek}</Badge>
//           </div>
//
//           <div className="flex items-center justify-between">
//             <span className="text-sm">New subscriptions this month</span>
//             <Badge variant="secondary">{newSubscriptionsThisMonth}</Badge>
//           </div>
//
//           <div className="flex items-center justify-between">
//             <span className="text-sm">Recent achievements</span>
//             <Badge variant="secondary">{recentAchievements}</Badge>
//           </div>
//         </div>
//       </CardContent>
//     </Card>
//   );
// }
