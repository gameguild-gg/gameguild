import { auth } from '@/auth';
import { notFound } from 'next/navigation';
import { TestingRequestDetails } from '@/components/testing-lab/testing-request-details';
import { getTestingRequestsById, getTestingRequestParticipants, getTestingRequestFeedback, getTestingRequestStatistics } from '@/lib/testing-lab/testing-lab.actions';
import { Alert, AlertDescription } from '@/components/ui/alert';

interface TestingRequestPageProps {
  params: {
    id: string;
  };
}

export default async function TestingRequestPage({ params }: TestingRequestPageProps) {
  const session = await auth();

  if (!session?.accessToken) {
    return (
      <div className="container mx-auto p-6">
        <Alert variant="destructive">
          <AlertDescription>Authentication required to view testing request details</AlertDescription>
        </Alert>
      </div>
    );
  }

  try {
    const [request, participants, feedback, statistics] = await Promise.all([
      getTestingRequestsById(params.id),
      getTestingRequestParticipants(params.id).catch(() => []),
      getTestingRequestFeedback(params.id).catch(() => []),
      getTestingRequestStatistics(params.id).catch(() => null),
    ]);

    if (!request) {
      notFound();
    }

    return (
      <TestingRequestDetails
        request={request}
        participants={participants}
        feedback={feedback}
        statistics={statistics}
      />
    );
  } catch (error) {
    console.error('Error loading testing request:', error);
    return (
      <div className="container mx-auto p-6">
        <Alert variant="destructive">
          <AlertDescription>Failed to load testing request details</AlertDescription>
        </Alert>
      </div>
    );
  }
}

interface FeedbackSubmission {
  userId: string;
  responses: Record<string, string>;
  rating: number;
  additionalNotes: string;
  wouldRecommend: boolean;
}

export default function TestingRequestPage() {
  const params = useParams();
  const router = useRouter();
  const { data: session } = useSession();
  const [request, setRequest] = useState<TestingRequest | null>(null);
  const [loading, setLoading] = useState(true);
  const [submittingFeedback, setSubmittingFeedback] = useState(false);
  const [feedback, setFeedback] = useState<FeedbackSubmission>({
    userId: '',
    responses: {},
    rating: 5,
    additionalNotes: '',
    wouldRecommend: true,
  });

  useEffect(() => {
    if (params.id) {
      fetchTestingRequest(params.id as string);
    }
  }, [params.id]);

  const fetchTestingRequest = async (id: string) => {
    try {
      setLoading(true);

      // Fetch from API
      const requestData = await testingLabApi.getTestingRequest(id);
      setRequest(requestData);

      if (session?.user?.id) {
        setFeedback((prev) => ({ ...prev, userId: session.user.id }));
      }
    } catch (error) {
      console.error('Error fetching testing request:', error);

      // Fallback to mock data
      setRequest({
        id,
        title: 'Alpha Build Testing - Team 01',
        description:
          'Testing the core gameplay mechanics for our RPG game. This version includes the basic combat system, inventory management, and character progression.',
        projectVersionId: 'pv1',
        downloadUrl: 'https://drive.google.com/file/d/example123/view',
        instructionsType: 'inline',
        instructionsContent: `1. Download and extract the game files to a folder on your desktop
2. Launch GameExecutable.exe
3. Create a new character and complete the tutorial
4. Test the combat system by fighting at least 3 different enemy types
5. Try the inventory management system
6. Test character progression by leveling up
7. Look for any bugs, glitches, or usability issues
8. Pay attention to game balance and difficulty
9. Test for at least 30 minutes of gameplay`,
        feedbackFormContent: `1. How intuitive is the game's control scheme? (1-10)
2. Did you encounter any bugs or glitches? Please describe.
3. How would you rate the visual design and art style? (1-10)
4. Was the tutorial clear and helpful?
5. How balanced did the combat feel?
6. What features would you like to see improved?
7. Any suggestions for new features?
8. Overall rating (1-10)?`,
        maxTesters: 8,
        currentTesterCount: 3,
        startDate: '2024-01-15',
        endDate: '2024-01-22',
        status: 'open',
        createdBy: {
          id: 'u1',
          name: 'John Developer',
          email: 'john.dev@mymail.champlain.edu',
        },
        projectVersion: {
          id: 'pv1',
          versionNumber: 'v0.1.0-alpha',
          project: {
            id: 'p1',
            title: 'fa23-capstone-2023-24-t01',
          },
        },
      });

      if (session?.user?.id) {
        setFeedback((prev) => ({ ...prev, userId: session.user.id }));
      }
    } finally {
      setLoading(false);
    }
  };

  const handleFeedbackSubmit = async () => {
    if (!request || !session?.user) return;

    try {
      setSubmittingFeedback(true);

      // Create the feedback DTO
      const feedbackData: SubmitFeedbackDto = {
        testingRequestId: request.id,
        feedbackResponses: JSON.stringify(feedback.responses),
        overallRating: feedback.rating,
        wouldRecommend: feedback.wouldRecommend,
        additionalNotes: feedback.additionalNotes,
      };

      // Submit feedback to API
      await testingLabApi.submitFeedback(feedbackData);

      // Show success message and redirect
      alert('Feedback submitted successfully!');
      router.push('/dashboard/testing-lab');
    } catch (error) {
      console.error('Error submitting feedback:', error);
      alert('Failed to submit feedback. Please try again.');
    } finally {
      setSubmittingFeedback(false);
    }
  };

  const updateFeedbackResponse = (question: string, answer: string) => {
    setFeedback((prev) => ({
      ...prev,
      responses: { ...prev.responses, [question]: answer },
    }));
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'active':
        return <CheckCircle className="h-4 w-4 text-green-600" />;
      case 'draft':
        return <Clock className="h-4 w-4 text-yellow-600" />;
      case 'completed':
        return <CheckCircle className="h-4 w-4 text-blue-600" />;
      case 'cancelled':
        return <AlertTriangle className="h-4 w-4 text-red-600" />;
      default:
        return null;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'active':
        return 'bg-green-100 text-green-800';
      case 'draft':
        return 'bg-yellow-100 text-yellow-800';
      case 'completed':
        return 'bg-blue-100 text-blue-800';
      case 'cancelled':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  if (loading) {
    return (
      <div className="container mx-auto p-6">
        <div className="text-center">Loading testing request...</div>
      </div>
    );
  }

  if (!request) {
    return (
      <div className="container mx-auto p-6">
        <Alert>
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>Testing request not found.</AlertDescription>
        </Alert>
      </div>
    );
  }

  const feedbackQuestions = request.feedbackFormContent?.split('\n').filter((q) => q.trim()) || [];

  return (
    <div className="container mx-auto p-6 max-w-4xl">
      <div className="mb-6">
        <Button variant="outline" onClick={() => router.back()} className="mb-4">
          ← Back to Testing Lab
        </Button>

        <div className="flex justify-between items-start">
          <div>
            <h1 className="text-3xl font-bold mb-2">{request.title}</h1>
            <p className="text-muted-foreground">
              {request.projectTitle} • {request.versionNumber}
            </p>
          </div>
          <Badge className={getStatusColor(request.status)}>
            {getStatusIcon(request.status)}
            <span className="ml-1 capitalize">{request.status}</span>
          </Badge>
        </div>
      </div>

      <div className="grid gap-6">
        {/* Project Information */}
        <Card>
          <CardHeader>
            <CardTitle>Project Information</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {request.description && (
              <div>
                <Label className="text-sm font-medium">Description</Label>
                <p className="text-sm text-muted-foreground mt-1">{request.description}</p>
              </div>
            )}

            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label className="text-sm font-medium">Testing Period</Label>
                <p className="text-sm text-muted-foreground mt-1">
                  {new Date(request.startDate).toLocaleDateString()} - {new Date(request.endDate).toLocaleDateString()}
                </p>
              </div>
              <div>
                <Label className="text-sm font-medium">Testers</Label>
                <p className="text-sm text-muted-foreground mt-1">
                  {request.currentTesterCount}/{request.maxTesters || 'Unlimited'} signed up
                </p>
              </div>
            </div>

            <div>
              <Label className="text-sm font-medium">Created by</Label>
              <p className="text-sm text-muted-foreground mt-1">{request.createdBy.name}</p>
            </div>
          </CardContent>
        </Card>

        {/* Download & Instructions */}
        <Card>
          <CardHeader>
            <CardTitle>Game Download & Instructions</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {request.downloadUrl && (
              <div>
                <Label className="text-sm font-medium">Download Game</Label>
                <div className="mt-2">
                  <a href={request.downloadUrl} target="_blank" rel="noopener noreferrer" className="inline-block">
                    <Button>
                      <Download className="mr-2 h-4 w-4" />
                      Download Game Build
                    </Button>
                  </a>
                </div>
              </div>
            )}

            <Separator />

            <div>
              <Label className="text-sm font-medium">Testing Instructions</Label>
              {request.instructionsType === 'inline' && request.instructionsContent && (
                <div className="mt-2 p-4 bg-muted rounded-md">
                  <pre className="whitespace-pre-wrap text-sm">{request.instructionsContent}</pre>
                </div>
              )}
              {request.instructionsType === 'url' && request.instructionsUrl && (
                <div className="mt-2">
                  <a href={request.instructionsUrl} target="_blank" rel="noopener noreferrer" className="inline-block">
                    <Button variant="outline">
                      <FileText className="mr-2 h-4 w-4" />
                      View Instructions
                    </Button>
                  </a>
                </div>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Feedback Form */}
        <Card>
          <CardHeader>
            <CardTitle>Submit Your Feedback</CardTitle>
            <CardDescription>Please test the game following the instructions above, then provide your feedback</CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            {feedbackQuestions.map((question, index) => (
              <div key={index}>
                <Label className="text-sm font-medium">{question}</Label>
                <Textarea
                  className="mt-2"
                  placeholder="Your response..."
                  value={feedback.responses[question] || ''}
                  onChange={(e) => updateFeedbackResponse(question, e.target.value)}
                  rows={3}
                />
              </div>
            ))}

            <Separator />

            <div>
              <Label className="text-sm font-medium">Overall Rating</Label>
              <RadioGroup
                value={feedback.rating.toString()}
                onValueChange={(value) => setFeedback((prev) => ({ ...prev, rating: parseInt(value) }))}
                className="flex mt-2"
              >
                {[1, 2, 3, 4, 5, 6, 7, 8, 9, 10].map((rating) => (
                  <div key={rating} className="flex items-center space-x-1">
                    <RadioGroupItem value={rating.toString()} id={`rating-${rating}`} />
                    <Label htmlFor={`rating-${rating}`} className="text-sm">
                      {rating}
                    </Label>
                  </div>
                ))}
              </RadioGroup>
            </div>

            <div>
              <Label className="text-sm font-medium">Would you recommend this game to others?</Label>
              <RadioGroup
                value={feedback.wouldRecommend.toString()}
                onValueChange={(value) => setFeedback((prev) => ({ ...prev, wouldRecommend: value === 'true' }))}
                className="mt-2"
              >
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="true" id="recommend-yes" />
                  <Label htmlFor="recommend-yes">Yes</Label>
                </div>
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="false" id="recommend-no" />
                  <Label htmlFor="recommend-no">No</Label>
                </div>
              </RadioGroup>
            </div>

            <div>
              <Label htmlFor="additionalNotes" className="text-sm font-medium">
                Additional Notes (Optional)
              </Label>
              <Textarea
                id="additionalNotes"
                className="mt-2"
                placeholder="Any additional comments, suggestions, or observations..."
                value={feedback.additionalNotes}
                onChange={(e) => setFeedback((prev) => ({ ...prev, additionalNotes: e.target.value }))}
                rows={4}
              />
            </div>

            <div className="flex justify-end">
              <Button onClick={handleFeedbackSubmit} disabled={submittingFeedback || !session?.user}>
                <Send className="mr-2 h-4 w-4" />
                {submittingFeedback ? 'Submitting...' : 'Submit Feedback'}
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
