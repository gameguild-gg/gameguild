import { appendFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';

export function selectUploadReceipt(baseName, attempts) {
  if (typeof baseName !== 'string' || !baseName || /[\r\n]/.test(baseName)) {
    throw new Error('A single-line artifact name is required.');
  }
  if (!Array.isArray(attempts) || attempts.length !== 3) {
    throw new Error('Exactly three bounded upload outcomes are required.');
  }
  const successful = attempts
    .map((attempt, index) => ({ ...attempt, number: index + 1 }))
    .filter((attempt) => attempt.outcome === 'success');
  if (successful.length !== 1) {
    throw new Error('Required evidence needs exactly one successful upload receipt.');
  }
  const upload = successful[0];
  if (!/^[1-9]\d*$/.test(upload.id ?? '')) {
    throw new Error('The successful upload did not return an artifact identifier.');
  }
  const digest = (upload.digest ?? '').replace(/^sha256:/, '').toLowerCase();
  if (!/^[a-f0-9]{64}$/.test(digest)) {
    throw new Error('The successful upload did not return a SHA-256 digest.');
  }
  if (typeof upload.url !== 'string' || /[\r\n]/.test(upload.url)) {
    throw new Error('The successful upload did not return a safe artifact URL.');
  }
  const url = new URL(upload.url);
  if (url.protocol !== 'https:' || !url.pathname.endsWith(`/artifacts/${upload.id}`)) {
    throw new Error('The upload URL does not identify the retained artifact.');
  }
  return {
    id: upload.id,
    url: upload.url,
    digest,
    name: upload.number === 1 ? baseName : `${baseName}-retry-${upload.number}`,
    attempt: upload.number,
  };
}

export function writeUploadReceipt(environment) {
  const attempts = [1, 2, 3].map((number) => ({
    outcome: environment[`UPLOAD_${number}_OUTCOME`],
    id: environment[`UPLOAD_${number}_ID`],
    url: environment[`UPLOAD_${number}_URL`],
    digest: environment[`UPLOAD_${number}_DIGEST`],
  }));
  const receipt = selectUploadReceipt(environment.ARTIFACT_BASE_NAME, attempts);
  if (!environment.GITHUB_OUTPUT) {
    throw new Error('The runner output file is required to publish the receipt.');
  }
  appendFileSync(environment.GITHUB_OUTPUT, [
    `artifact-id=${receipt.id}`,
    `artifact-url=${receipt.url}`,
    `artifact-digest=${receipt.digest}`,
    `artifact-name=${receipt.name}`,
    '',
  ].join('\n'));
  return receipt;
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    const receipt = writeUploadReceipt(process.env);
    console.log(`Required artifact retained: name=${receipt.name} id=${receipt.id} attempt=${receipt.attempt} sha256=${receipt.digest}`);
  } catch (error) {
    console.error(`::error::Required evidence retention failed: ${error.message}`);
    process.exitCode = 1;
  }
}
