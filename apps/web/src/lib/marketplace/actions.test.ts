import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  revalidatePath: vi.fn(),
  addItem: vi.fn(),
  updateItem: vi.fn(),
  removeItem: vi.fn(),
  checkout: vi.fn(),
  completeOrder: vi.fn(),
  preparePaymentIntent: vi.fn(),
  captureOrder: vi.fn(),
  createProduct: vi.fn(),
  setPricing: vi.fn(),
  activateProduct: vi.fn(),
  deactivateProduct: vi.fn(),
  serverConfig: null as null | { auth: { getAccessToken: () => Promise<string> }; tenant: { getTenantId: () => Promise<string | null> }; baseUrl: string },
}));

vi.mock('@/auth', () => ({ auth: mocks.auth, getToken: vi.fn(async () => 'token') }));
vi.mock('next/cache', () => ({ revalidatePath: mocks.revalidatePath }));
vi.mock('@game-guild/client', () => ({
  createServerClient: vi.fn((config) => { mocks.serverConfig = config; return {}; }),
  GeneratedApi: {
    CommerceMarketplaceCartModule: class {
      postVMarketplaceCartItems = mocks.addItem;
      patchVMarketplaceCartItems = mocks.updateItem;
      deleteVMarketplaceCartItems = mocks.removeItem;
      postVMarketplaceCartCheckout = mocks.checkout;
    },
    CommerceOrdersModule: class {
      postOrdersComplete = mocks.completeOrder;
      postOrdersPaymentIntent = mocks.preparePaymentIntent;
      postOrdersCapture = mocks.captureOrder;
    },
    CommerceProductsModule: class {
      postProducts = mocks.createProduct;
      putProductsPricing = mocks.setPricing;
      postProductsActivate = mocks.activateProduct;
      postProductsDeactivate = mocks.deactivateProduct;
    },
  },
}));

import {
  addMarketplaceCartItemAction,
  checkoutMarketplaceEconomyAction,
  createSellerProductAction,
  prepareMarketplaceStripeCheckoutAction,
  reconcileMarketplaceStripeOrderAction,
  removeMarketplaceCartItemAction,
  setMarketplaceCartQuantityAction,
  setSellerProductPricingAction,
  setSellerProductPublishedAction,
} from './actions';

describe('Marketplace server actions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.serverConfig = null;
    mocks.auth.mockResolvedValue({ user: { id: 'actor-1' }, tenantId: 'tenant-1' });
    for (const mock of [mocks.addItem, mocks.updateItem, mocks.removeItem, mocks.captureOrder,
      mocks.setPricing, mocks.activateProduct, mocks.deactivateProduct]) {
      mock.mockResolvedValue({ ok: true, data: {} });
    }
    mocks.checkout.mockResolvedValue({ ok: true, data: { orders: [{ orderId: 'order-1' }, { orderId: 'order-2' }] } });
    mocks.completeOrder.mockResolvedValue({ ok: true, data: {} });
    mocks.preparePaymentIntent
      .mockResolvedValueOnce({ ok: true, data: { clientSecret: 'secret-1' } })
      .mockResolvedValueOnce({ ok: true, data: { clientSecret: 'secret-2' } });
    mocks.createProduct.mockResolvedValue({ ok: true, data: { id: 'product-1' } });
  });

  it('validates cart intent before contacting the API', async () => {
    expect((await addMarketplaceCartItemAction({ idempotencyKey: 'key', productId: '', productPricingId: '', productPricingVersionId: '', quantity: 1 })).success).toBe(false);
    expect((await addMarketplaceCartItemAction({ idempotencyKey: 'key', productId: 'p', productPricingId: 'pp', productPricingVersionId: 'v', quantity: 0 })).success).toBe(false);
    expect(mocks.addItem).not.toHaveBeenCalled();

    await expect(addMarketplaceCartItemAction({ idempotencyKey: 'key', productId: 'p', productPricingId: 'pp', productPricingVersionId: 'v', quantity: 2 }))
      .resolves.toMatchObject({ success: true });
    expect(mocks.addItem).toHaveBeenCalledWith('1', expect.objectContaining({ quantity: 2 }));
  });

  it('updates and removes a versioned durable cart', async () => {
    await expect(setMarketplaceCartQuantityAction('item-1', 3, 7)).resolves.toMatchObject({ success: true });
    await expect(removeMarketplaceCartItemAction('item-1', 8)).resolves.toMatchObject({ success: true });
    expect(mocks.updateItem).toHaveBeenCalledWith('item-1', '1', { quantity: 3, expectedVersion: 7 });
    expect(mocks.removeItem).toHaveBeenCalledWith('item-1', '1', { expectedVersion: 8 });
  });

  it('settles every compatible Economy order with server-owned snapshots', async () => {
    const result = await checkoutMarketplaceEconomyAction(4, 'FixedMix', 'checkout-key');

    expect(result).toMatchObject({ success: true, data: { orderIds: ['order-1', 'order-2'] } });
    expect(mocks.completeOrder).toHaveBeenNthCalledWith(1, 'order-1', {
      marketplaceSettlement: { currencyChoice: 'FixedMix', idempotencyKey: 'checkout-key:order-1' },
    });
    expect(mocks.completeOrder).toHaveBeenCalledTimes(2);
  });

  it('prepares Stripe Payment Elements and reconciles only provider payment-method IDs', async () => {
    const prepared = await prepareMarketplaceStripeCheckoutAction(4, 'stripe-key');
    expect(prepared).toMatchObject({
      success: true,
      data: { orderIds: ['order-1', 'order-2'], clientActionTokens: ['secret-1', 'secret-2'] },
    });

    expect((await reconcileMarketplaceStripeOrderAction('', '')).success).toBe(false);
    const reconciled = await reconcileMarketplaceStripeOrderAction('order-1', 'pm_1');
    expect(reconciled.success).toBe(true);
    expect(mocks.captureOrder).toHaveBeenCalledWith('order-1', { paymentMethodId: 'pm_1' });
  });

  it('keeps seller identity out of create, pricing, and publication requests', async () => {
    expect((await createSellerProductAction({ name: ' ', type: 'Course' })).success).toBe(false);
    const created = await createSellerProductAction({ name: ' Secure course ', type: 'Course' });
    expect(created).toMatchObject({ success: true, data: { productId: 'product-1' } });
    expect(mocks.createProduct).toHaveBeenCalledWith(expect.not.objectContaining({ tenantId: expect.anything(), creatorId: expect.anything() }));

    await setSellerProductPricingAction({ basePrice: 20, currency: 'USD', isDefault: true, name: 'Default', productId: 'product-1' });
    await setSellerProductPublishedAction('product-1', true);
    await setSellerProductPublishedAction('product-1', false);
    expect(mocks.setPricing).toHaveBeenCalled();
    expect(mocks.activateProduct).toHaveBeenCalledWith('product-1');
    expect(mocks.deactivateProduct).toHaveBeenCalledWith('product-1');
  });

  it('returns explicit success for both seller publication transitions', async () => {
    mocks.activateProduct.mockReset().mockResolvedValue({ ok: true, data: {} });
    mocks.deactivateProduct.mockReset().mockResolvedValue({ ok: true, data: {} });

    await expect(setSellerProductPublishedAction('product-1', true)).resolves.toEqual({
      success: true,
      message: 'Product published.',
    });
    await expect(setSellerProductPublishedAction('product-1', false)).resolves.toEqual({
      success: true,
      message: 'Product deactivated.',
    });
  });

  it('fails closed when the session or API operation is unavailable', async () => {
    mocks.auth.mockResolvedValueOnce(null);
    expect((await setMarketplaceCartQuantityAction('item', 1, 1)).success).toBe(false);
    mocks.addItem.mockResolvedValueOnce({ ok: false, error: { message: 'stale price' } });
    expect((await addMarketplaceCartItemAction({ idempotencyKey: 'key', productId: 'p', productPricingId: 'pp', productPricingVersionId: 'v', quantity: 1 })).message).toBe('stale price');
    mocks.completeOrder.mockResolvedValueOnce({ ok: false, error: { message: 'review' } });
    expect((await checkoutMarketplaceEconomyAction(4, 'Hard', 'key')).success).toBe(false);
  });

  it('binds the generated client to access token, tenant, and configured API origin', async () => {
    process.env.API_URL = 'https://private-api.example';
    await setMarketplaceCartQuantityAction('item', 1, 1);
    expect(mocks.serverConfig?.baseUrl).toBe('https://private-api.example');
    await expect(mocks.serverConfig?.auth.getAccessToken()).resolves.toBe('token');
    await expect(mocks.serverConfig?.tenant.getTenantId()).resolves.toBe('tenant-1');
    delete process.env.API_URL;
    mocks.auth.mockResolvedValueOnce({ user: { id: 'actor' } });
    await setMarketplaceCartQuantityAction('item', 1, 1);
    await expect(mocks.serverConfig?.tenant.getTenantId()).resolves.toBeNull();
  });

  it('covers every fail-closed generated API response and safe default message', async () => {
    mocks.auth.mockResolvedValueOnce(null);
    expect((await addMarketplaceCartItemAction({ idempotencyKey: 'key', productId: 'p', productPricingId: 'pp', productPricingVersionId: 'v', quantity: 1 })).success).toBe(false);
    mocks.addItem.mockResolvedValueOnce({ ok: false, error: { message: null } });
    expect((await addMarketplaceCartItemAction({ idempotencyKey: 'key', productId: 'p', productPricingId: 'pp', productPricingVersionId: 'v', quantity: 1 })).message).toContain('could not be added');
    mocks.updateItem.mockResolvedValueOnce({ ok: false, error: { message: null } });
    expect((await setMarketplaceCartQuantityAction('item', 1, 1)).message).toContain('changed elsewhere');
    mocks.removeItem.mockResolvedValueOnce({ ok: false, error: { message: null } });
    expect((await removeMarketplaceCartItemAction('item', 1)).message).toContain('could not be removed');
    mocks.checkout.mockResolvedValueOnce({ ok: false, error: { message: null } });
    expect((await checkoutMarketplaceEconomyAction(1, 'Hard', 'key')).message).toContain('could not be checked out');
    mocks.checkout.mockResolvedValueOnce({ ok: true, data: { orders: [{}, { orderId: 'order' }] } });
    mocks.completeOrder.mockResolvedValueOnce({ ok: false, error: { message: null } });
    expect((await checkoutMarketplaceEconomyAction(1, 'Hard', 'key')).message).toContain('requires review');
    mocks.checkout.mockResolvedValueOnce({ ok: false, error: { message: null } });
    expect((await prepareMarketplaceStripeCheckoutAction(1, 'key')).message).toContain('could not be checked out');
    mocks.checkout.mockResolvedValueOnce({ ok: true, data: { orders: [{}, { orderId: 'order' }] } });
    mocks.preparePaymentIntent.mockReset().mockResolvedValueOnce({ ok: false, error: { message: null } });
    expect((await prepareMarketplaceStripeCheckoutAction(1, 'key')).message).toContain('could not prepare');
    mocks.captureOrder.mockResolvedValueOnce({ ok: false, error: { message: null } });
    expect((await reconcileMarketplaceStripeOrderAction('order', 'pm')).message).toContain('requires reconciliation');
    mocks.createProduct.mockResolvedValueOnce({ ok: false, error: { message: null } });
    expect((await createSellerProductAction({ name: 'name', type: 'Other' })).message).toContain('could not be created');
    mocks.setPricing.mockResolvedValueOnce({ ok: false, error: { message: null } });
    expect((await setSellerProductPricingAction({ basePrice: 1, currency: 'USD', isDefault: true, name: 'Default', productId: 'p' })).message).toContain('could not be published');
    mocks.activateProduct.mockResolvedValueOnce({ ok: false, error: { message: null } });
    expect((await setSellerProductPublishedAction('p', true)).message).toContain('could not be changed');
    mocks.deactivateProduct.mockResolvedValueOnce({ ok: false, error: { message: 'cannot deactivate' } });
    expect((await setSellerProductPublishedAction('p', false)).message).toBe('cannot deactivate');
    mocks.activateProduct.mockResolvedValueOnce({ ok: true, data: {} });
    expect((await setSellerProductPublishedAction('p', true)).success).toBe(true);
  });

  it('handles empty checkout batches, missing Stripe secrets, and pending provider state', async () => {
    mocks.checkout.mockResolvedValueOnce({ ok: true, data: { orders: null } });
    expect(await checkoutMarketplaceEconomyAction(1, 'Soft', 'key')).toMatchObject({ success: true, data: { orderIds: [] } });
    mocks.checkout.mockResolvedValueOnce({ ok: true, data: { orders: null } });
    expect(await prepareMarketplaceStripeCheckoutAction(1, 'key')).toMatchObject({ success: true, data: { orderIds: [], clientActionTokens: [] } });
    mocks.checkout.mockResolvedValueOnce({ ok: true, data: { orders: [{ orderId: 'order' }, {}] } });
    mocks.preparePaymentIntent.mockReset().mockResolvedValueOnce({ ok: true, data: {} });
    expect(await prepareMarketplaceStripeCheckoutAction(1, 'key')).toMatchObject({ success: true, data: { orderIds: ['order'], clientActionTokens: [] } });
    mocks.captureOrder.mockResolvedValueOnce({ ok: true, data: { paymentState: 'Pending' } });
    expect((await reconcileMarketplaceStripeOrderAction('order', 'pm')).message).toContain('pending');
    mocks.captureOrder.mockResolvedValueOnce({ ok: true, data: { paymentState: 'Succeeded' } });
    expect((await reconcileMarketplaceStripeOrderAction('order', 'pm')).message).toContain('confirmed');
  });

  it('rejects every protected mutation when authentication is invalid', async () => {
    const invalidSessions = [Promise.reject(new Error('auth offline')), Promise.resolve((() => undefined)), Promise.resolve({ user: {} })];
    for (const session of invalidSessions) {
      mocks.auth.mockImplementationOnce(() => session);
      expect((await removeMarketplaceCartItemAction('item', 1)).success).toBe(false);
    }
    for (const operation of [
      () => checkoutMarketplaceEconomyAction(1, 'Hard', 'key'),
      () => prepareMarketplaceStripeCheckoutAction(1, 'key'),
      () => reconcileMarketplaceStripeOrderAction('order', 'pm'),
      () => createSellerProductAction({ name: 'name', type: 'Bundle' }),
      () => setSellerProductPricingAction({ basePrice: 1, currency: 'USD', isDefault: true, name: 'n', productId: 'p' }),
      () => setSellerProductPublishedAction('p', false),
    ]) {
      mocks.auth.mockResolvedValueOnce(null);
      expect((await operation()).success).toBe(false);
    }
  });
});
