namespace LastSignal
{
    public interface IInteractable
    {
        string Prompt { get; }
        bool Available { get; }
        bool TryInteract();
    }
}
