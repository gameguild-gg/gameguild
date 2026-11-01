# Accessibility Statement

GameGuild is committed to making the platform usable by everyone, including
people with disabilities. GameGuild is where game creators learn, build, and
ship together — courses, community, game jams, mentorship, and tools — and all
of those experiences should be accessible.

This statement explains our conformance target, the technologies we support,
known limitations, and how to report accessibility barriers.

## Conformance Status

GameGuild targets **WCAG 2.1 Level AA** conformance.

We are currently **partially conformant** with WCAG 2.1 AA. This means most of
the platform meets the standard, but some content or functionality does not
(see [Known Limitations](#known-limitations)). We are actively working toward
full AA conformance and treat accessibility regressions as bugs.

## Supported Browsers and Assistive Technology

GameGuild is a web application built with Next.js. We support the current and
previous major versions of:

- Chrome
- Firefox
- Safari
- Edge

### Screen readers

| Screen reader | Platform | Browser |
| ------------- | -------- | ------- |
| NVDA | Windows | Chrome, Firefox |
| JAWS | Windows | Chrome, Edge |
| VoiceOver | macOS / iOS | Safari |

### Keyboard and other input

- Keyboard-only navigation is supported: interactive controls are reachable in
  a logical order and operable from the keyboard, with a visible focus
  indicator.
- Browser zoom up to 200% and operating-system text scaling are supported.
- Reduced-motion preferences are respected where the platform uses animation.

## Testing Approach

- **Automated:** [axe-core](https://github.com/dequelabs/axe-core) runs in CI
  against key user flows to catch regressions before they ship.
- **Manual:** manual testing with screen readers (NVDA, JAWS, VoiceOver) and
  keyboard-only navigation is ongoing across core journeys — courses, the
  community, game jams, mentorship, and tools.

## Known Limitations

- **Rich code editor.** The in-browser code editor (Emception/Monaco) has
  limited screen reader support. Editing code in the editor may be difficult
  or impossible with some screen reader and browser combinations. We are
  tracking this and exploring alternatives (including an accessible plain-text
  editing mode) while improvements land upstream.
- **Interactive visualizations.** Some interactive visualizations may not be
  fully accessible: they may lack complete keyboard operation, text
  alternatives, or screen reader announcements. We are iterating on these
  surfaces.

If one of these limitations blocks you, please report it — reports directly
influence our remediation order.

## Reporting an Accessibility Barrier

We welcome reports of any accessibility barrier on GameGuild:

- Open an issue using the **Accessibility** issue template
  (label it `accessibility`), or
- Email [accessibility@gameguild.gg](mailto:accessibility@gameguild.gg)

Please include the page or route, the assistive technology you use, and what
you expected vs. what happened. Our target is a first response within **5
business days**.

## Feedback on This Statement

Suggestions to improve this statement are welcome — open an issue with the
`accessibility` label or email
[accessibility@gameguild.gg](mailto:accessibility@gameguild.gg).
