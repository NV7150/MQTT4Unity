using System;
using System.Collections.Generic;

namespace MQTT4Unity {
    public class MqttSubscribePool {
        private class TopicTrieNode {
            public Dictionary<string, TopicTrieNode> Children = new();
            public List<SubscribeCallback> Callbacks = new();
            public TopicTrieNode PlusNode; // For '+'
            public TopicTrieNode HashNode; // For '#'
        }

        private readonly TopicTrieNode _root = new();

        public void AddSubscription(string topicFilter, SubscribeCallback callback) {
            var levels = topicFilter.Split('/');
            var node = _root;

            for (var i = 0; i < levels.Length; i++) {
                var level = levels[i];

                if (level == "+") {
                    node.PlusNode ??= new TopicTrieNode();
                    node = node.PlusNode;
                } else if (level == "#") {
                    if (i != levels.Length - 1)
                        throw new ArgumentException("'#' wildcard must be at the end of the topic filter");
                    node.HashNode ??= new TopicTrieNode();
                    node = node.HashNode;
                    break; // '#' must be at the end
                } else {
                    if (!node.Children.ContainsKey(level))
                        node.Children[level] = new TopicTrieNode();
                    node = node.Children[level];
                }
            }

            node.Callbacks.Add(callback);
        }

        public void RemoveSubscription(string topicFilter, SubscribeCallback callback) {
            var levels = topicFilter.Split('/');
            RemoveSubscriptionRecursive(_root, levels, 0, callback);
        }

        private bool RemoveSubscriptionRecursive(TopicTrieNode node, string[] levels, int index, SubscribeCallback callback) {
            if (node == null)
                return false;

            if (index == levels.Length) {
                node.Callbacks.Remove(callback);
                return node.Callbacks.Count == 0 && node.Children.Count == 0 && node.PlusNode == null && node.HashNode == null;
            }

            var level = levels[index];
            TopicTrieNode childNode = null;

            switch (level) {
                case "+": {
                    childNode = node.PlusNode;
                    if (RemoveSubscriptionRecursive(childNode, levels, index + 1, callback))
                        node.PlusNode = null;

                    break;
                }
                case "#": {
                    childNode = node.HashNode;
                    if (RemoveSubscriptionRecursive(childNode, levels, index + 1, callback))
                        node.HashNode = null;

                    break;
                }
                default: {
                    if (node.Children.TryGetValue(level, out childNode)) {
                        if (RemoveSubscriptionRecursive(childNode, levels, index + 1, callback)) {
                            node.Children.Remove(level);
                        }
                    }

                    break;
                }
            }

            return node.Callbacks.Count == 0 && node.Children.Count == 0 && node.PlusNode == null && node.HashNode == null;
        }

        public List<SubscribeCallback> GetCallbacks(string topic) {
            var callbacks = new List<SubscribeCallback>();
            var levels = topic.Split('/');
            GetCallbacksRecursive(_root, levels, 0, callbacks);
            return callbacks;
        }

        private void GetCallbacksRecursive(TopicTrieNode node, string[] levels, int index, List<SubscribeCallback> callbacks) {
            if (node == null)
                return;

            if (index == levels.Length) {
                callbacks.AddRange(node.Callbacks);
                if (node.HashNode != null)
                    callbacks.AddRange(node.HashNode.Callbacks);
                return;
            }

            // Exact match
            if (node.Children.TryGetValue(levels[index], out var childNode)) {
                GetCallbacksRecursive(childNode, levels, index + 1, callbacks);
            }

            // Single-level wildcard '+'
            if (node.PlusNode != null) {
                GetCallbacksRecursive(node.PlusNode, levels, index + 1, callbacks);
            }

            // Multi-level wildcard '#'
            if (node.HashNode != null) {
                callbacks.AddRange(node.HashNode.Callbacks);
            }
        }

        public bool HasSubscriptions(string topicFilter) {
            var levels = topicFilter.Split('/');
            return HasSubscriptionsRecursive(_root, levels, 0);
        }

        private bool HasSubscriptionsRecursive(TopicTrieNode node, string[] levels, int index) {
            if (node == null)
                return false;

            if (index == levels.Length) {
                return node.Callbacks.Count > 0;
            }

            var level = levels[index];

            switch (level) {
                case "+":
                    return node.PlusNode != null && HasSubscriptionsRecursive(node.PlusNode, levels, index + 1);
                case "#":
                    return node.HashNode != null && node.HashNode.Callbacks.Count > 0;
                default: {
                    if (node.Children.TryGetValue(level, out var childNode)) {
                        return HasSubscriptionsRecursive(childNode, levels, index + 1);
                    }

                    break;
                }
            }

            return false;
        }

        public void RemoveAllSubscriptions(string topicFilter) {
            var levels = topicFilter.Split('/');
            RemoveAllSubscriptionsRecursive(_root, levels, 0);
        }

        private bool RemoveAllSubscriptionsRecursive(TopicTrieNode node, string[] levels, int index) {
            if (node == null)
                return false;

            if (index == levels.Length) {
                node.Callbacks.Clear();
                node.Children.Clear();
                node.PlusNode = null;
                node.HashNode = null;
                return true;
            }

            var level = levels[index];
            TopicTrieNode childNode = null;

            switch (level) {
                case "+": {
                    childNode = node.PlusNode;
                    if (RemoveAllSubscriptionsRecursive(childNode, levels, index + 1))
                        node.PlusNode = null;

                    break;
                }
                case "#": {
                    childNode = node.HashNode;
                    if (RemoveAllSubscriptionsRecursive(childNode, levels, index + 1))
                        node.HashNode = null;
                    
                    break;
                }
                default: {
                    if (node.Children.TryGetValue(level, out childNode)) {
                        if (RemoveAllSubscriptionsRecursive(childNode, levels, index + 1)) {
                            node.Children.Remove(level);
                        }
                    }

                    break;
                }
            }

            return node.Callbacks.Count == 0 && node.Children.Count == 0 && node.PlusNode == null && node.HashNode == null;
        }
    }
}
