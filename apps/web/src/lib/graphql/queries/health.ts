import { gql } from '@apollo/client';

export const HEALTH_QUERY = gql`
  query Health {
    health
  }
`;
