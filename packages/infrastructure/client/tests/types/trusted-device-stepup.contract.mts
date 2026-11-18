import {
  createClient,
  AuthTrustedDevicesModule,
  AuthStepUpModule,
  IdentityAuthenticationStepUpAuthenticationRequiredOutputSchema,
  type ApiError,
  type IdentityAuthenticationStepUpAuthenticationRequiredOutput,
} from '../../dist/index.js';

export function readDemand(error: ApiError): IdentityAuthenticationStepUpAuthenticationRequiredOutput | undefined {
  if (error.status !== 403) return undefined;
  const parsed = IdentityAuthenticationStepUpAuthenticationRequiredOutputSchema.safeParse(error.metadata?.stepUp);
  return parsed.success ? parsed.data : undefined;
}

export async function trustDevice(client: ReturnType<typeof createClient>, deviceName: string, backupCode: string) {
  const devices = new AuthTrustedDevicesModule(client);
  const attempted = await devices.postAuthTrustedDevices({ deviceName });
  if (attempted.ok) return attempted;
  const demand = readDemand(attempted.error);
  if (!demand?.stepUpRequired || !demand.challengeId || !demand.availableMethods?.includes('BackupCode')) return attempted;
  const verified = await new AuthStepUpModule(client).postAuthStepUpChallengesVerify(demand.challengeId,
    { method: 'BackupCode', evidence: backupCode });
  if (!verified.ok) return verified;
  if (!verified.data.receipt) return attempted;
  return devices.postAuthTrustedDevices({ deviceName, stepUpReceipt: verified.data.receipt });
}
