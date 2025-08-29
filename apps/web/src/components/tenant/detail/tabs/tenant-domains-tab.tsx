'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import type { Tenant } from '@/lib/api/generated/types.gen';
import { Globe } from 'lucide-react';

interface TenantDomainsTabProps {
    tenant: Tenant;
}

export function TenantDomainsTab({ tenant }: TenantDomainsTabProps) {
    return (
        <div className="space-y-6">
            <Card>
                <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                        <Globe className="h-5 w-5" />
                        Domain Management
                    </CardTitle>
                </CardHeader>
                <CardContent className="text-center py-8">
                    <p className="text-muted-foreground">Domain management functionality will be implemented here.</p>
                    <p className="text-sm text-muted-foreground mt-2">
                        This will show all domains associated with tenant: {tenant.name}
                    </p>
                </CardContent>
            </Card>
        </div>
    );
}