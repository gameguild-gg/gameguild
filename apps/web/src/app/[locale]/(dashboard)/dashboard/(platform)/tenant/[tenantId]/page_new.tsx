import { getQueryClient } from '@/components/get-query-client';
import { TenantDetailContent } from '@/components/tenant/tenant-detail-content';
import { tenantQueries } from '@/lib/queries/tenants.query';
import { dehydrate, HydrationBoundary } from '@tanstack/react-query';
import { Loader2 } from 'lucide-react';
import { notFound } from 'next/navigation';
import { Suspense } from 'react';

interface TenantDetailPageRouteProps {
    params: Promise<{
        tenantId: string;
    }>;
}

export default async function TenantDetailPageRoute({ params }: TenantDetailPageRouteProps) {
    const { tenantId } = await params;

    if (!tenantId) {
        notFound();
    }

    const queryClient = getQueryClient();

    try {
        // Prefetch tenant data on the server
        await Promise.all([
            queryClient.prefetchQuery(tenantQueries.detail(tenantId)),
            queryClient.prefetchQuery(tenantQueries.domainsList(tenantId)),
            queryClient.prefetchQuery(tenantQueries.userGroupsList(tenantId)),
        ]);
    } catch (error) {
        // If tenant doesn't exist, redirect to 404
        console.error('Failed to prefetch tenant data:', error);
        notFound();
    }

    return (
        <HydrationBoundary state={dehydrate(queryClient)}>
            <Suspense
                fallback={
                    <div className="flex items-center justify-center min-h-96">
                        <Loader2 className="h-8 w-8 animate-spin mr-2" />
                        <span>Loading tenant details...</span>
                    </div>
                }
            >
                <TenantDetailContent tenantId={tenantId} />
            </Suspense>
        </HydrationBoundary>
    );
}