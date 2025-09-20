'use client';

import { Alert, AlertDescription } from '@/components/ui/alert';
import { Card } from '@/components/ui/card';
import { tenantQueries } from '@/lib/queries/tenants.query';
import { useQuery } from '@tanstack/react-query';
import { Loader2 } from 'lucide-react';
import { TenantsList } from './tenants-list';

export function TenantManagementContent() {
    const {
        data: tenants = [],
        isLoading,
        error
    } = useQuery(tenantQueries.list());

    const {
        data: stats,
        isLoading: isStatsLoading
    } = useQuery(tenantQueries.stats());

    if (isLoading) {
        return (
            <div className="flex items-center justify-center p-8">
                <Loader2 className="h-6 w-6 animate-spin mr-2" />
                <span>Loading tenant data...</span>
            </div>
        );
    }

    if (error) {
        return (
            <Alert variant="destructive">
                <AlertDescription>
                    Failed to load tenant data. Please try again.
                </AlertDescription>
            </Alert>
        );
    }

    return (
        <div className="space-y-6">
            {/* Tenant Statistics */}
            {stats && !isStatsLoading && (
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <Card className="p-4">
                        <div className="text-2xl font-bold">{stats.totalTenants}</div>
                        <div className="text-sm text-muted-foreground">Total Tenants</div>
                    </Card>
                    <Card className="p-4">
                        <div className="text-2xl font-bold">{stats.activeTenants}</div>
                        <div className="text-sm text-muted-foreground">Active Tenants</div>
                    </Card>
                    <Card className="p-4">
                        <div className="text-2xl font-bold">{stats.domainsCount}</div>
                        <div className="text-sm text-muted-foreground">Total Domains</div>
                    </Card>
                </div>
            )}

            {/* Tenants List */}
            <TenantsList tenants={tenants} />
        </div>
    );
}
