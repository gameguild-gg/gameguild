import { Button } from '@game-guild/ui/components/button';
import { BookX } from 'lucide-react';
import Link from 'next/link';

export default function LearningNotFound() {
  return (
    <section className="flex min-h-[28rem] flex-col items-center justify-center border-y text-center">
      <BookX className="size-9 text-muted-foreground" />
      <h1 className="mt-4 text-xl font-semibold">Learning resource not found</h1>
      <p className="mt-2 max-w-md text-sm text-muted-foreground">
        The course, lesson, or activity may have moved or is not available to your enrollment.
      </p>
      <Button asChild className="mt-6">
        <Link href="/courses">Return to my courses</Link>
      </Button>
    </section>
  );
}
