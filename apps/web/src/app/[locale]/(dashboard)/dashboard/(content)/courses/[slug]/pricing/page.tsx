import React from 'react';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard/common/ui/dashboard-page';
import { getProgramBySlugService } from '@/lib/content-management/programs/programs.service';
import ProgramPricingForm from '@/components/programs/program-pricing-form';
import { getProgramPricing, monetizeProgram, disableProgramMonetization, updateProgramPricing } from '@/lib/content-management/programs/programs.actions';
import { redirect } from 'next/navigation';
import type { PutApiProgramByIdPricingData } from '@/lib/api/generated/types.gen';

interface PageProps {
  params: Promise<{ slug: string; locale: string }>;
}

export default async function Page({ params }: PageProps): Promise<React.JSX.Element> {
  const { slug } = await params;

  const result = await getProgramBySlugService(slug);
  const program = result.success ? result.data : null;

  async function savePricing(formData: FormData) {
    'use server';
    if (!program?.id) return;
    const priceVal = formData.get('price');
    const price = priceVal === null || priceVal === '' ? null : Number(priceVal);
    const currency = String(formData.get('currency') ?? 'USD');
    const isSubscription = String(formData.get('isSubscription') ?? 'false') === 'true';
    const durVal = formData.get('subscriptionDurationDays');
    const subscriptionDurationDays = isSubscription ? (durVal ? Number(durVal) : 30) : null;

    const body: PutApiProgramByIdPricingData['body'] = {
      price: price as any,
      currency,
      isSubscription,
      subscriptionDurationDays: subscriptionDurationDays as any,
    };

    await updateProgramPricing(program.id, body as any);
    redirect(`/dashboard/courses/${slug}/pricing`);
  }

  async function enableMonetization() {
    'use server';
    if (!program?.id) return;
    await monetizeProgram(program.id);
    redirect(`/dashboard/courses/${slug}/pricing`);
  }

  async function disableMonetizationAction() {
    'use server';
    if (!program?.id) return;
    await disableProgramMonetization(program.id);
    redirect(`/dashboard/courses/${slug}/pricing`);
  }

  // Fetch current pricing
  const pricingRes = program?.id ? await getProgramPricing(program.id) : null;
  const initialPricing = pricingRes?.data ?? { price: 0, currency: 'USD', isSubscription: false, subscriptionDurationDays: null, isMonetizationEnabled: false };

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Sales & Pricing</DashboardPageTitle>
        <DashboardPageDescription>Configure course pricing, products, and sales showcase settings</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        {program ? (
          <ProgramPricingForm
            initial={initialPricing}
            saveAction={savePricing}
            monetizeAction={enableMonetization}
            disableMonetizationAction={disableMonetizationAction}
          />
        ) : (
          <div className="text-muted-foreground">Program not found.</div>
        )}
      </DashboardPageContent>
    </DashboardPage>
  );
}
