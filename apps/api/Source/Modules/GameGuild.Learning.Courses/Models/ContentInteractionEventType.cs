namespace GameGuild.Learning.Courses;

/// <summary>
/// Fine-grained events emitted while a learner consumes lesson content.
/// Values are persisted and must remain stable.
/// </summary>
public enum ContentInteractionEventType
{
    Opened = 0,
    Heartbeat = 1,
    Progressed = 2,
    Paused = 3,
    Resumed = 4,
    Seeked = 5,
    Completed = 6,
    QuizPresented = 7,
    QuizAnswered = 8,
}
