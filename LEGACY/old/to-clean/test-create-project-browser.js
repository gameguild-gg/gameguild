// Test script to create a project via API when user is authenticated
// This should be run from the browser console on the authenticated page

async function createTestProject() {
  try {
    console.log('Creating test project...');

    const response = await fetch('/api/test-create-project', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        title: 'Test Project from Browser',
        description: 'This is a test project created from the browser to verify authentication',
        shortDescription: 'Test project for authentication verification',
        status: 'not-started',
        visibility: 'Public',
        websiteUrl: 'https://example.com',
        repositoryUrl: 'https://github.com/example/test',
      }),
    });

    if (response.ok) {
      const project = await response.json();
      console.log('Project created successfully:', project);
      alert('Project created! Check the projects page.');
    } else {
      const error = await response.text();
      console.error('Failed to create project:', error);
      alert('Failed to create project: ' + error);
    }
  } catch (error) {
    console.error('Error:', error);
    alert('Error: ' + error.message);
  }
}

// Call the function
createTestProject();
