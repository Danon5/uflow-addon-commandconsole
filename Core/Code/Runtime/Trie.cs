using System.Collections.Generic;

namespace UFlow.Addon.CommandConsole.Runtime
{
    public sealed class Trie
    {
        private readonly TrieNode m_rootNode;

        public Trie(List<string> words)
        {
            m_rootNode = new TrieNode(false);
            foreach (string word in words)
            {
                //Debug.Log($"adding {word}");
                m_rootNode.AddToChildren(word);
            }
            
            //Debug.Log($"debugging children: ");
            //m_rootNode.DFSDebugChildren();
        }

        public int SuggestNonAlloc(string prefix, in string[] words, bool includePrefixIfFullWord = false)
        {
            if (prefix == "")
                return 0;
            
            TrieNode currNode = m_rootNode;
            int wordIndex = 0;
            for (int i = 0; i < prefix.Length; i++)
            {
                if (!currNode.children.TryGetValue(prefix[i], out currNode))
                    return wordIndex;
            }

            if (currNode.isWordEnd && includePrefixIfFullWord)
            {
                words[wordIndex] = prefix;
                wordIndex = 1;
            }

            foreach (KeyValuePair<char, TrieNode> nodeData in currNode.children)
                wordIndex = BuildWordsFromChildren(nodeData.Value, prefix + nodeData.Key, words, wordIndex);

            return wordIndex;
        }
        
        public List<string> Suggest(string prefix, bool includePrefixIfFullWord = false)
        {
            List<string> words = new List<string>();
            if (prefix == "")
                return words;
            
            TrieNode currNode = m_rootNode;
            for (int i = 0; i < prefix.Length; i++)
            {
                if (!currNode.children.TryGetValue(prefix[i], out currNode))
                    return words;
            }
            
            if (currNode.isWordEnd && includePrefixIfFullWord)
                words.Add(prefix);

            foreach (KeyValuePair<char, TrieNode> nodeData in currNode.children)
            {
                words.AddRange(BuildWordsFromChildren(nodeData.Value, prefix + nodeData.Key));
            }
            

            return words;
        }

        public List<string> GetAllWords() => BuildWordsFromChildren(m_rootNode, "");

        private List<string> BuildWordsFromChildren(in TrieNode node, string prefix)
        {
            List<string> words = new List<string>();
            BuildWordsFromChildrenHelper(node, prefix, ref words);
            return words;
        }
        
        private void BuildWordsFromChildrenHelper(in TrieNode node, string curr, ref List<string> words)
        {
            if (node.isWordEnd)
            {
                words.Add(curr);
                //Debug.Log($"adding {curr} to list");
            }

            foreach (KeyValuePair<char, TrieNode> child in node.children)
            {
                //Debug.Log($"recursively building on {child.Key}");
                BuildWordsFromChildrenHelper(child.Value, curr + child.Key, ref words);
            }
        }
        
        
        private int BuildWordsFromChildren(in TrieNode node, string curr, in string[] words, int wordIndex)
        {
            if (wordIndex >= words.Length)
                return wordIndex;

            if (node.isWordEnd)
            {
                words[wordIndex] = curr;
                wordIndex++;
            }

            foreach (KeyValuePair<char, TrieNode> child in node.children)
                wordIndex = BuildWordsFromChildren(child.Value, curr + child.Key, words, wordIndex);
            
            return wordIndex;
        }
    }
}