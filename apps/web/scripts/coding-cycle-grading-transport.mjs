function hasFinalGradeWitness(proof) {
  const queue = proof?.canonicalQueue;
  const items = queue?.items;
  return queue?.total === 1 && queue.needsGrading === 0
    && Array.isArray(items) && items.length === 1
    && items[0]?.status === "Graded"
    && items[0].activeGradeRoundStatus === "finalized"
    && items[0].requiresStaffReview === false
    && typeof proof.visibleBadge === "string" && proof.visibleBadge.trim() === "0 to grade"
    && typeof proof.releaseStatus === "string"
    && proof.releaseStatus.startsWith("Ready to release");
}

function wasCancelledAfterAcknowledgement(proof) {
  const mutation = proof.mutation;
  if (mutation.status !== 200 || mutation.streamComplete !== false || typeof mutation.transportError !== "string"
    || !mutation.transportError.endsWith("net::ERR_ABORTED")
    || !Number.isSafeInteger(mutation.requestId) || mutation.requestId < 1
    || !Array.isArray(proof.events)) return false;

  const at = (name, requestBound = false) => {
    const event = proof.events.find((entry) => entry.event === name
      && (!requestBound || entry.requestId === mutation.requestId));
    return Date.parse(event?.at ?? "");
  };
  const failure = proof.events.find((entry) => entry.event === "requestfailed"
    && entry.requestId === mutation.requestId && entry.error === "net::ERR_ABORTED");
  const response = proof.events.find((entry) => entry.event === "response"
    && entry.requestId === mutation.requestId && entry.status === 200);
  if (!failure || !response) return false;

  const click = at("click:submit");
  const requested = at("request", true);
  const responded = at("response", true);
  const zeroBadge = at("ui:zero-badge");
  const finalResult = at("ui:final-result");
  const failed = Date.parse(failure.at);
  return [click, requested, responded, zeroBadge, finalResult, failed].every(Number.isFinite)
    && click <= requested && requested <= responded
    && responded < zeroBadge && responded < finalResult
    && zeroBadge < failed && finalResult < failed;
}

/** Keep transport faults visible; accept only a fully attested grade. */
export function evaluateGradingTransport(proof) {
  const mutation = proof?.mutation;
  if (!mutation || !(mutation.status >= 200 && mutation.status < 300)) {
    return { accepted: false, reason: "The grading mutation did not return a successful response." };
  }
  if (!hasFinalGradeWitness(proof)) {
    return { accepted: false, reason: "The canonical queue and rendered final grade do not confirm completion." };
  }
  if (!mutation.transportError && mutation.streamComplete === true && mutation.bodyBytes > 0) {
    return { accepted: true, mode: "body-complete", reason: "The response and final grade completed." };
  }
  if (wasCancelledAfterAcknowledgement(proof)) {
    return { accepted: true, mode: "post-acknowledgement-cancellation",
      reason: "The confirmed grade preceded cancellation of the remaining RSC stream." };
  }
  return { accepted: false, reason: mutation.transportError ?? "The grading response body did not complete." };
}
