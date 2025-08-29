import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { TenantDetailContent } from '@/components/tenant';
import { getTenantByIdAction } from '@/lib/admin/tenants/tenants.actions';
import type { Tenant } from '@/lib/api/generated/types.gen';
import { ArrowLeft } from 'lucide-react';
import Link from 'next/link';
import { notFound } from 'next/navigation';
import { use } from 'react';

interface TenantDetailPageRouteProps {
    params: Promise<{
        tenantId: string;
    }>;
}

export default function TenantDetailPageRoute({ params }: TenantDetailPageRouteProps) {
    const { tenantId } = use(params);
    return <TenantDetailPage tenantId={tenantId} />;
}

interface TenantDetailPageProps {
    tenantId: string;
}

async function TenantDetailPage({ tenantId }: TenantDetailPageProps) {
    // Load tenant data
    let tenant: Tenant | null = null;
    try {
        const result = await getTenantByIdAction(tenantId);
        if (result.data) {
            tenant = result.data as Tenant;
        }
    } catch (error) {
        console.error('Failed to load tenant:', error);
    }

    // If tenant not found, show 404
    if (!tenant) {
        notFound();
    }

    return (
        <DashboardPage>
            <DashboardPageHeader>
                <div className="flex items-center gap-4">
                    <Link
                        href="/dashboard/tenant"
                        className="flex items-center justify-center w-8 h-8 rounded-lg border border-border hover:bg-accent"
                    >
                        <ArrowLeft className="h-4 w-4" />
                    </Link>
                    <div>
                        <DashboardPageTitle>{tenant.name}</DashboardPageTitle>
                        <DashboardPageDescription>
                            Detailed view and management of tenant information
                        </DashboardPageDescription>
                    </div>
                </div>
            </DashboardPageHeader>
            <DashboardPageContent>
                <TenantDetailContent tenant={tenant} />
            </DashboardPageContent>
        </DashboardPage>
    );
}
