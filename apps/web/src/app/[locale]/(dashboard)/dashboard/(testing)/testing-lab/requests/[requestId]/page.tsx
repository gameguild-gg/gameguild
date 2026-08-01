import { redirect } from "@/i18n/navigation";

export default async function LegacyTestingRequestPage({
  params,
}: {
  params: Promise<{ locale: string; requestId: string }>;
}) {
  const { locale, requestId } = await params;
  redirect({ href: `/dashboard/testing-lab/projects/${requestId}`, locale });
}
