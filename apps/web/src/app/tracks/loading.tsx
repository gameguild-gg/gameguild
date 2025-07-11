import { LoadingSpinner } from '@/components/ui/loading-spinner';

export default function TracksLoading() {
  return (
    <div className="flex items-center justify-center min-h-[400px]">
      <LoadingSpinner size="lg" />
      <span className="ml-2">Loading tracks...</span>
    </div>
  );
}
