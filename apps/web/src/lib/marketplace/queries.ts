import { auth, getToken } from '@/auth';
import {
  createServerClient,
  GeneratedApi,
  type CommerceOrdersMarketplaceCart,
  type CommerceOrdersOrder,
  type CommerceProductsProduct,
  type CommerceProductsProductType,
} from '@game-guild/client';
import { cache } from 'react';

function getApiUrl() {
  return process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
}

async function createMarketplaceModules() {
  const session = await auth().catch(() => null);
  const client = createServerClient({
    baseUrl: getApiUrl(),
    auth: { getAccessToken: () => getToken() },
    tenant: {
      getTenantId: async () =>
        session && typeof session !== 'function' ? (session.tenantId ?? null) : null,
    },
  });

  return {
    cart: new GeneratedApi.CommerceMarketplaceCartModule(client),
    orders: new GeneratedApi.CommerceOrdersModule(client),
    products: new GeneratedApi.CommerceProductsModule(client),
  };
}

export interface MarketplaceCatalogFilters {
  search?: string;
  skip?: number;
  take?: number;
  type?: CommerceProductsProductType;
}

export const getMarketplaceCatalog = cache(async (filters: MarketplaceCatalogFilters = {}) => {
  const { products } = await createMarketplaceModules();
  const result = await products.getProductsForGetProducts({
    includeUnpublished: false,
    searchTerm: filters.search,
    skip: filters.skip ?? 0,
    take: filters.take ?? 24,
    type: filters.type,
  });
  return {
    issue: result.ok ? null : (result.error.message || 'Marketplace unavailable'),
    items: result.ok ? (result.data.items ?? []) : [],
    totalCount: result.ok ? (result.data.totalCount ?? 0) : 0,
  };
});

export const getMarketplaceProduct = cache(async (productId: string): Promise<CommerceProductsProduct | null> => {
  const { products } = await createMarketplaceModules();
  const result = await products.getProductsForGetProductsByProductId(productId, {
    includePricing: true,
    includeUnpublished: false,
  });
  return result.ok ? result.data : null;
});

export const getMarketplaceCart = cache(async (): Promise<CommerceOrdersMarketplaceCart | null> => {
  const { cart } = await createMarketplaceModules();
  const result = await cart.getVMarketplaceCart('1');
  return result.ok ? result.data : null;
});

export const getMyMarketplaceOrders = cache(async (): Promise<CommerceOrdersOrder[]> => {
  const { orders } = await createMarketplaceModules();
  const result = await orders.getOrdersForGetOrders();
  return result.ok ? result.data : [];
});

export const getMyMarketplaceOrder = cache(async (orderId: string): Promise<CommerceOrdersOrder | null> => {
  const { orders } = await createMarketplaceModules();
  const result = await orders.getOrdersForGetOrdersByOrderId(orderId);
  return result.ok ? result.data : null;
});

export const getSellerProducts = cache(async (): Promise<CommerceProductsProduct[]> => {
  const { products } = await createMarketplaceModules();
  const result = await products.getProductsForGetProducts({
    includeUnpublished: true,
    take: 100,
  });
  return result.ok ? (result.data.items ?? []) : [];
});
