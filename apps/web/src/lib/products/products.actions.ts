import { revalidateTag, unstable_cache } from 'next/cache';
import { auth } from '@/auth';

// Types for Products
export interface Product {
  id: string;
  name: string;
  description?: string;
  price: number;
  currency: string;
  isActive: boolean;
  category?: string;
  tags?: string[];
  imageUrl?: string;
  createdAt: string;
  updatedAt: string;
  tenantId: string;
}

export interface ProductFormData {
  name: string;
  description?: string;
  price: number;
  currency: string;
  isActive: boolean;
  category?: string;
  tags?: string[];
  imageUrl?: string;
}

export interface ProductsResponse {
  success: boolean;
  data?: Product[];
  error?: string;
  pagination?: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
  };
}

export interface ProductStatistics {
  totalProducts: number;
  activeProducts: number;
  inactiveProducts: number;
  totalRevenue: number;
  averagePrice: number;
  productsCreatedToday: number;
  productsCreatedThisWeek: number;
  productsCreatedThisMonth: number;
}

// Cache configuration
const CACHE_TAGS = {
  PRODUCTS: 'products',
  PRODUCT_DETAIL: 'product-detail',
  PRODUCT_STATISTICS: 'product-statistics',
} as const;

const REVALIDATION_TIME = 300; // 5 minutes

/**
 * Get products with authentication
 */
export async function getProducts(
  page: number = 1,
  limit: number = 20,
  category?: string,
  isActive?: boolean
): Promise<ProductsResponse> {
  try {
    const session = await auth();
    if (!session?.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const queryParams = new URLSearchParams({
      page: page.toString(),
      limit: limit.toString(),
      ...(category && { category }),
      ...(isActive !== undefined && { isActive: isActive.toString() }),
    });

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/products?${queryParams}`, {
      headers: {
        'Authorization': `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.PRODUCTS],
      },
    });

    if (!response.ok) {
      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    const products: Product[] = await response.json();
    
    return {
      success: true,
      data: products,
      pagination: {
        page,
        limit,
        total: products.length, // TODO: Get from headers if API provides it
        totalPages: Math.ceil(products.length / limit),
      },
    };
  } catch (error) {
    console.error('Error fetching products:', error);
    return { 
      success: false, 
      error: error instanceof Error ? error.message : 'Unknown error' 
    };
  }
}

/**
 * Get single product by ID
 */
export async function getProduct(id: string): Promise<{ success: boolean; data?: Product; error?: string }> {
  try {
    const session = await auth();
    if (!session?.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/products/${id}`, {
      headers: {
        'Authorization': `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.PRODUCT_DETAIL, `product-${id}`],
      },
    });

    if (!response.ok) {
      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    const product: Product = await response.json();
    return { success: true, data: product };
  } catch (error) {
    console.error('Error fetching product:', error);
    return { 
      success: false, 
      error: error instanceof Error ? error.message : 'Unknown error' 
    };
  }
}

/**
 * Get product statistics
 */
export async function getProductStatistics(
  fromDate?: string,
  toDate?: string
): Promise<{ success: boolean; data?: ProductStatistics; error?: string }> {
  try {
    const session = await auth();
    if (!session?.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const queryParams = new URLSearchParams({
      ...(fromDate && { fromDate }),
      ...(toDate && { toDate }),
    });

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/products/statistics?${queryParams}`, {
      headers: {
        'Authorization': `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.PRODUCT_STATISTICS],
      },
    });

    if (!response.ok) {
      // If endpoint doesn't exist, return mock data
      if (response.status === 404) {
        return {
          success: true,
          data: {
            totalProducts: 0,
            activeProducts: 0,
            inactiveProducts: 0,
            totalRevenue: 0,
            averagePrice: 0,
            productsCreatedToday: 0,
            productsCreatedThisWeek: 0,
            productsCreatedThisMonth: 0,
          }
        };
      }
      
      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    const statistics: ProductStatistics = await response.json();
    return { success: true, data: statistics };
  } catch (error) {
    console.error('Error fetching product statistics:', error);
    return { 
      success: false, 
      error: error instanceof Error ? error.message : 'Unknown error' 
    };
  }
}

/**
 * Create a new product
 */
export async function createProduct(formData: ProductFormData): Promise<{ success: boolean; data?: Product; error?: string }> {
  'use server';
  
  try {
    const session = await auth();
    if (!session?.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/products`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
      body: JSON.stringify(formData),
    });

    if (!response.ok) {
      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    const product: Product = await response.json();
    
    // Revalidate cache
    revalidateTag(CACHE_TAGS.PRODUCTS);
    revalidateTag(CACHE_TAGS.PRODUCT_STATISTICS);
    
    return { success: true, data: product };
  } catch (error) {
    console.error('Error creating product:', error);
    return { 
      success: false, 
      error: error instanceof Error ? error.message : 'Unknown error' 
    };
  }
}

/**
 * Update an existing product
 */
export async function updateProduct(id: string, formData: ProductFormData): Promise<{ success: boolean; data?: Product; error?: string }> {
  'use server';
  
  try {
    const session = await auth();
    if (!session?.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/products/${id}`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
      body: JSON.stringify(formData),
    });

    if (!response.ok) {
      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    const product: Product = await response.json();
    
    // Revalidate cache
    revalidateTag(CACHE_TAGS.PRODUCTS);
    revalidateTag(CACHE_TAGS.PRODUCT_DETAIL);
    revalidateTag(`product-${id}`);
    revalidateTag(CACHE_TAGS.PRODUCT_STATISTICS);
    
    return { success: true, data: product };
  } catch (error) {
    console.error('Error updating product:', error);
    return { 
      success: false, 
      error: error instanceof Error ? error.message : 'Unknown error' 
    };
  }
}

/**
 * Delete a product
 */
export async function deleteProduct(id: string): Promise<{ success: boolean; error?: string }> {
  'use server';
  
  try {
    const session = await auth();
    if (!session?.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/products/${id}`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
    });

    if (!response.ok) {
      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    // Revalidate cache
    revalidateTag(CACHE_TAGS.PRODUCTS);
    revalidateTag(CACHE_TAGS.PRODUCT_DETAIL);
    revalidateTag(`product-${id}`);
    revalidateTag(CACHE_TAGS.PRODUCT_STATISTICS);
    
    return { success: true };
  } catch (error) {
    console.error('Error deleting product:', error);
    return { 
      success: false, 
      error: error instanceof Error ? error.message : 'Unknown error' 
    };
  }
}

/**
 * Server-only function to refresh products cache
 */
export async function refreshProducts(): Promise<void> {
  'use server';
  revalidateTag(CACHE_TAGS.PRODUCTS);
  revalidateTag(CACHE_TAGS.PRODUCT_STATISTICS);
}

/**
 * Cached version of getProducts for better performance
 */
export const getCachedProducts = unstable_cache(
  async (page: number, limit: number, category?: string, isActive?: boolean) => {
    return getProducts(page, limit, category, isActive);
  },
  ['products'],
  {
    tags: [CACHE_TAGS.PRODUCTS],
    revalidate: REVALIDATION_TIME,
  }
);
