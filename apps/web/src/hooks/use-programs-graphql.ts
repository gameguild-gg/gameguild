import {
    CREATE_PROGRAM,
    DELETE_PROGRAM,
    GET_MY_PROGRAMS,
    GET_PROGRAM_BY_ID,
    GET_PROGRAM_BY_SLUG,
    GET_PUBLISHED_PROGRAMS,
    PUBLISH_PROGRAM,
    TEST_AUTH,
    UPDATE_PROGRAM
} from '@/lib/graphql/queries/programs';
import { useApolloClient, useMutation, useQuery } from '@apollo/client';

// Types for Program (these should match your GraphQL schema)
export interface Program {
    id: string;
    title: string;
    description?: string;
    slug: string;
    thumbnail?: string;
    videoShowcaseUrl?: string;
    category?: string;
    difficulty?: string;
    estimatedHours?: number;
    visibility: string;
    status: string;
    createdAt: string;
    updatedAt?: string;
    creator?: {
        id: string;
        displayName: string;
        avatar?: string;
    };
}

export interface CreateProgramInput {
    title: string;
    description?: string;
    category?: string;
    difficulty?: string;
    visibility?: string;
    estimatedHours?: number;
    thumbnail?: string;
    videoShowcaseUrl?: string;
}

export interface UpdateProgramInput {
    title?: string;
    description?: string;
    category?: string;
    difficulty?: string;
    visibility?: string;
    estimatedHours?: number;
    thumbnail?: string;
    videoShowcaseUrl?: string;
}

// Transform Program to Course format for compatibility with existing UI
export const transformProgramToCourse = (program: Program) => ({
    id: program.id,
    title: program.title,
    description: program.description || '',
    thumbnail: program.thumbnail || '/default-course-thumbnail.jpg',
    videoShowcaseUrl: program.videoShowcaseUrl,
    category: program.category || 'General',
    difficulty: program.difficulty || 'Beginner',
    estimatedHours: program.estimatedHours || 0,
    visibility: program.visibility,
    status: program.status,
    slug: program.slug,
    createdAt: program.createdAt,
    updatedAt: program.updatedAt,
    creator: program.creator
});

// Hook to get programs the current user can edit
export const useMyPrograms = (options?: { skip?: number; take?: number }) => {
    const { data, loading, error, refetch } = useQuery(GET_MY_PROGRAMS, {
        variables: {
            skip: options?.skip || 0,
            take: options?.take || 50
        },
        errorPolicy: 'all'
    });

    return {
        programs: data?.myPrograms || [],
        courses: data?.myPrograms?.map(transformProgramToCourse) || [], // For compatibility
        loading,
        error,
        refetch
    };
};

// Hook to get published programs (public)
export const usePublishedPrograms = (options?: {
    skip?: number;
    take?: number;
}) => {
    const { data, loading, error, refetch } = useQuery(GET_PUBLISHED_PROGRAMS, {
        variables: {
            skip: options?.skip || 0,
            take: options?.take || 50
        },
        errorPolicy: 'all'
    }); return {
        programs: data?.publishedPrograms || [],
        courses: data?.publishedPrograms?.map(transformProgramToCourse) || [], // For compatibility
        loading,
        error,
        refetch
    };
};

// Hook to get a program by ID
export const useProgramById = (id: string) => {
    const { data, loading, error } = useQuery(GET_PROGRAM_BY_ID, {
        variables: { id },
        skip: !id,
        errorPolicy: 'all'
    });

    return {
        program: data?.programById,
        course: data?.programById ? transformProgramToCourse(data.programById) : null, // For compatibility
        loading,
        error
    };
};

// Hook to get a program by slug
export const useProgramBySlug = (slug: string) => {
    const { data, loading, error } = useQuery(GET_PROGRAM_BY_SLUG, {
        variables: { slug },
        skip: !slug,
        errorPolicy: 'all'
    });

    return {
        program: data?.programBySlug,
        course: data?.programBySlug ? transformProgramToCourse(data.programBySlug) : null, // For compatibility
        loading,
        error
    };
};

// Hook to test auth
export const useTestAuth = () => {
    const { data, loading, error, refetch } = useQuery(TEST_AUTH, {
        errorPolicy: 'all'
    });

    return {
        result: data?.testAuth,
        loading,
        error,
        refetch
    };
};

// Hook to create a program
export const useCreateProgram = () => {
    const client = useApolloClient();

    const [createProgram, { loading, error }] = useMutation(CREATE_PROGRAM, {
        onCompleted: () => {
            // Refetch the programs list
            client.refetchQueries({ include: [GET_MY_PROGRAMS] });
        }
    });

    return {
        createProgram: (input: CreateProgramInput) => createProgram({ variables: { input } }),
        loading,
        error
    };
};

// Hook to update a program
export const useUpdateProgram = () => {
    const client = useApolloClient();

    const [updateProgram, { loading, error }] = useMutation(UPDATE_PROGRAM, {
        onCompleted: () => {
            client.refetchQueries({ include: [GET_MY_PROGRAMS, GET_PROGRAM_BY_ID] });
        }
    });

    return {
        updateProgram: (id: string, input: UpdateProgramInput) =>
            updateProgram({ variables: { id, input } }),
        loading,
        error
    };
};

// Hook to delete a program
export const useDeleteProgram = () => {
    const client = useApolloClient();

    const [deleteProgram, { loading, error }] = useMutation(DELETE_PROGRAM, {
        onCompleted: () => {
            client.refetchQueries({ include: [GET_MY_PROGRAMS] });
        }
    });

    return {
        deleteProgram: (id: string) => deleteProgram({ variables: { id } }),
        loading,
        error
    };
};

// Hook to publish a program
export const usePublishProgram = () => {
    const client = useApolloClient();

    const [publishProgram, { loading, error }] = useMutation(PUBLISH_PROGRAM, {
        onCompleted: () => {
            client.refetchQueries({ include: [GET_MY_PROGRAMS, GET_PROGRAM_BY_ID] });
        }
    });

    return {
        publishProgram: (id: string) => publishProgram({ variables: { id } }),
        loading,
        error
    };
};