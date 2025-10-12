import React, { Suspense } from 'react';
import { getProducts, getProductStatistics } from '../../../../../../../../../old/quick-fix/products/products.actions';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { DollarSign, Edit, Eye, Package, Plus, ShoppingCart, Trash2, TrendingUp } from 'lucide-react';
import Link from 'next/link';
import Image from 'next/image';

interface ProductsPageProps {
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
}

// Loading component for products
function ProductsLoading() {
  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div className="h-8 w-48 bg-gray-200 rounded animate-pulse" />
        <div className="h-10 w-32 bg-gray-200 rounded animate-pulse" />
      </div>

      {/* Statistics skeleton */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {Array.from({ length: 4 }).map((_, i) => (
          <Card key={i}>
            <CardContent className="p-6">
              <div className="flex items-center justify-between space-y-0 pb-2">
                <div className="h-4 w-24 bg-gray-200 rounded animate-pulse" />
                <div className="h-6 w-6 bg-gray-200 rounded animate-pulse" />
              </div>
              <div className="h-8 w-16 bg-gray-200 rounded animate-pulse" />
              <div className="h-3 w-20 bg-gray-200 rounded animate-pulse mt-2" />
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Products list skeleton */}
      <Card>
        <CardHeader>
          <div className="h-6 w-32 bg-gray-200 rounded animate-pulse" />
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {Array.from({ length: 5 }).map((_, i) => (
              <div key={i} className="flex items-center justify-between p-4 border rounded-lg">
                <div className="flex items-center space-x-4">
                  <div className="h-12 w-12 bg-gray-200 rounded animate-pulse" />
                  <div className="space-y-2">
                    <div className="h-4 w-32 bg-gray-200 rounded animate-pulse" />
                    <div className="h-3 w-48 bg-gray-200 rounded animate-pulse" />
                  </div>
                </div>
                <div className="flex items-center space-x-2">
                  <div className="h-6 w-16 bg-gray-200 rounded animate-pulse" />
                  <div className="h-8 w-8 bg-gray-200 rounded animate-pulse" />
                  <div className="h-8 w-8 bg-gray-200 rounded animate-pulse" />
                  <div className="h-8 w-8 bg-gray-200 rounded animate-pulse" />
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

// Error component for products
function ProductsError({ error }: { error: string }) {
  return (
    <Card>
      <CardContent className="p-6">
        <div className="text-center">
          <Package className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-semibold text-gray-900 mb-2">Unable to load products</h3>
          <p className="text-gray-600 mb-4">{error}</p>
          <Button variant="outline" asChild>
            <Link href="/dashboard/products">Try again</Link>
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}

// Main products content component
async function ProductsContent({ searchParams }: ProductsPageProps) {
  const params = await searchParams;

  // Extract search parameters
  const page = typeof params.page === 'string' ? parseInt(params.page) : 1;
  const limit = typeof params.limit === 'string' ? parseInt(params.limit) : 20;
  const category = typeof params.category === 'string' ? params.category : undefined;
  const isActive = params.active === 'true' ? true : params.active === 'false' ? false : undefined;

  // Fetch data
  const [productsResult, statisticsResult] = await Promise.all([getProducts(page, limit, category, isActive), getProductStatistics()]);

  if (!productsResult.success) {
    return <ProductsError error={productsResult.error || 'Unknown error'} />;
  }

  const products = productsResult.data || [];
  const statistics = statisticsResult.data;

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Products</h1>
          <p className="text-gray-600">Manage your product catalog</p>
        </div>
        <Button asChild>
          <Link href="/dashboard/products/create">
            <Plus className="h-4 w-4 mr-2" />
            Add Product
          </Link>
        </Button>
      </div>

      {/* Statistics */}
      {statistics && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between space-y-0 pb-2">
                <p className="text-sm font-medium">Total Products</p>
                <Package className="h-4 w-4 text-muted-foreground" />
              </div>
              <div className="text-2xl font-bold">{statistics.totalProducts}</div>
              <p className="text-xs text-muted-foreground">+{statistics.productsCreatedThisMonth} this month</p>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between space-y-0 pb-2">
                <p className="text-sm font-medium">Active Products</p>
                <ShoppingCart className="h-4 w-4 text-muted-foreground" />
              </div>
              <div className="text-2xl font-bold">{statistics.activeProducts}</div>
              <p className="text-xs text-muted-foreground">{statistics.inactiveProducts} inactive</p>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between space-y-0 pb-2">
                <p className="text-sm font-medium">Total Revenue</p>
                <DollarSign className="h-4 w-4 text-muted-foreground" />
              </div>
              <div className="text-2xl font-bold">${statistics.totalRevenue.toLocaleString()}</div>
              <p className="text-xs text-muted-foreground">Avg: ${statistics.averagePrice.toFixed(2)}</p>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between space-y-0 pb-2">
                <p className="text-sm font-medium">This Week</p>
                <TrendingUp className="h-4 w-4 text-muted-foreground" />
              </div>
              <div className="text-2xl font-bold">{statistics.productsCreatedThisWeek}</div>
              <p className="text-xs text-muted-foreground">+{statistics.productsCreatedToday} today</p>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Products List */}
      <Card>
        <CardHeader>
          <CardTitle>Products</CardTitle>
        </CardHeader>
        <CardContent>
          {products.length === 0 ? (
            <div className="text-center py-12">
              <Package className="h-12 w-12 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-semibold text-gray-900 mb-2">No products found</h3>
              <p className="text-gray-600 mb-4">Get started by creating your first product.</p>
              <Button asChild>
                <Link href="/dashboard/products/create">
                  <Plus className="h-4 w-4 mr-2" />
                  Create Product
                </Link>
              </Button>
            </div>
          ) : (
            <div className="space-y-4">
              {products.map((product) => (
                <div key={product.id} className="flex items-center justify-between p-4 border rounded-lg hover:bg-gray-50 transition-colors">
                  <div className="flex items-center space-x-4">
                    {product.imageUrl ? (
                      <Image src={product.imageUrl} alt={product.name} className="h-12 w-12 rounded-lg object-cover" width={48} height={48} />
                    ) : (
                      <div className="h-12 w-12 rounded-lg bg-gray-200 flex items-center justify-center">
                        <Package className="h-6 w-6 text-gray-400" />
                      </div>
                    )}
                    <div>
                      <h3 className="font-semibold text-gray-900">{product.name}</h3>
                      <p className="text-sm text-gray-600 max-w-md truncate">{product.description || 'No description'}</p>
                      <div className="flex items-center space-x-2 mt-1">
                        <span className="text-sm font-medium text-gray-900">
                          {product.price} {product.currency}
                        </span>
                        {product.category && (
                          <Badge variant="secondary" className="text-xs">
                            {product.category}
                          </Badge>
                        )}
                      </div>
                    </div>
                  </div>
                  <div className="flex items-center space-x-2">
                    <Badge variant={product.isActive ? 'default' : 'secondary'}>{product.isActive ? 'Active' : 'Inactive'}</Badge>
                    <Button variant="ghost" size="sm" asChild>
                      <Link href={`/dashboard/products/${product.id}`}>
                        <Eye className="h-4 w-4" />
                      </Link>
                    </Button>
                    <Button variant="ghost" size="sm" asChild>
                      <Link href={`/dashboard/products/${product.id}/edit`}>
                        <Edit className="h-4 w-4" />
                      </Link>
                    </Button>
                    <Button variant="ghost" size="sm">
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Pagination */}
      {productsResult.pagination && productsResult.pagination.totalPages > 1 && (
        <div className="flex justify-center space-x-2">
          {Array.from({ length: productsResult.pagination.totalPages }, (_, i) => i + 1).map((pageNum) => (
            <Button key={pageNum} variant={pageNum === page ? 'default' : 'outline'} size="sm" asChild>
              <Link href={`/dashboard/products?page=${pageNum}`}>{pageNum}</Link>
            </Button>
          ))}
        </div>
      )}
    </div>
  );
}

export default async function ProductsPage({ searchParams }: ProductsPageProps) {
  return (
    <Suspense fallback={<ProductsLoading />}>
      <ProductsContent searchParams={searchParams} />
    </Suspense>
  );
}
