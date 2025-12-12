import { gql } from '@apollo/client';

// Get programs that the current user can edit
export const GET_MY_PROGRAMS = gql`
  query GetMyPrograms($skip: Int = 0, $take: Int = 50) {
    myPrograms(skip: $skip, take: $take) {
      id
      title
      description
      slug
      thumbnail
      videoShowcaseUrl
      category
      difficulty
      estimatedHours
      visibility
      status
      createdAt
      updatedAt
    }
  }
`;

// Get all published programs (public)
export const GET_PUBLISHED_PROGRAMS = gql`
  query GetPublishedPrograms(
    $skip: Int = 0, 
    $take: Int = 50
  ) {
    publishedPrograms(
      skip: $skip, 
      take: $take
    ) {
      id
      title
      description
      slug
      thumbnail
      videoShowcaseUrl
      category
      difficulty
      estimatedHours
      visibility
      status
      createdAt
      updatedAt
    }
  }
`;

// Get a program by ID
export const GET_PROGRAM_BY_ID = gql`
  query GetProgramById($id: UUID!) {
    programById(id: $id) {
      id
      title
      description
      slug
      thumbnail
      videoShowcaseUrl
      category
      difficulty
      estimatedHours
      visibility
      status
      createdAt
      updatedAt
    }
  }
`;

// Get a program by slug (public)
export const GET_PROGRAM_BY_SLUG = gql`
  query GetProgramBySlug($slug: String!) {
    programBySlug(slug: $slug) {
      id
      title
      description
      slug
      thumbnail
      videoShowcaseUrl
      category
      difficulty
      estimatedHours
      visibility
      status
      createdAt
      updatedAt
    }
  }
`;

// Test auth resolver
export const TEST_AUTH = gql`
  query TestAuth {
    testAuth
  }
`;

// Create program mutation
export const CREATE_PROGRAM = gql`
  mutation CreateProgram($input: CreateProgramInput!) {
    createProgram(input: $input) {
      id
      title
      description
      slug
      thumbnail
      videoShowcaseUrl
      category
      difficulty
      estimatedHours
      visibility
      status
      createdAt
      updatedAt
    }
  }
`;

// Update program mutation
export const UPDATE_PROGRAM = gql`
  mutation UpdateProgram($id: UUID!, $input: UpdateProgramInput!) {
    updateProgram(id: $id, input: $input) {
      id
      title
      description
      slug
      thumbnail
      videoShowcaseUrl
      category
      difficulty
      estimatedHours
      visibility
      status
      createdAt
      updatedAt
    }
  }
`;

// Delete program mutation
export const DELETE_PROGRAM = gql`
  mutation DeleteProgram($id: UUID!) {
    deleteProgram(id: $id)
  }
`;

// Publish program mutation
export const PUBLISH_PROGRAM = gql`
  mutation PublishProgram($id: UUID!) {
    publishProgram(id: $id) {
      id
      title
      description
      slug
      thumbnail
      videoShowcaseUrl
      category
      difficulty
      estimatedHours
      visibility
      status
      createdAt
      updatedAt
    }
  }
`;