import { redirect } from "@/i18n/navigation";

export default async function LegacyTestingReportsPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  redirect({ href: "/console/community/testing-lab/analytics", locale });
}
