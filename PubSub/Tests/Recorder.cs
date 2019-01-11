using System;
using System.Collections.Generic;

namespace PubSub.Tests
{
    internal class Recorder<T>
    {
        public Action<string, T> Handler => (topic, message) => Messages.Add(Tuple.Create(topic, message));

        public List<Tuple<string, T>> Messages { get; } = new List<Tuple<string, T>>();
    }
}