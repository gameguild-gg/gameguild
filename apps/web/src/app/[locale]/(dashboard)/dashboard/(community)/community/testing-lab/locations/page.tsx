import { redirect } from "@/i18n/navigation";

export default async function LegacyTestingLocationsPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  redirect({ href: "/dashboard/community/testing-lab/settings/locations", locale });
}
