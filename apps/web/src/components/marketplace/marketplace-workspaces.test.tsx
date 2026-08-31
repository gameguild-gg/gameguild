import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  add: vi.fn(), checkout: vi.fn(), create: vi.fn(), prepare: vi.fn(), publish: vi.fn(),
  remove: vi.fn(), setPricing: vi.fn(), setQuantity: vi.fn(), stripeProps: [] as Array<Record<string, unknown>>,
}));
vi.mock('@/lib/marketplace/actions', () => ({
  addMarketplaceCartItemAction: mocks.add,
  checkoutMarketplaceEconomyAction: mocks.checkout,
  createSellerProductAction: mocks.create,
  prepareMarketplaceStripeCheckoutAction: mocks.prepare,
  removeMarketplaceCartItemAction: mocks.remove,
  setMarketplaceCartQuantityAction: mocks.setQuantity,
  setSellerProductPricingAction: mocks.setPricing,
  setSellerProductPublishedAction: mocks.publish,
}));
vi.mock('next-intl', () => ({ useTranslations: () => (key: string) => key }));
vi.mock('./stripe-payment-element', () => ({ StripePaymentElement: ({ clientSecret, locale, orderId, publishableKey }: Record<string, unknown>) => {
  mocks.stripeProps.push({ clientSecret, locale, orderId, publishableKey });
  return <div data-testid="stripe-order" />;
} }));

import { AddToCartForm } from './add-to-cart-form';
import { MarketplaceCartWorkspace } from './marketplace-cart-workspace';
import { MarketplaceCheckoutWorkspace } from './marketplace-checkout-workspace';
import { SellerStudioWorkspace } from './seller-studio-workspace';

const success = { success: true, message: 'recorded' };
const labels = { empty: 'empty', quantity: 'quantity', remove: 'remove', title: 'title', update: 'update' };

describe('Marketplace workspaces', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.stripeProps = [];
    for (const mock of [mocks.add, mocks.checkout, mocks.create, mocks.publish, mocks.remove, mocks.setPricing, mocks.setQuantity]) mock.mockResolvedValue(success);
    mocks.prepare.mockResolvedValue({ success: true, message: 'prepared', data: { orderIds: ['one', 'two'], clientActionTokens: ['secret'] } });
  });

  it('adds only products with an authoritative published price', async () => {
    const { rerender } = render(<AddToCartForm labels={{ add: 'add', quantity: 'quantity', unavailable: 'unavailable' }} product={{ id: 'product', pricing: [{ id: 'price', currentVersionId: 'version', isDefault: true }] } as never} />);
    fireEvent.change(screen.getByRole('spinbutton'), { target: { value: '3' } });
    fireEvent.submit(screen.getByRole('button', { name: 'add' }).closest('form')!);
    await waitFor(() => expect(mocks.add).toHaveBeenCalledWith(expect.objectContaining({ productId: 'product', productPricingId: 'price', productPricingVersionId: 'version', quantity: 3 })));
    await screen.findByText('recorded');

    rerender(<AddToCartForm labels={{ add: 'add', quantity: 'quantity', unavailable: 'unavailable' }} product={{ id: 'product', pricing: [] } as never} />);
    expect(screen.getByRole('button', { name: 'unavailable' })).toBeDisabled();
    fireEvent.submit(screen.getByRole('button', { name: 'unavailable' }).closest('form')!);
    expect(mocks.add).toHaveBeenCalledTimes(1);
  });

  it('updates and removes durable cart items with optimistic versions', async () => {
    const { rerender } = render(<MarketplaceCartWorkspace labels={labels} cart={{ version: 4, items: [
      { id: 'item', productId: 'product', quantity: 1 },
      { id: 'fallback', productId: 'unknown', quantity: 2 },
    ] } as never} products={{ product: { id: 'product', name: 'Named product' } as never }} />);
    expect(screen.getByText('Named product')).toBeInTheDocument();
    expect(screen.getByText('unknown')).toBeInTheDocument();
    fireEvent.change(screen.getAllByRole('spinbutton')[0]!, { target: { value: '5' } });
    fireEvent.submit(screen.getAllByRole('button', { name: 'update' })[0]!.closest('form')!);
    await waitFor(() => expect(mocks.setQuantity).toHaveBeenCalledWith('item', 5, 4));
    await waitFor(() => expect(screen.getAllByRole('button', { name: 'remove' })[0]).toBeEnabled());
    fireEvent.click(screen.getAllByRole('button', { name: 'remove' })[0]!);
    await waitFor(() => expect(mocks.remove).toHaveBeenCalledWith('item', 4));

    rerender(<MarketplaceCartWorkspace labels={labels} cart={null} products={{}} />);
    expect(screen.getByText('empty')).toBeInTheDocument();
  });

  it('uses safe cart fallbacks for incomplete generated records', async () => {
    render(<MarketplaceCartWorkspace labels={labels} cart={{ version: undefined, items: [
      { id: undefined, productId: undefined, quantity: 1 },
    ] } as never} products={{}} />);
    fireEvent.submit(screen.getByRole('button', { name: 'update' }).closest('form')!);
    await waitFor(() => expect(mocks.setQuantity).toHaveBeenCalledWith(undefined, 1, 0));
    await waitFor(() => expect(screen.getByRole('button', { name: 'remove' })).toBeEnabled());
    fireEvent.click(screen.getByRole('button', { name: 'remove' }));
    await waitFor(() => expect(mocks.remove).toHaveBeenCalledWith(undefined, 0));
  });

  it('runs protected Economy checkout and prepares only matched Stripe intents', async () => {
    const cart = { version: undefined, items: [{ id: 'item' }] } as never;
    const { rerender } = render(<MarketplaceCheckoutWorkspace cart={cart} labels={{ economy: 'economy', empty: 'empty', stripe: 'stripe', title: 'checkout' }} locale="pt-BR" />);
    expect(screen.getByText('stripeBlocked')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Soft' }));
    fireEvent.click(screen.getByRole('button', { name: 'economy' }));
    await waitFor(() => expect(mocks.checkout).toHaveBeenCalledWith(0, 'Soft', expect.any(String)));

    rerender(<MarketplaceCheckoutWorkspace cart={cart} labels={{ economy: 'economy', empty: 'empty', stripe: 'stripe', title: 'checkout' }} locale="pt-BR" stripePublishableKey="pk" />);
    fireEvent.click(screen.getByRole('button', { name: 'FixedMix' }));
    fireEvent.click(screen.getByRole('button', { name: 'stripe' }));
    await screen.findByTestId('stripe-order');
    expect(mocks.prepare).toHaveBeenCalledWith(0, expect.any(String));
    expect(mocks.stripeProps.at(-1)).toEqual({ clientSecret: 'secret', locale: 'pt-BR', orderId: 'one', publishableKey: 'pk' });

    mocks.prepare.mockResolvedValueOnce({ success: false, message: 'Stripe unavailable' });
    await waitFor(() => expect(screen.getByRole('button', { name: 'stripe' })).toBeEnabled());
    fireEvent.click(screen.getByRole('button', { name: 'stripe' }));
    await screen.findByText('Stripe unavailable');

    rerender(<MarketplaceCheckoutWorkspace cart={null} labels={{ economy: 'economy', empty: 'empty', stripe: 'stripe', title: 'checkout' }} locale="en-US" />);
    expect(screen.getByText('empty')).toBeInTheDocument();
  });

  it('creates drafts, records pricing, and toggles publication', async () => {
    const studioLabels = { create: 'create', defaultPrice: 'default price', draft: 'draft', name: 'name', pricing: 'pricing', publish: 'publish', published: 'published', shortDescription: 'description', title: 'studio', unpublish: 'unpublish' };
    render(<SellerStudioWorkspace labels={studioLabels} products={[
      { id: 'draft', name: 'Draft product', isPublished: false, pricing: [] },
      { id: 'live', name: 'Live product', isPublished: true, pricing: [{ id: 'price', name: 'Current', basePrice: 12, currency: 'BRL' }] },
    ] as never} />);
    fireEvent.change(screen.getByPlaceholderText('name'), { target: { value: 'New product' } });
    fireEvent.change(screen.getByPlaceholderText('description'), { target: { value: 'Summary' } });
    fireEvent.change(screen.getByRole('combobox'), { target: { value: 'Bundle' } });
    fireEvent.submit(screen.getAllByRole('button', { name: 'create' })[0]!.closest('form')!);
    await waitFor(() => expect(mocks.create).toHaveBeenCalledWith({ name: 'New product', shortDescription: 'Summary', type: 'Bundle' }));
    await waitFor(() => expect(screen.getAllByRole('button', { name: 'pricing' })[0]).toBeEnabled());

    const pricingForms = screen.getAllByRole('button', { name: 'pricing' });
    fireEvent.submit(pricingForms[0]!.closest('form')!);
    await waitFor(() => expect(mocks.setPricing).toHaveBeenCalledWith(expect.objectContaining({ productId: 'draft', pricingId: undefined, name: 'default price', basePrice: 0, currency: 'USD', isDefault: true })));
    await waitFor(() => expect(screen.getByRole('button', { name: 'publish' })).toBeEnabled());
    fireEvent.click(screen.getByRole('button', { name: 'publish' }));
    await waitFor(() => expect(mocks.publish).toHaveBeenCalledWith('draft', true));
    await waitFor(() => expect(screen.getByRole('button', { name: 'unpublish' })).toBeEnabled());
    fireEvent.click(screen.getByRole('button', { name: 'unpublish' }));
    await waitFor(() => expect(mocks.publish).toHaveBeenCalledWith('live', false));
  });

  it('applies explicit server-safe form fallbacks when optional seller fields are absent', async () => {
    const studioLabels = { create: 'create', defaultPrice: 'default price', draft: 'draft', name: 'name', pricing: 'pricing', publish: 'publish', published: 'published', shortDescription: 'description', title: 'studio', unpublish: 'unpublish' };
    render(<SellerStudioWorkspace labels={studioLabels} products={[{ id: 'draft', name: 'Draft', isPublished: false, pricing: [] }] as never} />);
    const createForm = screen.getAllByRole('button', { name: 'create' })[0]!.closest('form')!;
    createForm.querySelector('[name="name"]')?.remove();
    createForm.querySelector('[name="shortDescription"]')?.remove();
    createForm.querySelector('[name="type"]')?.remove();
    fireEvent.submit(createForm);
    await waitFor(() => expect(mocks.create).toHaveBeenCalledWith({ name: '', shortDescription: '', type: 'Other' }));
    await waitFor(() => expect(screen.getByRole('button', { name: 'pricing' })).toBeEnabled());
    const pricingForm = screen.getByRole('button', { name: 'pricing' }).closest('form')!;
    pricingForm.querySelector('[name="name"]')?.remove();
    pricingForm.querySelector('[name="currency"]')?.remove();
    fireEvent.submit(pricingForm);
    await waitFor(() => expect(mocks.setPricing).toHaveBeenCalledWith(expect.objectContaining({ name: 'Default', currency: 'USD' })));
  });
});
