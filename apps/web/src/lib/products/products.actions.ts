'use server';

import { revalidateTag } from 'next/cache';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  // Product CRUD
  getApiProduct,
  postApiProduct,
  deleteApiProductById,
  getApiProductById,
  putApiProductById,
  // Product Discovery
  getApiProductPublished,
  getApiProductSearch,
  getApiProductTypeByType,
  getApiProductCreatorByCreatorId,
  getApiProductPriceRange,
  getApiProductPopular,
  getApiProductRecent,
  // Product Management
  postApiProductByIdPublish,
  postApiProductByIdUnpublish,
  postApiProductByIdArchive,
  putApiProductByIdVisibility,
  // Bundle Management
  getApiProductByIdBundleItems,
  deleteApiProductByBundleIdBundleItemsByProductId,
  postApiProductByBundleIdBundleItemsByProductId,
  // Pricing & Subscription
  getApiProductByIdPricingCurrent,
  getApiProductByIdPricingHistory,
  postApiProductByIdPricing,
  getApiProductByIdSubscriptionPlans,
  postApiProductByIdSubscriptionPlans,
  getApiProductSubscriptionPlansByPlanId,
  // Access Control
  deleteApiProductByIdAccessByUserId,
  getApiProductByIdAccessByUserId,
  postApiProductByIdAccessByUserId,
  getApiProductByIdUserProductByUserId,
  // Analytics
  getApiProductAnalyticsCount,
  getApiProductByIdAnalyticsUserCount,
  getApiProductByIdAnalyticsRevenue,
} from '@/lib/api/generated/sdk.gen';
import type {
  CreateProductCommand,
  UpdateProductCommand,
  ProductType,
  AccessLevel,
  ProductSubscriptionPlan,
  ContentStatus,
} from '@/lib/api/generated/types.gen';

// =============================================================================
// PRODUCT CRUD OPERATIONS
// =============================================================================

/**
 * Get all products with optional filtering
 */
export async function getProducts() {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProduct({
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return Array.isArray(response.data) ? response.data : [];
  } catch (error) {
    console.error('Error fetching products:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch products');
  }
}

/**
 * Create a new product
 */
export async function createProduct(productData: { name: string; description?: string; type?: ProductType; price?: number; isActive?: boolean }) {
  await configureAuthenticatedClient();

  try {
    const createCommand: CreateProductCommand = {
      name: productData.name,
      description: productData.description,
      type: productData.type,
    };

    const response = await postApiProduct({
      body: createCommand,
    });

    if (!response.data) {
      throw new Error('Failed to create product');
    }

    revalidateTag('products');
    revalidateTag('product-analytics');
    return response.data;
  } catch (error) {
    console.error('Error creating product:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to create product');
  }
}

/**
 * Get product by ID
 */
export async function getProductById(productId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductById({
      path: { id: productId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data;
  } catch (error) {
    console.error('Error fetching product:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch product');
  }
}

/**
 * Update product
 */
export async function updateProduct(
  productId: string,
  productData: { name?: string; description?: string; type?: ProductType; price?: number; isActive?: boolean },
) {
  await configureAuthenticatedClient();

  try {
    const updateCommand: UpdateProductCommand = {
      productId,
      name: productData.name,
      description: productData.description,
      type: productData.type,
    };

    const response = await putApiProductById({
      path: { id: productId },
      body: updateCommand,
    });

    if (!response.data) {
      throw new Error('Failed to update product');
    }

    revalidateTag('products');
    revalidateTag(`product-${productId}`);
    return response.data;
  } catch (error) {
    console.error('Error updating product:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to update product');
  }
}

/**
 * Delete product
 */
export async function deleteProduct(productId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await deleteApiProductById({
      path: { id: productId },
    });

    revalidateTag('products');
    revalidateTag(`product-${productId}`);
    return response.data;
  } catch (error) {
    console.error('Error deleting product:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to delete product');
  }
}

// =============================================================================
// PRODUCT DISCOVERY & SEARCH
// =============================================================================

/**
 * Get published products
 */
export async function getPublishedProducts() {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductPublished({
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return Array.isArray(response.data) ? response.data : [];
  } catch (error) {
    console.error('Error fetching published products:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch published products');
  }
}

/**
 * Search products
 */
export async function searchProducts() {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductSearch({
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return Array.isArray(response.data) ? response.data : [];
  } catch (error) {
    console.error('Error searching products:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to search products');
  }
}

/**
 * Get products by type
 */
export async function getProductsByType(type: ProductType) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductTypeByType({
      path: { type },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return Array.isArray(response.data) ? response.data : [];
  } catch (error) {
    console.error('Error fetching products by type:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch products by type');
  }
}

/**
 * Get products by creator
 */
export async function getProductsByCreator(creatorId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductCreatorByCreatorId({
      path: { creatorId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return Array.isArray(response.data) ? response.data : [];
  } catch (error) {
    console.error('Error fetching products by creator:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch products by creator');
  }
}

/**
 * Get product price range
 */
export async function getProductPriceRange() {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductPriceRange({
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data;
  } catch (error) {
    console.error('Error fetching product price range:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch product price range');
  }
}

/**
 * Get popular products
 */
export async function getPopularProducts() {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductPopular({
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return Array.isArray(response.data) ? response.data : [];
  } catch (error) {
    console.error('Error fetching popular products:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch popular products');
  }
}

/**
 * Get recent products
 */
export async function getRecentProducts() {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductRecent({
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return Array.isArray(response.data) ? response.data : [];
  } catch (error) {
    console.error('Error fetching recent products:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch recent products');
  }
}

// =============================================================================
// PRODUCT MANAGEMENT
// =============================================================================

/**
 * Publish product
 */
export async function publishProduct(productId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProductByIdPublish({
      path: { id: productId },
    });

    revalidateTag('products');
    revalidateTag(`product-${productId}`);
    return response.data;
  } catch (error) {
    console.error('Error publishing product:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to publish product');
  }
}

/**
 * Unpublish product
 */
export async function unpublishProduct(productId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProductByIdUnpublish({
      path: { id: productId },
    });

    revalidateTag('products');
    revalidateTag(`product-${productId}`);
    return response.data;
  } catch (error) {
    console.error('Error unpublishing product:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to unpublish product');
  }
}

/**
 * Archive product
 */
export async function archiveProduct(productId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProductByIdArchive({
      path: { id: productId },
    });

    revalidateTag('products');
    revalidateTag(`product-${productId}`);
    return response.data;
  } catch (error) {
    console.error('Error archiving product:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to archive product');
  }
}

/**
 * Update product visibility
 */
export async function updateProductVisibility(productId: string, visibility: AccessLevel) {
  await configureAuthenticatedClient();

  try {
    const response = await putApiProductByIdVisibility({
      path: { id: productId },
      body: visibility,
    });

    revalidateTag('products');
    revalidateTag(`product-${productId}`);
    return response.data;
  } catch (error) {
    console.error('Error updating product visibility:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to update product visibility');
  }
}

// =============================================================================
// BUNDLE MANAGEMENT
// =============================================================================

/**
 * Get bundle items for a product
 */
export async function getBundleItems(bundleId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductByIdBundleItems({
      path: { id: bundleId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return Array.isArray(response.data) ? response.data : [];
  } catch (error) {
    console.error('Error fetching bundle items:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch bundle items');
  }
}

/**
 * Add product to bundle
 */
export async function addProductToBundle(bundleId: string, productId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProductByBundleIdBundleItemsByProductId({
      path: { bundleId, productId },
    });

    revalidateTag('products');
    revalidateTag(`product-${bundleId}`);
    revalidateTag(`bundle-${bundleId}`);
    return response.data;
  } catch (error) {
    console.error('Error adding product to bundle:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to add product to bundle');
  }
}

/**
 * Remove product from bundle
 */
export async function removeProductFromBundle(bundleId: string, productId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await deleteApiProductByBundleIdBundleItemsByProductId({
      path: { bundleId, productId },
    });

    revalidateTag('products');
    revalidateTag(`product-${bundleId}`);
    revalidateTag(`bundle-${bundleId}`);
    return response.data;
  } catch (error) {
    console.error('Error removing product from bundle:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to remove product from bundle');
  }
}

// =============================================================================
// PRICING & SUBSCRIPTION MANAGEMENT
// =============================================================================

/**
 * Get current pricing for a product
 */
export async function getProductCurrentPricing(productId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductByIdPricingCurrent({
      path: { id: productId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data;
  } catch (error) {
    console.error('Error fetching current pricing:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch current pricing');
  }
}

/**
 * Get pricing history for a product
 */
export async function getProductPricingHistory(productId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductByIdPricingHistory({
      path: { id: productId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return Array.isArray(response.data) ? response.data : [];
  } catch (error) {
    console.error('Error fetching pricing history:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch pricing history');
  }
}

/**
 * Create pricing for a product
 */
export async function createProductPricing(productId: string, pricingData: { price: number; currency?: string; isActive?: boolean }) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProductByIdPricing({
      path: { id: productId },
      body: pricingData,
    });

    if (!response.data) {
      throw new Error('Failed to create pricing');
    }

    revalidateTag('products');
    revalidateTag(`product-${productId}`);
    revalidateTag('product-pricing');
    return response.data;
  } catch (error) {
    console.error('Error creating pricing:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to create pricing');
  }
}

/**
 * Get subscription plans for a product
 */
export async function getProductSubscriptionPlans(productId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductByIdSubscriptionPlans({
      path: { id: productId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return Array.isArray(response.data) ? response.data : [];
  } catch (error) {
    console.error('Error fetching subscription plans:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch subscription plans');
  }
}

/**
 * Create subscription plan for a product
 */
export async function createProductSubscriptionPlan(productId: string, planData: { name: string; price?: number; currency?: string; intervalCount?: number }) {
  await configureAuthenticatedClient();

  try {
    const subscriptionPlan: ProductSubscriptionPlan = {
      productId,
      name: planData.name,
      price: planData.price,
      currency: planData.currency,
      intervalCount: planData.intervalCount,
    };

    const response = await postApiProductByIdSubscriptionPlans({
      path: { id: productId },
      body: subscriptionPlan,
    });

    if (!response.data) {
      throw new Error('Failed to create subscription plan');
    }

    revalidateTag('products');
    revalidateTag(`product-${productId}`);
    revalidateTag('subscription-plans');
    return response.data;
  } catch (error) {
    console.error('Error creating subscription plan:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to create subscription plan');
  }
}

/**
 * Get subscription plan by ID
 */
export async function getSubscriptionPlan(planId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductSubscriptionPlansByPlanId({
      path: { planId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data;
  } catch (error) {
    console.error('Error fetching subscription plan:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch subscription plan');
  }
}

// =============================================================================
// ACCESS CONTROL
// =============================================================================

/**
 * Grant user access to product
 */
export async function grantProductAccess(
  productId: string,
  userId: string,
  accessData: { level?: string; expiresAt?: string; metadata?: Record<string, unknown> },
) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProductByIdAccessByUserId({
      path: { id: productId, userId },
      body: accessData,
    });

    if (!response.data) {
      throw new Error('Failed to grant product access');
    }

    revalidateTag('products');
    revalidateTag(`product-${productId}`);
    revalidateTag('product-access');
    return response.data;
  } catch (error) {
    console.error('Error granting product access:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to grant product access');
  }
}

/**
 * Get user access to product
 */
export async function getUserProductAccess(productId: string, userId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductByIdAccessByUserId({
      path: { id: productId, userId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data;
  } catch (error) {
    console.error('Error fetching user product access:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user product access');
  }
}

/**
 * Revoke user access to product
 */
export async function revokeProductAccess(productId: string, userId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await deleteApiProductByIdAccessByUserId({
      path: { id: productId, userId },
    });

    revalidateTag('products');
    revalidateTag(`product-${productId}`);
    revalidateTag('product-access');
    return response.data;
  } catch (error) {
    console.error('Error revoking product access:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to revoke product access');
  }
}

/**
 * Get user product relationship
 */
export async function getUserProductRelationship(productId: string, userId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductByIdUserProductByUserId({
      path: { id: productId, userId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data;
  } catch (error) {
    console.error('Error fetching user product relationship:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user product relationship');
  }
}

// =============================================================================
// ANALYTICS
// =============================================================================

/**
 * Get product analytics count
 */
export async function getProductAnalyticsCount(options?: { type?: ProductType; status?: ContentStatus }) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductAnalyticsCount({
      query: options,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data;
  } catch (error) {
    console.error('Error fetching product analytics count:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch product analytics count');
  }
}

/**
 * Get product user count analytics
 */
export async function getProductUserCount(productId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductByIdAnalyticsUserCount({
      path: { id: productId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data;
  } catch (error) {
    console.error('Error fetching product user count:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch product user count');
  }
}

/**
 * Get product revenue analytics
 */
export async function getProductRevenue(productId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductByIdAnalyticsRevenue({
      path: { id: productId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data;
  } catch (error) {
    console.error('Error fetching product revenue:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch product revenue');
  }
}

// =============================================================================
// COMPREHENSIVE ANALYTICS
// =============================================================================

/**
 * Get comprehensive product analytics dashboard
 */
export async function getProductAnalyticsDashboard(productId: string) {
  await configureAuthenticatedClient();

  try {
    const [product, userCount, revenue, pricing] = await Promise.all([
      getProductById(productId),
      getProductUserCount(productId),
      getProductRevenue(productId),
      getProductCurrentPricing(productId),
    ]);

    return {
      product,
      analytics: {
        userCount,
        revenue,
        pricing,
      },
      summary: {
        productId,
        generatedAt: new Date().toISOString(),
      },
    };
  } catch (error) {
    console.error('Error generating product analytics dashboard:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to generate product analytics dashboard');
  }
}

/**
 * Get comprehensive products overview
 */
export async function getProductsOverview(options?: { includeAnalytics?: boolean; includePopular?: boolean; includeRecent?: boolean; limit?: number }) {
  await configureAuthenticatedClient();

  try {
    const promises: Promise<unknown>[] = [getProducts()];

    if (options?.includeAnalytics) {
      promises.push(getProductAnalyticsCount());
    }

    if (options?.includePopular) {
      promises.push(getPopularProducts());
    }

    if (options?.includeRecent) {
      promises.push(getRecentProducts());
    }

    const [products, analyticsCount, popularProducts, recentProducts] = await Promise.all(promises);

    return {
      products,
      analytics: options?.includeAnalytics ? analyticsCount : undefined,
      popular: options?.includePopular ? popularProducts : undefined,
      recent: options?.includeRecent ? recentProducts : undefined,
      summary: {
        totalProducts: Array.isArray(products) ? products.length : 0,
        generatedAt: new Date().toISOString(),
      },
    };
  } catch (error) {
    console.error('Error generating products overview:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to generate products overview');
  }
}
