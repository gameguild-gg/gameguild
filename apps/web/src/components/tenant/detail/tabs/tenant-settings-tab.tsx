'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import type { Tenant } from '@/lib/api/generated/types.gen';
import { Settings } from 'lucide-react';

interface TenantSettingsTabProps {
    tenant: Tenant;
}

export function TenantSettingsTab({ tenant }: TenantSettingsTabProps) {
    return (
        <div className="space-y-6">
            <Card>
                <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                        <Settings className="h-5 w-5" />
                        Tenant Settings
                    </CardTitle>
                </CardHeader>
                <CardContent className="text-center py-8">
                    <p className="text-muted-foreground">Tenant settings functionality will be implemented here.</p>
                    <p className="text-sm text-muted-foreground mt-2">
                        This will show configuration options for tenant: {tenant.name}
                    </p>
                </CardContent>
            </Card>
        </div>
    );
}