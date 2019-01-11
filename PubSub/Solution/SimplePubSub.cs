using System;

namespace PubSub.Solution
{
    /// <summary>
    /// Implement your solution here!
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="IPubSub{T}" />
    public class SimplePubSub<T> : IPubSub<T>
    {
        /// <summary>
        /// Publishes the message to specified topic.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="topic">The topic.</param>
        /// <exception cref="InvalidTopicException"></exception>
        public void Publish(string topic, T message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Subscribes the specified topic.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="callback">The event.</param>
        /// <exception cref="InvalidTopicException">Topic is invalid.</exception>
        public void Subscribe(string topic, Action<string, T> callback)
        {
            throw new NotImplementedException();
        }
    }
}