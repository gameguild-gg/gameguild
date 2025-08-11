// import React from 'react';
// import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
// import { Badge } from '@/components/ui/badge';
//
// interface PlatformHealthProps {
//   activeUsers: number;
//   totalUsers: number;
//   publishedPrograms: number;
//   draftPrograms: number;
//   activeProducts: number;
//   inactiveProducts: number;
// }
//
// export function DashboardPlatformHealth({ activeUsers, totalUsers, publishedPrograms, draftPrograms, activeProducts, inactiveProducts }: PlatformHealthProps) {
//   const activeUserPercentage = totalUsers > 0 ? ((activeUsers / totalUsers) * 100).toFixed(1) : '0';
//
//   return (
//     <Card>
//       <CardHeader>
//         <CardTitle>Platform Health</CardTitle>
//       </CardHeader>
//       <CardContent>
//         <div className="space-y-4">
//           <div className="flex items-center justify-between">
//             <span className="text-sm font-medium">Active Users</span>
//             <div className="flex items-center space-x-2">
//               <Badge variant="default">{activeUsers}</Badge>
//               <span className="text-xs text-gray-500">{activeUserPercentage}%</span>
//             </div>
//           </div>
//
//           <div className="flex items-center justify-between">
//             <span className="text-sm font-medium">Published Programs</span>
//             <div className="flex items-center space-x-2">
//               <Badge variant="default">{publishedPrograms}</Badge>
//               <span className="text-xs text-gray-500">{draftPrograms} drafts</span>
//             </div>
//           </div>
//
//           <div className="flex items-center justify-between">
//             <span className="text-sm font-medium">Active Products</span>
//             <div className="flex items-center space-x-2">
//               <Badge variant="default">{activeProducts}</Badge>
//               <span className="text-xs text-gray-500">{inactiveProducts} inactive</span>
//             </div>
//           </div>
//         </div>
//       </CardContent>
//     </Card>
//   );
// }
