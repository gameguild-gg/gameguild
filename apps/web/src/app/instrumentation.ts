import { type Instrumentation } from 'next';

export const onRequestError: Instrumentation.onRequestError = async (error, request, context): Promise<void> => {
  // Log the error details for debugging purposes
  console.error('Request Error:', {
    error,
    request,
    context,
  });
  //
  // TODO: add additional error handling logic here, such as sending the error to an external logging service
  // or notifying the development team.
  // error: { digest: string } & Error,
  // await fetch('https://.../report-error', {
  //   method: 'POST',
  //   body: JSON.stringify({
  //     message: error.message,
  //     request,
  //     context,
  //   }),
  //   headers: {
  //     'Content-Type': 'application/json',
  //   },
  // });
};
