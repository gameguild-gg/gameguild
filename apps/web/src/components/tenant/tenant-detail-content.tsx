'use client';

import { Alert, AlertDescription } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { tenantQueries, useUpdateTenant } from '@/lib/queries/tenants.query';
import type { ModulesTenantsTenant, ModulesTenantsTenantDomain, ModulesTenantsTenantUserGroup } from '@/lib/api/generated/types.gen';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AlertCircle, ArrowLeft, Building, Calendar, Copy, Edit, Loader2, Save, X } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

interface TenantDetailContentProps {
    tenantId: string;
}

// Helper functions for access level display
function getAccessLevelName(visibility: number): string {
    switch (visibility) {
        case 0: return 'Private';
        case 1: return 'Public';
        case 2: return 'Restricted';
        case 5: return 'Unlisted';
        case 6: return 'Protected';
        default: return 'Unknown';
    }
}

function getVisibilityBadgeVariant(visibility: number): 'default' | 'secondary' | 'destructive' | 'outline' {
    switch (visibility) {
        case 1: return 'default';    // Public
        case 0: return 'secondary';  // Private
        case 2: return 'outline';    // Restricted
        case 5: return 'destructive'; // Unlisted
        case 6: return 'outline';    // Protected
        default: return 'secondary';
    }
}

export function TenantDetailContent({ tenantId }: TenantDetailContentProps) {
    const router = useRouter();
    const queryClient = useQueryClient();

    // Queries
    const {
        data: tenant,
        isLoading,
        error
    } = useQuery(tenantQueries.detail(tenantId));

    const { 
        data: domains = [] 
    } = useQuery(tenantQueries.domainsList(tenantId));
    
    const { 
        data: userGroups = [] 
    } = useQuery(tenantQueries.userGroupsList(tenantId));    // Mutations
    const updateTenantMutation = useUpdateTenant();
    
    // Handle success/error states
    useEffect(() => {
        if (updateTenantMutation.isSuccess) {
            toast.success('Tenant updated successfully');
            setEditingBasicInfo(false);
        }
        if (updateTenantMutation.isError) {
            toast.error('Failed to update tenant');
        }
    }, [updateTenantMutation.isSuccess, updateTenantMutation.isError]);

    // State management
    const [editingBasicInfo, setEditingBasicInfo] = useState(false);
    const [basicForm, setBasicForm] = useState({
        name: '',
        title: '',
        description: '',
        slug: '',
        visibility: 0,
    });

    // Update form when tenant data loads
    useEffect(() => {
        if (tenant) {
            setBasicForm({
                name: tenant.name || '',
                title: tenant.title || '',
                description: tenant.description || '',
                slug: tenant.slug || '',
                visibility: tenant.visibility || 0,
            });
        }
    }, [tenant]);

    const handleBack = () => {
        router.back();
    };

    const handleSaveBasicInfo = async () => {
        if (!tenant?.id) return;

        updateTenantMutation.mutate({
            id: tenant.id,
            data: basicForm,
        });
    };

    const formatDate = (dateString?: string) => {
        if (!dateString) return 'Not available';
        return new Date(dateString).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'long',
            day: 'numeric',
        });
    };

    const copyToClipboard = (text: string) => {
        navigator.clipboard.writeText(text);
        toast.success('Copied to clipboard');
    };

    if (isLoading) {
        return (
            <div className="flex items-center justify-center min-h-96">
                <Loader2 className="h-8 w-8 animate-spin mr-2" />
                <span>Loading tenant details...</span>
            </div>
        );
    }

    if (error || !tenant) {
        return (
            <div className="container mx-auto p-6">
                <div className="flex items-center gap-4 mb-6">
                    <Button
                        variant="ghost"
                        size="sm"
                        onClick={handleBack}
                        className="flex items-center gap-2"
                    >
                        <ArrowLeft className="h-4 w-4" />
                        Back
                    </Button>
                </div>

                <Alert variant="destructive">
                    <AlertCircle className="h-4 w-4" />
                    <AlertDescription>
                        {error ? 'Failed to load tenant details' : 'Tenant not found'}
                    </AlertDescription>
                </Alert>
            </div>
        );
    }

    return (
        <div className="container mx-auto p-6 max-w-6xl">
            {/* Header */}
            <div className="flex items-center justify-between mb-6">
                <div className="flex items-center gap-4">
                    <Button
                        variant="ghost"
                        size="sm"
                        onClick={handleBack}
                        className="flex items-center gap-2"
                    >
                        <ArrowLeft className="h-4 w-4" />
                        Back
                    </Button>
                    <div className="flex items-center gap-2">
                        <Building className="h-5 w-5" />
                        <h1 className="text-2xl font-bold">{tenant.name}</h1>
                        <Badge variant={getVisibilityBadgeVariant(tenant.visibility || 0)}>
                            {getAccessLevelName(tenant.visibility || 0)}
                        </Badge>
                    </div>
                </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                {/* Main Content */}
                <div className="lg:col-span-2 space-y-6">
                    {/* Basic Information Card */}
                    <Card>
                        <CardHeader className="flex flex-row items-center justify-between">
                            <CardTitle>Basic Information</CardTitle>
                            <Button
                                variant="outline"
                                size="sm"
                                onClick={() => setEditingBasicInfo(!editingBasicInfo)}
                                className="flex items-center gap-2"
                                disabled={updateTenantMutation.isPending}
                            >
                                {editingBasicInfo ? (
                                    <>
                                        <X className="h-4 w-4" />
                                        Cancel
                                    </>
                                ) : (
                                    <>
                                        <Edit className="h-4 w-4" />
                                        Edit
                                    </>
                                )}
                            </Button>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            {editingBasicInfo ? (
                                <>
                                    <div className="grid grid-cols-2 gap-4">
                                        <div>
                                            <Label htmlFor="tenant-name">Name</Label>
                                            <Input
                                                id="tenant-name"
                                                value={basicForm.name}
                                                onChange={(e) => setBasicForm(prev => ({ ...prev, name: e.target.value }))}
                                                placeholder="Enter tenant name"
                                            />
                                        </div>
                                        <div>
                                            <Label htmlFor="tenant-title">Title</Label>
                                            <Input
                                                id="tenant-title"
                                                value={basicForm.title}
                                                onChange={(e) => setBasicForm(prev => ({ ...prev, title: e.target.value }))}
                                                placeholder="Enter tenant title"
                                            />
                                        </div>
                                    </div>
                                    <div>
                                        <Label htmlFor="tenant-slug">Slug</Label>
                                        <Input
                                            id="tenant-slug"
                                            value={basicForm.slug}
                                            onChange={(e) => setBasicForm(prev => ({ ...prev, slug: e.target.value }))}
                                            placeholder="Enter tenant slug"
                                        />
                                    </div>
                                    <div>
                                        <Label htmlFor="tenant-description">Description</Label>
                                        <Textarea
                                            id="tenant-description"
                                            value={basicForm.description}
                                            onChange={(e) => setBasicForm(prev => ({ ...prev, description: e.target.value }))}
                                            placeholder="Enter tenant description"
                                            rows={3}
                                        />
                                    </div>
                                    <div>
                                        <Label htmlFor="tenant-visibility">Visibility</Label>
                                        <Select
                                            value={basicForm.visibility.toString()}
                                            onValueChange={(value) => setBasicForm(prev => ({ ...prev, visibility: parseInt(value) }))}
                                        >
                                            <SelectTrigger>
                                                <SelectValue />
                                            </SelectTrigger>
                                            <SelectContent>
                                                <SelectItem value="0">Private</SelectItem>
                                                <SelectItem value="1">Public</SelectItem>
                                                <SelectItem value="2">Restricted</SelectItem>
                                                <SelectItem value="5">Unlisted</SelectItem>
                                                <SelectItem value="6">Protected</SelectItem>
                                            </SelectContent>
                                        </Select>
                                    </div>
                                    <div className="flex justify-end gap-2">
                                        <Button
                                            variant="outline"
                                            onClick={() => setEditingBasicInfo(false)}
                                            disabled={updateTenantMutation.isPending}
                                        >
                                            Cancel
                                        </Button>
                                        <Button
                                            onClick={handleSaveBasicInfo}
                                            disabled={updateTenantMutation.isPending}
                                        >
                                            {updateTenantMutation.isPending ? (
                                                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                                            ) : (
                                                <Save className="h-4 w-4 mr-2" />
                                            )}
                                            Save Changes
                                        </Button>
                                    </div>
                                </>
                            ) : (
                                <>
                                    <div className="grid grid-cols-2 gap-4">
                                        <div>
                                            <Label className="text-sm font-medium text-gray-500">Name</Label>
                                            <p className="text-sm">{tenant.name}</p>
                                        </div>
                                        <div>
                                            <Label className="text-sm font-medium text-gray-500">Title</Label>
                                            <p className="text-sm">{tenant.title || 'Not set'}</p>
                                        </div>
                                    </div>
                                    <div>
                                        <Label className="text-sm font-medium text-gray-500">Slug</Label>
                                        <div className="flex items-center gap-2">
                                            <p className="text-sm font-mono bg-gray-100 px-2 py-1 rounded">{tenant.slug}</p>
                                            <Button
                                                variant="ghost"
                                                size="sm"
                                                onClick={() => copyToClipboard(tenant.slug || '')}
                                            >
                                                <Copy className="h-3 w-3" />
                                            </Button>
                                        </div>
                                    </div>
                                    <div>
                                        <Label className="text-sm font-medium text-gray-500">Description</Label>
                                        <p className="text-sm">{tenant.description || 'No description provided'}</p>
                                    </div>
                                    <div>
                                        <Label className="text-sm font-medium text-gray-500">Visibility</Label>
                                        <div className="flex items-center gap-2">
                                            <Badge variant={getVisibilityBadgeVariant(tenant.visibility || 0)}>
                                                {getAccessLevelName(tenant.visibility || 0)}
                                            </Badge>
                                        </div>
                                    </div>
                                </>
                            )}
                        </CardContent>
                    </Card>

                    {/* Domains Section */}
                    {(domains as ModulesTenantsTenantDomain[]).length > 0 && (
                        <Card>
                            <CardHeader>
                                <CardTitle>Domains ({(domains as ModulesTenantsTenantDomain[]).length})</CardTitle>
                            </CardHeader>
                            <CardContent>
                                <div className="space-y-2">
                                    {(domains as ModulesTenantsTenantDomain[]).map((domain) => (
                                        <div key={domain.id} className="flex items-center justify-between p-2 border rounded">
                                            <span className="font-mono text-sm">{domain.fullDomainName || `${domain.subdomain || ''}.${domain.topLevelDomain}`}</span>
                                            <Badge variant={domain.isMainDomain ? 'default' : 'secondary'}>
                                                {domain.isMainDomain ? 'Main' : 'Secondary'}
                                            </Badge>
                                        </div>
                                    ))}
                                </div>
                            </CardContent>
                        </Card>
                    )}

                    {/* User Groups Section */}
                    {(userGroups as ModulesTenantsTenantUserGroup[]).length > 0 && (
                        <Card>
                            <CardHeader>
                                <CardTitle>User Groups ({(userGroups as ModulesTenantsTenantUserGroup[]).length})</CardTitle>
                            </CardHeader>
                            <CardContent>
                                <div className="space-y-2">
                                    {(userGroups as ModulesTenantsTenantUserGroup[]).map((group) => (
                                        <div key={group.id} className="flex items-center justify-between p-2 border rounded">
                                            <div>
                                                <div className="font-medium">{group.name}</div>
                                                <div className="text-sm text-gray-500">{group.description}</div>
                                            </div>
                                            <Badge variant={group.isActive ? 'default' : 'secondary'}>
                                                {group.memberships?.length || 0} members
                                            </Badge>
                                        </div>
                                    ))}
                                </div>
                            </CardContent>
                        </Card>
                    )}

                    {/* Additional Information */}
                    <Card>
                        <CardHeader>
                            <CardTitle>Additional Information</CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label className="text-sm font-medium text-gray-500">Tenant ID</Label>
                                    <div className="flex items-center gap-2">
                                        <p className="text-sm font-mono bg-gray-100 px-2 py-1 rounded">{tenant.id}</p>
                                        <Button
                                            variant="ghost"
                                            size="sm"
                                            onClick={() => copyToClipboard(tenant.id || '')}
                                        >
                                            <Copy className="h-3 w-3" />
                                        </Button>
                                    </div>
                                </div>
                                <div>
                                    <Label className="text-sm font-medium text-gray-500">Admin Email</Label>
                                    <p className="text-sm">{tenant.adminEmail || 'Not set'}</p>
                                </div>
                            </div>
                        </CardContent>
                    </Card>
                </div>

                {/* Sidebar */}
                <div className="space-y-6">
                    {/* Timestamps */}
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Calendar className="h-4 w-4" />
                                Timeline
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-3">
                            <div>
                                <Label className="text-sm font-medium text-gray-500">Created</Label>
                                <p className="text-sm">{formatDate(tenant.createdAt)}</p>
                            </div>
                            <div>
                                <Label className="text-sm font-medium text-gray-500">Updated</Label>
                                <p className="text-sm">{formatDate(tenant.updatedAt)}</p>
                            </div>
                        </CardContent>
                    </Card>

                    {/* Metadata */}
                    {tenant.metadata && Object.keys(tenant.metadata).length > 0 && (
                        <Card>
                            <CardHeader>
                                <CardTitle>Metadata</CardTitle>
                            </CardHeader>
                            <CardContent>
                                <pre className="text-xs bg-gray-50 p-3 rounded border overflow-auto">
                                    {JSON.stringify(tenant.metadata, null, 2)}
                                </pre>
                            </CardContent>
                        </Card>
                    )}

                    {/* Settings */}
                    {tenant.settings && Object.keys(tenant.settings).length > 0 && (
                        <Card>
                            <CardHeader>
                                <CardTitle>Settings</CardTitle>
                            </CardHeader>
                            <CardContent>
                                <pre className="text-xs bg-gray-50 p-3 rounded border overflow-auto">
                                    {JSON.stringify(tenant.settings, null, 2)}
                                </pre>
                            </CardContent>
                        </Card>
                    )}
                </div>
            </div>

            {/* Full-width sections */}
            <div className="mt-8 space-y-6">
                {/* Localizations */}
                {tenant.localizations && tenant.localizations.length > 0 && (
                    <Card>
                        <CardHeader>
                            <CardTitle>Localizations</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <div className="text-sm text-gray-600 mb-4">
                                This tenant has {tenant.localizations.length} localization(s) available.
                            </div>
                            <div className="text-xs text-gray-500">
                                Detailed localization display would require examining the ResourceLocalization interface structure.
                            </div>
                        </CardContent>
                    </Card>
                )}
            </div>
        </div>
    );
}
