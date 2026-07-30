import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  redirect: vi.fn(),
}));

vi.mock("@/i18n/navigation", () => ({
  redirect: mocks.redirect,
}));

import LegacyTestingAccessPage from "./access/page";
import LegacyTestingLocationsPage from "./locations/page";
import LegacyTestingPeoplePage from "./people/page";
import LegacyTestingReportsPage from "./reports/page";
import LegacyTestingRequestPage from "./requests/[requestId]/page";
import LegacyTestingRequestsPage from "./requests/page";
import TestingLabSettingsPage from "./settings/page";

describe("Testing Lab legacy routes", () => {
  beforeEach(() => {
    mocks.redirect.mockReset();
  });

  it.each([
    [LegacyTestingRequestsPage, "/dashboard/testing-lab/projects"],
    [LegacyTestingPeoplePage, "/dashboard/testing-lab/participants"],
    [LegacyTestingReportsPage, "/dashboard/testing-lab/analytics"],
    [LegacyTestingLocationsPage, "/dashboard/testing-lab/settings/locations"],
    [LegacyTestingAccessPage, "/dashboard/testing-lab/settings/access"],
    [TestingLabSettingsPage, "/dashboard/testing-lab/settings/general"],
  ])(
    "preserves locale while redirecting a legacy workspace route",
    async (page, href) => {
      await page({ params: Promise.resolve({ locale: "pt-BR" }) });

      expect(mocks.redirect).toHaveBeenCalledWith({ href, locale: "pt-BR" });
    },
  );

  it("preserves the request identifier when redirecting a project detail", async () => {
    await LegacyTestingRequestPage({
      params: Promise.resolve({ locale: "pt-BR", requestId: "request-42" }),
    });

    expect(mocks.redirect).toHaveBeenCalledWith({
      href: "/dashboard/testing-lab/projects/request-42",
      locale: "pt-BR",
    });
  });
});
