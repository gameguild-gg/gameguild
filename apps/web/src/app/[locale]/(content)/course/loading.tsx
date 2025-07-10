import { Skeleton } from '@/components/ui/skeleton';
import { Card, CardContent } from '@/components/ui/card';

export default function CourseLoading() {
  return (
    <div className="min-h-screen bg-gray-950 text-white">
      <div className="container mx-auto px-4 py-8">
        {/* Breadcrumb skeleton */}
        <div className="flex items-center gap-2 mb-6">
          <Skeleton className="h-4 w-16 bg-gray-800" />
          <span className="text-gray-600">/</span>
          <Skeleton className="h-4 w-20 bg-gray-800" />
          <span className="text-gray-600">/</span>
          <Skeleton className="h-4 w-32 bg-gray-800" />
        </div>

        {/* Hero section skeleton */}
        <div className="grid lg:grid-cols-2 gap-12 mb-16">
          <div className="space-y-6">
            <div className="flex items-center gap-3">
              <Skeleton className="h-6 w-20 bg-gray-800" />
              <Skeleton className="h-6 w-16 bg-gray-800" />
            </div>
            <Skeleton className="h-12 w-full bg-gray-800" />
            <Skeleton className="h-6 w-3/4 bg-gray-800" />
            <Skeleton className="h-4 w-full bg-gray-800" />
            <Skeleton className="h-4 w-5/6 bg-gray-800" />

            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              {[...Array(4)].map((_, i) => (
                <div key={i} className="text-center">
                  <Skeleton className="h-8 w-8 rounded mx-auto mb-2 bg-gray-800" />
                  <Skeleton className="h-4 w-16 mx-auto mb-1 bg-gray-800" />
                  <Skeleton className="h-5 w-12 mx-auto bg-gray-800" />
                </div>
              ))}
            </div>
          </div>

          <div className="relative">
            <Skeleton className="w-full h-80 rounded-lg bg-gray-800" />
          </div>
        </div>

        {/* Content sections skeleton */}
        <div className="grid lg:grid-cols-3 gap-12">
          <div className="lg:col-span-2 space-y-12">
            {/* Overview section */}
            <Card className="bg-gray-800 border-gray-700">
              <CardContent className="p-6">
                <Skeleton className="h-8 w-32 mb-4 bg-gray-700" />
                <div className="space-y-3">
                  <Skeleton className="h-4 w-full bg-gray-700" />
                  <Skeleton className="h-4 w-full bg-gray-700" />
                  <Skeleton className="h-4 w-3/4 bg-gray-700" />
                </div>
              </CardContent>
            </Card>

            {/* Learning objectives skeleton */}
            <Card className="bg-gray-800 border-gray-700">
              <CardContent className="p-6">
                <Skeleton className="h-8 w-48 mb-4 bg-gray-700" />
                <div className="space-y-3">
                  {[...Array(6)].map((_, i) => (
                    <div key={i} className="flex items-start gap-3">
                      <Skeleton className="h-5 w-5 rounded bg-gray-700 mt-0.5" />
                      <Skeleton className="h-4 w-full bg-gray-700" />
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Sidebar skeleton */}
          <div className="space-y-6">
            <Card className="bg-gray-800 border-gray-700">
              <CardContent className="p-6">
                <Skeleton className="h-8 w-24 mb-4 bg-gray-700" />
                <div className="space-y-3">
                  <Skeleton className="h-4 w-full bg-gray-700" />
                  <Skeleton className="h-4 w-3/4 bg-gray-700" />
                </div>
              </CardContent>
            </Card>

            <Card className="bg-gray-800 border-gray-700">
              <CardContent className="p-6">
                <Skeleton className="h-8 w-32 mb-4 bg-gray-700" />
                <div className="space-y-2">
                  {[...Array(4)].map((_, i) => (
                    <Skeleton key={i} className="h-8 w-20 bg-gray-700" />
                  ))}
                </div>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </div>
  );
}
