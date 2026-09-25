"use client";

import type { TestingLabPublicTestingEventGameProjection } from "@game-guild/client";
import { Badge } from "@game-guild/ui/components/badge";
import { Link } from "@/i18n/navigation";
import { ArrowLeft, ArrowRight, Gamepad2 } from "lucide-react";
import Image from "next/image";
import { useState, type ReactNode } from "react";

type GameApplicationSummary = {
  id: string;
  projectId?: string | null;
  title: string;
  status: string;
};

function GameArtwork({
  game,
  alt,
  className,
}: {
  game: TestingLabPublicTestingEventGameProjection;
  alt: string;
  className: string;
}) {
  return (
    <div className={"relative shrink-0 overflow-hidden bg-muted " + className}>
      {game.imageUrl ? (
        <Image
          src={game.imageUrl}
          alt={alt}
          fill
          sizes="(max-width: 1024px) 100vw, calc(100vw - 38rem)"
          unoptimized
          className="object-cover"
        />
      ) : (
        <div
          aria-hidden="true"
          className="absolute inset-0 grid place-items-center bg-muted/80"
        >
          <Gamepad2 className="size-12 text-muted-foreground/60" />
        </div>
      )}
    </div>
  );
}

function gameTitle(game: TestingLabPublicTestingEventGameProjection) {
  return game.title?.trim() || "Untitled game";
}

function gameDescription(game: TestingLabPublicTestingEventGameProjection) {
  return game.description?.trim() || game.shortDescription?.trim() || null;
}

export function TestingEventGames({
  games,
  eventId,
  applications = [],
  emptyState,
  children,
}: {
  games: readonly TestingLabPublicTestingEventGameProjection[];
  eventId: string;
  applications?: readonly GameApplicationSummary[];
  emptyState?: ReactNode;
  children?: ReactNode;
}) {
  const [selectedIndex, setSelectedIndex] = useState(0);
  const selectedGame = games[selectedIndex];
  const applicationsByProjectId = new Map(
    applications.flatMap((application) =>
      application.projectId
        ? [[application.projectId, application] as const]
        : [],
    ),
  );
  const publishedProjectIds = new Set(
    games.flatMap((game) => (game.projectId ? [game.projectId] : [])),
  );
  const unlistedApplications = applications.filter(
    (application) =>
      !application.projectId || !publishedProjectIds.has(application.projectId),
  );

  const showPreviousGame = () => {
    setSelectedIndex((current) =>
      current === 0 ? games.length - 1 : current - 1,
    );
  };
  const showNextGame = () => {
    setSelectedIndex((current) => (current + 1) % games.length);
  };

  return (
    <section
      aria-label="Game content"
      className="min-w-0 pl-4 pt-0 sm:pl-6 lg:col-start-2 lg:row-start-1 lg:pl-0 lg:pt-0"
    >
      {selectedGame ? (
        <section
          id="event-hero"
          role="region"
          aria-label={"Featured game: " + gameTitle(selectedGame)}
          aria-roledescription="carousel"
          className="relative isolate h-[62vh] min-h-[24rem] max-h-[52rem] overflow-hidden bg-neutral-950 sm:h-[68vh]"
        >
          <div className="absolute inset-0">
            <GameArtwork
              game={selectedGame}
              alt={"Artwork for " + gameTitle(selectedGame)}
              className="h-full w-full"
            />
          </div>
          <div
            aria-hidden="true"
            className="absolute inset-0 bg-gradient-to-t from-black/90 via-black/20 to-transparent"
          />

          <div className="absolute left-5 top-5 rounded-full border border-white/25 bg-black/55 px-3 py-1 text-xs font-medium text-white backdrop-blur-sm">
            {String(selectedIndex + 1).padStart(2, "0")} /{" "}
            {String(games.length).padStart(2, "0")}
          </div>

          <div
            aria-live="polite"
            aria-atomic="true"
            className="absolute inset-x-0 bottom-0 px-6 pb-7 pt-20 text-white sm:px-10 sm:pb-10"
          >
            <h2 className="max-w-4xl text-3xl font-semibold leading-tight tracking-tight sm:text-5xl">
              {gameTitle(selectedGame)}
            </h2>
            {gameDescription(selectedGame) ? (
              <p className="mt-3 max-w-3xl whitespace-pre-line text-sm leading-6 text-white/90 sm:text-base">
                {gameDescription(selectedGame)}
              </p>
            ) : null}
          </div>

          {games.length > 1 ? (
            <>
              <button
                type="button"
                aria-label="Previous game"
                onClick={showPreviousGame}
                className="absolute left-3 top-1/2 grid size-11 -translate-y-1/2 place-items-center rounded-full border border-white/35 bg-black/55 text-white backdrop-blur transition hover:bg-black/80 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white sm:left-5"
              >
                <ArrowLeft className="size-5" aria-hidden="true" />
              </button>
              <button
                type="button"
                aria-label="Next game"
                onClick={showNextGame}
                className="absolute right-3 top-1/2 grid size-11 -translate-y-1/2 place-items-center rounded-full border border-white/35 bg-black/55 text-white backdrop-blur transition hover:bg-black/80 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white sm:right-5"
              >
                <ArrowRight className="size-5" aria-hidden="true" />
              </button>
            </>
          ) : null}
        </section>
      ) : emptyState ? (
        <div className="grid min-h-72 place-items-center border-y border-border p-8 text-center">
          {emptyState}
        </div>
      ) : null}

      {games.length > 0 || unlistedApplications.length > 0 ? (
        <ol
          aria-label="Games and submissions"
          className="mt-6 divide-y divide-border border-t border-border"
        >
          {games.map((game, index) => {
            const title = gameTitle(game);
            const description = gameDescription(game);
            const selected = selectedIndex === index;
            const application = game.projectId
              ? applicationsByProjectId.get(game.projectId)
              : undefined;
            const applicationHref = application?.projectId
              ? `/testing-lab/events/${eventId}?submitGame=1&projectId=${encodeURIComponent(application.projectId)}#submit-game`
              : "#submit-game";

            return (
              <li
                key={game.projectId ?? title + "-" + index}
                className="py-2 first:pt-1 last:pb-1"
              >
                <button
                  type="button"
                  aria-label={"Show " + title + " in carousel"}
                  aria-current={selected ? "true" : undefined}
                  onClick={() => setSelectedIndex(index)}
                  className={
                    "grid w-full grid-cols-[4rem_minmax(0,1fr)] items-center gap-3 py-2 text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring " +
                    (selected ? "text-foreground" : "text-muted-foreground")
                  }
                >
                  <GameArtwork
                    game={game}
                    alt=""
                    className="aspect-square w-16 rounded-sm"
                  />
                  <span className="min-w-0">
                    <span className="block text-sm font-semibold leading-5">
                      {title}
                    </span>
                    {description ? (
                      <span className="mt-1 block line-clamp-3 text-sm leading-5">
                        {description}
                      </span>
                    ) : null}
                  </span>
                </button>
                {application ? (
                  <div className="ml-[4.75rem] flex flex-wrap items-center gap-x-2 gap-y-1 pb-2 text-xs">
                    <Badge variant="outline">Your game</Badge>
                    <Badge variant="secondary">{application.status}</Badge>
                    <Link
                      href={applicationHref}
                      aria-label={`Review ${title} application`}
                      className="min-h-8 inline-flex items-center font-medium text-primary underline-offset-4 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                    >
                      Manage submission
                    </Link>
                  </div>
                ) : null}
              </li>
            );
          })}
          {unlistedApplications.map((application) => {
            const applicationHref = application.projectId
              ? `/testing-lab/events/${eventId}?submitGame=1&projectId=${encodeURIComponent(application.projectId)}#submit-game`
              : `/testing-lab/events/${eventId}?submitGame=1&applicationId=${encodeURIComponent(application.id)}#submit-game`;

            return (
              <li
                key={`submission-${application.id}`}
                className="flex flex-wrap items-start justify-between gap-3 py-3"
              >
                <div className="min-w-0">
                  <p className="text-sm font-semibold">{application.title}</p>
                  <p className="text-xs text-muted-foreground">
                    Your game submission
                  </p>
                </div>
                <div className="flex items-center gap-3">
                  <Badge variant="secondary">{application.status}</Badge>
                  <Link
                    href={applicationHref}
                    aria-label={`Review ${application.title} application`}
                    className="min-h-8 inline-flex items-center text-sm font-medium text-primary underline-offset-4 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                  >
                    Manage
                  </Link>
                </div>
              </li>
            );
          })}
        </ol>
      ) : null}
      {children}
    </section>
  );
}
