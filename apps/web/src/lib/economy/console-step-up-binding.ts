import { createHash } from 'node:crypto';
import type { EconomyConsoleAction } from './console-actions';

type ActionValues = Record<string, unknown>;

export interface EconomyStepUpBinding {
  operationType: string;
  payloadHash: string;
  targetReference: string;
}

function required(values: ActionValues, key: string): string {
  const value = String(values[key] ?? '').trim();
  if (!value) throw new Error(`${key} is required.`);
  return value;
}

function positiveInteger(values: ActionValues, key: string): number {
  const value = Number(required(values, key));
  if (!Number.isSafeInteger(value) || value <= 0) throw new Error(`${key} must be a positive integer.`);
  return value;
}

function uuidN(value: string): string {
  const normalized = value.trim().toLowerCase().replaceAll('-', '');
  if (!/^[0-9a-f]{32}$/.test(normalized)) throw new Error('A valid identifier is required.');
  return normalized;
}

function hashFields(prefix: string, fields: string[]): string {
  let canonical = prefix;
  for (const field of fields) canonical += `|${Buffer.byteLength(field, 'utf8')}:${field}`;
  return createHash('sha256').update(canonical, 'utf8').digest('hex');
}

export function buildEconomyStepUpBinding(action: EconomyConsoleAction, values: ActionValues): EconomyStepUpBinding {
  const id = (key: string) => uuidN(required(values, key));
  let operationType: string;
  let targetReference: string;
  let payloadValues: string[];

  switch (action) {
    case 'payout.reserve': {
      const requestId = id('requestId');
      const transaction = hashFields('economy-payout-protected-operation-v1', ['reservation', requestId]);
      operationType = 'economy.payout.reserve'; targetReference = `payout-request:${requestId}`; payloadValues = [transaction];
      break;
    }
    case 'payout.dispatch': {
      const operationId = id('operationId');
      const version = String(positiveInteger(values, 'expectedVersion'));
      const transaction = hashFields('economy-payout-protected-operation-v1', ['dispatch', operationId, version]);
      operationType = 'economy.payout.dispatch'; targetReference = `payout-operation:${operationId}`; payloadValues = [transaction];
      break;
    }
    case 'treasury.propose': {
      const period = required(values, 'periodStart');
      if (!/^\d{4}-\d{2}-01$/.test(period)) throw new Error('periodStart must be the first day of a month.');
      const amount = String(positiveInteger(values, 'amountUnits'));
      const destination = required(values, 'destinationHash').toLowerCase();
      const idempotencyKey = required(values, 'idempotencyKey');
      const transaction = hashFields('economy-treasury-protected-operation-v1', ['proposal', period, amount, destination, idempotencyKey]);
      operationType = 'economy.treasury.propose'; targetReference = `treasury-period:${period}`; payloadValues = [transaction];
      break;
    }
    case 'treasury.approve': {
      const runId = id('runId');
      operationType = 'economy.treasury.approve'; targetReference = `treasury-withdrawal:${runId}`;
      payloadValues = [runId, String(positiveInteger(values, 'expectedVersion'))];
      break;
    }
    case 'treasury.dispatch': {
      const runId = id('runId');
      const version = String(positiveInteger(values, 'expectedVersion'));
      const transaction = hashFields('economy-treasury-protected-operation-v1', ['dispatch', runId, version]);
      operationType = 'economy.treasury.dispatch'; targetReference = `treasury-withdrawal:${runId}`; payloadValues = [transaction];
      break;
    }
    case 'policy.approve': {
      const policyId = id('policyId');
      operationType = 'economy.policy.approve'; targetReference = `policy:${policyId}`; payloadValues = [policyId];
      break;
    }
    case 'reserve.approve': {
      const proposalId = id('proposalId');
      operationType = 'economy.reserve.approve'; targetReference = `reserve:${proposalId}`; payloadValues = [proposalId];
      break;
    }
    case 'projection.approve': {
      const generation = String(positiveInteger(values, 'generation'));
      operationType = 'economy.projection.approve'; targetReference = `projection:${generation}`; payloadValues = [generation];
      break;
    }
    case 'kill-switch.release.propose':
    case 'kill-switch.release.approve': {
      const killSwitchId = id('killSwitchId');
      const verb = action.endsWith('propose') ? 'propose' : 'approve';
      operationType = `economy.kill-switch.release.${verb}`; targetReference = `kill-switch:${killSwitchId}`; payloadValues = [killSwitchId];
      break;
    }
    case 'hold.release.propose':
    case 'hold.release.approve': {
      const holdId = id('holdId');
      const verb = action.endsWith('propose') ? 'propose' : 'approve';
      operationType = `economy.compliance-hold.release.${verb}`; targetReference = `compliance-hold:${holdId}`; payloadValues = [holdId];
      break;
    }
    case 'legacy.cutover.propose':
    case 'legacy.cutover.approve':
    case 'legacy.cutover.rollback': {
      const batchId = id('batchId');
      const verb = action.split('.').at(-1)!;
      operationType = `economy.legacy-cutover.${verb}`; targetReference = `legacy-cutover:${batchId}`;
      payloadValues = verb === 'approve' ? [batchId] : [batchId, String(values.reason ?? '').trim()];
      break;
    }
    default:
      throw new Error('This action does not require step-up authentication.');
  }

  return { operationType, targetReference, payloadHash: hashFields('economy-step-up-payload-v1', payloadValues) };
}

