import { redirect } from 'next/navigation';

export default async function CourseContentPage({ params }: PageProps<'/[locale]/courses/[course]/content'>): Promise<React.JSX.Element> {
    const { course, locale } = await params;

    // Keep legacy public course-content URLs on the same application. An external
    // learning origin can point back here during a staged host migration.
    redirect(`/${locale}/learn/courses/${encodeURIComponent(course)}/content`);
}
