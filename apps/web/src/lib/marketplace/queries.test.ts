import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  getToken: vi.fn(async () => 'token'),
  createServerClient: vi.fn((config: unknown) => config),
  catalog: vi.fn(),
  product: vi.fn(),
  cart: vi.fn(),
  orders: vi.fn(),
  order: vi.fn(),
}));

vi.mock('@/auth', () => ({ auth: mocks.auth, getToken: mocks.getToken }));
vi.mock('react', async (importOriginal) => ({
  ...(await importOriginal<typeof import('react')>()),
  cache: <T extends (...args: never[]) => unknown>(fn: T) => fn,
}));
vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    CommerceMarketplaceCartModule: class { getVMarketplaceCart = mocks.cart; },
    CommerceOrdersModule: class {
      getOrdersForGetOrders = mocks.orders;
      getOrdersForGetOrdersByOrderId = mocks.order;
    },
    CommerceProductsModule: class {
      getProductsForGetProducts = mocks.catalog;
      getProductsForGetProductsByProductId = mocks.product;
    },
  },
}));

import {
  getMarketplaceCart,
  getMarketplaceCatalog,
  getMarketplaceProduct,
  getMyMarketplaceOrder,
  getMyMarketplaceOrders,
  getSellerProducts,
} from './queries';

const ok = (data: unknown) => ({ ok: true, data });
const fail = (message?: string | null) => ({ ok: false, error: { message } });

describe('Marketplace server queries', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.auth.mockResolvedValue({ user: { id: 'actor' }, tenantId: 'tenant' });
    mocks.catalog.mockResolvedValue(ok({ items: [{ id: 'product' }], totalCount: 1 }));
    mocks.product.mockResolvedValue(ok({ id: 'product' }));
    mocks.cart.mockResolvedValue(ok({ id: 'cart' }));
    mocks.orders.mockResolvedValue(ok([{ id: 'order' }]));
    mocks.order.mockResolvedValue(ok({ id: 'order' }));
    delete process.env.API_URL;
    delete process.env.NEXT_PUBLIC_API_URL;
  });

  it('uses authoritative filters and authenticated tenant context', async () => {
    process.env.API_URL = 'https://api.internal';
    await expect(getMarketplaceCatalog({ search: 'course', skip: 4, take: 8, type: 'Course' })).resolves.toEqual({
      issue: null,
      items: [{ id: 'product' }],
      totalCount: 1,
    });
    expect(mocks.catalog).toHaveBeenCalledWith({
      includeUnpublished: false,
      searchTerm: 'course',
      skip: 4,
      take: 8,
      type: 'Course',
    });
    const config = mocks.createServerClient.mock.calls[0][0] as {
      auth: { getAccessToken: () => Promise<string> };
      baseUrl: string;
      tenant: { getTenantId: () => Promise<string | null> };
    };
    expect(config.baseUrl).toBe('https://api.internal');
    await expect(config.auth.getAccessToken()).resolves.toBe('token');
    await expect(config.tenant.getTenantId()).resolves.toBe('tenant');
  });

  it('uses safe catalog defaults and empty optional result fields', async () => {
    mocks.catalog.mockResolvedValueOnce(ok({}));
    await expect(getMarketplaceCatalog()).resolves.toEqual({ issue: null, items: [], totalCount: 0 });
    expect(mocks.catalog).toHaveBeenCalledWith({
      includeUnpublished: false,
      searchTerm: undefined,
      skip: 0,
      take: 24,
      type: undefined,
    });
    mocks.catalog.mockResolvedValueOnce(ok({}));
    await expect(getSellerProducts()).resolves.toEqual([]);
  });

  it('loads product, cart, orders, order detail, and seller inventory', async () => {
    await expect(getMarketplaceProduct('product')).resolves.toEqual({ id: 'product' });
    expect(mocks.product).toHaveBeenCalledWith('product', { includePricing: true, includeUnpublished: false });
    await expect(getMarketplaceCart()).resolves.toEqual({ id: 'cart' });
    expect(mocks.cart).toHaveBeenCalledWith('1');
    await expect(getMyMarketplaceOrders()).resolves.toEqual([{ id: 'order' }]);
    await expect(getMyMarketplaceOrder('order')).resolves.toEqual({ id: 'order' });
    await expect(getSellerProducts()).resolves.toEqual([{ id: 'product' }]);
    expect(mocks.catalog).toHaveBeenLastCalledWith({ includeUnpublished: true, take: 100 });
  });

  it('fails closed for unavailable Marketplace reads', async () => {
    mocks.catalog.mockResolvedValue(fail(null));
    mocks.product.mockResolvedValue(fail('missing'));
    mocks.cart.mockResolvedValue(fail('down'));
    mocks.orders.mockResolvedValue(fail('down'));
    mocks.order.mockResolvedValue(fail('missing'));

    await expect(getMarketplaceCatalog()).resolves.toEqual({ issue: 'Marketplace unavailable', items: [], totalCount: 0 });
    await expect(getMarketplaceProduct('product')).resolves.toBeNull();
    await expect(getMarketplaceCart()).resolves.toBeNull();
    await expect(getMyMarketplaceOrders()).resolves.toEqual([]);
    await expect(getMyMarketplaceOrder('order')).resolves.toBeNull();
    await expect(getSellerProducts()).resolves.toEqual([]);
  });

  it('preserves provider diagnostics and handles public, local, and tenantless clients', async () => {
    mocks.catalog.mockResolvedValue(fail('catalog down'));
    process.env.NEXT_PUBLIC_API_URL = 'https://api.public';
    mocks.auth.mockResolvedValueOnce(null);
    await expect(getMarketplaceCatalog()).resolves.toMatchObject({ issue: 'catalog down' });
    let config = mocks.createServerClient.mock.calls.at(-1)?.[0] as { baseUrl: string; tenant: { getTenantId: () => Promise<string | null> } };
    expect(config.baseUrl).toBe('https://api.public');
    await expect(config.tenant.getTenantId()).resolves.toBeNull();

    delete process.env.NEXT_PUBLIC_API_URL;
    mocks.auth.mockResolvedValueOnce(() => undefined);
    await getMarketplaceProduct('product');
    config = mocks.createServerClient.mock.calls.at(-1)?.[0] as typeof config;
    expect(config.baseUrl).toBe('http://localhost:8080');
    await expect(config.tenant.getTenantId()).resolves.toBeNull();

    mocks.auth.mockResolvedValueOnce({ user: { id: 'actor' }, tenantId: null });
    await getMarketplaceCart();
    config = mocks.createServerClient.mock.calls.at(-1)?.[0] as typeof config;
    await expect(config.tenant.getTenantId()).resolves.toBeNull();

    mocks.auth.mockRejectedValueOnce(new Error('auth down'));
    await getMarketplaceCart();
    config = mocks.createServerClient.mock.calls.at(-1)?.[0] as typeof config;
    await expect(config.tenant.getTenantId()).resolves.toBeNull();
  });
});
