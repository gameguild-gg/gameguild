#!/usr/bin/env node

import { assertAuthoringHostEvidence, resolveAuthoringHostEnvironment } from './coding-authoring-browser-contracts.mts';
import { runAuthoringHostProof } from './coding-authoring-browser-proof.mts';

const environment = resolveAuthoringHostEnvironment(process.env);
const evidence = await runAuthoringHostProof(environment, assertAuthoringHostEvidence);
for (const error of [...evidence.errors, ...evidence.cleanupErrors]) console.error(`[coding-authoring-host] ${error}`);
assertAuthoringHostEvidence(evidence);
