'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import type { Tenant } from '@/lib/api/generated/types.gen';
import { Users } from 'lucide-react';

interface TenantUsersTabProps {
    tenant: Tenant;
}

export function TenantUsersTab({ tenant }: TenantUsersTabProps) {
    return (
        <div className="space-y-6">
            <Card>
                <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                        <Users className="h-5 w-5" />
                        Users Management
                    </CardTitle>
                </CardHeader>
                <CardContent className="text-center py-8">
                    <p className="text-muted-foreground">User management functionality will be implemented here.</p>
                    <p className="text-sm text-muted-foreground mt-2">
                        This will show all users belonging to tenant: {tenant.name}
                    </p>
                </CardContent>
            </Card>
        </div>
    );
}