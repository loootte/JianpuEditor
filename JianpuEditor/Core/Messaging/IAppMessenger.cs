namespace JianpuEditor.Core.Messaging
{
    public interface IAppMessenger
    {
        void Send<TMessage>(TMessage message)
            where TMessage : class;

        void Register<TRecipient, TMessage>(
            TRecipient recipient,
            AppMessageHandler<TRecipient, TMessage> handler)
            where TRecipient : class
            where TMessage : class;

        void UnregisterAll<TRecipient>(TRecipient recipient)
            where TRecipient : class;
    }

    public delegate void AppMessageHandler<in TRecipient, in TMessage>(TRecipient recipient, TMessage message)
        where TRecipient : class
        where TMessage : class;
}
