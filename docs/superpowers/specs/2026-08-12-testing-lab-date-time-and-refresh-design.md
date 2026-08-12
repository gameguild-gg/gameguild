# Testing Lab Date-Time Picker and Refresh Reliability

## Context

Testing Lab forms currently use native `datetime-local` inputs. Their Chrome/Windows picker is inconsistent with the GameGuild design system and makes dense event, slot, request, and session workflows difficult to use.

The authenticated interface can also remain visible while a refresh token is rotated without the replacement cookie reaching the browser. The next Server Action then presents the already-revoked token and returns `Authentication required` even though the user appeared signed in.

## Date-Time Picker Design

Add one reusable `DateTimePicker` to `@game-guild/ui` and use it for every Testing Lab date-time field.

- A shadcn `Popover` owns the interaction surface.
- A shadcn `Calendar` selects the date.
- Separate hour and minute controls preserve arbitrary existing values without invoking the browser's native date-time picker.
- Apply confirms the draft value, Clear removes optional values, and Cancel restores the last committed value.
- The trigger has a stable accessible name, keyboard focus, formatted date/time text, and an explicit `UTC` timezone indicator.
- A hidden named input preserves all existing `FormData` contracts as `YYYY-MM-DDTHH:mm` without timezone conversion.
- Required fields cannot be cleared and expose `aria-required`.
- Event scheduling becomes controlled React state so changing one date can continue moving dependent dates chronologically; direct DOM writes must not desynchronize the picker display.
- Replace native pickers in event creation/editing, recurrence, slots, testing requests, testing sessions, and access expiration.

## Refresh Reliability Design

Session refresh must be an atomic browser-visible operation.

- The Next.js `auth(handler)` wrapper collects session-cookie writes produced by `processSession` and appends them to the returned proxy response.
- Invalid encrypted sessions produce expired session cookies, preventing a stale authenticated shell from surviving another navigation.
- The application proxy remains a small composition of the existing auth wrapper and `next-intl` middleware; routing logic is not reintroduced.
- The API accepts a narrowly bounded replay of a token that was rotated moments earlier from the same client IP. It verifies that the replacement token exists and is active, issues a new access token, and returns the same replacement refresh token rather than rotating again.
- The grace interval defaults to 30 seconds and is configurable as `Jwt:RefreshTokenRotationGraceSeconds`.
- Replays outside the grace interval, from another IP, without a replacement, or with an inactive replacement remain unauthorized.

This closes both failure modes: a lost `Set-Cookie` header and concurrent refresh attempts across rendering boundaries or production pods.

## Testing

- UI component tests prove Apply, Cancel, Clear, required behavior, exact hidden form values, and absence of `datetime-local` in Testing Lab workflows.
- Client integration tests prove a refreshed session is returned with replacement `Set-Cookie` headers and an invalid session is expired.
- API unit tests prove same-IP recent rotation recovery and rejection outside the guarded window.
- Focused web/client/API tests, type checks, build, and browser smoke verification run before merge.

## Non-goals

- Changing API date serialization or converting existing Testing Lab schedule semantics to per-user timezones.
- Adding a third-party date-time picker dependency.
- Replacing `next-intl` routing or adding custom route-rewrite logic.
