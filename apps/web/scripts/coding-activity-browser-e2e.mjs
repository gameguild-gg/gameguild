#!/usr/bin/env node

import { assertLearnerHostEvidence, resolveLearnerHostEnvironment } from './coding-learner-browser-contracts.mts';
import { runLearnerHostProof } from './coding-learner-browser-proof.mts';

const environment = resolveLearnerHostEnvironment(process.env);
const evidence = await runLearnerHostProof(environment, assertLearnerHostEvidence);
for (const error of [...evidence.errors, ...evidence.cleanupErrors]) console.error(`[coding-solving-host] ${error}`);
assertLearnerHostEvidence(evidence);
