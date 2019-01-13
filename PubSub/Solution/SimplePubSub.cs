using System;
using System.Collections.Generic;
using System.Linq;

namespace PubSub.Solution
{
    /// <summary>
    /// Implement your solution here!
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="IPubSub{T}" />
    public class SimplePubSub<T> : IPubSub<T>
    {
        private readonly List<TopicNode<T>> topLevelNodes;
        private const string NumberSign = "#";
        private const string PlusSign = "+";

        public SimplePubSub()
        {
            topLevelNodes = new List<TopicNode<T>>();
        }

        #region Publishing
        /// <summary>
        /// Publishes the message to specified topic.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="topic">The topic.</param>
        /// <exception cref="InvalidTopicException"></exception>
        public void Publish(string topic, T message)
        {
            if(!TopicIsValid(topic))
            {
                throw new InvalidTopicException(topic);
            }

            var result = GetCallbacksForPublishing(topic.Substring(1), message);

            result.ForEach(c => c.Invoke(topic, message));
        }

        private List<Action<string, T>> GetCallbacksForPublishing(string topic, T message)
        {
            List<Action<string, T>> result = new List<Action<string, T>>();
            var topicNodeNames = topic.Split('/').ToList();

            if(topicNodeNames.FirstOrDefault() == NumberSign)
            {
                foreach(var topLevelNode in topLevelNodes)
                {
                    result.AddRange(GetCallbacksRecursive(topLevelNode, message));
                }
            }
            else
            {
                var parentNodes = topLevelNodes.Where(x => x.Name == topicNodeNames.FirstOrDefault() || x.Name == PlusSign).ToList();

                if(parentNodes != null && parentNodes.Any())
                {
                    topicNodeNames.RemoveAt(0);
                    
                    foreach(var node in parentNodes)
                    {
                        result.AddRange(GetSubTopicCallbacks(node, message, topicNodeNames));
                    }
                }
            }

            return result;
        }

        private List<Action<string, T>> GetSubTopicCallbacks(TopicNode<T> parentNode, T message, List<string> topicNodeNames)
        {
            var result = new List<Action<string, T>>();

            if(!topicNodeNames.Any())
            {
                result.AddRange(parentNode.Callbacks);
            }
            else if(topicNodeNames.FirstOrDefault() == NumberSign)
            {
                result.AddRange(GetAllSubTopicCallbacks(parentNode, message));
            }
            else
            {
                var parentNodes = parentNode.SubTopics.Where(x => x.Name == topicNodeNames.FirstOrDefault() || x.Name == PlusSign).ToList();

                if (parentNodes != null && parentNodes.Any())
                {
                    topicNodeNames.RemoveAt(0);

                    foreach (var node in parentNodes)
                    {
                        result.AddRange(GetSubTopicCallbacks(node, message, topicNodeNames));
                    }
                }

                if(parentNode.SubTopics.Any(t => t.Name == "#"))
                {
                    var numberSignNode = parentNode.SubTopics.FirstOrDefault(t => t.Name == "#");
                    result.AddRange(numberSignNode.Callbacks);
                }
            }

            return result;
        }

        private List<Action<string, T>> GetAllSubTopicCallbacks(TopicNode<T> parentNode, T message)
        {
            var result = new List<Action<string, T>>();

            foreach (TopicNode<T> subNode in parentNode.SubTopics)
            {
                result.AddRange(GetCallbacksRecursive(subNode, message));
            }

            return result;
        }

        private List<Action<string, T>> GetCallbacksRecursive(TopicNode<T> rootNode, T message)
        {
            var result = new List<Action<string, T>>();

            result.AddRange(rootNode.Callbacks);

            foreach (var subNode in rootNode.SubTopics)
            {
                result.AddRange(GetCallbacksRecursive(subNode, message));
            }

            return result;
        }

        #endregion

        #region Subscribing
        /// <summary>
        /// Subscribes the specified topic.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="callback">The event.</param>
        /// <exception cref="InvalidTopicException">Topic is invalid.</exception>
        public void Subscribe(string topic, Action<string, T> callback)
        {
            if (!TopicIsValid(topic))
            {
                throw new InvalidTopicException(topic);
            }

            SubscribeToTopic(topic.Substring(1), callback);

        }

        private void SubscribeToTopic(string topic, Action<string, T> callback)
        {
            var topicNodeNames = topic.Split('/').ToList();

            var topLevelNode = topLevelNodes.FirstOrDefault(x => x.Name == topicNodeNames.FirstOrDefault());

            if(topLevelNode == null)
            {
                topLevelNode = new TopicNode<T>(topicNodeNames.FirstOrDefault());
                topLevelNodes.Add(topLevelNode);
            }

            topicNodeNames.RemoveAt(0);

            CreateSubTopicNodes(topicNodeNames, callback, topLevelNode);            
        }

        private void CreateSubTopicNodes(List<string> topicNodeNames, Action<string, T> callback, TopicNode<T> parentNode)
        {
            if(!topicNodeNames.Any())
            {
                parentNode.Callbacks.Add(callback);
            }
            else
            {
                if(parentNode.SubTopics != null && parentNode.SubTopics.Any(x => x.Name == topicNodeNames.FirstOrDefault()))
                {
                    var subTopic = parentNode.SubTopics.FirstOrDefault(x => x.Name == topicNodeNames.FirstOrDefault());

                    topicNodeNames.RemoveAt(0);
                    CreateSubTopicNodes(topicNodeNames, callback, subTopic);
                }
                else
                {
                    var subTopic = new TopicNode<T>(topicNodeNames.FirstOrDefault());
                    parentNode.SubTopics.Add(subTopic);

                    topicNodeNames.RemoveAt(0);
                    CreateSubTopicNodes(topicNodeNames, callback, subTopic);
                }
            }
        }

        #endregion
        
        private bool TopicIsValid(string topic)
        {
            if(string.IsNullOrEmpty(topic))
            {
                return false;
            }

            if(!topic.StartsWith("/"))
            {
                return false;
            }

            topic = topic.Substring(1);
            var nodes = topic.Split('/');

            if(nodes.Contains(string.Empty) || nodes.Contains(null))
            {
                return false;
            }
            
            if(nodes.Any(x => x.Contains("+") && x.Length > 1) || 
                nodes.Any(x => x.Contains("#") && x.Length > 1))
            {
                return false;
            }

            if (nodes.Where(x => x == "#").Count() > 1)
            {
                return false;
            }

            if (nodes.Contains("#") && nodes.ToList().FindIndex(n => n == "#") != nodes.Count() - 1)
            {
                return false;
            }

            return true;
            
        }

        #region TopicNode

        private class TopicNode<T>
        {
            public readonly List<Action<string, T>> Callbacks;

            public string Name { get; }

            public List<TopicNode<T>> SubTopics { get; }

            public TopicNode(string name)
            {
                Name = name;
                Callbacks = new List<Action<string, T>>();
                SubTopics = new List<TopicNode<T>>();
            }
        }

        #endregion
    }
}