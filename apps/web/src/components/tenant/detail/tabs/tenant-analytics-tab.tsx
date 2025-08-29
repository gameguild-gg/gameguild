'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import type { Tenant } from '@/lib/api/generated/types.gen';
import { BarChart3 } from 'lucide-react';

interface TenantAnalyticsTabProps {
    tenant: Tenant;
}

export function TenantAnalyticsTab({ tenant }: TenantAnalyticsTabProps) {
    return (
        <div className="space-y-6">
            <Card>
                <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                        <BarChart3 className="h-5 w-5" />
                        Analytics & Reports
                    </CardTitle>
                </CardHeader>
                <CardContent className="text-center py-8">
                    <p className="text-muted-foreground">Analytics functionality will be implemented here.</p>
                    <p className="text-sm text-muted-foreground mt-2">
                        This will show usage statistics and reports for tenant: {tenant.name}
                    </p>
                </CardContent>
            </Card>
        </div>
    );
}