import { Skeleton } from '@/components/ui/skeleton';
import { Card, CardContent, CardHeader } from '@/components/ui/card';

export default function UserProfileLoading() {
  return (
    <div className="container mx-auto py-8 px-4">
      <div className="max-w-4xl mx-auto">
        <Card>
          <CardHeader>
            <div className="flex items-center space-x-4">
              <Skeleton className="h-20 w-20 rounded-full" />
              <div className="space-y-2">
                <div>
                  <Skeleton className="h-8 w-48" />
                  <Skeleton className="h-4 w-32 mt-2" />
                </div>
                <Skeleton className="h-6 w-20" />
              </div>
            </div>
          </CardHeader>
          <CardContent>
            <div className="space-y-6">
              <Skeleton className="h-px w-full" />
              
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Skeleton className="h-5 w-40" />
                  <div className="pl-6 space-y-1">
                    <Skeleton className="h-4 w-36" />
                    <Skeleton className="h-4 w-32" />
                  </div>
                </div>
                
                <div className="space-y-2">
                  <Skeleton className="h-5 w-32" />
                  <div className="pl-6">
                    <Skeleton className="h-4 w-28" />
                  </div>
                </div>
              </div>

              <Skeleton className="h-px w-full" />
              
              <div className="text-center">
                <Skeleton className="h-4 w-80 mx-auto" />
                <Skeleton className="h-4 w-64 mx-auto mt-1" />
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
