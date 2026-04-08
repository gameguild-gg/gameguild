import React from 'react';

/**
 * Course Unauthorized (401)
 *
 * Displayed when the user is not authenticated.
 * Triggered by calling `unauthorized()` from 'next/navigation' in page/layout.
 *
 * Next.js will return a 401 status code automatically.
 *
 * @see https://nextjs.org/docs/app/api-reference/functions/unauthorized
 */
export default function Unauthorized(): React.JSX.Element {
  // TODO: Implement unauthorized UI with:
  // - "Please log in" message
  // - Login form or link to login page
  // - Redirect back to this course after login

  return <></>;
}
