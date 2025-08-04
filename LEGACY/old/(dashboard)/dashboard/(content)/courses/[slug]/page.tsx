import { PropsWithSlugParams } from '@/types';
import { redirect } from 'next/navigation';

export default async function Page({ params }: PropsWithSlugParams): Promise<void> {
  const { slug } = await params;

  redirect(`/dashboard/courses/${slug}/overview`);
}
