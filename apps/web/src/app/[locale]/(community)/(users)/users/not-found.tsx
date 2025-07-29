import { Button } from '@/components/ui/button';
import Link from 'next/link';

export default function UserNotFound() {
  return (
    <div className="container mx-auto max-w-2xl px-4 py-16 text-center">
      <div className="space-y-6">
        <div className="space-y-2">
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white">User Not Found</h1>
          <p className="text-lg text-gray-600 dark:text-gray-400">The user you're looking for doesn't exist or may have been removed.</p>
        </div>

        <div className="space-y-4">
          <p className="text-gray-500 dark:text-gray-500">This could happen if:</p>
          <ul className="text-sm text-gray-500 dark:text-gray-500 space-y-1">
            <li>• The username was typed incorrectly</li>
            <li>• The user account has been deactivated</li>
            <li>• The user has changed their username</li>
          </ul>
        </div>

        <div className="flex gap-4 justify-center pt-6">
          <Button asChild>
            <Link href="/courses">Browse Courses</Link>
          </Button>
          <Button variant="outline" asChild>
            <Link href="/old/public">Go Home</Link>
          </Button>
        </div>
      </div>
    </div>
  );
}
