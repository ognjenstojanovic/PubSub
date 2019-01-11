using System;

namespace PubSub.Solution
{
    /// <summary>
    /// PubSub Server
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPubSub<T>
    {
        /// <summary>
        /// Publishes the message to specified topic.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="topic">The topic.</param>
        void Publish(string topic, T message);

        /// <summary>
        /// Subscribes to the specified topic.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="callback">The callback.</param>
        void Subscribe(string topic, Action<string, T> callback);
    }
}