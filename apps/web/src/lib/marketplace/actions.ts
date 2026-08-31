'use server';

import { auth, getToken } from '@/auth';
import { createServerClient, GeneratedApi, type CommerceProductsProductType } from '@game-guild/client';
import { revalidatePath } from 'next/cache';

export interface MarketplaceActionResult<T = never> {
  data?: T;
  message: string;
  success: boolean;
}

function getApiUrl() {
  return process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
}

async function createMarketplaceModules() {
  const session = await auth().catch(() => null);
  if (!session || typeof session === 'function' || !session.user?.id) return null;
  const client = createServerClient({
    baseUrl: getApiUrl(),
    auth: { getAccessToken: () => getToken() },
    tenant: { getTenantId: async () => session.tenantId ?? null },
  });
  return {
    cart: new GeneratedApi.CommerceMarketplaceCartModule(client),
    orders: new GeneratedApi.CommerceOrdersModule(client),
    products: new GeneratedApi.CommerceProductsModule(client),
  };
}

function failure(message: string): MarketplaceActionResult {
  return { success: false, message };
}

function refreshMarketplace() {
  revalidatePath('/marketplace');
  revalidatePath('/workspace/economy', 'layout');
}

export async function addMarketplaceCartItemAction(input: {
  idempotencyKey: string;
  productId: string;
  productPricingId: string;
  productPricingVersionId: string;
  quantity: number;
}): Promise<MarketplaceActionResult> {
  if (!input.productId || !input.productPricingId || !input.productPricingVersionId) {
    return failure('A published price version is required.');
  }
  if (!Number.isSafeInteger(input.quantity) || input.quantity < 1 || input.quantity > 100) {
    return failure('Quantity must be between 1 and 100.');
  }
  const modules = await createMarketplaceModules();
  if (!modules) return failure('Sign in before adding items to the cart.');
  const result = await modules.cart.postVMarketplaceCartItems('1', input);
  if (!result.ok) return failure(result.error.message || 'The item could not be added.');
  refreshMarketplace();
  return { success: true, message: 'Item added to the durable cart.' };
}

export async function setMarketplaceCartQuantityAction(
  itemId: string,
  quantity: number,
  expectedVersion: number,
): Promise<MarketplaceActionResult> {
  const modules = await createMarketplaceModules();
  if (!modules) return failure('Sign in before changing the cart.');
  const result = await modules.cart.patchVMarketplaceCartItems(itemId, '1', { quantity, expectedVersion });
  if (!result.ok) return failure(result.error.message || 'The cart changed elsewhere. Refresh and try again.');
  refreshMarketplace();
  return { success: true, message: 'Cart updated.' };
}

export async function removeMarketplaceCartItemAction(
  itemId: string,
  expectedVersion: number,
): Promise<MarketplaceActionResult> {
  const modules = await createMarketplaceModules();
  if (!modules) return failure('Sign in before changing the cart.');
  const result = await modules.cart.deleteVMarketplaceCartItems(itemId, '1', { expectedVersion });
  if (!result.ok) return failure(result.error.message || 'The item could not be removed.');
  refreshMarketplace();
  return { success: true, message: 'Item removed.' };
}

export async function checkoutMarketplaceEconomyAction(
  expectedVersion: number,
  currencyChoice: 'Hard' | 'Soft' | 'FixedMix',
  idempotencyKey: string,
): Promise<MarketplaceActionResult<{ orderIds: string[] }>> {
  const modules = await createMarketplaceModules();
  if (!modules) return failure('Sign in before checkout.');
  const checkout = await modules.cart.postVMarketplaceCartCheckout('1', { expectedVersion, idempotencyKey });
  if (!checkout.ok) return failure(checkout.error.message || 'The cart could not be checked out.');

  const orderIds: string[] = [];
  for (const order of checkout.data.orders ?? []) {
    if (!order.orderId) continue;
    const settlement = await modules.orders.postOrdersComplete(order.orderId, {
      marketplaceSettlement: {
        currencyChoice,
        idempotencyKey: `${idempotencyKey}:${order.orderId}`,
      },
    });
    if (!settlement.ok) return failure(settlement.error.message || `Order ${order.orderId} requires review.`);
    orderIds.push(order.orderId);
  }
  refreshMarketplace();
  return { success: true, message: 'Checkout submitted to the protected Economy workflow.', data: { orderIds } };
}

export async function prepareMarketplaceStripeCheckoutAction(
  expectedVersion: number,
  idempotencyKey: string,
): Promise<MarketplaceActionResult<{ clientActionTokens: string[]; orderIds: string[] }>> {
  const modules = await createMarketplaceModules();
  if (!modules) return failure('Sign in before checkout.');
  const checkout = await modules.cart.postVMarketplaceCartCheckout('1', { expectedVersion, idempotencyKey });
  if (!checkout.ok) return failure(checkout.error.message || 'The cart could not be checked out.');

  const clientActionTokens: string[] = [];
  const orderIds: string[] = [];
  for (const order of checkout.data.orders ?? []) {
    if (!order.orderId) continue;
    const preparation = await modules.orders.postOrdersPaymentIntent(order.orderId);
    if (!preparation.ok) return failure(preparation.error.message || `Stripe could not prepare order ${order.orderId}.`);
    orderIds.push(order.orderId);
    if (preparation.data.clientSecret) clientActionTokens.push(preparation.data.clientSecret);
  }
  refreshMarketplace();
  return {
    success: true,
    message: 'Payment intents created. Fulfillment waits for durable Stripe webhooks.',
    data: { clientActionTokens, orderIds },
  };
}

export async function reconcileMarketplaceStripeOrderAction(
  orderId: string,
  paymentMethodId: string,
): Promise<MarketplaceActionResult> {
  if (!orderId.trim() || !paymentMethodId.trim()) return failure('Stripe confirmation is incomplete.');
  const modules = await createMarketplaceModules();
  if (!modules) return failure('Sign in before reconciling payment.');
  const result = await modules.orders.postOrdersCapture(orderId, { paymentMethodId });
  if (!result.ok) return failure(result.error.message || 'Payment confirmation requires reconciliation.');
  refreshMarketplace();
  return {
    success: true,
    message: result.data.paymentState === 'Succeeded'
      ? 'Stripe confirmed payment. Fulfillment remains webhook-authoritative.'
      : 'Stripe confirmation is pending durable reconciliation.',
  };
}

export interface SellerProductIntent {
  description?: string;
  imageUrl?: string;
  name: string;
  shortDescription?: string;
  type: CommerceProductsProductType;
}

export async function createSellerProductAction(input: SellerProductIntent): Promise<MarketplaceActionResult<{ productId?: string }>> {
  if (!input.name.trim()) return failure('Product name is required.');
  const modules = await createMarketplaceModules();
  if (!modules) return failure('Sign in before creating a product.');
  const result = await modules.products.postProducts({
    name: input.name.trim(),
    description: input.description,
    shortDescription: input.shortDescription,
    imageUrl: input.imageUrl,
    type: input.type,
    isBundle: input.type === 'Bundle',
  });
  if (!result.ok) return failure(result.error.message || 'Product draft could not be created.');
  refreshMarketplace();
  return { success: true, message: 'Product draft created.', data: { productId: result.data.id } };
}

export async function setSellerProductPricingAction(input: {
  basePrice: number;
  currency: string;
  isDefault: boolean;
  name: string;
  pricingId?: string;
  productId: string;
  saleEndDate?: string;
  salePrice?: number;
  saleStartDate?: string;
}): Promise<MarketplaceActionResult> {
  const modules = await createMarketplaceModules();
  if (!modules) return failure('Sign in before changing pricing.');
  const result = await modules.products.putProductsPricing(input.productId, {
    basePrice: input.basePrice,
    currency: input.currency,
    isDefault: input.isDefault,
    name: input.name,
    pricingId: input.pricingId,
    saleEndDate: input.saleEndDate,
    salePrice: input.salePrice,
    saleStartDate: input.saleStartDate,
  });
  if (!result.ok) return failure(result.error.message || 'Pricing could not be published.');
  refreshMarketplace();
  return { success: true, message: 'A new immutable price version was recorded.' };
}

export async function setSellerProductPublishedAction(productId: string, published: boolean): Promise<MarketplaceActionResult> {
  const modules = await createMarketplaceModules();
  if (!modules) return failure('Sign in before changing publication.');
  const result = published
    ? await modules.products.postProductsActivate(productId)
    : await modules.products.postProductsDeactivate(productId);
  if (!result.ok) return failure(result.error.message || 'Publication state could not be changed.');
  refreshMarketplace();
  return { success: true, message: published ? 'Product published.' : 'Product deactivated.' };
}
