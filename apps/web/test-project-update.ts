// Test script for project update functionality
// Run this with: node --import tsx test-project-update.ts

import { getProjectById, updateProject } from './src/lib/api/projects-server.actions';

async function testProjectUpdate() {
    console.log('🧪 Testing project update functionality...');

    const projectId = '69ad8578-3020-4c36-871e-ef7920326f39';

    try {
        // First, get the current project
        console.log('📖 Fetching current project...');
        const currentProject = await getProjectById(projectId);

        if (!currentProject) {
            console.error('❌ Could not fetch project');
            return;
        }

        console.log('✅ Current project:', {
            id: currentProject.id,
            name: currentProject.name,
            description: currentProject.description,
            tags: currentProject.tags
        });

        // Now test an update
        console.log('🔄 Testing project update...');
        const updateData = {
            name: `${currentProject.name} - Updated ${new Date().toISOString()}`,
            description: `Updated description - ${new Date().toISOString()}`,
            tags: ['space', 'exploration', 'test-updated']
        };

        const updatedProject = await updateProject(projectId, updateData);

        if (updatedProject) {
            console.log('✅ Project updated successfully:', {
                id: updatedProject.id,
                name: updatedProject.name,
                description: updatedProject.description,
                tags: updatedProject.tags
            });
        } else {
            console.error('❌ Failed to update project');
        }

    } catch (error) {
        console.error('💥 Test failed:', error);
    }
}

// Run the test
testProjectUpdate().catch(console.error);
