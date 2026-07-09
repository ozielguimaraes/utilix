namespace Utilix.Abstractions.Engines;

public sealed class EngineExecutionException : Exception
{
    public string UserMessageKey { get; }

    public EngineExecutionException(string message, string userMessageKey) : base(message)
    {
        UserMessageKey = userMessageKey;
    }
}
