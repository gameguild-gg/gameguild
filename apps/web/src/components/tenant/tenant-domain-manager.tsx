'use client';

import { useState, useEffect, useCallback, useMemo } from 'react';
import { TenantDomain, CreateTenantDomainRequest } from '@/types/tenant-domain';
import { TenantDomainApiClient } from '@/lib/api/tenant-domain-client';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Checkbox } from '@/components/ui/checkbox';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Badge } from '@/components/ui/badge';
import { Trash2, Edit, Plus, Globe, Shield } from 'lucide-react';
import { toast } from 'sonner';
import { TenantService } from '@/lib/services/tenant.service';

interface TenantDomainManagerProps {
  tenantId: string | null;
  apiBaseUrl: string;
  accessToken?: string;
}

export function TenantDomainManager({ tenantId, apiBaseUrl, accessToken }: TenantDomainManagerProps) {
  const [domains, setDomains] = useState<TenantDomain[]>([]);
  const [loading, setLoading] = useState(true);
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
  const [editingDomain, setEditingDomain] = useState<TenantDomain | null>(null);
  const [formData, setFormData] = useState({
    topLevelDomain: '',
    subdomain: '',
    isMainDomain: false,
    isSecondaryDomain: false,
  });
  const [allTenants, setAllTenants] = useState<{ id: string; name: string }[]>([]);
  const [selectedTenantId, setSelectedTenantId] = useState<string>('');
  const isAdminView = tenantId === null || tenantId === '' || tenantId === 'default-tenant-id';

  const apiClient = useMemo(() => new TenantDomainApiClient(apiBaseUrl, accessToken), [apiBaseUrl, accessToken]);

  const fetchDomains = useCallback(async () => {
    try {
      setLoading(true);
      const data = await apiClient.getDomains(tenantId);
      // Filter out any null or invalid domains
      const validDomains = data.filter(domain => domain && domain.id);
      setDomains(validDomains);
    } catch (error) {
      toast.error('Failed to fetch domains');
      console.error('Error fetching domains:', error);
      // Set empty array on error to prevent null issues
      setDomains([]);
    } finally {
      setLoading(false);
    }
  }, [tenantId, apiClient]);

  useEffect(() => {
    void fetchDomains();
  }, [fetchDomains]);

  useEffect(() => {
    if (isAdminView && accessToken) {
      TenantService.getAllTenants(accessToken).then(tenants => {
        setAllTenants(tenants.map(t => ({ id: t.id, name: t.name })));
      });
    }
  }, [isAdminView, accessToken]);

  const handleCreateDomain = async () => {
    let domainTenantId: string | null = tenantId;
    if (isAdminView) {
      domainTenantId = selectedTenantId || null;
    }
    try {
      // Only include tenantId if not null/empty
      const request: any = {
        topLevelDomain: formData.topLevelDomain,
        subdomain: formData.subdomain || undefined,
        isMainDomain: formData.isMainDomain,
        isSecondaryDomain: formData.isSecondaryDomain,
      };
      if (domainTenantId) {
        request.tenantId = domainTenantId;
      }
      await apiClient.createDomain(request);
      toast.success('Domain created successfully');
      setIsCreateDialogOpen(false);
      resetForm();
      fetchDomains();
    } catch (error) {
      toast.error('Failed to create domain');
      console.error('Error creating domain:', error);
    }
  };

  const handleUpdateDomain = async () => {
    if (!editingDomain) return;

    try {
      await apiClient.updateDomain(editingDomain.id, {
        isMainDomain: formData.isMainDomain,
        isSecondaryDomain: formData.isSecondaryDomain,
      });
      toast.success('Domain updated successfully');
      setIsEditDialogOpen(false);
      setEditingDomain(null);
      resetForm();
      fetchDomains();
    } catch (error) {
      toast.error('Failed to update domain');
      console.error('Error updating domain:', error);
    }
  };

  const handleDeleteDomain = async (domain: TenantDomain) => {
    if (!confirm(`Are you sure you want to delete ${domain.topLevelDomain}?`)) return;

    try {
      await apiClient.deleteDomain(domain.id);
      toast.success('Domain deleted successfully');
      fetchDomains();
    } catch (error) {
      toast.error('Failed to delete domain');
      console.error('Error deleting domain:', error);
    }
  };

  const handleSetMainDomain = async (domain: TenantDomain) => {
    if (!tenantId) {
      toast.error('No tenant ID available for setting main domain');
      return;
    }
    
    try {
      await apiClient.setMainDomain(tenantId, domain.id);
      toast.success('Main domain updated successfully');
      fetchDomains();
    } catch (error) {
      toast.error('Failed to set main domain');
      console.error('Error setting main domain:', error);
    }
  };

  const resetForm = () => {
    setFormData({
      topLevelDomain: '',
      subdomain: '',
      isMainDomain: false,
      isSecondaryDomain: false,
    });
  };

  const openEditDialog = (domain: TenantDomain) => {
    setEditingDomain(domain);
    setFormData({
      topLevelDomain: domain.topLevelDomain,
      subdomain: domain.subdomain || '',
      isMainDomain: domain.isMainDomain,
      isSecondaryDomain: domain.isSecondaryDomain,
    });
    setIsEditDialogOpen(true);
  };

  const formatDomainName = (domain: TenantDomain | null) => {
    if (!domain) return 'Unknown Domain';
    return domain.subdomain ? `${domain.subdomain}.${domain.topLevelDomain}` : domain.topLevelDomain;
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center p-8">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">Domain Management</h2>
          <p className="text-muted-foreground">
            Manage domains and auto-assignment rules for your tenant
          </p>
        </div>
        <Dialog open={isCreateDialogOpen} onOpenChange={setIsCreateDialogOpen}>
          <DialogTrigger asChild>
            <Button>
              <Plus className="h-4 w-4 mr-2" />
              Add Domain
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Add New Domain</DialogTitle>
              <DialogDescription>
                Add a new domain to your tenant for user auto-assignment
              </DialogDescription>
            </DialogHeader>
            <div className="space-y-4">
              {isAdminView && (
                <div>
                  <Label htmlFor="tenant">Tenant *</Label>
                  <select
                    id="tenant"
                    className="w-full p-2 border rounded-md"
                    value={selectedTenantId}
                    onChange={e => setSelectedTenantId(e.target.value)}
                  >
                    <option value="">Global (no tenant)</option>
                    {allTenants.map(t => (
                      <option key={t.id} value={t.id}>{t.name}</option>
                    ))}
                  </select>
                </div>
              )}
              <div>
                <Label htmlFor="topLevelDomain">Top Level Domain *</Label>
                <Input
                  id="topLevelDomain"
                  placeholder="example.com"
                  value={formData.topLevelDomain}
                  onChange={(e) => setFormData({ ...formData, topLevelDomain: e.target.value })}
                />
              </div>
              <div>
                <Label htmlFor="subdomain">Subdomain (optional)</Label>
                <Input
                  id="subdomain"
                  placeholder="cs"
                  value={formData.subdomain}
                  onChange={(e) => setFormData({ ...formData, subdomain: e.target.value })}
                />
              </div>
              <div className="flex items-center space-x-2">
                <Checkbox
                  id="isMainDomain"
                  checked={formData.isMainDomain}
                  onCheckedChange={(checked) => setFormData({ ...formData, isMainDomain: !!checked })}
                />
                <Label htmlFor="isMainDomain">Main Domain</Label>
              </div>
              <div className="flex items-center space-x-2">
                <Checkbox
                  id="isSecondaryDomain"
                  checked={formData.isSecondaryDomain}
                  onCheckedChange={(checked) => setFormData({ ...formData, isSecondaryDomain: !!checked })}
                />
                <Label htmlFor="isSecondaryDomain">Secondary Domain</Label>
              </div>
            </div>
            <DialogFooter>
              <Button variant="outline" onClick={() => setIsCreateDialogOpen(false)}>
                Cancel
              </Button>
              <Button onClick={handleCreateDomain}>Create Domain</Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {domains.map((domain) => (
          <Card key={domain.id} className="relative">
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle className="flex items-center gap-2">
                  <Globe className="h-4 w-4" />
                  {formatDomainName(domain)}
                </CardTitle>
                <div className="flex gap-1">
                  {domain.isMainDomain && (
                    <Badge variant="default">
                      <Shield className="h-3 w-3 mr-1" />
                      Main
                    </Badge>
                  )}
                  {domain.isSecondaryDomain && (
                    <Badge variant="secondary">Secondary</Badge>
                  )}
                </div>
              </div>
              <CardDescription>
                Created on {new Date(domain.createdAt).toLocaleDateString()}
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-2 text-sm">
                <div>
                  <span className="font-medium">Top Level Domain:</span> {domain.topLevelDomain}
                </div>
                {domain.subdomain && (
                  <div>
                    <span className="font-medium">Subdomain:</span> {domain.subdomain}
                  </div>
                )}
              </div>
            </CardContent>
            <CardFooter className="flex justify-between">
              <Button
                variant="outline"
                size="sm"
                onClick={() => openEditDialog(domain)}
              >
                <Edit className="h-4 w-4 mr-1" />
                Edit
              </Button>
              <div className="flex gap-2">
                {!domain.isMainDomain && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handleSetMainDomain(domain)}
                  >
                    Set as Main
                  </Button>
                )}
                <Button
                  variant="destructive"
                  size="sm"
                  onClick={() => handleDeleteDomain(domain)}
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>
            </CardFooter>
          </Card>
        ))}
      </div>

      {domains.length === 0 && (
        <div className="text-center py-12">
          <Globe className="mx-auto h-12 w-12 text-gray-400" />
          <h3 className="mt-4 text-lg font-medium">No domains found</h3>
          <p className="mt-2 text-sm text-gray-500">
            Get started by adding your first domain for user auto-assignment.
          </p>
        </div>
      )}

      <Dialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Edit Domain</DialogTitle>
            <DialogDescription>
              Update domain configuration settings
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label>Domain</Label>
              <Input value={formatDomainName(editingDomain!)} disabled />
            </div>
            <div className="flex items-center space-x-2">
              <Checkbox
                id="editIsMainDomain"
                checked={formData.isMainDomain}
                onCheckedChange={(checked) => setFormData({ ...formData, isMainDomain: !!checked })}
              />
              <Label htmlFor="editIsMainDomain">Main Domain</Label>
            </div>
            <div className="flex items-center space-x-2">
              <Checkbox
                id="editIsSecondaryDomain"
                checked={formData.isSecondaryDomain}
                onCheckedChange={(checked) => setFormData({ ...formData, isSecondaryDomain: !!checked })}
              />
              <Label htmlFor="editIsSecondaryDomain">Secondary Domain</Label>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsEditDialogOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handleUpdateDomain}>Update Domain</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
