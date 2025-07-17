import { fetchCourses } from '@/components/courses/actions';
import CourseList from '@/components/courses/course-list';
import Link from 'next/link';

export default async function CategoryCatalogPage({ params }: { params: { category: string } }) {
  const allCourses = await fetchCourses();
  const { category: categoryParam } = await params;
  const category = decodeURIComponent(categoryParam);
  const courses = allCourses.filter((c) => c.category.toLowerCase() === category.toLowerCase());

  // For sidebar: gather all unique categories
  const categories = Array.from(new Set(allCourses.map((c) => c.category)));

  return (
    <div className="flex flex-col md:flex-row gap-8">
      {/* Sidebar */}
      <aside className="md:w-64 w-full md:sticky top-24 flex-shrink-0 border-r border-border bg-background/80 p-4 rounded-xl">
        <h2 className="font-semibold mb-4 text-lg">Categories</h2>
        <ul className="space-y-2">
          {categories.map((cat) => (
            <li key={cat}>
              <Link
                href={`/courses/${encodeURIComponent(cat)}`}
                className={`block px-3 py-2 rounded hover:bg-accent transition ${cat.toLowerCase() === category.toLowerCase() ? 'bg-primary text-primary-foreground font-bold' : ''}`}
              >
                {cat}
              </Link>
            </li>
          ))}
        </ul>
      </aside>
      {/* Main Content */}
      <main className="flex-1 min-w-0">
        <h1 className="text-3xl font-bold mb-2 capitalize">{category} Courses</h1>
        <p className="mb-6 text-muted-foreground">
          Browse all courses in the <span className="font-semibold">{category}</span> category.
        </p>
        <CourseList courses={courses} />
      </main>
    </div>
  );
}
