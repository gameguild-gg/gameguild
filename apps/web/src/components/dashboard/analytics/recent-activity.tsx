// 'use client';
//
// import { useEffect, useState } from 'react';
// import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
// import { Badge } from '@/components/ui/badge';
// import { Button } from '@/components/ui/button';
// import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
// import { ScrollArea } from '@/components/ui/scroll-area';
// import { Activity, BookOpen, DollarSign, RefreshCw, Settings, User, UserMinus, UserPlus } from 'lucide-react';
//
// interface ActivityItem {
//   id: string;
//   type: 'user_created' | 'user_updated' | 'user_deleted' | 'course_enrolled' | 'payment_received' | 'course_completed' | 'settings_changed';
//   title: string;
//   description: string;
//   user?: {
//     id: string;
//     name: string;
//     avatar?: string;
//   };
//   timestamp: string;
//   metadata?: {
//     course?: string;
//     amount?: number;
//     currency?: string;
//     email?: string;
//     completionRate?: number;
//   };
// }
//
// const activityIcons = {
//   user_created: UserPlus,
//   user_updated: User,
//   user_deleted: UserMinus,
//   course_enrolled: BookOpen,
//   payment_received: DollarSign,
//   course_completed: BookOpen,
//   settings_changed: Settings,
// };
//
// const activityColors = {
//   user_created: 'text-green-400',
//   user_updated: 'text-blue-400',
//   user_deleted: 'text-red-400',
//   course_enrolled: 'text-purple-400',
//   payment_received: 'text-green-400',
//   course_completed: 'text-blue-400',
//   settings_changed: 'text-slate-400',
// };
//
// const mockActivities: ActivityItem[] = [
//   {
//     id: '1',
//     type: 'user_created',
//     title: 'New User Registration',
//     description: 'John Doe has joined the platform',
//     user: {
//       id: '1',
//       name: 'John Doe',
//       avatar: '/avatars/john.jpg',
//     },
//     timestamp: '2 minutes ago',
//     metadata: { email: 'john@example.com' },
//   },
//   {
//     id: '2',
//     type: 'course_enrolled',
//     title: 'Course Enrollment',
//     description: 'Sarah Smith enrolled in "Advanced React Patterns"',
//     user: {
//       id: '2',
//       name: 'Sarah Smith',
//       avatar: '/avatars/sarah.jpg',
//     },
//     timestamp: '15 minutes ago',
//     metadata: { course: 'Advanced React Patterns', amount: 99.99 },
//   },
//   {
//     id: '3',
//     type: 'payment_received',
//     title: 'Payment Received',
//     description: 'Payment of $149.99 received from Mike Johnson',
//     user: {
//       id: '3',
//       name: 'Mike Johnson',
//     },
//     timestamp: '1 hour ago',
//     metadata: { amount: 149.99, currency: 'USD' },
//   },
//   {
//     id: '4',
//     type: 'course_completed',
//     title: 'Course Completed',
//     description: 'Emma Wilson completed "JavaScript Fundamentals"',
//     user: {
//       id: '4',
//       name: 'Emma Wilson',
//       avatar: '/avatars/emma.jpg',
//     },
//     timestamp: '2 hours ago',
//     metadata: { course: 'JavaScript Fundamentals', completionRate: 100 },
//   },
//   {
//     id: '5',
//     type: 'user_updated',
//     title: 'Profile Updated',
//     description: 'Alex Chen updated their profile information',
//     user: {
//       id: '5',
//       name: 'Alex Chen',
//     },
//     timestamp: '3 hours ago',
//   },
//   {
//     id: '6',
//     type: 'course_enrolled',
//     title: 'Course Enrollment',
//     description: 'Lisa Park enrolled in "Node.js Masterclass"',
//     user: {
//       id: '6',
//       name: 'Lisa Park',
//       avatar: '/avatars/lisa.jpg',
//     },
//     timestamp: '4 hours ago',
//     metadata: { course: 'Node.js Masterclass', amount: 199.99 },
//   },
//   {
//     id: '7',
//     type: 'payment_received',
//     title: 'Payment Received',
//     description: 'Payment of $79.99 received from David Brown',
//     user: {
//       id: '7',
//       name: 'David Brown',
//     },
//     timestamp: '5 hours ago',
//     metadata: { amount: 79.99, currency: 'USD' },
//   },
//   {
//     id: '8',
//     type: 'settings_changed',
//     title: 'Settings Updated',
//     description: 'System notification settings were modified',
//     timestamp: '6 hours ago',
//   },
// ];
//
// export function RecentActivity() {
//   const [activities, setActivities] = useState<ActivityItem[]>([]);
//   const [isLoading, setIsLoading] = useState(true);
//
//   useEffect(() => {
//     // Simulate API call
//     const timer = setTimeout(() => {
//       setActivities(mockActivities);
//       setIsLoading(false);
//     }, 1000);
//
//     return () => clearTimeout(timer);
//   }, []);
//
//   const refreshActivities = () => {
//     setIsLoading(true);
//     // Simulate refresh
//     setTimeout(() => {
//       setActivities([...mockActivities].sort(() => Math.random() - 0.5));
//       setIsLoading(false);
//     }, 1000);
//   };
//
//   const formatTimestamp = (timestamp: string) => {
//     return timestamp;
//   };
//
//   const getActivityBadge = (type: ActivityItem['type']) => {
//     const badges = {
//       user_created: { variant: 'default' as const, label: 'New User' },
//       user_updated: { variant: 'secondary' as const, label: 'Updated' },
//       user_deleted: { variant: 'destructive' as const, label: 'Deleted' },
//       course_enrolled: { variant: 'default' as const, label: 'Enrolled' },
//       payment_received: { variant: 'default' as const, label: 'Payment' },
//       course_completed: { variant: 'default' as const, label: 'Completed' },
//       settings_changed: { variant: 'outline' as const, label: 'Settings' },
//     };
//
//     return badges[type];
//   };
//
//   if (isLoading) {
//     return (
//       <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
//         <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-3">
//           <CardTitle className="text-base font-medium text-white">Recent Activity</CardTitle>
//           <Button variant="ghost" size="sm" disabled className="text-slate-400">
//             <RefreshCw className="h-4 w-4 animate-spin" />
//           </Button>
//         </CardHeader>
//         <CardContent>
//           <div className="space-y-4">
//             {[...Array(5)].map((_, i) => (
//               <div key={i} className="flex items-center space-x-4">
//                 <div className="h-8 w-8 rounded-full bg-slate-700/50 animate-pulse" />
//                 <div className="flex-1 space-y-2">
//                   <div className="h-4 bg-slate-700/50 rounded animate-pulse" />
//                   <div className="h-3 bg-slate-700/50 rounded w-2/3 animate-pulse" />
//                 </div>
//               </div>
//             ))}
//           </div>
//         </CardContent>
//       </Card>
//     );
//   }
//
//   return (
//     <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
//       <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-3">
//         <CardTitle className="text-base font-medium flex items-center gap-2 text-white">
//           <Activity className="h-4 w-4" />
//           Recent Activity
//         </CardTitle>
//         <Button variant="ghost" size="sm" onClick={refreshActivities} className="text-slate-400 hover:text-white hover:bg-slate-700/50">
//           <RefreshCw className="h-4 w-4" />
//         </Button>
//       </CardHeader>
//       <CardContent className="p-0">
//         <ScrollArea className="h-[400px]">
//           <div className="p-6 pt-0">
//             <div className="space-y-4">
//               {activities.map((activity) => {
//                 const Icon = activityIcons[activity.type];
//                 const iconColor = activityColors[activity.type];
//                 const badge = getActivityBadge(activity.type);
//
//                 return (
//                   <div key={activity.id} className="flex items-start space-x-4 pb-4 border-b border-slate-700/50 last:border-0 last:pb-0">
//                     <div className={`mt-1 p-2 rounded-full bg-slate-700/50 ${iconColor}`}>
//                       <Icon className="h-3 w-3" />
//                     </div>
//
//                     <div className="flex-1 min-w-0">
//                       <div className="flex items-center justify-between mb-1">
//                         <p className="text-sm font-medium text-white truncate">{activity.title}</p>
//                         <Badge variant={badge.variant} className="text-xs">
//                           {badge.label}
//                         </Badge>
//                       </div>
//
//                       <p className="text-sm text-slate-400 mb-2">{activity.description}</p>
//
//                       {activity.user && (
//                         <div className="flex items-center space-x-2 mb-2">
//                           <Avatar className="h-6 w-6">
//                             <AvatarImage src={activity.user.avatar} alt={activity.user.name} />
//                             <AvatarFallback className="text-xs bg-slate-700 text-white">
//                               {activity.user.name
//                                 .split(' ')
//                                 .map((n) => n[0])
//                                 .join('')}
//                             </AvatarFallback>
//                           </Avatar>
//                           <span className="text-xs text-slate-400">{activity.user.name}</span>
//                         </div>
//                       )}
//
//                       {activity.metadata && (
//                         <div className="text-xs text-slate-400 mb-2">
//                           {activity.metadata.course && <span className="inline-block mr-3">Course: {activity.metadata.course}</span>}
//                           {activity.metadata.amount && <span className="inline-block mr-3">Amount: ${activity.metadata.amount}</span>}
//                           {activity.metadata.email && <span className="inline-block mr-3">Email: {activity.metadata.email}</span>}
//                         </div>
//                       )}
//
//                       <p className="text-xs text-slate-500">{formatTimestamp(activity.timestamp)}</p>
//                     </div>
//                   </div>
//                 );
//               })}
//             </div>
//           </div>
//         </ScrollArea>
//
//         <div className="p-4 border-t">
//           <Button variant="outline" className="w-full" size="sm">
//             View All Activity
//           </Button>
//         </div>
//       </CardContent>
//     </Card>
//   );
// }
