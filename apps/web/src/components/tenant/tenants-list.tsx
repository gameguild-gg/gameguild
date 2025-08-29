'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { searchTenantsAction } from '@/lib/admin/tenants/tenants.actions';
import { Tenant } from '@/lib/api/generated/types.gen';
import { Building, RefreshCw } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useCallback, useState } from 'react';

interface TenantsListProps {
    initialTenants: Tenant[];
}

export function TenantsList({ initialTenants }: TenantsListProps) {
    const router = useRouter();
    const [tenants, setTenants] = useState<Tenant[]>(initialTenants);
    const [search, setSearch] = useState('');
    const [searching, setSearching] = useState(false);
    const [isRefreshing, setIsRefreshing] = useState(false);

    // Search/filter tenants
    const handleSearch = async (e: React.FormEvent) => {
        e.preventDefault();
        setSearching(true);
        try {
            const res = await searchTenantsAction({ query: { searchTerm: search } });
            setTenants((res.data as Tenant[]) || []);
        } finally {
            setSearching(false);
        }
    };

    // Reset search
    const handleResetSearch = async () => {
        setSearch('');
        setTenants(initialTenants);
    };

    const refreshData = useCallback(async () => {
        setIsRefreshing(true);
        try {
            router.refresh();
        } finally {
            setIsRefreshing(false);
        }
    }, [router]);

    const formatDate = (dateString: string) => {
        return new Date(dateString).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
        });
    };

    return (
        <div className="space-y-6">
            {/* Search/filter */}
            <form onSubmit={handleSearch} className="flex items-center gap-2 mb-2">
                <Input
                    value={search}
                    onChange={e => setSearch(e.target.value)}
                    placeholder="Search tenants..."
                    className="max-w-xs"
                />
                <Button type="submit" size="sm" disabled={searching}>Search</Button>
                <Button type="button" size="sm" variant="outline" onClick={handleResetSearch} disabled={!search}>Reset</Button>
            </form>

            <div className="flex items-center justify-between">
                <div className="flex items-center space-x-3">
                    <Building className="h-6 w-6 text-blue-600" />
                    <div>
                        <h2 className="text-xl font-semibold">Tenants ({tenants.length})</h2>
                        <p className="text-sm text-gray-600">View all tenants and organizations in the system</p>
                    </div>
                </div>
                <div className="flex items-center space-x-2">
                    <Button variant="outline" size="sm" onClick={refreshData} disabled={isRefreshing}>
                        <RefreshCw className={`h-4 w-4 ${isRefreshing ? 'animate-spin' : ''}`} />
                        Refresh
                    </Button>
                </div>
            </div>

            {/* Tenants Table */}
            <Card>
                <CardHeader>
                    <CardTitle>Tenant List</CardTitle>
                </CardHeader>
                <CardContent>
                    {tenants.length === 0 ? (
                        <div className="text-center py-8">
                            <Building className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                            <h3 className="text-lg font-medium text-gray-900 mb-2">No tenants found</h3>
                            <p className="text-gray-500 mb-4">No tenants have been created yet or match your search criteria.</p>
                        </div>
                    ) : (
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead>Name</TableHead>
                                    <TableHead>Description</TableHead>
                                    <TableHead>Status</TableHead>
                                    <TableHead>Created</TableHead>
                                    <TableHead>Updated</TableHead>
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {tenants.map((tenant) => (
                                    <TableRow key={tenant.id}>
                                        <TableCell className="font-medium">
                                            {tenant.name}
                                        </TableCell>
                                        <TableCell className="max-w-xs truncate">
                                            {tenant.description || <span className="text-gray-400">No description</span>}
                                        </TableCell>
                                        <TableCell>
                                            <Badge variant={tenant.isActive ? 'default' : 'secondary'}>
                                                {tenant.isActive ? 'Active' : 'Inactive'}
                                            </Badge>
                                        </TableCell>
                                        <TableCell>{tenant.createdAt ? formatDate(tenant.createdAt) : '-'}</TableCell>
                                        <TableCell>{tenant.updatedAt ? formatDate(tenant.updatedAt) : '-'}</TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    )}
                </CardContent>
            </Card>
        </div>
    );
}
