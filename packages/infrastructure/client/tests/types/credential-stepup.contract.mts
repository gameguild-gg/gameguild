import {
  createClient,
  AuthApiKeysModule,
  AuthServiceAccountsModule,
  AuthStepUpModule,
  IdentityAuthenticationStepUpAuthenticationRequiredOutputSchema,
  type ApiError,
} from '../../dist/index.js';

type Client = ReturnType<typeof createClient>;
type KeyIntent = Omit<Parameters<AuthApiKeysModule['postAuthApiKeys']>[0], 'stepUpReceipt'>;

async function verifyDemand(client: Client, error: ApiError, backupCode: string): Promise<string | undefined> {
  if (error.status !== 403) return undefined;
  const parsed = IdentityAuthenticationStepUpAuthenticationRequiredOutputSchema.safeParse(error.metadata?.stepUp);
  if (!parsed.success || !parsed.data.stepUpRequired || !parsed.data.challengeId ||
      !parsed.data.availableMethods?.includes('BackupCode')) return undefined;
  const result = await new AuthStepUpModule(client).postAuthStepUpChallengesVerify(parsed.data.challengeId,
    { method: 'BackupCode', evidence: backupCode });
  return result.ok ? result.data.receipt ?? undefined : undefined;
}

export async function createKey(client: Client, intent: KeyIntent, backupCode: string) {
  const keys = new AuthApiKeysModule(client);
  const result = await keys.postAuthApiKeys(intent);
  if (result.ok) return result;
  const receipt = await verifyDemand(client, result.error, backupCode);
  return receipt ? keys.postAuthApiKeys({ ...intent, stepUpReceipt: receipt }) : result;
}

export async function rotateSecret(client: Client, id: string, backupCode: string) {
  const accounts = new AuthServiceAccountsModule(client);
  const result = await accounts.postAuthServiceAccountsRotateSecret(id);
  if (result.ok) return result;
  const receipt = await verifyDemand(client, result.error, backupCode);
  return receipt ? accounts.postAuthServiceAccountsRotateSecret(id, { stepUpReceipt: receipt }) : result;
}

// Compile-only public declaration controls; never invoked by this contract file.
export function invalidCalls(client: Client, id: string) {
  const accounts = new AuthServiceAccountsModule(client);
  const keys = new AuthApiKeysModule(client);
  // @ts-expect-error The account selected by the route remains required.
  void accounts.postAuthServiceAccountsRotateSecret();
  // @ts-expect-error An optional body must still validate its supplied receipt type.
  void accounts.postAuthServiceAccountsRotateSecret(id, { stepUpReceipt: 42 });
  // @ts-expect-error API-key creation still requires an intent body.
  void keys.postAuthApiKeys();
  // @ts-expect-error The new API-key receipt is a string, not a numeric factor.
  void keys.postAuthApiKeys({ name: 'Synthetic', scopes: ['reports:read'], stepUpReceipt: 42 });
}
