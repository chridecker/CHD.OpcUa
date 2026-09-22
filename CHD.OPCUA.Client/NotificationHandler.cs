using Opc.Ua.Client.Subscriptions;
using System;
using System.Collections.Generic;
using System.Text;
using chd.OpcUa.Contracts;
using Microsoft.Extensions.Logging;

namespace chd.OpcUa.Client
{
    public class NotificationHandler : ISubscriptionNotificationHandler
    {
        public Func<ISubscription, uint, DateTime, DataValueChange[], ValueTask> DataChangeCallback { get; set; }

        /// <summary>
        /// Called for every event notification.
        /// </summary>
        public Func<ISubscription, uint, DateTime, EventNotification[], ValueTask> EventCallback { get; set; }

        /// <summary>
        /// Called for every keep alive notification.
        /// </summary>
        public Func<ISubscription, uint, DateTime, PublishState, ValueTask> KeepAliveCallback { get; set; }

        /// <summary>
        /// Called whenever the state of the subscription changes, which is also what
        /// reports that pending monitored item changes have been applied.
        /// </summary>
        public Func<ISubscription, SubscriptionState, PublishState, CancellationToken, ValueTask> StateChangedCallback { get; set; }


        ValueTask ISubscriptionNotificationHandler.OnDataChangeNotificationAsync(
            ISubscription subscription,
            uint sequenceNumber,
            DateTime publishTime,
            ReadOnlyMemory<DataValueChange> notifications,
            PublishState publishStateMask,
            IReadOnlyList<string> stringTable)
        => DataChangeCallback?.Invoke(subscription, sequenceNumber, publishTime, notifications.ToArray())
        ?? ValueTask.CompletedTask;


        /// <inheritdoc/>
        ValueTask ISubscriptionNotificationHandler.OnEventDataNotificationAsync(
            ISubscription subscription,
            uint sequenceNumber,
            DateTime publishTime,
            ReadOnlyMemory<EventNotification> notifications,
            PublishState publishStateMask,
            IReadOnlyList<string> stringTable)
            => EventCallback?.Invoke(subscription, sequenceNumber, publishTime, notifications.ToArray()) ?? ValueTask.CompletedTask;

        /// <inheritdoc/>
        ValueTask ISubscriptionNotificationHandler.OnKeepAliveNotificationAsync(
            ISubscription subscription,
            uint sequenceNumber,
            DateTime publishTime,
            PublishState publishStateMask)
        => KeepAliveCallback?.Invoke(subscription, sequenceNumber, publishTime, publishStateMask)
        ?? ValueTask.CompletedTask;

        /// <inheritdoc/>
        ValueTask ISubscriptionNotificationHandler.OnSubscriptionStateChangedAsync(
            ISubscription subscription,
            SubscriptionState state,
            PublishState publishStateMask,
            CancellationToken ct)
            => StateChangedCallback?.Invoke(subscription, state, publishStateMask, ct)
               ?? ValueTask.CompletedTask;
    }
}
