import React from 'react';
import { ChapterCatalog } from '@/components/chapter-catalog';
import { getAllChapters } from '@/lib/courses/course.actions';

export default async function Page(): Promise<React.JSX.Element> {
  const chapters = await getAllChapters();

  return (
    <>
      <ChapterCatalog chapters={chapters} />
    </>
  );
}
