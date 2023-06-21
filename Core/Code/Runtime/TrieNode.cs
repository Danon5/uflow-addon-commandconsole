using System.Collections.Generic;
using UnityEngine;

namespace UFlow.Addon.CommandConsole.Runtime
{
    public class TrieNode
    {
        public readonly Dictionary<char, TrieNode> children;
        public bool isWordEnd;

        public TrieNode(bool isWordEnd)
        {
            this.isWordEnd = isWordEnd;
            children = new Dictionary<char, TrieNode>();
        }

        public void AddToChildren(string word)
        {
            if (word == "")
                return;
            
            bool atLastLetter = word.Length == 1;
            
            TrieNode child;
            char c = word[0];
            if (!children.TryGetValue(c, out child))
            {
                //Debug.Log($"child did not exist, creating one for {c}. isWordEnd: {atLastLetter}");
                child = new TrieNode(atLastLetter);
                children.Add(c, child);
            }
            else if (atLastLetter)
            {
                //Debug.Log($"setting child {c} to be word end");
                child.isWordEnd = true;
            }

            child.AddToChildren(word.Substring(1));
        }
        
        // DPS == Depth First Search
        public void DFSDebugChildren()
        {
            foreach (KeyValuePair<char, TrieNode> data in children)
            {
                Debug.Log($"enter node {data.Key}, isWordEnd: {data.Value.isWordEnd}");
                data.Value.DFSDebugChildren();
                Debug.Log($"return node  {data.Key}");
            }
        }
    }
}
