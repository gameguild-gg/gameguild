'use client';

import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import type { Tenant } from '@/lib/api/generated';
import { Building, Calendar, Globe } from 'lucide-react';

type TenantWithVisibility = Tenant & { visibility?: string | number | null };

interface TenantOverviewTabProps {
    tenant: TenantWithVisibility;
}

export function TenantOverviewTab({ tenant }: TenantOverviewTabProps) {
    const formatDate = (dateString?: string) => {
        if (!dateString) return 'N/A';
        return new Date(dateString).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'long',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    };

    const mapVisibility = (visibility: TenantWithVisibility['visibility']) => {
        const value = typeof visibility === 'number' ? visibility : String(visibility ?? '').toLowerCase();
        if (value === 0 || value === '0' || value === 'private') return 'Private';
        if (value === 1 || value === '1' || value === 'public') return 'Public';
        if (value === 2 || value === '2' || value === 'restricted') return 'Restricted';
        if (!visibility) return undefined;
        return String(visibility);
    };

    return (
        <div className="space-y-6">
            {/* Tenant Information */}
            <div className="grid gap-6 md:grid-cols-2">
                <Card>
                    <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                            <Building className="h-5 w-5" />
                            Basic Information
                        </CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        <div>
                            <label className="text-sm font-medium text-muted-foreground">Name</label>
                            <p className="text-sm">{tenant.name}</p>
                        </div>
                        <div>
                            <label className="text-sm font-medium text-muted-foreground">Slug</label>
                            <p className="text-sm font-mono">{tenant.slug}</p>
                        </div>
                        <div>
                            <label className="text-sm font-medium text-muted-foreground">Description</label>
                            <p className="text-sm">{tenant.description || 'No description provided'}</p>
                        </div>
                        <div>
                            <label className="text-sm font-medium text-muted-foreground">Status</label>
                            <div>
                                <Badge variant={tenant.isActive ? 'default' : 'secondary'} className={tenant.isActive ? 'bg-green-100 text-green-800' : ''}>
                                    {tenant.isActive ? 'Active' : 'Inactive'}
                                </Badge>
                            </div>
                        </div>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                            <Calendar className="h-5 w-5" />
                            Timestamps
                        </CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        <div>
                            <label className="text-sm font-medium text-muted-foreground">Created</label>
                            <p className="text-sm">{formatDate(tenant.createdAt)}</p>
                        </div>
                        <div>
                            <label className="text-sm font-medium text-muted-foreground">Last Updated</label>
                            <p className="text-sm">{formatDate(tenant.updatedAt)}</p>
                        </div>
                        <div>
                            <label className="text-sm font-medium text-muted-foreground">Tenant ID</label>
                            <p className="text-sm font-mono">{tenant.id}</p>
                        </div>
                    </CardContent>
                </Card>
            </div>

            {/* Additional Information */}
            {(() => {
                const visibilityLabel = mapVisibility(tenant.visibility);
                if (!visibilityLabel) return null;
                return (
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Globe className="h-5 w-5" />
                                Visibility Settings
                            </CardTitle>
                        </CardHeader>
                        <CardContent>
                            <div>
                                <label className="text-sm font-medium text-muted-foreground">Visibility</label>
                                <p className="text-sm">{visibilityLabel}</p>
                            </div>
                        </CardContent>
                    </Card>
                );
            })()}
        </div>
    );
}