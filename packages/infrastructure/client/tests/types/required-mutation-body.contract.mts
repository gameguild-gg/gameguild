import {
  createClient,
  CommercePaymentsModule,
  CommercePaymentsWalletsModule,
  UsersModule,
  AuthTrustedDevicesModule,
  AuthServiceAccountsModule,
} from '../../dist/index.js';

// Compile-only calls: production mutations keep their payloads; challenge-only
// secret rotation still permits an absent body. This function is never invoked.
export function mutationBodies(client: ReturnType<typeof createClient>, payloads: {
  payment: Parameters<CommercePaymentsModule['postPayments']>[0];
  transfer: Parameters<CommercePaymentsWalletsModule['postWalletsTransfer']>[0];
  user: Parameters<UsersModule['postUsers']>[0];
  device: Parameters<AuthTrustedDevicesModule['postAuthTrustedDevices']>[0];
}) {
  const payments = new CommercePaymentsModule(client);
  const wallets = new CommercePaymentsWalletsModule(client);
  const users = new UsersModule(client);
  const devices = new AuthTrustedDevicesModule(client);
  void payments.postPayments(payloads.payment);
  void wallets.postWalletsTransfer(payloads.transfer);
  void users.postUsers(payloads.user);
  void devices.postAuthTrustedDevices(payloads.device);
  // @ts-expect-error Payment processing requires an explicit validated payload.
  void payments.postPayments();
  // @ts-expect-error Wallet transfers require an amount and destination intent.
  void wallets.postWalletsTransfer();
  // @ts-expect-error User creation requires its creation payload.
  void users.postUsers();
  // @ts-expect-error Trusting a device requires the bound device intent.
  void devices.postAuthTrustedDevices();
  const accounts = new AuthServiceAccountsModule(client);
  void accounts.postAuthServiceAccountsRotateSecret('11111111-1111-4111-8111-111111111111');
  void accounts.postAuthServiceAccountsRotateSecret('11111111-1111-4111-8111-111111111111', { stepUpReceipt: 'receipt' });
}
