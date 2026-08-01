"use client";

import { usePathname, useRouter } from "@/i18n/navigation";
import type { TestingLabTestingSlotRegistrationStatus } from "@game-guild/client";
import { Button } from "@game-guild/ui/components/button";
import { Input } from "@game-guild/ui/components/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@game-guild/ui/components/select";
import { Search, X } from "lucide-react";
import { type FormEvent, useState, useTransition } from "react";

const statusOptions: Array<{
  label: string;
  value: TestingLabTestingSlotRegistrationStatus;
}> = [
  { label: "Registered", value: "Registered" },
  { label: "Waitlisted", value: "Waitlisted" },
  { label: "Checked in", value: "CheckedIn" },
  { label: "Attended", value: "Attended" },
  { label: "Completed", value: "Completed" },
  { label: "Cancelled", value: "Cancelled" },
  { label: "No-show", value: "NoShow" },
];

export function TestingParticipantFilters({
  search,
  status,
}: {
  search?: string;
  status?: TestingLabTestingSlotRegistrationStatus;
}) {
  const pathname = usePathname();
  const router = useRouter();
  const [query, setQuery] = useState(search ?? "");
  const [isPending, startTransition] = useTransition();

  const navigate = (
    nextSearch: string,
    nextStatus?: TestingLabTestingSlotRegistrationStatus,
  ) => {
    const params = new URLSearchParams();
    const normalizedSearch = nextSearch.trim();
    if (normalizedSearch) params.set("q", normalizedSearch);
    if (nextStatus) params.set("status", nextStatus);
    const suffix = params.toString();

    startTransition(() =>
      router.replace(suffix ? `${pathname}?${suffix}` : pathname),
    );
  };

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    navigate(query, status);
  };

  return (
    <div className="flex flex-col gap-3 border-y bg-muted/15 p-3 sm:flex-row sm:items-center">
      <form onSubmit={handleSubmit} className="flex min-w-0 flex-1 gap-2">
        <div className="relative min-w-0 flex-1 sm:max-w-xl">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            aria-label="Search participants"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            className="pl-9"
            placeholder="Search member, email, event, campus, or room"
          />
        </div>
        <Button type="submit" variant="outline" disabled={isPending}>
          <Search className="size-4" />
          Search
        </Button>
      </form>

      <div className="flex items-center gap-2">
        <Select
          value={status ?? "all"}
          onValueChange={(value) =>
            navigate(
              query,
              value === "all"
                ? undefined
                : (value as TestingLabTestingSlotRegistrationStatus),
            )
          }
        >
          <SelectTrigger
            className="w-full sm:w-44"
            aria-label="Filter participants by status"
          >
            <SelectValue placeholder="All statuses" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All statuses</SelectItem>
            {statusOptions.map((option) => (
              <SelectItem key={option.value} value={option.value}>
                {option.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        {(search || status) && (
          <Button
            type="button"
            variant="ghost"
            size="icon"
            title="Clear participant filters"
            aria-label="Clear participant filters"
            onClick={() => {
              setQuery("");
              navigate("", undefined);
            }}
          >
            <X className="size-4" />
          </Button>
        )}
      </div>
    </div>
  );
}
