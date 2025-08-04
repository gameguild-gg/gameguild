// import React from 'react';
// import Link from 'next/link';
// import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
// import { Button } from '@/components/ui/button';
// import { Activity, BookOpen, Calendar, CreditCard, Package, Target, Trophy, Users } from 'lucide-react';
//
// export function DashboardEnhancedQuickActions() {
//   return (
//     <Card>
//       <CardHeader>
//         <CardTitle>Quick Actions</CardTitle>
//       </CardHeader>
//       <CardContent>
//         <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
//           <Button variant="outline" className="h-20 flex flex-col items-center justify-center" asChild>
//             <Link href="/dashboard/users">
//               <Users className="h-6 w-6 mb-2" />
//               <span className="text-sm">Manage Users</span>
//             </Link>
//           </Button>
//
//           <Button variant="outline" className="h-20 flex flex-col items-center justify-center" asChild>
//             <Link href="/dashboard/programs">
//               <BookOpen className="h-6 w-6 mb-2" />
//               <span className="text-sm">Programs</span>
//             </Link>
//           </Button>
//
//           <Button variant="outline" className="h-20 flex flex-col items-center justify-center" asChild>
//             <Link href="/dashboard/products">
//               <Package className="h-6 w-6 mb-2" />
//               <span className="text-sm">Products</span>
//             </Link>
//           </Button>
//
//           <Button variant="outline" className="h-20 flex flex-col items-center justify-center" asChild>
//             <Link href="/dashboard/achievements">
//               <Trophy className="h-6 w-6 mb-2" />
//               <span className="text-sm">Achievements</span>
//             </Link>
//           </Button>
//
//           <Button variant="outline" className="h-20 flex flex-col items-center justify-center" asChild>
//             <Link href="/dashboard/subscriptions">
//               <CreditCard className="h-6 w-6 mb-2" />
//               <span className="text-sm">Subscriptions</span>
//             </Link>
//           </Button>
//
//           <Button variant="outline" className="h-20 flex flex-col items-center justify-center" asChild>
//             <Link href="/dashboard/projects">
//               <Target className="h-6 w-6 mb-2" />
//               <span className="text-sm">Projects</span>
//             </Link>
//           </Button>
//
//           <Button variant="outline" className="h-20 flex flex-col items-center justify-center" asChild>
//             <Link href="/dashboard/analytics">
//               <Activity className="h-6 w-6 mb-2" />
//               <span className="text-sm">Analytics</span>
//             </Link>
//           </Button>
//
//           <Button variant="outline" className="h-20 flex flex-col items-center justify-center" asChild>
//             <Link href="/dashboard/testing-lab">
//               <Calendar className="h-6 w-6 mb-2" />
//               <span className="text-sm">Testing Lab</span>
//             </Link>
//           </Button>
//         </div>
//       </CardContent>
//     </Card>
//   );
// }
