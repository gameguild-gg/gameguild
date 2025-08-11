import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { ArrowLeft, Search } from 'lucide-react';

export default function TrackNotFound() {
  return (
    <div className="min-h-screen bg-gray-950 text-white">
      <div className="container mx-auto px-4 py-16">
        <div className="text-center max-w-2xl mx-auto">
          <div className="mb-8">
            <Search className="h-24 w-24 text-gray-600 mx-auto mb-4" />
          </div>

          <h1 className="text-4xl font-bold text-red-400 mb-4">Track Not Found</h1>

          <p className="text-gray-300 mb-8 text-lg">The learning track you&apos;re looking for doesn&apos;t exist or may have been moved.</p>

          <div className="bg-gray-800/50 rounded-lg p-6 mb-8">
            <h2 className="text-xl font-semibold text-white mb-3">Available Learning Tracks:</h2>
            <ul className="text-gray-300 space-y-2 text-left">
              <li>• Beginner Track - Game Development Fundamentals</li>
              <li>• Intermediate Track - 2D Game Development</li>
              <li>• Advanced Track - Multiplayer Game Development</li>
              <li>• Specialization Track - AI in Games</li>
              <li>• Creative Track - Game Art and Animation</li>
              <li>• Business Track - Game Marketing and Monetization</li>
            </ul>
          </div>

          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Button asChild variant="outline" className="bg-transparent border-gray-600 text-white hover:bg-gray-800">
              <Link href="/tracks">
                <ArrowLeft className="mr-2 h-4 w-4" />
                Back to Learning Tracks
              </Link>
            </Button>

            <Button asChild className="bg-blue-600 hover:bg-blue-700">
              <Link href="/courses/catalog">Browse All Courses</Link>
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
