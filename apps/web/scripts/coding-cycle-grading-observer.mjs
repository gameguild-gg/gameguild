/** Observe the two independent UI acknowledgements without waiting on the RSC body. */
export async function observeGradingAcknowledgement(page) {
  await page.evaluate(() => {
    const key = "__codingCycleGradingAcknowledgement";
    if (window[key]) throw new Error("A grading acknowledgement observer is already active.");
    const badge = () => document.querySelector('[data-testid="needs-grading-badge"]');
    const result = () => document.querySelector('[data-testid="result-release-status"]');
    if (!badge() || badge().textContent.trim() === "0 to grade"
      || result()?.textContent.trim().startsWith("Ready to release")) {
      throw new Error("Grading acknowledgement observation must begin with a pending review.");
    }
    const events = [];
    const visible = (element) => element !== null && element.getClientRects().length > 0
      && getComputedStyle(element).visibility !== "hidden";
    const record = (event) => {
      if (!events.some((entry) => entry.event === event)) events.push({ event, at: new Date().toISOString() });
    };
    const observer = new MutationObserver(() => {
      const currentBadge = badge();
      const currentResult = result();
      if (visible(currentBadge) && currentBadge.textContent.trim() === "0 to grade") record("ui:zero-badge");
      if (visible(currentResult) && currentResult.textContent.trim().startsWith("Ready to release")) record("ui:final-result");
    });
    window[key] = { events, observer };
    observer.observe(document.documentElement, { subtree: true, childList: true, characterData: true,
      attributes: true, attributeFilter: ["style", "class", "hidden"] });
  });
  return {
    collect: () => page.evaluate(() => window.__codingCycleGradingAcknowledgement?.events ?? []),
    dispose: () => page.evaluate(() => {
      window.__codingCycleGradingAcknowledgement?.observer.disconnect();
      delete window.__codingCycleGradingAcknowledgement;
    }),
  };
}
