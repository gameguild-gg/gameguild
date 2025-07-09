import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { ArrowLeft, BookOpen } from 'lucide-react';

export default function CourseNotFound() {
  return (
    <div className="min-h-screen bg-gray-950 text-white">
      <div className="container mx-auto px-4 py-16">
        <div className="text-center max-w-2xl mx-auto">
          <div className="mb-8">
            <BookOpen className="h-24 w-24 text-gray-600 mx-auto mb-4" />
            <h1 className="text-4xl font-bold text-white mb-4">Course Not Found</h1>
            <p className="text-xl text-gray-300 mb-8">The course you&apos;re looking for doesn&apos;t exist or is not available yet.</p>
          </div>

          <div className="space-y-4">
            <div className="grid sm:grid-cols-2 gap-4 max-w-lg mx-auto">
              <Button asChild className="bg-blue-600 hover:bg-blue-700">
                <Link href="/courses">
                  <ArrowLeft className="mr-2 h-4 w-4" />
                  Browse Courses
                </Link>
              </Button>
              <Button asChild variant="outline" className="border-gray-600 text-white hover:bg-gray-800">
                <Link href="/tracks">View Learning Tracks</Link>
              </Button>
            </div>

            <div className="mt-8 p-6 bg-gray-800/50 rounded-lg border border-gray-700">
              <h3 className="text-lg font-semibold mb-2">Looking for something specific?</h3>
              <p className="text-gray-400 text-sm">
                Our course catalog is constantly growing. Check out our learning tracks to find structured paths to your game development goals.
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
