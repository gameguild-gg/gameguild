import { getToken } from '@/auth';
import { getMyLearningCourses, type CourseAttendanceData } from '@/lib/courses';
import {
    createServerClient,
    GeneratedApi,
    type LearningAssessmentsAssessment,
    type LearningAssessmentsLearnerAssessmentSubmission,
    type LearningCertificatesCertificate,
    type LearningCohortsCohort,
    type LearningCohortsCohortCalendarEntry,
    type LearningExperienceSocialServicesCourseDiscussion,
    type LearningEnrollmentsEnrollment,
    type ProjectsProjectApiOutput,
} from '@game-guild/client';

export interface LearnerCourseRecord {
    course: CourseAttendanceData;
    context: LearnerCourseContext;
}

export interface LearnerCourseContext {
    enrollmentId: string | null;
    cohort: LearningCohortsCohort | null;
    calendar: LearningCohortsCohortCalendarEntry[];
    assessments: LearningAssessmentsAssessment[];
    submissions: LearningAssessmentsLearnerAssessmentSubmission[];
    discussions: LearningExperienceSocialServicesCourseDiscussion[];
    certificates: LearningCertificatesCertificate[];
}

function getApiUrl() {
    return process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
}

async function getClient() {
    const token = await getToken();
    if (!token) return null;
    return createServerClient({ baseUrl: getApiUrl(), auth: { getAccessToken: async () => token } });
}

export async function getCourseLearnerContext(courseId: string): Promise<LearnerCourseContext> {
    const empty: LearnerCourseContext = { enrollmentId: null, cohort: null, calendar: [], assessments: [], submissions: [], discussions: [], certificates: [] };
    const client = await getClient();
    if (!client) return empty;

    const programs = new GeneratedApi.LearningCoursesProgramModule(client);
    const assessmentsApi = new GeneratedApi.LearningAssessmentsModule(client);
    const cohortsApi = new GeneratedApi.LearningCohortsModule(client);
    const schedulesApi = new GeneratedApi.LearningCohortsSchedulesModule(client);
    const certificatesApi = new GeneratedApi.LearningCertificatesModule(client);
    const discussionsApi = new GeneratedApi.LearningExperienceSocialDiscussionsModule(client);

    const progressResult = await programs.getCoursesMeProgress(courseId);
    if (!progressResult.ok) return empty;
    const enrollmentId = progressResult.data.enrollmentId ?? null;

    const [activeCohortsResult, assessmentsResult, discussionsResult, certificatesResult] = await Promise.all([
        cohortsApi.getApiCohortsCourseActive(courseId),
        assessmentsApi.getAssessmentsCourse(courseId),
        discussionsApi.getApiSocialCoursesDiscussions(courseId, { take: 50, pinnedFirst: true }),
        certificatesApi.getApiCertificatesMy(),
    ]);

    let enrollment: LearningEnrollmentsEnrollment | null = null;
    if (enrollmentId) {
        const enrollmentResult = await client.request<LearningEnrollmentsEnrollment>({ method: 'GET', path: `/api/learning/enrollments/${enrollmentId}`, requiresAuth: true });
        enrollment = enrollmentResult.ok ? enrollmentResult.data : null;
    }

    const activeCohorts = activeCohortsResult.ok ? activeCohortsResult.data : [];
    let cohort = enrollment?.cohortId
        ? activeCohorts.find((candidate) => candidate.id === enrollment?.cohortId) ?? null
        : activeCohorts.length === 1 ? activeCohorts[0] : null;

    if (!cohort && enrollment?.cohortId) {
        const cohortResult = await cohortsApi.getApiCohorts(enrollment.cohortId);
        cohort = cohortResult.ok ? cohortResult.data : null;
    }

    const [calendarResult, submissionsResult] = await Promise.all([
        cohort?.id
            ? schedulesApi.getCoursesCohortsCalendar(courseId, { cohortId: cohort.id })
            : Promise.resolve(null),
        enrollmentId
            ? assessmentsApi.getAssessmentsMySubmissions(enrollmentId)
            : Promise.resolve(null),
    ]);

    return {
        enrollmentId,
        cohort,
        calendar: calendarResult?.ok ? calendarResult.data.entries ?? [] : [],
        assessments: assessmentsResult.ok ? assessmentsResult.data : [],
        submissions: submissionsResult?.ok ? submissionsResult.data : [],
        discussions: discussionsResult.ok ? discussionsResult.data : [],
        certificates: certificatesResult.ok ? certificatesResult.data.filter((certificate) => certificate.courseId === courseId) : [],
    };
}

export async function getMyCertificates(): Promise<LearningCertificatesCertificate[]> {
    const client = await getClient();
    if (!client) return [];
    const result = await new GeneratedApi.LearningCertificatesModule(client).getApiCertificatesMy();
    return result.ok ? result.data : [];
}
export async function getMyProjects(userId: string): Promise<ProjectsProjectApiOutput[]> {
    const client = await getClient();
    if (!client || !userId) return [];
    const result = await new GeneratedApi.ProjectsModule(client).getProjectsCreator(userId, { take: 100 });
    return result.ok ? result.data : [];
}
export async function getMyLearnerRecords(): Promise<LearnerCourseRecord[]> {
    const courses = await getMyLearningCourses();
    const contexts = await Promise.all(courses.map((course) => getCourseLearnerContext(course.id)));
    return courses.map((course, index) => ({ course, context: contexts[index]! }));
}