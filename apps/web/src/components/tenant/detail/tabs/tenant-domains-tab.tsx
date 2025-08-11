'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Switch } from '@/components/ui/switch';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { 
  Globe, 
  Plus, 
  MoreHorizontal, 
  Edit, 
  Trash2, 
  Star,
  Shield,
  ExternalLink,
  CheckCircle,
  AlertCircle,
  Loader2
} from 'lucide-react';
import type { Tenant } from '@/lib/api/generated/types.gen';

interface TenantDomainsTabProps {
  tenant: Tenant;
  isAdmin?: boolean;
}

interface TenantDomain {
  id: string;
  domain: string;
  isMain: boolean;
  isVerified: boolean;
  sslEnabled: boolean;
  isActive: boolean;
  createdAt: string;
  verificationToken?: string;
  dnsRecords?: {
    type: string;
    name: string;
    value: string;
    status: 'pending' | 'verified' | 'failed';
  }[];
}

export function TenantDomainsTab({ tenant, isAdmin = false }: TenantDomainsTabProps) {
  const [domains, setDomains] = useState<TenantDomain[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isAddDialogOpen, setIsAddDialogOpen] = useState(false);
  const [isVerifyDialogOpen, setIsVerifyDialogOpen] = useState(false);
  const [selectedDomain, setSelectedDomain] = useState<TenantDomain | null>(null);
  const [newDomain, setNewDomain] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    const loadDomains = async () => {
      setIsLoading(true);
      try {
        // Mock data - replace with actual API call
        const mockDomains: TenantDomain[] = [
          {
            id: '1',
            domain: `${tenant.slug}.yourdomain.com`,
            isMain: true,
            isVerified: true,
            sslEnabled: true,
            isActive: true,
            createdAt: '2024-01-15T10:00:00Z'
          },
          {
            id: '2',
            domain: 'custom.example.com',
            isMain: false,
            isVerified: false,
            sslEnabled: false,
            isActive: true,
            createdAt: '2024-02-20T14:30:00Z',
            verificationToken: 'abc123def456',
            dnsRecords: [
              { type: 'CNAME', name: 'custom.example.com', value: 'yourdomain.com', status: 'pending' },
              { type: 'TXT', name: '_verification.custom.example.com', value: 'abc123def456', status: 'pending' }
            ]
          }
        ];
        setDomains(mockDomains);
      } catch (error) {
        console.error('Failed to load domains:', error);
      } finally {
        setIsLoading(false);
      }
    };

    if (tenant.id) {
      loadDomains();
    }
  }, [tenant]);

  const handleAddDomain = async () => {
    if (!newDomain.trim()) return;

    setIsSubmitting(true);
    try {
      // Mock API call - replace with actual implementation
      console.log('Adding domain:', { domain: newDomain, tenantId: tenant.id });
      
      const newDomainObj: TenantDomain = {
        id: Date.now().toString(),
        domain: newDomain,
        isMain: domains.length === 0,
        isVerified: false,
        sslEnabled: false,
        isActive: true,
        createdAt: new Date().toISOString(),
        verificationToken: 'new-token-' + Date.now(),
        dnsRecords: [
          { type: 'CNAME', name: newDomain, value: 'yourdomain.com', status: 'pending' },
          { type: 'TXT', name: `_verification.${newDomain}`, value: 'new-token-' + Date.now(), status: 'pending' }
        ]
      };
      
      setDomains(prev => [...prev, newDomainObj]);
      setIsAddDialogOpen(false);
      setNewDomain('');
    } catch (error) {
      console.error('Failed to add domain:', error);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleSetMainDomain = async (domainId: string) => {
    try {
      // Mock API call
      console.log('Setting main domain:', domainId);
      setDomains(prev =>
        prev.map(domain => ({
          ...domain,
          isMain: domain.id === domainId
        }))
      );
    } catch (error) {
      console.error('Failed to set main domain:', error);
    }
  };

  const handleVerifyDomain = async (domainId: string) => {
    try {
      // Mock API call
      console.log('Verifying domain:', domainId);
      setDomains(prev =>
        prev.map(domain =>
          domain.id === domainId
            ? {
                ...domain,
                isVerified: true,
                dnsRecords: domain.dnsRecords?.map(record => ({ ...record, status: 'verified' as const }))
              }
            : domain
        )
      );
    } catch (error) {
      console.error('Failed to verify domain:', error);
    }
  };

  const handleDeleteDomain = async (domainId: string) => {
    try {
      // Mock API call
      console.log('Deleting domain:', domainId);
      setDomains(prev => prev.filter(domain => domain.id !== domainId));
    } catch (error) {
      console.error('Failed to delete domain:', error);
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
        <span className="ml-2 text-gray-600">Loading domains...</span>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-3">
          <Globe className="h-5 w-5 text-blue-600" />
          <div>
            <h3 className="text-lg font-medium">Custom Domains ({domains.length})</h3>
            <p className="text-sm text-gray-600">Manage custom domains for this tenant</p>
          </div>
        </div>
        {isAdmin && (
          <Dialog open={isAddDialogOpen} onOpenChange={setIsAddDialogOpen}>
            <DialogTrigger asChild>
              <Button size="sm">
                <Plus className="h-4 w-4 mr-2" />
                Add Domain
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Add Custom Domain</DialogTitle>
                <DialogDescription>
                  Add a custom domain for your tenant. You&apos;ll need to configure DNS records to verify ownership.
                </DialogDescription>
              </DialogHeader>
              <div className="space-y-4">
                <div>
                  <Label htmlFor="domain">Domain Name *</Label>
                  <Input
                    id="domain"
                    placeholder="example.com"
                    value={newDomain}
                    onChange={(e) => setNewDomain(e.target.value)}
                  />
                  <p className="text-xs text-gray-500 mt-1">
                    Enter your custom domain (e.g., app.yourcompany.com)
                  </p>
                </div>
              </div>
              <DialogFooter>
                <Button variant="outline" onClick={() => setIsAddDialogOpen(false)}>
                  Cancel
                </Button>
                <Button onClick={handleAddDomain} disabled={isSubmitting || !newDomain.trim()}>
                  {isSubmitting && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
                  Add Domain
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        )}
      </div>

      {/* Default Domain Info */}
      <Alert>
        <Globe className="h-4 w-4" />
        <AlertDescription>
          Your default subdomain is <strong>{tenant.slug}.yourdomain.com</strong>. 
          You can add custom domains below for a professional appearance.
        </AlertDescription>
      </Alert>

      {/* Domains Table */}
      <Card>
        <CardHeader>
          <CardTitle>Domain List</CardTitle>
        </CardHeader>
        <CardContent>
          {domains.length === 0 ? (
            <div className="text-center py-8">
              <Globe className="h-12 w-12 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">No custom domains</h3>
              <p className="text-gray-500 mb-4">Add a custom domain to enhance your brand presence.</p>
              {isAdmin && (
                <Button onClick={() => setIsAddDialogOpen(true)}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add First Domain
                </Button>
              )}
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Domain</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>SSL</TableHead>
                  <TableHead>Added</TableHead>
                  {isAdmin && <TableHead className="w-[50px]">Actions</TableHead>}
                </TableRow>
              </TableHeader>
              <TableBody>
                {domains.map((domain) => (
                  <TableRow key={domain.id}>
                    <TableCell>
                      <div className="flex items-center space-x-2">
                        <div>
                          <div className="flex items-center space-x-2">
                            <span className="font-medium">{domain.domain}</span>
                            {domain.isMain && (
                              <Badge variant="default" className="text-xs">
                                <Star className="h-3 w-3 mr-1" />
                                Main
                              </Badge>
                            )}
                          </div>
                          <div className="flex items-center space-x-1 mt-1">
                            <a 
                              href={`https://${domain.domain}`} 
                              target="_blank" 
                              rel="noopener noreferrer"
                              className="text-xs text-blue-600 hover:underline flex items-center"
                            >
                              <ExternalLink className="h-3 w-3 mr-1" />
                              Visit
                            </a>
                          </div>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center space-x-2">
                        {domain.isVerified ? (
                          <Badge className="bg-green-100 text-green-800">
                            <CheckCircle className="h-3 w-3 mr-1" />
                            Verified
                          </Badge>
                        ) : (
                          <Badge variant="secondary">
                            <AlertCircle className="h-3 w-3 mr-1" />
                            Pending
                          </Badge>
                        )}
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge variant={domain.sslEnabled ? 'default' : 'secondary'}>
                        <Shield className="h-3 w-3 mr-1" />
                        {domain.sslEnabled ? 'Enabled' : 'Disabled'}
                      </Badge>
                    </TableCell>
                    <TableCell>{formatDate(domain.createdAt)}</TableCell>
                    {isAdmin && (
                      <TableCell>
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button variant="ghost" size="sm">
                              <MoreHorizontal className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            {!domain.isMain && (
                              <DropdownMenuItem onClick={() => handleSetMainDomain(domain.id)}>
                                <Star className="h-4 w-4 mr-2" />
                                Set as Main
                              </DropdownMenuItem>
                            )}
                            {!domain.isVerified && (
                              <DropdownMenuItem onClick={() => {
                                setSelectedDomain(domain);
                                setIsVerifyDialogOpen(true);
                              }}>
                                <CheckCircle className="h-4 w-4 mr-2" />
                                Verify Domain
                              </DropdownMenuItem>
                            )}
                            <DropdownMenuItem 
                              onClick={() => handleDeleteDomain(domain.id)}
                              className="text-red-600"
                              disabled={domain.isMain}
                            >
                              <Trash2 className="h-4 w-4 mr-2" />
                              Delete
                            </DropdownMenuItem>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </TableCell>
                    )}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Verification Dialog */}
      {selectedDomain && (
        <Dialog open={isVerifyDialogOpen} onOpenChange={setIsVerifyDialogOpen}>
          <DialogContent className="max-w-2xl">
            <DialogHeader>
              <DialogTitle>Verify Domain Ownership</DialogTitle>
              <DialogDescription>
                Add the following DNS records to verify ownership of {selectedDomain.domain}
              </DialogDescription>
            </DialogHeader>
            <div className="space-y-4">
              <Alert>
                <AlertCircle className="h-4 w-4" />
                <AlertDescription>
                  Add these DNS records at your domain registrar or DNS provider. 
                  Verification may take up to 24 hours to complete.
                </AlertDescription>
              </Alert>
              
              {selectedDomain.dnsRecords && (
                <div className="space-y-3">
                  {selectedDomain.dnsRecords.map((record, index) => (
                    <div key={index} className="border rounded-lg p-4 bg-gray-50">
                      <div className="grid grid-cols-3 gap-4">
                        <div>
                          <Label className="text-xs text-gray-500">Type</Label>
                          <p className="font-mono text-sm">{record.type}</p>
                        </div>
                        <div>
                          <Label className="text-xs text-gray-500">Name</Label>
                          <p className="font-mono text-sm break-all">{record.name}</p>
                        </div>
                        <div>
                          <Label className="text-xs text-gray-500">Value</Label>
                          <p className="font-mono text-sm break-all">{record.value}</p>
                        </div>
                      </div>
                      <div className="mt-2">
                        <Badge variant={record.status === 'verified' ? 'default' : 'secondary'}>
                          {record.status}
                        </Badge>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
            <DialogFooter>
              <Button variant="outline" onClick={() => setIsVerifyDialogOpen(false)}>
                Close
              </Button>
              <Button onClick={() => handleVerifyDomain(selectedDomain.id)}>
                <CheckCircle className="h-4 w-4 mr-2" />
                Check Verification
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )}
    </div>
  );
}
