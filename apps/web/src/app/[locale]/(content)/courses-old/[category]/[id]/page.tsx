import { getCourse } from '@/components/courses/actions';
import Image from 'next/image';
import Link from 'next/link';

export default async function CourseDetailPage({ params }: { params: { category: string; id: string } }) {
  const { id, category } = await params;
  const course = await getCourse(Number(id));
  if (!course) {
    return <div className="p-8 text-center text-destructive">Course not found.</div>;
  }
  return (
    <div className="max-w-3xl mx-auto p-6 bg-card rounded-xl shadow-lg mt-8">
      <Link href={`/courses/${encodeURIComponent(category)}`} className="text-primary hover:underline text-sm mb-4 inline-block">
        ← Back to {category} courses
      </Link>
      <div className="flex flex-col md:flex-row gap-6">
        <div className="md:w-1/3 w-full flex-shrink-0">
          <div className="relative w-full aspect-[4/3] rounded-lg overflow-hidden bg-muted">
            <Image src={course.image} alt={course.title} fill className="object-cover" />
          </div>
        </div>
        <div className="flex-1 flex flex-col gap-2">
          <h1 className="text-2xl font-bold mb-2">{course.title}</h1>
          <div className="flex gap-2 mb-2">
            {course.level && <span className="bg-muted text-xs text-muted-foreground px-2 py-1 rounded-full">{course.level}</span>}
            {course.duration && <span className="bg-muted text-xs text-muted-foreground px-2 py-1 rounded-full">{course.duration}</span>}
          </div>
          <p className="text-base text-muted-foreground mb-4">{course.description}</p>
          <div className="mt-auto">
            <button className="px-6 py-2 rounded bg-primary text-primary-foreground font-semibold hover:bg-primary/80 transition">Enroll Now</button>
          </div>
        </div>
      </div>
    </div>
  );
}
