using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// ReSharper disable InconsistentNaming
namespace $rootnamespace$.Events
{
    /*
     * EventAggregator origins based on work from StatLight's EventAggregator. Which 
     * is based on original work by Jermey Miller's EventAggregator in StoryTeller 
     * with some concepts pulled from Rob Eisenberg in caliburnmicro.
     * 
     * TODO:
     *		Possibly provide well defined initial thread marshalling actions (depending on platform (WinForm, WPF, Silverlight, WP7???)
     *		Document the public API better.
     */

    /// <summary>
    /// Specifies a class that would like to receive particular messages.
    /// </summary>
    /// <typeparam name="TMessage">The type of message object to subscribe to.</typeparam>
    public interface IListener<in TMessage>
    {
        /// <summary>
        /// This will be called every time a TMessage is published through the event aggregator
        /// </summary>
        void Handle(TMessage message);
    }

    /// <summary>
    /// Provides a way to add and remove a listener object from the EventAggregator
    /// </summary>
    public interface IEventSubscriptionManager
    {
        /// <summary>
        /// Adds
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="holdStrongReference">determines if the EventAggregator should hold a weak or strong reference to the listener object. Use the Config level option unless overriden by the parameter.</param>
        /// <returns>Returns the current instance of IEventSubscriptionManager to allow for easy additional</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        IEventSubscriptionManager AddListener(object listener, bool? holdStrongReference = null);
        IEventSubscriptionManager RemoveListener(object listener);
    }

    public interface IEventPublisher
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        void SendMessage<TMessage>(TMessage message, Action<Action> marshal = null);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        void SendMessage<TMessage>(Action<Action> marshal = null)
            where TMessage : new();
    }

    public interface IEventAggregator : IEventPublisher, IEventSubscriptionManager { }
    public class EventAggregator : IEventPublisher, IEventSubscriptionManager
    {
        private readonly ListenerWrapperCollection _listeners;
        private readonly Config _config;

        public EventAggregator()
            : this(new Config())
        {
        }

        public EventAggregator(Config config)
        {
            _config = config;
            _listeners = new ListenerWrapperCollection();
        }

        /// <summary>
        /// This will send the message to each IListener that is subscribing to TMessage.
        /// </summary>
        /// <typeparam name="TMessage">The type of message being sent</typeparam>
        /// <param name="message">The message instance</param>
        /// <param name="marshal">You can optionally override how the message publication action is marshalled</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public void SendMessage<TMessage>(TMessage message, Action<Action> marshal = null)
        {
            if (marshal == null)
                marshal = _config.DefaultThreadMarshaler;

            Call<IListener<TMessage>>(message, marshal);
        }

        /// <summary>
        /// This will create a new default instance of TMessage and send the message to each IListener that is subscribing to TMessage.
        /// </summary>
        /// <typeparam name="TMessage">The type of message being sent</typeparam>
        /// <param name="marshal">You can optionally override how the message publication action is marshalled</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public void SendMessage<TMessage>(Action<Action> marshal = null)
            where TMessage : new()
        {
            SendMessage(new TMessage(), marshal);
        }

        private void Call<TListener>(object message, Action<Action> marshaller)
            where TListener : class
        {
            int listenerCalledCount = 0;
            marshaller(() =>
            {
                foreach (ListenerWrapper o in _listeners)
                {
                    if (o.Handles<TListener>())
                    {
                        bool wasThisOneCalled;
                        o.TryHandle<TListener>(message, out wasThisOneCalled);
                        if (wasThisOneCalled)
                            listenerCalledCount++;
                    }
                }
            });

            var wasAnyListenerCalled = listenerCalledCount > 0;

            if (!wasAnyListenerCalled)
            {
                _config.OnMessageNotPublishedBecauseZeroListeners(message);
            }
        }

        public IEventSubscriptionManager AddListener(object listener)
        {
            return AddListener(listener, null);
        }

        public IEventSubscriptionManager AddListener(object listener, bool? holdStrongReference)
        {
            bool holdRef = _config.HoldReferences;
            if (holdStrongReference.HasValue)
                holdRef = holdStrongReference.Value;
            _listeners.AddListener(listener, holdRef);

            return this;
        }

        public IEventSubscriptionManager RemoveListener(object listener)
        {
            _listeners.RemoveListener(listener);
            return this;
        }

        /// <summary>
        /// Wrapper collection of ListenerWrappers to manage things like 
        /// threadsafe manipulation to the collection, and convenience 
        /// methods to configure the collection
        /// </summary>
        class ListenerWrapperCollection : IEnumerable<ListenerWrapper>
        {
            private readonly List<ListenerWrapper> _listeners = new List<ListenerWrapper>();
            private readonly object _sync = new object();

            public void RemoveListener(object listener)
            {
                ListenerWrapper listenerWrapper;
                lock (_sync)
                    if (TryGetListenerWrapperByListener(listener, out listenerWrapper))
                        _listeners.Remove(listenerWrapper);
            }

            private void RemoveListenerWrapper(ListenerWrapper listenerWrapper)
            {
                lock (_sync)
                    _listeners.Remove(listenerWrapper);
            }

            public IEnumerator<ListenerWrapper> GetEnumerator()
            {
                lock (_sync)
                    return _listeners.ToList().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private bool ContainsListener(object listener)
            {
                ListenerWrapper listenerWrapper;
                return TryGetListenerWrapperByListener(listener, out listenerWrapper);
            }

            private bool TryGetListenerWrapperByListener(object listener, out ListenerWrapper listenerWrapper)
            {
                lock (_sync)
                    listenerWrapper = _listeners.SingleOrDefault(x => x.ListenerInstance == listener);

                return listenerWrapper != null;
            }

            public void AddListener(object listener, bool holdStrongReference)
            {
                lock (_sync)
                {

                    if (ContainsListener(listener))
                        return;

                    var listenerWrapper = new ListenerWrapper(listener, RemoveListenerWrapper, holdStrongReference);

                    _listeners.Add(listenerWrapper);
                }
            }
        }

        #region IReference

        private interface IReference
        {
            object Target { get; }
        }

        private class WeakReferenceImpl : IReference
        {
            private readonly WeakReference _reference;

            public WeakReferenceImpl(object listener)
            {
                _reference = new WeakReference(listener);
            }

            public object Target
            {
                get { return _reference.Target; }
            }
        }

        private class StrongReferenceImpl : IReference
        {
            private readonly object _target;

            public StrongReferenceImpl(object target)
            {
                _target = target;
            }

            public object Target
            {
                get { return _target; }
            }
        }
        #endregion

        class ListenerWrapper
        {
            private const string HandleMethodName = "Handle";
            private readonly Dictionary<Type, MethodInfo> _supportedListeners = new Dictionary<Type, MethodInfo>();
            private readonly Action<ListenerWrapper> _onRemoveCallback;
            private readonly IReference _reference;

            private static IEnumerable<Type> GetBaseInterfaceType(Type type)
            {
                if (type == null)
                    return new Type[0];

                List<Type> interfaces = type.GetInterfaces().ToList();

                foreach (var @interface in interfaces.ToArray())
                {
                    interfaces.AddRange(GetBaseInterfaceType(@interface));
                }

                if (type.IsInterface)
                    interfaces.Add(type);

                return interfaces.Distinct();
            }

            public ListenerWrapper(object listener, Action<ListenerWrapper> onRemoveCallback, bool holdReferences)
            {
                _onRemoveCallback = onRemoveCallback;

                if (holdReferences)
                    _reference = new StrongReferenceImpl(listener);
                else
                    _reference = new WeakReferenceImpl(listener);

                var listenerInterfaces = GetBaseInterfaceType(listener.GetType())
                    .Where(w => DirectlyClosesGeneric(w, typeof(IListener<>)));

                foreach (var listenerInterface in listenerInterfaces)
                {
                    var handleMethod = listenerInterface.GetMethod(HandleMethodName);
                    var messageType = listenerInterface.GetGenericArguments().First();
                    _supportedListeners.Add(messageType, handleMethod);
                }
            }

            public object ListenerInstance { get { return _reference.Target; } }

            public bool Handles<TListener>()
                where TListener : class
            {
                var messageType = typeof(TListener).GetGenericArguments().First();
                if (_supportedListeners.ContainsKey(messageType))
                    return true;
                return false;
            }

            public void TryHandle<TListener>(object message, out bool wasHandled)
                where TListener : class
            {
                var target = _reference.Target;
                wasHandled = false;
                if (target == null)
                {
                    _onRemoveCallback(this);
                    return;
                }

                var messageType = typeof(TListener).GetGenericArguments().First();

                if (!_supportedListeners.ContainsKey(messageType))
                    return;

                _supportedListeners[messageType].Invoke(target, new[] { message });
                wasHandled = true;
            }

            private static bool DirectlyClosesGeneric(Type type, Type openType)
            {
                if (type == null)
                    return false;

                if (type.IsGenericType && type.GetGenericTypeDefinition() == openType)
                    return true;

                return false;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public class Config
        {
            private Action<object> _onMessageNotPublishedBecauseZeroListeners = msg => { /* TODO: possibly Trace message?*/ };
            public Action<object> OnMessageNotPublishedBecauseZeroListeners
            {
                get { return _onMessageNotPublishedBecauseZeroListeners; }
                set { _onMessageNotPublishedBecauseZeroListeners = value; }
            }

            private Action<Action> _defaultThreadMarshaler = action => action();
            public Action<Action> DefaultThreadMarshaler
            {
                get { return _defaultThreadMarshaler; }
                set { _defaultThreadMarshaler = value; }
            }

            /// <summary>
            /// If true instructs the EventAggregator to hold onto a reference to all listener objects. You will then have to explicitly remove them from the EventAggrator.
            /// If false then a WeakReference is used and the garbage collector can remove the listener when not in scope any longer.
            /// </summary>
            public bool HoldReferences { get; set; }
        }
    }
}
// ReSharper enable InconsistentNaming