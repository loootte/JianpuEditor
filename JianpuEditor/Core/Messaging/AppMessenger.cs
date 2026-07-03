using CommunityToolkit.Mvvm.Messaging;

namespace JianpuEditor.Core.Messaging
{
    public sealed class AppMessenger : IAppMessenger
    {
        private readonly IMessenger _messenger;

        public AppMessenger()
            : this(WeakReferenceMessenger.Default)
        {
        }

        public AppMessenger(IMessenger messenger)
        {
            _messenger = messenger;
        }

        public void Send<TMessage>(TMessage message)
            where TMessage : class
        {
            _messenger.Send(message);
        }

        public void Register<TRecipient, TMessage>(
            TRecipient recipient,
            AppMessageHandler<TRecipient, TMessage> handler)
            where TRecipient : class
            where TMessage : class
        {
            _messenger.Register<TRecipient, TMessage>(
                recipient,
                (recipientInstance, message) => handler(recipientInstance, message));
        }

        public void UnregisterAll<TRecipient>(TRecipient recipient)
            where TRecipient : class
        {
            _messenger.UnregisterAll(recipient);
        }
    }
}
