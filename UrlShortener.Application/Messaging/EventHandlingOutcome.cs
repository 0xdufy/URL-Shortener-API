namespace UrlShortener.Application.Messaging;

public enum EventHandlingOutcome
{
    Completed,
    Retry,
    DeadLetter
}
