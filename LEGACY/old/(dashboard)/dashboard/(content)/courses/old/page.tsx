import { PropsWithSlugParams } from '@/types';
import { redirect } from 'next/navigation';

export default async function Page({ params }: PropsWithSlugParams): Promise<void> {
  const { slug } = await params;

  redirect(`/dashboard/courses/${slug}/overview`);
}

// 'use client';
//
// import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
// import { Button } from '@/components/ui/button';
// import { Badge } from '@/components/ui/badge';
// import { Progress } from '@/components/ui/progress';
// import { BookOpen, Clock, DollarSign, Edit, ExternalLink, Eye, FileText, Image, Settings, TrendingUp, Users } from 'lucide-react';
// import { useCourseEditor } from '@/lib/courses/course-editor.context';
// import Link from 'next/link';
// import { useParams } from 'next/navigation';
//
// const QUICK_ACTIONS = [
//   {
//     id: 'details',
//     label: 'Course Details',
//     description: 'Edit title, description, and basic information',
//     icon: FileText,
//     path: '/details',
//     color: 'text-blue-600',
//     bgColor: 'bg-blue-50 hover:bg-blue-100',
//     borderColor: 'border-blue-200',
//   },
//   {
//     id: 'content',
//     label: 'Manage Content',
//     description: 'Add lessons, modules, and course materials',
//     icon: BookOpen,
//     path: '/content',
//     color: 'text-green-600',
//     bgColor: 'bg-green-50 hover:bg-green-100',
//     borderColor: 'border-green-200',
//   },
//   {
//     id: 'media',
//     label: 'Upload Media',
//     description: 'Add thumbnails, videos, and images',
//     icon: Image,
//     path: '/media',
//     color: 'text-purple-600',
//     bgColor: 'bg-purple-50 hover:bg-purple-100',
//     borderColor: 'border-purple-200',
//   },
//   {
//     id: 'pricing',
//     label: 'Set Pricing',
//     description: 'Configure products and pricing options',
//     icon: DollarSign,
//     path: '/pricing',
//     color: 'text-yellow-600',
//     bgColor: 'bg-yellow-50 hover:bg-yellow-100',
//     borderColor: 'border-yellow-200',
//   },
//   {
//     id: 'settings',
//     label: 'Course Settings',
//     description: 'Manage enrollment, access, and publishing',
//     icon: Settings,
//     path: '/settings',
//     color: 'text-gray-600',
//     bgColor: 'bg-gray-50 hover:bg-gray-100',
//     borderColor: 'border-gray-200',
//   },
// ];
//
// export default function CourseOverviewPage() {
//   const { state } = useCourseEditor();
//   const params = useParams();
//   const slug = params.slug as string;
//
//   // Calculate completion progress
//   const getCompletionProgress = () => {
//     let completed = 0;
//     const total = 5;
//
//     if (state.title && state.description) completed++;
//     if (state.media.thumbnail || state.media.showcaseVideo) completed++;
//     if (state.products.length > 0) completed++;
//     // Add more checks as needed
//
//     return Math.round((completed / total) * 100);
//   };
//
//   const completionProgress = getCompletionProgress();
//
//   return (
//     <div className="flex-1 flex flex-col min-h-0">
//       {/* Sticky Header */}
//       <div className="sticky top-0 z-10 border-b border-border bg-background/95 backdrop-blur-sm">
//         <div className="p-6">
//           <div className="flex items-center justify-between">
//             <div>
//               <h1 className="text-2xl font-bold text-foreground">Course Overview</h1>
//               <p className="text-sm text-muted-foreground">Manage and monitor your course development progress</p>
//             </div>
//             <div className="flex gap-2">
//               <Button variant="outline" size="sm" className="gap-2">
//                 <Eye className="h-4 w-4" />
//                 Preview
//               </Button>
//               <Button size="sm" className="gap-2">
//                 <ExternalLink className="h-4 w-4" />
//                 Publish
//               </Button>
//             </div>
//           </div>
//         </div>
//       </div>
//
//       {/* Content */}
//       <div className="flex-1 p-6 overflow-auto">
//         <div className="max-w-6xl mx-auto space-y-6">
//           {/* Course Header */}
//           <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
//             <CardHeader>
//               <div className="flex items-start justify-between">
//                 <div className="space-y-2">
//                   <div className="flex items-center gap-3">
//                     <h2 className="text-xl font-semibold">{state.title || 'Untitled Course'}</h2>
//                     <Badge variant={state.status === 'published' ? 'default' : 'secondary'}>{state.status === 'published' ? 'Published' : 'Draft'}</Badge>
//                   </div>
//                   <p className="text-sm text-muted-foreground max-w-2xl">
//                     {state.description || 'No description provided yet. Add a compelling description to attract students.'}
//                   </p>
//                   <div className="flex gap-4 text-xs text-muted-foreground">
//                     <span className="flex items-center gap-1">
//                       <Users className="h-3 w-3" />
//                       {state.enrollment.currentEnrollments} enrolled
//                     </span>
//                     <span className="flex items-center gap-1">
//                       <Clock className="h-3 w-3" />
//                       {state.estimatedHours}h estimated
//                     </span>
//                     <span className="flex items-center gap-1">
//                       <TrendingUp className="h-3 w-3" />
//                       {state.difficulty}/5 difficulty
//                     </span>
//                   </div>
//                 </div>
//                 <Button variant="outline" size="sm" asChild>
//                   <Link href={`/dashboard/courses/${slug}/details`} className="gap-2">
//                     <Edit className="h-4 w-4" />
//                     Edit Details
//                   </Link>
//                 </Button>
//               </div>
//             </CardHeader>
//           </Card>
//
//           {/* Progress Section */}
//           <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
//             <CardHeader>
//               <CardTitle className="flex items-center gap-2">📊 Course Development Progress</CardTitle>
//             </CardHeader>
//             <CardContent>
//               <div className="space-y-4">
//                 <div className="flex items-center justify-between">
//                   <span className="text-sm font-medium">Overall Completion</span>
//                   <span className="text-sm text-muted-foreground">{completionProgress}%</span>
//                 </div>
//                 <Progress value={completionProgress} className="h-2" />
//                 <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-center">
//                   <div className="p-3 rounded-lg bg-muted/30">
//                     <div className="text-lg font-bold text-primary">{state.title ? '✓' : '○'}</div>
//                     <div className="text-xs text-muted-foreground">Basic Info</div>
//                   </div>
//                   <div className="p-3 rounded-lg bg-muted/30">
//                     <div className="text-lg font-bold text-primary">○</div>
//                     <div className="text-xs text-muted-foreground">Content</div>
//                   </div>
//                   <div className="p-3 rounded-lg bg-muted/30">
//                     <div className="text-lg font-bold text-primary">{state.media.thumbnail ? '✓' : '○'}</div>
//                     <div className="text-xs text-muted-foreground">Media</div>
//                   </div>
//                   <div className="p-3 rounded-lg bg-muted/30">
//                     <div className="text-lg font-bold text-primary">{state.products.length > 0 ? '✓' : '○'}</div>
//                     <div className="text-xs text-muted-foreground">Pricing</div>
//                   </div>
//                 </div>
//               </div>
//             </CardContent>
//           </Card>
//
//           {/* Quick Actions */}
//           <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
//             <CardHeader>
//               <CardTitle className="flex items-center gap-2">� Quick Actions</CardTitle>
//             </CardHeader>
//             <CardContent>
//               <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
//                 {QUICK_ACTIONS.map((action) => {
//                   const Icon = action.icon;
//                   return (
//                     <Button
//                       key={action.id}
//                       variant="outline"
//                       asChild
//                       className={`h-auto p-4 flex flex-col items-start gap-3 ${action.bgColor} ${action.borderColor} hover:shadow-md transition-all`}
//                     >
//                       <Link href={`/dashboard/courses/${slug}${action.path}`}>
//                         <div className="flex items-center gap-2 w-full">
//                           <Icon className={`h-5 w-5 ${action.color}`} />
//                           <span className="font-medium">{action.label}</span>
//                         </div>
//                         <p className="text-xs text-muted-foreground text-left">{action.description}</p>
//                       </Link>
//                     </Button>
//                   );
//                 })}
//               </div>
//             </CardContent>
//           </Card>
//
//           {/* Course Stats */}
//           <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
//             <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
//               <CardHeader className="pb-3">
//                 <CardTitle className="text-sm font-medium text-muted-foreground">Students</CardTitle>
//               </CardHeader>
//               <CardContent>
//                 <div className="text-2xl font-bold">{state.enrollment.currentEnrollments}</div>
//                 <p className="text-xs text-muted-foreground">Total enrollments</p>
//               </CardContent>
//             </Card>
//
//             <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
//               <CardHeader className="pb-3">
//                 <CardTitle className="text-sm font-medium text-muted-foreground">Content</CardTitle>
//               </CardHeader>
//               <CardContent>
//                 <div className="text-2xl font-bold">0</div>
//                 <p className="text-xs text-muted-foreground">Lessons created</p>
//               </CardContent>
//             </Card>
//
//             <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
//               <CardHeader className="pb-3">
//                 <CardTitle className="text-sm font-medium text-muted-foreground">Revenue</CardTitle>
//               </CardHeader>
//               <CardContent>
//                 <div className="text-2xl font-bold">$0</div>
//                 <p className="text-xs text-muted-foreground">Total earnings</p>
//               </CardContent>
//             </Card>
//           </div>
//         </div>
//       </div>
//     </div>
//   );
// }
