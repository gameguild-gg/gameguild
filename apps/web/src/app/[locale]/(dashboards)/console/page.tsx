import { redirect } from 'next/navigation';

export default async function ConsoleHomePage({ params }: { params: Promise<{ locale: string }> }): Promise<never> {
  const { locale } = await params;
  redirect(`/${locale}/console/community`);
}
