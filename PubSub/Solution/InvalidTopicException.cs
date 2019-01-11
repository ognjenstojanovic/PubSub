using System;

namespace PubSub.Solution
{
    [Serializable]
    public class InvalidTopicException : Exception
    {
        public InvalidTopicException(string topic) 
            : base($"Topic '{topic}' is invalid.")
        {
        }
    }
}
