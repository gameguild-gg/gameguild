import { gql } from '@apollo/client';

export const GET_PUBLISHED_PRODUCTS_WITH_PROGRAMS = gql`
  query GetPublishedProductsWithPrograms {
    publishedProducts {
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

export const GET_ALL_PRODUCTS_WITH_PROGRAMS = gql`
  query GetAllProductsWithPrograms {
    products {
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

export const SEARCH_PRODUCTS_WITH_PROGRAMS = gql`
  query SearchProductsWithPrograms($searchTerm: String!) {
    searchProducts(searchTerm: $searchTerm) {
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

export const GET_MY_PRODUCTS_WITH_PROGRAMS = gql`
  query GetMyProductsWithPrograms($skip: Int = 0, $take: Int = 50) {
    myProducts(skip: $skip, take: $take) {
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