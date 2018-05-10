using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser.Symbols;

namespace NaiveLanguageTools.Generator.Symbols
{
    // all chunks of symbols which can happen "inside" productions for given symbol
    // cover set is not smaller than first set by definition
    // it contains non-terminals too (not only non-terminals) until computation of the other sets
    // after that non-terminals are removed
    // cover set is written for lookahead width = 1 only
    public sealed class CoverSets<SYMBOL_ENUM> 
        where SYMBOL_ENUM : struct
    {
        // we track not only what given symbol covers, but where from that data came from, example:
        // a -> B c;
        // c -> F;
        // "a" covers "B", "c", "F", two first ones come from "a", while "F" comes from "c"

        // for given symbol --> { symbol it covers + source of that cover (only the first source -- winnner -- counts ) }
        private DynamicDictionary<SYMBOL_ENUM, Dictionary<SymbolChunk<SYMBOL_ENUM>, SYMBOL_ENUM>> covers;
        public IEnumerable<KeyValuePair<SYMBOL_ENUM, Dictionary<SymbolChunk<SYMBOL_ENUM>, SYMBOL_ENUM>>> Entries { get { return covers; } }
        public bool HasNonTerminals { get; private set; }
        
        // value is the track of the sources -- if empty the given symbol is not recursive
        private Dictionary<SYMBOL_ENUM, SYMBOL_ENUM[]> recurDict;

        public int LookaheadWidth { get { return 1; } }

        public CoverSets()
        {
            HasNonTerminals = true;
            recurDict = null;
            covers = DynamicDictionary.CreateWithDefault<SYMBOL_ENUM,Dictionary<SymbolChunk<SYMBOL_ENUM>,SYMBOL_ENUM>>();
        }
        public void RemoveNonTerminals(IEnumerable<SYMBOL_ENUM> nonTerminals)
        {
            // because non-terminals are about to be removed, we have to keep the info
            // about recursive non-terminals
            recurDict = new Dictionary<SYMBOL_ENUM, SYMBOL_ENUM[]>();
            foreach (SYMBOL_ENUM non_term in nonTerminals)
                recurDict.Add(non_term, recursiveTrackBySet(non_term).ToArray());

            foreach (Dictionary<SymbolChunk<SYMBOL_ENUM>, SYMBOL_ENUM> set in covers.Values)
                foreach (SymbolChunk<SYMBOL_ENUM> key in set.Keys.ToArray())
                    if (nonTerminals.Contains(key.Symbols.Single()))
                        set.Remove(key);

            HasNonTerminals = false;
        }
        public bool IsRecursive(SYMBOL_ENUM nonTerm)
        {
            return RecursiveTrack(nonTerm).Any();
        }
        public IEnumerable<SYMBOL_ENUM> RecursiveTrack(SYMBOL_ENUM nonTerm)
        {
            if (HasNonTerminals)
                return recursiveTrackBySet(nonTerm);
            else
                return recurDict[nonTerm];
        }
        public SymbolChunkSet<SYMBOL_ENUM> this[SYMBOL_ENUM symbol]
        {
            get
            {
                return coverAsChunkSet(symbol);
            }
        }

        private SymbolChunkSet<SYMBOL_ENUM> coverAsChunkSet(SYMBOL_ENUM symbol)
        {
            return SymbolChunkSet.Create(covers[symbol].Keys);
        }

        private IEnumerable<SYMBOL_ENUM> recursiveTrackBySet(SYMBOL_ENUM nonTerm)
        {
            var track = new LinkedList<SYMBOL_ENUM>();
            SYMBOL_ENUM current = nonTerm;
            while (true)
            {
                SYMBOL_ENUM sym;
                if (!covers[nonTerm].TryGetValue(SymbolChunk.Create(current), out sym))
                {
                    if (current.Equals(nonTerm))
                        return track;
                    else
                        throw new NotImplementedException("Internal error");
                }
                track.AddFirst(sym);
                if (sym.Equals(nonTerm))
                    return track;
                
                current = sym;
            }
        }

        internal bool Add(SYMBOL_ENUM symbol, SymbolChunkSet<SYMBOL_ENUM> set, SYMBOL_ENUM source)
        {
            bool change = false;
            foreach (SymbolChunk<SYMBOL_ENUM> chunk in set.Chunks)
                if (!covers[symbol].ContainsKey(chunk))
                {
                    covers[symbol].Add(chunk, source);
                    change = true;
                }

            return change;
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }

        public string ToString(StringRep<SYMBOL_ENUM> symbolsRep)
        {
            return "COVERAGE INFO:" + Environment.NewLine
                  + "--------------" + Environment.NewLine
                  +"LHS symbol = symbols that are covered"
                + Environment.NewLine + Environment.NewLine
                + covers.Keys.Select(it => symbolsRep.Get(it) + " = " + coverAsChunkSet(it).ToString(symbolsRep, verboseMode: false)).Join(Environment.NewLine);
        }

    }
}
