import { Metadata } from 'next';
import React from 'react';

export const metadata: Metadata = {
  title: 'Community Feed | Game Guild',
  description: 'Stay updated with the latest posts, announcements, and community activities.',
};

export default async function Page(): Promise<React.JSX.Element> {
  return (
    <div className="flex items-center justify-center h-screen">
      <h1 className="text-2xl font-bold">This is the home page</h1>
    </div>
  );
}

// import { Suspense } from 'react';

// import { CommunityFeed } from '@/components/feed';
//

//
// function FeedSkeleton() {
//   return (
//     <div className="min-h-screen bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
//       <div className="max-w-4xl mx-auto px-6 py-8">
//         {/* Header Skeleton */}
//         <div className="mb-8">
//           <div className="h-8 bg-slate-700 rounded w-64 mb-4 animate-pulse"></div>
//           <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
//             {[...Array(3)].map((_, i) => (
//               <div key={i} className="bg-slate-800/50 rounded-xl p-6 animate-pulse">
//                 <div className="h-4 bg-slate-700 rounded w-20 mb-2"></div>
//                 <div className="h-8 bg-slate-600 rounded w-16"></div>
//               </div>
//             ))}
//           </div>
//         </div>
//
//         {/* Filters Skeleton */}
//         <div className="mb-8 bg-slate-800/50 rounded-xl p-6">
//           <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
//             {[...Array(4)].map((_, i) => (
//               <div key={i} className="h-10 bg-slate-700 rounded animate-pulse"></div>
//             ))}
//           </div>
//         </div>
//
//         {/* Posts Skeleton */}
//         <div className="grid gap-6">
//           {[...Array(6)].map((_, i) => (
//             <div key={i} className="bg-slate-800/50 rounded-xl p-6 animate-pulse">
//               <div className="flex items-start gap-4 mb-4">
//                 <div className="w-10 h-10 bg-slate-700 rounded-full"></div>
//                 <div className="flex-1">
//                   <div className="h-4 bg-slate-700 rounded w-32 mb-2"></div>
//                   <div className="h-3 bg-slate-600 rounded w-20"></div>
//                 </div>
//               </div>
//               <div className="space-y-2 mb-4">
//                 <div className="h-4 bg-slate-700 rounded w-full"></div>
//                 <div className="h-4 bg-slate-700 rounded w-3/4"></div>
//               </div>
//               <div className="flex gap-4">
//                 {[...Array(3)].map((_, j) => (
//                   <div key={j} className="h-8 bg-slate-700 rounded w-16"></div>
//                 ))}
//               </div>
//             </div>
//           ))}
//         </div>
//       </div>
//     </div>
//   );
// }
//
// export default function Page() {
//   return (
//     <Suspense fallback={<FeedSkeleton />}>
//       <CommunityFeed />
//     </Suspense>
//   );
// }
