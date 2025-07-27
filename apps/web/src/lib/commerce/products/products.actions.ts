'use server';

import { revalidateTag } from 'next/cache';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  // Product CRUD Operations
  getApiProduct,
  postApiProduct,
  deleteApiProductById,
  getApiProductById,
  putApiProductById,
  
  // Product Discovery & Search
  getApiProductTypeByType,
  getApiProductPublished,
  getApiProductSearch,
  getApiProductCreatorByCreatorId,
  getApiProductPriceRange,
  getApiProductPopular,
  getApiProductRecent,
  
  // Product Management & Publishing
  postApiProductByIdPublish,
  postApiProductByIdUnpublish,
  postApiProductByIdArchive,
  putApiProductByIdVisibility,
  
  // Bundle Management
  getApiProductByIdBundleItems,
  deleteApiProductByBundleIdBundleItemsByProductId,
  postApiProductByBundleIdBundleItemsByProductId,
  
  // Pricing & Subscription Management
  getApiProductByIdPricingCurrent,
  getApiProductByIdPricingHistory,
  postApiProductByIdPricing,
  getApiProductByIdSubscriptionPlans,
  postApiProductByIdSubscriptionPlans,
  getApiProductSubscriptionPlansByPlanId,
  
  // Access Control & User Products
  deleteApiProductByIdAccessByUserId,
  getApiProductByIdAccessByUserId,
  postApiProductByIdAccessByUserId,
  getApiProductByIdUserProductByUserId,
  
  // Analytics & Reporting
  getApiProductAnalyticsCount,
  getApiProductByIdAnalyticsUserCount,
  getApiProductByIdAnalyticsRevenue,
} from '@/lib/api/generated/sdk.gen';

import type {
  GetApiProductData,
  PostApiProductData,
  DeleteApiProductByIdData,
  GetApiProductByIdData,
  PutApiProductByIdData,
  GetApiProductTypeByTypeData,
  GetApiProductPublishedData,
  GetApiProductSearchData,
  GetApiProductCreatorByCreatorIdData,
  GetApiProductPriceRangeData,
  GetApiProductPopularData,
  GetApiProductRecentData,
  PostApiProductByIdPublishData,
  PostApiProductByIdUnpublishData,
  PostApiProductByIdArchiveData,
  PutApiProductByIdVisibilityData,
  GetApiProductByIdBundleItemsData,
  DeleteApiProductByBundleIdBundleItemsByProductIdData,
  PostApiProductByBundleIdBundleItemsByProductIdData,
  GetApiProductByIdPricingCurrentData,
  GetApiProductByIdPricingHistoryData,
  PostApiProductByIdPricingData,
  GetApiProductByIdSubscriptionPlansData,
  PostApiProductByIdSubscriptionPlansData,
  GetApiProductSubscriptionPlansByPlanIdData,
  DeleteApiProductByIdAccessByUserIdData,
  GetApiProductByIdAccessByUserIdData,
  PostApiProductByIdAccessByUserIdData,
  GetApiProductByIdUserProductByUserIdData,
  GetApiProductAnalyticsCountData,
  GetApiProductByIdAnalyticsUserCountData,
  GetApiProductByIdAnalyticsRevenueData,
} from '@/lib/api/generated/types.gen';

// =============================================================================
// PRODUCT CRUD OPERATIONS
// =============================================================================

/**
 * Get all products with optional filtering
 */
export async function getProducts(params?: GetApiProductData) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProduct({
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data || [];
  } catch (error) {
    console.error('Error fetching products:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch products');
  }
}

/**
 * Create a new product
 */
export async function createProduct(data: PostApiProductData) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProduct({
      body: data,
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
 * Delete product by ID
 */
export async function deleteProduct(productId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await deleteApiProductById({
      path: { id: productId },
    });

    revalidateTag('products');
    revalidateTag('product-analytics');
    return response.data;
  } catch (error) {
    console.error('Error deleting product:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to delete product');
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
 * Update product by ID
 */
export async function updateProduct(productId: string, data: PutApiProductByIdData) {
  await configureAuthenticatedClient();

  try {
    const response = await putApiProductById({
      path: { id: productId },
      body: data,
    });

    if (!response.data) {
      throw new Error('Failed to update product');
    }

    revalidateTag('products');
    revalidateTag(`product-${productId}`);
    revalidateTag('product-analytics');
    return response.data;
  } catch (error) {
    console.error('Error updating product:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to update product');
  }
}

// =============================================================================
// PRODUCT DISCOVERY & SEARCH OPERATIONS
// =============================================================================

/**
 * Get products by type
 */
export async function getProductsByType(type: string, params?: GetApiProductTypeByTypeData) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductTypeByType({
      path: { type },
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data || [];
  } catch (error) {
    console.error('Error fetching products by type:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch products by type');
  }
}

/**
 * Get published products
 */
export async function getPublishedProducts(params?: GetApiProductPublishedData) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductPublished({
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data || [];
  } catch (error) {
    console.error('Error fetching published products:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch published products');
  }
}

/**
 * Search products
 */
export async function searchProducts(params: GetApiProductSearchData) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductSearch({
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data || [];
  } catch (error) {
    console.error('Error searching products:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to search products');
  }
}

/**
 * Get products by creator
 */
export async function getProductsByCreator(creatorId: string, params?: GetApiProductCreatorByCreatorIdData) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductCreatorByCreatorId({
      path: { creatorId },
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data || [];
  } catch (error) {
    console.error('Error fetching products by creator:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch products by creator');
  }
}

/**
 * Get product price range
 */
export async function getProductPriceRange(params?: GetApiProductPriceRangeData) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductPriceRange({
      query: params,
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
export async function getPopularProducts(params?: GetApiProductPopularData) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductPopular({
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data || [];
  } catch (error) {
    console.error('Error fetching popular products:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch popular products');
  }
}

/**
 * Get recent products
 */
export async function getRecentProducts(params?: GetApiProductRecentData) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductRecent({
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data || [];
  } catch (error) {
    console.error('Error fetching recent products:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch recent products');
  }
}

// =============================================================================
// PRODUCT MANAGEMENT & PUBLISHING OPERATIONS
// =============================================================================

/**
 * Publish product
 */
export async function publishProduct(productId: string, data?: PostApiProductByIdPublishData) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProductByIdPublish({
      path: { id: productId },
      body: data,
    });

    revalidateTag('products');
    revalidateTag(`product-${productId}`);
    revalidateTag('published-products');
    return response.data;
  } catch (error) {
    console.error('Error publishing product:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to publish product');
  }
}

/**
 * Unpublish product
 */
export async function unpublishProduct(productId: string, data?: PostApiProductByIdUnpublishData) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProductByIdUnpublish({
      path: { id: productId },
      body: data,
    });

    revalidateTag('products');
    revalidateTag(`product-${productId}`);
    revalidateTag('published-products');
    return response.data;
  } catch (error) {
    console.error('Error unpublishing product:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to unpublish product');
  }
}

/**
 * Archive product
 */
export async function archiveProduct(productId: string, data?: PostApiProductByIdArchiveData) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProductByIdArchive({
      path: { id: productId },
      body: data,
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
export async function updateProductVisibility(productId: string, data: PutApiProductByIdVisibilityData) {
  await configureAuthenticatedClient();

  try {
    const response = await putApiProductByIdVisibility({
      path: { id: productId },
      body: data,
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
// BUNDLE MANAGEMENT OPERATIONS
// =============================================================================

/**
 * Get product bundle items
 */
export async function getProductBundleItems(productId: string, params?: GetApiProductByIdBundleItemsData) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductByIdBundleItems({
      path: { id: productId },
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data || [];
  } catch (error) {
    console.error('Error fetching product bundle items:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch product bundle items');
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

/**
 * Add product to bundle
 */
export async function addProductToBundle(bundleId: string, productId: string, data?: PostApiProductByBundleIdBundleItemsByProductIdData) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProductByBundleIdBundleItemsByProductId({
      path: { bundleId, productId },
      body: data,
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

// =============================================================================
// PRICING & SUBSCRIPTION MANAGEMENT OPERATIONS
// =============================================================================

/**
 * Get product current pricing
 */
export async function getProductCurrentPricing(productId: string, params?: GetApiProductByIdPricingCurrentData) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductByIdPricingCurrent({
      path: { id: productId },
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data;
  } catch (error) {
    console.error('Error fetching product current pricing:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch product current pricing');
  }
}

/**
 * Get product pricing history
 */
export async function getProductPricingHistory(productId: string, params?: GetApiProductByIdPricingHistoryData) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductByIdPricingHistory({
      path: { id: productId },
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data || [];
  } catch (error) {
    console.error('Error fetching product pricing history:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch product pricing history');
  }
}

/**
 * Create product pricing
 */
export async function createProductPricing(productId: string, data: PostApiProductByIdPricingData) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProductByIdPricing({
      path: { id: productId },
      body: data,
    });

    revalidateTag('products');
    revalidateTag(`product-${productId}`);
    revalidateTag(`product-pricing-${productId}`);
    return response.data;
  } catch (error) {
    console.error('Error creating product pricing:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to create product pricing');
  }
}

/**
 * Get product subscription plans
 */
export async function getProductSubscriptionPlans(productId: string, params?: GetApiProductByIdSubscriptionPlansData) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductByIdSubscriptionPlans({
      path: { id: productId },
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data || [];
  } catch (error) {
    console.error('Error fetching product subscription plans:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch product subscription plans');
  }
}

/**
 * Create product subscription plan
 */
export async function createProductSubscriptionPlan(productId: string, data: PostApiProductByIdSubscriptionPlansData) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProductByIdSubscriptionPlans({
      path: { id: productId },
      body: data,
    });

    revalidateTag('products');
    revalidateTag(`product-${productId}`);
    revalidateTag(`product-subscriptions-${productId}`);
    return response.data;
  } catch (error) {
    console.error('Error creating product subscription plan:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to create product subscription plan');
  }
}

/**
 * Get subscription plan by ID
 */
export async function getSubscriptionPlanById(planId: string, params?: GetApiProductSubscriptionPlansByPlanIdData) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductSubscriptionPlansByPlanId({
      path: { planId },
      query: params,
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
// ACCESS CONTROL & USER PRODUCTS OPERATIONS
// =============================================================================

/**
 * Remove user access to product
 */
export async function removeProductAccess(productId: string, userId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await deleteApiProductByIdAccessByUserId({
      path: { id: productId, userId },
    });

    revalidateTag('products');
    revalidateTag(`product-${productId}`);
    revalidateTag(`user-products-${userId}`);
    return response.data;
  } catch (error) {
    console.error('Error removing product access:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to remove product access');
  }
}

/**
 * Get user access to product
 */
export async function getProductAccess(productId: string, userId: string, params?: GetApiProductByIdAccessByUserIdData) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductByIdAccessByUserId({
      path: { id: productId, userId },
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data;
  } catch (error) {
    console.error('Error fetching product access:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch product access');
  }
}

/**
 * Grant user access to product
 */
export async function grantProductAccess(productId: string, userId: string, data?: PostApiProductByIdAccessByUserIdData) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProductByIdAccessByUserId({
      path: { id: productId, userId },
      body: data,
    });

    revalidateTag('products');
    revalidateTag(`product-${productId}`);
    revalidateTag(`user-products-${userId}`);
    return response.data;
  } catch (error) {
    console.error('Error granting product access:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to grant product access');
  }
}

/**
 * Get user product relationship
 */
export async function getUserProduct(productId: string, userId: string, params?: GetApiProductByIdUserProductByUserIdData) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductByIdUserProductByUserId({
      path: { id: productId, userId },
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data;
  } catch (error) {
    console.error('Error fetching user product:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user product');
  }
}

// =============================================================================
// ANALYTICS & REPORTING OPERATIONS
// =============================================================================

/**
 * Get product analytics count
 */
export async function getProductAnalyticsCount(params?: GetApiProductAnalyticsCountData) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductAnalyticsCount({
      query: params,
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
export async function getProductUserCountAnalytics(productId: string, params?: GetApiProductByIdAnalyticsUserCountData) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductByIdAnalyticsUserCount({
      path: { id: productId },
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data;
  } catch (error) {
    console.error('Error fetching product user count analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch product user count analytics');
  }
}

/**
 * Get product revenue analytics
 */
export async function getProductRevenueAnalytics(productId: string, params?: GetApiProductByIdAnalyticsRevenueData) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProductByIdAnalyticsRevenue({
      path: { id: productId },
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data;
  } catch (error) {
    console.error('Error fetching product revenue analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch product revenue analytics');
  }
}
