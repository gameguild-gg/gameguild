'use server';

// STUB: Commerce product actions are stubbed due to missing or migrated endpoints in the SDK.
// Each function throws and serves as a placeholder until V1 mappings are finalized.

export async function getProducts(_params?: any): Promise<any[]> {
  throw new Error('Not implemented (STUB): getProducts');
}

export async function createProduct(_data: any): Promise<any> {
  throw new Error('Not implemented (STUB): createProduct');
}

export async function deleteProduct(_productId: string): Promise<any> {
  throw new Error('Not implemented (STUB): deleteProduct');
}

export async function getProductById(_productId: string): Promise<any> {
  throw new Error('Not implemented (STUB): getProductById');
}

export async function updateProduct(_productId: string, _data: any): Promise<any> {
  throw new Error('Not implemented (STUB): updateProduct');
}

export async function getProductsByType(_type: string, _params?: any): Promise<any[]> {
  throw new Error('Not implemented (STUB): getProductsByType');
}

export async function getPublishedProducts(_params?: any): Promise<any[]> {
  throw new Error('Not implemented (STUB): getPublishedProducts');
}

export async function searchProducts(_params: any): Promise<any[]> {
  throw new Error('Not implemented (STUB): searchProducts');
}

export async function getProductsByCreator(_creatorId: string, _params?: any): Promise<any[]> {
  throw new Error('Not implemented (STUB): getProductsByCreator');
}

export async function getProductPriceRange(_params?: any): Promise<any> {
  throw new Error('Not implemented (STUB): getProductPriceRange');
}

export async function getPopularProducts(_params?: any): Promise<any[]> {
  throw new Error('Not implemented (STUB): getPopularProducts');
}

export async function getRecentProducts(_params?: any): Promise<any[]> {
  throw new Error('Not implemented (STUB): getRecentProducts');
}

export async function publishProduct(_productId: string, _data?: any): Promise<any> {
  throw new Error('Not implemented (STUB): publishProduct');
}

export async function unpublishProduct(_productId: string, _data?: any): Promise<any> {
  throw new Error('Not implemented (STUB): unpublishProduct');
}

export async function archiveProduct(_productId: string, _data?: any): Promise<any> {
  throw new Error('Not implemented (STUB): archiveProduct');
}

export async function updateProductVisibility(_productId: string, _data: any): Promise<any> {
  throw new Error('Not implemented (STUB): updateProductVisibility');
}

export async function getProductBundleItems(_productId: string, _params?: any): Promise<any[]> {
  throw new Error('Not implemented (STUB): getProductBundleItems');
}

export async function removeProductFromBundle(_bundleId: string, _productId: string): Promise<any> {
  throw new Error('Not implemented (STUB): removeProductFromBundle');
}

export async function addProductToBundle(_bundleId: string, _productId: string, _data?: any): Promise<any> {
  throw new Error('Not implemented (STUB): addProductToBundle');
}

export async function getProductCurrentPricing(_productId: string, _params?: any): Promise<any> {
  throw new Error('Not implemented (STUB): getProductCurrentPricing');
}

export async function getProductPricingHistory(_productId: string, _params?: any): Promise<any[]> {
  throw new Error('Not implemented (STUB): getProductPricingHistory');
}

export async function createProductPricing(_productId: string, _data: any): Promise<any> {
  throw new Error('Not implemented (STUB): createProductPricing');
}

export async function getProductSubscriptionPlans(_productId: string, _params?: any): Promise<any[]> {
  throw new Error('Not implemented (STUB): getProductSubscriptionPlans');
}

export async function createProductSubscriptionPlan(_productId: string, _data: any): Promise<any> {
  throw new Error('Not implemented (STUB): createProductSubscriptionPlan');
}

export async function getSubscriptionPlanById(_planId: string, _params?: any): Promise<any> {
  throw new Error('Not implemented (STUB): getSubscriptionPlanById');
}

export async function removeProductAccess(_productId: string, _userId: string): Promise<any> {
  throw new Error('Not implemented (STUB): removeProductAccess');
}

export async function getProductAccess(_productId: string, _userId: string, _params?: any): Promise<any> {
  throw new Error('Not implemented (STUB): getProductAccess');
}

export async function grantProductAccess(_productId: string, _userId: string, _data?: any): Promise<any> {
  throw new Error('Not implemented (STUB): grantProductAccess');
}

export async function getUserProduct(_productId: string, _userId: string, _params?: any): Promise<any> {
  throw new Error('Not implemented (STUB): getUserProduct');
}

export async function getProductAnalyticsCount(_params?: any): Promise<any> {
  throw new Error('Not implemented (STUB): getProductAnalyticsCount');
}

export async function getProductUserCountAnalytics(_productId: string, _params?: any): Promise<any> {
  throw new Error('Not implemented (STUB): getProductUserCountAnalytics');
}

export async function getProductRevenueAnalytics(_productId: string, _params?: any): Promise<any> {
  throw new Error('Not implemented (STUB): getProductRevenueAnalytics');
}
