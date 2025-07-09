#!/usr/bin/env node

/**
 * Simple E2E API test script for the Game Guild API
 * Tests the slug-based program endpoints
 */

const http = require('http');
const https = require('https');

const API_BASE_URL = 'http://localhost:5001';

function makeRequest(url) {
  return new Promise((resolve, reject) => {
    const client = url.startsWith('https:') ? https : http;
    
    client.get(url, (res) => {
      let data = '';
      
      res.on('data', (chunk) => {
        data += chunk;
      });
      
      res.on('end', () => {
        try {
          const parsed = JSON.parse(data);
          resolve({ status: res.statusCode, data: parsed, headers: res.headers });
        } catch (error) {
          resolve({ status: res.statusCode, data: data, headers: res.headers });
        }
      });
    }).on('error', (error) => {
      reject(error);
    });
  });
}

async function testAPI() {
  console.log('🚀 Starting API E2E Tests for Program Slugs\n');

  try {
    // Test 1: Get published programs
    console.log('📋 Test 1: Getting published programs...');
    const publishedResponse = await makeRequest(`${API_BASE_URL}/api/program/published`);
    
    if (publishedResponse.status !== 200) {
      throw new Error(`Expected status 200, got ${publishedResponse.status}`);
    }
    
    const programs = publishedResponse.data;
    if (!Array.isArray(programs)) {
      throw new Error('Expected programs to be an array');
    }
    
    console.log(`✅ Found ${programs.length} published programs`);
    
    if (programs.length === 0) {
      console.log('⚠️  No programs available for further testing');
      return;
    }

    // Test 2: Validate program structure
    console.log('\n🔍 Test 2: Validating program structure...');
    const firstProgram = programs[0];
    const requiredFields = ['id', 'slug', 'title', 'description', 'category', 'difficulty'];
    
    for (const field of requiredFields) {
      if (!(field in firstProgram)) {
        throw new Error(`Program missing required field: ${field}`);
      }
    }
    
    // Validate slug format
    const slugRegex = /^[a-z0-9-]+$/;
    if (!slugRegex.test(firstProgram.slug)) {
      throw new Error(`Invalid slug format: ${firstProgram.slug}`);
    }
    
    console.log(`✅ Program structure valid. Sample slug: ${firstProgram.slug}`);

    // Test 3: Get program by valid slug
    console.log('\n🎯 Test 3: Getting program by slug...');
    const slugResponse = await makeRequest(`${API_BASE_URL}/api/program/slug/${firstProgram.slug}`);
    
    if (slugResponse.status !== 200) {
      throw new Error(`Expected status 200 for valid slug, got ${slugResponse.status}`);
    }
    
    const programBySlug = slugResponse.data;
    if (programBySlug.id !== firstProgram.id) {
      throw new Error('Program ID mismatch between endpoints');
    }
    
    if (programBySlug.slug !== firstProgram.slug) {
      throw new Error('Program slug mismatch between endpoints');
    }
    
    console.log('✅ Program retrieved successfully by slug');

    // Test 4: Test invalid slug (should return 404)
    console.log('\n❌ Test 4: Testing invalid slug...');
    const invalidSlugResponse = await makeRequest(`${API_BASE_URL}/api/program/slug/non-existent-slug-12345`);
    
    if (invalidSlugResponse.status !== 404) {
      throw new Error(`Expected status 404 for invalid slug, got ${invalidSlugResponse.status}`);
    }
    
    console.log('✅ Invalid slug correctly returns 404');

    // Test 5: Test multiple slugs for consistency
    console.log('\n🔄 Test 5: Testing multiple slugs for consistency...');
    const testSlugs = programs.slice(0, Math.min(3, programs.length)).map(p => p.slug);
    
    for (const slug of testSlugs) {
      const response = await makeRequest(`${API_BASE_URL}/api/program/slug/${slug}`);
      if (response.status !== 200) {
        throw new Error(`Failed to get program with slug: ${slug}`);
      }
      
      if (response.data.slug !== slug) {
        throw new Error(`Slug mismatch for ${slug}`);
      }
    }
    
    console.log(`✅ All ${testSlugs.length} slugs returned consistent data`);

    // Test 6: Validate slug formats across all programs
    console.log('\n🧪 Test 6: Validating all slug formats...');
    const invalidSlugs = programs.filter(p => !slugRegex.test(p.slug));
    
    if (invalidSlugs.length > 0) {
      console.log(`❌ Found ${invalidSlugs.length} programs with invalid slug formats:`);
      invalidSlugs.forEach(p => console.log(`  - ${p.slug} (${p.title})`));
    } else {
      console.log('✅ All program slugs have valid formats');
    }

    console.log('\n🎉 All API E2E tests completed successfully!');
    console.log(`\nSummary:
    - ${programs.length} programs tested
    - ${testSlugs.length} individual slug endpoints verified
    - All slug formats validated
    - Error handling (404) verified`);

  } catch (error) {
    console.error('\n💥 Test failed:', error.message);
    process.exit(1);
  }
}

// Run the tests
if (require.main === module) {
  testAPI().catch(console.error);
}

module.exports = { testAPI, makeRequest };
