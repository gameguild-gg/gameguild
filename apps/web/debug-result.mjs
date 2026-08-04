#!/usr/bin/env node
// Debug script to inspect the actual result structure

const { createClient } = await import('@game-guild/client');

const client = createClient({
  baseUrl: 'http://localhost:8080',
  headers: {
    'X-Tenant-Id': 'default',
  },
});

console.log('Making request to /health...\n');

const result = await client.request({
  method: 'GET',
  path: '/health',
});

console.log('Result structure:');
console.log('================');
console.log('result.ok:', result.ok);
console.log('result.value:', result.value);
console.log('result.error:', result.error);
console.log('result.data:', result.data);
console.log('\nFull result object:');
console.log(JSON.stringify(result, null, 2));
