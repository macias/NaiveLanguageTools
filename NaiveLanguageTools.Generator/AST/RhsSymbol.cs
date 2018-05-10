using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NaiveLanguageTools.Parser;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Generator.Builder;
using NaiveLanguageTools.Generator.Feed;

namespace NaiveLanguageTools.Generator.AST
{
    public interface ILabeledSymbol
    {
        // the name user gave locally to object (can be null)
        string ObjName { get; }
        string SymbolName { get; }
    }
    public class LabeledSymbol : ILabeledSymbol
    {
        // the name user gave locally to object (can be null)
        public string ObjName { get; private set; }
        private readonly string __symbolName;
        public string SymbolName { get { return __symbolName; } }
        
        public LabeledSymbol(string userLabel, string symbolName)
        {
            this.ObjName = userLabel;
            this.__symbolName = symbolName;

            if (ObjName != null && new Regex("^_+\\d+$").IsMatch(ObjName))
                throw ParseControlException.NewAndRun("\"" + ObjName + "\" is reserved name");
        }
        public void SetImplicitUserObjName(string userLabel)
        {
            this.ObjName = userLabel;
        }
        internal void ResetUserObjName()
        {
            ObjName = null;
        }

    }
    public class RhsSymbol  : LabeledSymbol,IRhsEntity
    {
        public int Count { get { return 1;}}
        public readonly SymbolPosition Coords;
        public bool SkipInitially { get; private set; }
        public readonly IEnumerable<string> TabooSymbols;
        public readonly bool IsMarked;
        public IEnumerable<IRhsEntity> Elements { get { return Enumerable.Empty<IRhsEntity>(); } }

        internal bool IsError { get { return SymbolName == Grammar.ErrorSymbol; } }

        // if this symbol covers several others, like in production (x:Int y:String)+  
        // there would be created a tuple for this, but original names (x,y) have to be preserved
        // such fragment is replace by some "list_x_y" rhs symbol bound with seed and append productions for this combo list
        // which has to know how to name individual lists ("x" and "y" here)
        public IEnumerable<AtomicSymbolInfo> CombinedSymbols { get; private set; }

        public override string ToString()
        {
            return (IsMarked ? "^" : "")
                + (ObjName != null ? ObjName + ":" : "")
                + SymbolName
                + (SkipInitially ? "-" : "")
                + String.Join(" ", TabooSymbols.Select(it => "#" + it));
        }
        public RhsSymbol(SymbolPosition coords, string userLabel, string symbolName, IEnumerable<string> tabooSymbols, bool marked, bool skip)
            : base(userLabel,symbolName)
        {
            this.Coords = coords;
            this.SkipInitially = skip;
            this.IsMarked = marked;
            this.TabooSymbols = (tabooSymbols ?? new string[] { }).ToList();
            this.CombinedSymbols = null;
        }
        public RhsSymbol(SymbolPosition coords, string userLabel, string symbolName, IEnumerable<string> tabooSymbols, bool marked)
            : this(coords,userLabel, symbolName, tabooSymbols, marked: marked, skip: false)
        {
        }
        public RhsSymbol(SymbolPosition coords, string userLabel, string symbolName)
            : this(coords,userLabel, symbolName, tabooSymbols:null, marked: false)
        {
        }

        internal IEnumerable<CodeBody> UnpackTuple(bool enabled)
        {
            // only count of combined symbols is important here
            int count = CombinedSymbols.Count();

            if (!enabled)
                return Enumerable.Range(1,count).Select(_ => new CodeBody().AddIdentifier(CodeWords.Null));
            else if (count == 1)
                return new[] { new CodeBody().AddWithIdentifier(this.ObjName) };
            else
                return Enumerable.Range(1, count).Select(i => new CodeBody().AddWithIdentifier(this.ObjName, ".", CodeWords.Item(i)));
        }

        internal RhsSymbol ShallowClone()
        {
            return Renamed(ObjName);
        }
        internal RhsSymbol Renamed(string newObjName)
        {
            return Renamed(newObjName, this.SymbolName);
        }
        internal RhsSymbol Renamed(string newObjName,string newSymbolName)
        {
            return new RhsSymbol(Coords, newObjName, newSymbolName, this.TabooSymbols, marked: this.IsMarked, skip: this.SkipInitially)
                {
                    CombinedSymbols = this.CombinedSymbols
                };
        }

        internal RhsSymbol SetSkip(bool skip)
        {
            SkipInitially = skip;
            return this;
        }

        internal void SetCombinedSymbols(IEnumerable<AtomicSymbolInfo> tupleInfo)
        {
            CombinedSymbols = tupleInfo.ToArray();
        }
        // the data the user code is expected to see
        internal IEnumerable<string> GetCodeArgumentTypes(Grammar grammar)
        {
            // if this is compound object, return atomic symbols
            if (CombinedSymbols != null)
                return CombinedSymbols.Select(it => it.TypeName);
            else // if not, regular one
                return new[] { grammar.GetTypeNameOfSymbol(this) };
        }
        internal IEnumerable<string> GetCodeArgumentNames()
        {
            // if this is compound object, return atomic symbols
            if (CombinedSymbols != null)
                return CombinedSymbols.Select(it => it.UserObjName);
            else // if not, regular one
                return new[] { ObjName };
        }
        public IEnumerable<RhsSymbol> GetSymbols()
        {
            yield return this;
        }

    }
}
