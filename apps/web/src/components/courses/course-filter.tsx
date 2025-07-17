import Link from 'next/link';

export default function CourseFilter({ categories, selected }: { categories: string[]; selected: string }) {
  return (
    <div className="flex gap-2 mb-6">
      <Link
        href={{ query: { category: undefined } }}
        className={`px-3 py-1 rounded ${selected === 'All' ? 'bg-primary text-primary-foreground' : 'bg-muted text-muted-foreground'}`}
        scroll={false}
      >
        All
      </Link>
      {categories.map((cat) => (
        <Link
          key={cat}
          href={{ query: { category: cat } }}
          className={`px-3 py-1 rounded ${selected === cat ? 'bg-primary text-primary-foreground' : 'bg-muted text-muted-foreground'}`}
          scroll={false}
        >
          {cat}
        </Link>
      ))}
    </div>
  );
}
