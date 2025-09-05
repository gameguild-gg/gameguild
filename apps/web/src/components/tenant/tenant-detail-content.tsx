'use client';

import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import type { Tenant } from '@/lib/api/generated/types.gen';
import { BarChart3, Building, Globe, Settings, Users } from 'lucide-react';
import { TenantAnalyticsTab } from './detail/tabs/tenant-analytics-tab';
import { TenantDomainsTab } from './detail/tabs/tenant-domains-tab';
import { TenantOverviewTab } from './detail/tabs/tenant-overview-tab';
import { TenantSettingsTab } from './detail/tabs/tenant-settings-tab';
import { TenantUsersTab } from './detail/tabs/tenant-users-tab';

interface TenantDetailContentProps {
    tenant: Tenant;
}

export function TenantDetailContent({ tenant }: TenantDetailContentProps) {
    return (
        <div className="space-y-6">
            <Tabs defaultValue="overview" className="w-full">
                <TabsList className="grid w-full grid-cols-5">
                    <TabsTrigger value="overview" className="flex items-center gap-2">
                        <Building className="h-4 w-4" />
                        Overview
                    </TabsTrigger>
                    <TabsTrigger value="users" className="flex items-center gap-2">
                        <Users className="h-4 w-4" />
                        Users
                    </TabsTrigger>
                    <TabsTrigger value="domains" className="flex items-center gap-2">
                        <Globe className="h-4 w-4" />
                        Domains
                    </TabsTrigger>
                    <TabsTrigger value="analytics" className="flex items-center gap-2">
                        <BarChart3 className="h-4 w-4" />
                        Analytics
                    </TabsTrigger>
                    <TabsTrigger value="settings" className="flex items-center gap-2">
                        <Settings className="h-4 w-4" />
                        Settings
                    </TabsTrigger>
                </TabsList>

                <TabsContent value="overview" className="mt-6">
                    <TenantOverviewTab tenant={tenant} />
                </TabsContent>

                <TabsContent value="users" className="mt-6">
                    <TenantUsersTab tenant={tenant} />
                </TabsContent>

                <TabsContent value="domains" className="mt-6">
                    <TenantDomainsTab tenant={tenant} />
                </TabsContent>

                <TabsContent value="analytics" className="mt-6">
                    <TenantAnalyticsTab tenant={tenant} />
                </TabsContent>

                <TabsContent value="settings" className="mt-6">
                    <TenantSettingsTab tenant={tenant} />
                </TabsContent>
            </Tabs>
        </div>
    );
}
