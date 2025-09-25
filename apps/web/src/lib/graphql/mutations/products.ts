import { gql } from '@apollo/client';

// Mutation to create a product
export const CREATE_PRODUCT = gql`
  mutation CreateProduct($input: CreateProductInput!) {
    createProduct(input: $input) {
      id
      title
      name
      description
      shortDescription
      imageUrl
      slug
      status
      type
      isBundle
      hasAccess
      currentPricing {
        id
        basePrice
        currency
        isDefault
      }
      creator {
        id
        name
        email
      }
      productPrograms {
        id
        sortOrder
        program {
          id
          title
          description
          slug
          thumbnail
          videoShowcaseUrl
          category
          difficulty
          estimatedHours
        }
      }
      createdAt
      updatedAt
    }
  }
`;

// Mutation to update a product
export const UPDATE_PRODUCT = gql`
  mutation UpdateProduct($input: UpdateProductInput!) {
    updateProduct(input: $input) {
      id
      title
      name
      description
      shortDescription
      imageUrl
      slug
      status
      type
      isBundle
      hasAccess
      currentPricing {
        id
        basePrice
        currency
        isDefault
      }
      creator {
        id
        name
        email
      }
      productPrograms {
        id
        sortOrder
        program {
          id
          title
          description
          slug
          thumbnail
          videoShowcaseUrl
          category
          difficulty
          estimatedHours
        }
      }
      createdAt
      updatedAt
    }
  }
`;