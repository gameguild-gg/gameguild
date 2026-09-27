'use client';

import { TestingProjectApplication } from '@/components/testing-lab/testing-project-application';
import { TestingSlotRegistration } from '@/components/testing-lab/testing-slot-registration';
import { Button } from '@game-guild/ui/components/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@game-guild/ui/components/dialog';
import { ArrowLeft, ArrowRight, FolderKanban, Gamepad2 } from 'lucide-react';
import { Link } from '@/i18n/navigation';
import { useState, type ComponentProps } from 'react';

type RegistrationProps = ComponentProps<typeof TestingSlotRegistration>;
type ProjectApplicationFlowProps = Pick<
  ComponentProps<typeof TestingProjectApplication>,
  | 'acceptsApplications'
  | 'projectVersions'
  | 'applications'
  | 'applicationSchema'
  | 'generalRules'
  | 'candidateInstructions'
  | 'requiresFeedback'
  | 'initialProjectId'
  | 'selectedApplicationId'
>;

interface TesterSessionOption {
  id: string;
  label: string;
  slot: RegistrationProps['slot'];
  registration?: RegistrationProps['registration'];
  registrationOpen: boolean;
}

type JoinStep = 'roles' | 'tester-sessions' | 'tester-form' | 'developer-form';

export function TestingEventJoin({
  eventId,
  isAuthenticated,
  testerSessions,
  testerUnavailableReason,
  testerRegistrationSchema,
  generalRules,
  testerInstructions,
  timeZoneId,
  locale,
  hour12,
  projectApplication,
  initialMode,
  initialSlotId,
  initialProjectId,
  selectedApplicationId,
}: {
  eventId: string;
  isAuthenticated: boolean;
  testerSessions: TesterSessionOption[];
  testerUnavailableReason: string;
  testerRegistrationSchema?: RegistrationProps['registrationSchema'];
  generalRules?: string | null;
  testerInstructions?: string | null;
  timeZoneId: string;
  locale: string;
  hour12: boolean;
  projectApplication: ProjectApplicationFlowProps;
  initialMode?: 'tester' | 'developer';
  initialSlotId?: string;
  initialProjectId?: string;
  selectedApplicationId?: string;
}) {
  const initialSession = initialSlotId
    ? testerSessions.find((session) => session.id === initialSlotId)
    : undefined;
  const canJoinAsTester = testerSessions.some((session) => session.registrationOpen);
  const hasExistingSubmission = projectApplication.applications?.some((application) =>
    selectedApplicationId
      ? application.id === selectedApplicationId
      : Boolean(initialProjectId && application.projectId === initialProjectId),
  ) ?? false;
  const canJoinAsDeveloper = projectApplication.acceptsApplications || hasExistingSubmission;
  const canStartJoin = canJoinAsTester || canJoinAsDeveloper;
  const validInitialMode = initialMode === 'developer' && canJoinAsDeveloper
    ? 'developer'
    : initialMode === 'tester' && initialSession?.registrationOpen
      ? 'tester'
      : undefined;
  const [open, setOpen] = useState(Boolean(validInitialMode));
  const [step, setStep] = useState<JoinStep>(() =>
    validInitialMode === 'developer'
      ? 'developer-form'
      : validInitialMode === 'tester'
        ? 'tester-form'
        : 'roles',
  );
  const [selectedTesterSessionId, setSelectedTesterSessionId] = useState(initialSession?.id ?? '');
  const [hasStartedProjectFlow, setHasStartedProjectFlow] = useState(validInitialMode === 'developer');

  const selectedTesterSession = testerSessions.find((session) => session.id === selectedTesterSessionId);
  const projectSignInReturnUrl = `/testing-lab/events/${eventId}?joinAs=developer${initialProjectId ? `&projectId=${encodeURIComponent(initialProjectId)}` : ''}${selectedApplicationId ? `&applicationId=${encodeURIComponent(selectedApplicationId)}` : ''}#join`;

  function resetAndOpen() {
    setStep('roles');
    setSelectedTesterSessionId('');
    setHasStartedProjectFlow(false);
    setOpen(true);
  }

  function startTesterFlow() {
    if (!canJoinAsTester) return;
    if (testerSessions.filter((session) => session.registrationOpen).length > 1) {
      setStep('tester-sessions');
      return;
    }
    const session = testerSessions.find((item) => item.registrationOpen);
    if (!session) return;
    setSelectedTesterSessionId(session.id);
    setStep('tester-form');
  }

  function startDeveloperFlow() {
    if (!canJoinAsDeveloper) return;
    setHasStartedProjectFlow(true);
    setStep('developer-form');
  }

  function closeDialog(nextOpen: boolean) {
    setOpen(nextOpen);
    if (!nextOpen) {
      setStep('roles');
      setSelectedTesterSessionId('');
      setHasStartedProjectFlow(false);
    }
  }

  const title = step === 'roles'
    ? 'How would you like to join?'
    : step === 'tester-sessions'
      ? 'Choose a session'
      : step === 'tester-form'
        ? 'Join as a tester'
        : 'Submit a game';
  const description = step === 'tester-sessions'
    ? 'Choose the session you want to join.'
    : step === 'tester-form'
      ? selectedTesterSession?.label
      : step === 'developer-form'
        ? 'Choose a game and build to submit for testing.'
        : undefined;
  const contentWidth = step === 'developer-form' ? 'sm:max-w-2xl' : step === 'tester-form' ? 'sm:max-w-xl' : 'sm:max-w-md';

  return (
    <div id="join" className="grid scroll-mt-6 gap-3 border-t border-border pt-4">
      <Button className="w-full" disabled={!canStartJoin} onClick={resetAndOpen}>
        {canStartJoin ? 'Join' : 'Sign-up closed'}
      </Button>
      <Dialog open={open} onOpenChange={closeDialog}>
        <DialogContent className={`max-h-[90svh] overflow-y-auto ${contentWidth}`}>
          <DialogHeader>
            <DialogTitle>{title}</DialogTitle>
            {description ? <DialogDescription>{description}</DialogDescription> : null}
          </DialogHeader>

          {step === 'roles' ? (
            <div className="divide-y divide-border border-y border-border" role="group" aria-label="Participation type">
              <button
                type="button"
                disabled={!canJoinAsTester}
                aria-label={canJoinAsTester ? 'As a tester' : 'As a tester, Closed'}
                aria-describedby="tester-join-description"
                onClick={startTesterFlow}
                className="flex min-h-20 w-full items-center gap-3 py-3 text-left transition-colors hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-60 disabled:hover:text-foreground"
              >
                <Gamepad2 className="size-5 shrink-0 text-muted-foreground" aria-hidden="true" />
                <span className="min-w-0 flex-1">
                  <span className="block font-medium">As a tester</span>
                  <span id="tester-join-description" className="mt-1 block text-sm font-normal text-muted-foreground">
                    {canJoinAsTester ? 'Choose a session to test its games or join the waitlist.' : testerUnavailableReason}
                  </span>
                </span>
                {canJoinAsTester
                  ? <ArrowRight className="size-4 shrink-0" aria-hidden="true" />
                  : <span className="text-sm text-muted-foreground">Closed</span>}
              </button>

              <button
                type="button"
                disabled={!canJoinAsDeveloper}
                aria-label={canJoinAsDeveloper ? 'As a developer' : 'As a developer, Closed'}
                aria-describedby="developer-join-description"
                onClick={startDeveloperFlow}
                className="flex min-h-20 w-full items-center gap-3 py-3 text-left transition-colors hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-60 disabled:hover:text-foreground"
              >
                <FolderKanban className="size-5 shrink-0 text-muted-foreground" aria-hidden="true" />
                <span className="min-w-0 flex-1">
                  <span className="block font-medium">As a developer</span>
                  <span id="developer-join-description" className="mt-1 block text-sm font-normal text-muted-foreground">
                    {canJoinAsDeveloper ? 'Submit a test-ready build or review an existing submission.' : 'Game submissions are closed for this playtest.'}
                  </span>
                </span>
                {canJoinAsDeveloper
                  ? <ArrowRight className="size-4 shrink-0" aria-hidden="true" />
                  : <span className="text-sm text-muted-foreground">Closed</span>}
              </button>
            </div>
          ) : null}

          {step === 'tester-sessions' ? (
            <div>
              <Button type="button" variant="ghost" className="mb-2 w-fit px-2" onClick={() => setStep('roles')}>
                <ArrowLeft className="mr-2 size-4" aria-hidden="true" /> Back
              </Button>
              <div className="divide-y divide-border border-y border-border">
                {testerSessions.filter((session) => session.registrationOpen).map((session) => (
                  <button
                    key={session.id}
                    type="button"
                    className="flex min-h-14 w-full items-center justify-between gap-3 py-3 text-left transition-colors hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                    onClick={() => {
                      setSelectedTesterSessionId(session.id);
                      setStep('tester-form');
                    }}
                  >
                    <span>{session.label}</span>
                    <ArrowRight className="size-4 shrink-0" aria-hidden="true" />
                  </button>
                ))}
              </div>
            </div>
          ) : null}

          {selectedTesterSession ? (
            <div hidden={step !== 'tester-form'}>
              <TestingSlotRegistration
                key={selectedTesterSession.id}
                eventId={eventId}
                isAuthenticated={isAuthenticated}
                registrationOpen={selectedTesterSession.registrationOpen}
                showClosedMessage={false}
                timeZoneId={timeZoneId}
                locale={locale}
                hour12={hour12}
                slot={selectedTesterSession.slot}
                registration={selectedTesterSession.registration}
                registrationSchema={testerRegistrationSchema}
                generalRules={generalRules}
                testerInstructions={testerInstructions}
                initiallyJoining
                onBack={() => setStep(testerSessions.filter((session) => session.registrationOpen).length > 1 ? 'tester-sessions' : 'roles')}
                signInReturnUrl={`/testing-lab/events/${eventId}?joinAs=tester&slotId=${encodeURIComponent(selectedTesterSession.id)}#join`}
              />
            </div>
          ) : null}

          {hasStartedProjectFlow ? (
            <div hidden={step !== 'developer-form'}>
              <Button type="button" variant="ghost" className="mb-3 w-fit px-2" onClick={() => setStep('roles')}>
                <ArrowLeft className="mr-2 size-4" aria-hidden="true" /> Back
              </Button>
              <TestingProjectApplication
                eventId={eventId}
                isAuthenticated={isAuthenticated}
                {...projectApplication}
                initialProjectId={initialProjectId}
                selectedApplicationId={selectedApplicationId}
                signInReturnUrl={projectSignInReturnUrl}
              />
            </div>
          ) : null}
        </DialogContent>
      </Dialog>
      <Link
        href="/testing-lab"
        className="inline-flex min-h-8 items-center justify-center gap-2 text-sm font-medium text-primary underline-offset-4 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
      >
        <ArrowLeft className="size-4" aria-hidden="true" />
        Browse other playtests
      </Link>
    </div>
  );
}
