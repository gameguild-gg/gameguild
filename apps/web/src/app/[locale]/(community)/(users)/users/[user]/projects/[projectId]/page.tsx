import { ProjectDetails } from '@/components/profile/project-details';
import { getUserByUsername } from '@/lib/api/users';
import { auth } from '@/lib/auth';
import { notFound } from 'next/navigation';

interface Props {
    params: Promise<{ user: string; projectId: string; }>;
}

export async function generateStaticParams(): Promise<{ user: string; projectId: string; }[]> {
    // TODO: Replace with actual data from your API
    const projects = [
        { user: 'john_doe', projectId: '1' },
        { user: 'jane_smith', projectId: '2' },
        // Add more project combinations as needed
    ];

    return projects;
}

export default async function ProjectPage({ params }: Props) {
    const { user, projectId } = await params;
    const session = await auth();

    // Fetch user data to verify the user exists
    const userData = await getUserByUsername(user);
    if (!userData || userData.isDeleted || !userData.isActive) {
        notFound();
    }

    // Check if the current user is the owner of this profile
    const isOwner = session?.user?.email === userData.email ||
        session?.user?.id === userData.id;

    return (
        <ProjectDetails
            projectId={projectId}
            username={user}
            isOwner={isOwner}
        />
    );
}
