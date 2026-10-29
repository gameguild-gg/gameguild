/** Existing physical-response criterion, extracted for regression coverage. */
export function evaluateGradingTransport(proof) {
  const mutation = proof?.mutation;
  if (mutation?.transportError) {
    return { accepted: false, reason: mutation.transportError };
  }
  if (!mutation || !(mutation.status >= 200 && mutation.status < 300)) {
    return { accepted: false, reason: "The grading mutation did not return a successful response." };
  }
  if (mutation.streamComplete !== true || !(mutation.bodyBytes > 0)) {
    return { accepted: false, reason: "The grading response body did not complete." };
  }
  return { accepted: true, reason: "The grading response body completed." };
}
