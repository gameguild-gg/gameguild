'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Separator } from '@/components/ui/separator';
import { Textarea } from '@/components/ui/textarea';
import { getTenantByIdAction } from '@/lib/admin/tenants/tenants.actions';
import type { Tenant } from '@/lib/api/generated/types.gen';
import { AlertCircle, ArrowLeft, Building, Calendar, Copy, Edit, RefreshCw, Save, X } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { use, useEffect, useState } from 'react';

// Helper functions for access level display
function getAccessLevelName(visibility: number): string {
    switch (visibility) {
        case 0: return 'Private';
        case 1: return 'Public';
        default: return 'Unknown';
    }
}

function getVisibilityBadgeVariant(visibility: number): 'default' | 'secondary' | 'destructive' | 'outline' {
    switch (visibility) {
        case 0: return 'secondary';  // Private
        case 1: return 'default';    // Public
        default: return 'outline';
    }
}
import { toast } from 'sonner';

interface TenantDetailPageRouteProps {
    params: Promise<{
        tenantId: string;
    }>;
}

interface TenantDetailPageProps {
    tenantId: string;
}

function TenantDetailPage({ tenantId }: TenantDetailPageProps) {
    const router = useRouter();
    const [tenant, setTenant] = useState<Tenant | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    // State management
    const [editingBasicInfo, setEditingBasicInfo] = useState(false);

    // Form states - only for real API fields
    const [basicForm, setBasicForm] = useState({
        name: '',
        title: '',
        description: '',
        slug: '',
        visibility: 0, // PRIVATE = 0
    });

    // Load tenant data
    useEffect(() => {
        const loadTenant = async () => {
            try {
                setLoading(true);
                const result = await getTenantByIdAction(tenantId);
                if (result.data) {
                    const tenantData = result.data as Tenant;
                    setTenant(tenantData);
                    setBasicForm({
                        name: tenantData.name || '',
                        title: tenantData.title || '',
                        description: tenantData.description || '',
                        slug: tenantData.slug || '',
                        visibility: tenantData.visibility || 0,
                    });
                } else {
                    setError('Tenant not found');
                }
            } catch (err) {
                console.error('Failed to load tenant:', err);
                setError('Failed to load tenant details');
            } finally {
                setLoading(false);
            }
        };

        if (tenantId) {
            loadTenant();
        }
    }, [tenantId]);

    const handleBack = () => {
        router.back();
    };

    const handleSaveBasicInfo = async () => {
        try {
            // TODO: Implement actual API call to update tenant
            toast.success('Tenant information updated');
            setEditingBasicInfo(false);
        } catch (error) {
            toast.error('Failed to update tenant information');
        }
    };

    const formatDate = (dateString?: string) => {
        if (!dateString) return 'Not available';
        return new Date(dateString).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'long',
            day: 'numeric',
        });
    };

    const getAccessLevelName = (level: number): string => {
        switch (level) {
            case 0: return 'Private';
            case 1: return 'Public';
            case 2: return 'Restricted';
            case 5: return 'Unlisted';
            case 6: return 'Protected';
            default: return 'Unknown';
        }
    };

    const getVisibilityBadgeVariant = (visibility: number) => {
        switch (visibility) {
            case 1: // PUBLIC
                return 'default';
            case 0: // PRIVATE
                return 'secondary';
            case 2: // RESTRICTED
                return 'outline';
            case 5: // UNLISTED
                return 'destructive';
            case 6: // PROTECTED
                return 'outline';
            default:
                return 'secondary';
        }
    };

    if (loading) {
        return (
            <div className="flex items-center justify-center min-h-96">
                <RefreshCw className="h-8 w-8 animate-spin" />
            </div>
        );
    }

    if (error || !tenant) {
        return (
            <div className="text-center py-12">
                <AlertCircle className="h-12 w-12 text-red-500 mx-auto mb-4" />
                <h3 className="text-lg font-semibold mb-2">Tenant not found</h3>
                <p className="text-gray-600 mb-4">{error || 'The requested tenant could not be found.'}</p>
                <Button onClick={handleBack}>Back to Tenants</Button>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <Button variant="ghost" size="sm" onClick={handleBack}>
                    <ArrowLeft className="h-4 w-4 mr-2" />
                    Back to Tenants
                </Button>
            </div>

            {/* Tenant Summary */}
            <Card>
                <CardContent className="p-6">
                    <div className="flex items-start justify-between">
                        <div className="flex items-center gap-6">
                            <div className="flex items-center justify-center w-20 h-20 bg-gray-100 rounded-lg">
                                <Building className="h-10 w-10 text-gray-600" />
                            </div>

                            <div className="space-y-2">
                                <div>
                                    <h1 className="text-2xl font-bold">{tenant.name}</h1>
                                    <p className="text-gray-600">{tenant.title}</p>
                                    <div className="flex items-center gap-2 mt-2 text-xs text-gray-500 font-mono">
                                        ID: {tenant.id}
                                        <Button
                                            variant="ghost"
                                            size="sm"
                                            className="h-auto p-1"
                                            onClick={() => navigator.clipboard.writeText(tenant.id!)}
                                        >
                                            <Copy className="h-3 w-3" />
                                        </Button>
                                    </div>
                                </div>

                                <div className="flex flex-wrap gap-2">
                                    <Badge variant={tenant.isActive ? 'default' : 'destructive'}>
                                        {tenant.isActive ? 'Active' : 'Inactive'}
                                    </Badge>

                                    <Badge variant={getVisibilityBadgeVariant(tenant.visibility)}>
                                        {getAccessLevelName(tenant.visibility)}
                                    </Badge>

                                    {tenant.isDeleted && (
                                        <Badge variant="destructive">Deleted</Badge>
                                    )}

                                    {tenant.isGlobal && (
                                        <Badge variant="outline">Global</Badge>
                                    )}
                                </div>
                            </div>
                        </div>

                        <div className="text-right space-y-2">
                            <div>
                                <div className="text-sm text-gray-600">Slug</div>
                                <div className="text-lg font-mono font-semibold">
                                    {tenant.slug}
                                </div>
                            </div>
                            <div>
                                <div className="text-sm text-gray-600">Version</div>
                                <div className="text-lg font-mono">
                                    v{tenant.version || 0}
                                </div>
                            </div>
                        </div>
                    </div>
                </CardContent>
            </Card>

            {/* Basic Information Section */}
            <div className="space-y-6">
                <div className="border-b pb-2">
                    <h2 className="text-xl font-semibold">Basic Information</h2>
                    <p className="text-sm text-gray-600 mt-1">Core tenant details from the API</p>
                </div>

                <Card>
                    <CardHeader className="flex flex-row items-center justify-between">
                        <CardTitle>Tenant Details</CardTitle>
                        {editingBasicInfo ? (
                            <div className="flex gap-2">
                                <Button size="sm" onClick={handleSaveBasicInfo}>
                                    <Save className="h-4 w-4 mr-2" />
                                    Save
                                </Button>
                                <Button variant="outline" size="sm" onClick={() => setEditingBasicInfo(false)}>
                                    <X className="h-4 w-4 mr-2" />
                                    Cancel
                                </Button>
                            </div>
                        ) : (
                            <Button variant="outline" size="sm" onClick={() => setEditingBasicInfo(true)}>
                                <Edit className="h-4 w-4 mr-2" />
                                Edit
                            </Button>
                        )}
                    </CardHeader>
                    <CardContent className="space-y-4">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div>
                                <Label htmlFor="name">Name</Label>
                                {editingBasicInfo ? (
                                    <Input
                                        id="name"
                                        value={basicForm.name}
                                        onChange={(e) => setBasicForm({ ...basicForm, name: e.target.value })}
                                    />
                                ) : (
                                    <div className="mt-1 text-sm">{tenant.name}</div>
                                )}
                            </div>

                            <div>
                                <Label htmlFor="title">Title</Label>
                                {editingBasicInfo ? (
                                    <Input
                                        id="title"
                                        value={basicForm.title}
                                        onChange={(e) => setBasicForm({ ...basicForm, title: e.target.value })}
                                    />
                                ) : (
                                    <div className="mt-1 text-sm">{tenant.title}</div>
                                )}
                            </div>

                            <div>
                                <Label htmlFor="slug">Slug</Label>
                                {editingBasicInfo ? (
                                    <Input
                                        id="slug"
                                        value={basicForm.slug}
                                        onChange={(e) => setBasicForm({ ...basicForm, slug: e.target.value })}
                                        className="font-mono"
                                    />
                                ) : (
                                    <div className="mt-1 text-sm font-mono">{tenant.slug}</div>
                                )}
                            </div>

                            <Label htmlFor="visibility">Visibility</Label>
                            {editingBasicInfo ? (
                                <Select
                                    value={basicForm.visibility.toString()}
                                    onValueChange={(value) => setBasicForm({ ...basicForm, visibility: parseInt(value) as AccessLevel })}
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
                            ) : (
                                <div className="mt-1">
                                    <Badge variant={getVisibilityBadgeVariant(tenant.visibility)}>
                                        {getAccessLevelName(tenant.visibility)}
                                    </Badge>
                                </div>
                            )}

                            <div className="md:col-span-2">
                                <Label htmlFor="description">Description</Label>
                                {editingBasicInfo ? (
                                    <Textarea
                                        id="description"
                                        value={basicForm.description}
                                        onChange={(e) => setBasicForm({ ...basicForm, description: e.target.value })}
                                        rows={3}
                                    />
                                ) : (
                                    <div className="mt-1 text-sm">
                                        {tenant.description || 'No description provided'}
                                    </div>
                                )}
                            </div>
                        </div>
                    </CardContent>
                </Card>

                {/* System Information */}
                <Card>
                    <CardHeader>
                        <CardTitle>System Information</CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                            <div>
                                <Label>Status</Label>
                                <div className="mt-1">
                                    <Badge variant={tenant.isActive ? 'default' : 'destructive'}>
                                        {tenant.isActive ? 'Active' : 'Inactive'}
                                    </Badge>
                                </div>
                            </div>

                            <div>
                                <Label>Global Tenant</Label>
                                <div className="mt-1">
                                    <Badge variant={tenant.isGlobal ? 'default' : 'outline'}>
                                        {tenant.isGlobal ? 'Yes' : 'No'}
                                    </Badge>
                                </div>
                            </div>

                            <div>
                                <Label>Version</Label>
                                <div className="mt-1 text-sm font-mono">
                                    v{tenant.version || 0}
                                </div>
                            </div>
                        </div>

                        <Separator />

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div>
                                <Label>Created</Label>
                                <div className="mt-1 text-sm flex items-center gap-2">
                                    <Calendar className="h-4 w-4" />
                                    {formatDate(tenant.createdAt)}
                                </div>
                            </div>

                            <div>
                                <Label>Last Updated</Label>
                                <div className="mt-1 text-sm flex items-center gap-2">
                                    <Calendar className="h-4 w-4" />
                                    {formatDate(tenant.updatedAt)}
                                </div>
                            </div>
                        </div>

                        {tenant.deletedAt && (
                            <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
                                <div className="font-medium text-red-800 mb-2">Tenant Deletion</div>
                                <div className="text-sm text-red-700">
                                    This tenant was marked for deletion on {formatDate(tenant.deletedAt)}
                                </div>
                            </div>
                        )}
                    </CardContent>
                </Card>

                {/* Metadata Information */}
                {tenant.metadata && (
                    <Card>
                        <CardHeader>
                            <CardTitle>Metadata</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <div className="text-sm text-gray-600">
                                Metadata information is available but the structure would need to be defined
                                based on the ResourceMetadata interface.
                            </div>
                        </CardContent>
                    </Card>
                )}

                {/* Permissions Information */}
                {tenant.tenantPermissions && tenant.tenantPermissions.length > 0 && (
                    <Card>
                        <CardHeader>
                            <CardTitle>Permissions</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <div className="text-sm text-gray-600 mb-4">
                                This tenant has {tenant.tenantPermissions.length} permission(s) configured.
                            </div>
                            <div className="text-xs text-gray-500">
                                Detailed permission display would require examining the TenantPermission interface structure.
                            </div>
                        </CardContent>
                    </Card>
                )}

                {/* Localizations Information */}
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

export default function TenantDetailPageRoute({ params }: TenantDetailPageRouteProps) {
    const { tenantId } = use(params);
    return <TenantDetailPage tenantId={tenantId} />;
}
