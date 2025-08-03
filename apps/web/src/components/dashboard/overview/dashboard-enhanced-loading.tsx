// import React from 'react';
// import { Card, CardContent, CardHeader } from '@/components/ui/card';
//
// export function DashboardEnhancedLoading() {
//   return (
//     <div className="space-y-6">
//       <div className="h-8 w-64 bg-gray-200 rounded animate-pulse" />
//
//       {/* Statistics cards skeleton */}
//       <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
//         {Array.from({ length: 8 }).map((_, i) => (
//           <Card key={i}>
//             <CardContent className="p-6">
//               <div className="flex items-center justify-between space-y-0 pb-2">
//                 <div className="h-4 w-24 bg-gray-200 rounded animate-pulse" />
//                 <div className="h-6 w-6 bg-gray-200 rounded animate-pulse" />
//               </div>
//               <div className="h-8 w-16 bg-gray-200 rounded animate-pulse" />
//               <div className="h-3 w-20 bg-gray-200 rounded animate-pulse mt-2" />
//             </CardContent>
//           </Card>
//         ))}
//       </div>
//
//       {/* Quick actions skeleton */}
//       <Card>
//         <CardHeader>
//           <div className="h-6 w-32 bg-gray-200 rounded animate-pulse" />
//         </CardHeader>
//         <CardContent>
//           <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
//             {Array.from({ length: 8 }).map((_, i) => (
//               <div key={i} className="h-20 bg-gray-200 rounded animate-pulse" />
//             ))}
//           </div>
//         </CardContent>
//       </Card>
//     </div>
//   );
// }
