namespace BotSharp.Core.Infrastructures;

public enum ChangeEventType
{
    Added,
    Updated,
    Deleted,
    LastState
}

public class ChangeRecord
{
    public ChangeEventType EventType { get; set; }

    public IReadOnlyDictionary<string, object> Fields { get; set; }
}

public interface IChangeDataCaptureHook
{
    Task<bool> OnChangeCaptured(ChangeRecord record);
}