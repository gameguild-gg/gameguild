'use server';

import { getProgramById, getProgramWithContent, getPrograms, getPublishedPrograms, getProgramBySlug } from './programs.actions';
import { Program } from '@/lib/api/generated/types.gen';

/**
 * Get a program by slug
 */
export async function getProgramBySlugService(slug: string) {
  try {
    const response = await getProgramBySlug({
      path: { slug },
      url: '/api/program/slug/{slug}',
    });

    if (response.data) {
      return { success: true, data: response.data };
    }

    return { success: false, error: 'Program not found' };
  } catch (error) {
    console.error('Error fetching program by slug:', error);
    return { success: false, error: 'Failed to fetch program' };
  }
}

/**
 * Get a program by ID with content
 */
export async function getProgramWithContentService(id: string) {
  try {
    const response = await getProgramWithContent({
      path: { id },
      url: '/api/program/{id}/with-content',
    });

    if (response.data) {
      return { success: true, data: response.data };
    }

    return { success: false, error: 'Program not found' };
  } catch (error) {
    console.error('Error fetching program with content:', error);
    return { success: false, error: 'Failed to fetch program' };
  }
}

/**
 * Get a program by ID
 */
export async function getProgramByIdService(id: string) {
  try {
    const response = await getProgramById({
      path: { id },
      url: '/api/program/{id}',
    });

    if (response.data) {
      return { success: true, data: response.data };
    }

    return { success: false, error: 'Program not found' };
  } catch (error) {
    console.error('Error fetching program by ID:', error);
    return { success: false, error: 'Failed to fetch program' };
  }
}

/**
 * Get all programs
 */
export async function getAllProgramsService() {
  try {
    const response = await getPrograms();

    return {
      success: true,
      data: response.data || [],
    };
  } catch (error) {
    console.error('Error fetching programs:', error);
    return { success: false, error: 'Failed to fetch programs', data: [] };
  }
}

/**
 * Get published programs
 */
export async function getPublishedProgramsService() {
  try {
    const response = await getPublishedPrograms();

    return {
      success: true,
      data: response.data || [],
    };
  } catch (error) {
    console.error('Error fetching published programs:', error);
    return { success: false, error: 'Failed to fetch published programs', data: [] };
  }
}

/**
 * Get program level configuration
 */
export async function getProgramLevelConfig(level: number) {
  switch (level) {
    case 1:
      return {
        name: 'Beginner',
        color: 'text-green-400',
        bgColor: 'bg-green-500/20',
        borderColor: 'border-green-500/30',
        description: 'No prior experience required',
      };
    case 2:
      return {
        name: 'Intermediate',
        color: 'text-yellow-400',
        bgColor: 'bg-yellow-500/20',
        borderColor: 'border-yellow-500/30',
        description: 'Some experience recommended',
      };
    case 3:
      return {
        name: 'Advanced',
        color: 'text-orange-400',
        bgColor: 'bg-orange-500/20',
        borderColor: 'border-orange-500/30',
        description: 'Significant experience required',
      };
    case 4:
      return {
        name: 'Expert',
        color: 'text-red-400',
        bgColor: 'bg-red-500/20',
        borderColor: 'border-red-500/30',
        description: 'Advanced practitioner level',
      };
    default:
      return {
        name: 'Unknown',
        color: 'text-slate-400',
        bgColor: 'bg-slate-500/20',
        borderColor: 'border-slate-500/30',
        description: 'Level not specified',
      };
  }
}

/**
 * Transform program data for compatibility
 */
export async function transformProgramData(program: Program) {
  return {
    ...program,
    // Ensure backward compatibility with course interface
    area: program.category || 'general',
    level: program.difficulty || 1,
    estimatedHours: program.estimatedHours || 0,
  };
}
