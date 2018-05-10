using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.Parser.InOut
{
    public class ParseHistory
    {
        public const string Reduce = " REDUCE";
        public const string Shift = " SHIFT";

        public int Step { get; set; }
        public string Stack { get; set; }
        public string Input { get; set; }
        public int NodeId { get; set; }
        public List<string> Reduced { get; private set; }
        public string Shifted { get; set; }
        public bool Recovered { get; set; }
        // not all paths of parsing are survivors :-)
        public bool Killed { get; set; }

        public ParseHistory()
        {
            Shifted = "";
            Reduced = new List<string>();
        }
        private bool isForked()
        {
            return ((Shifted==""?0:1)+Reduced.Count)>1;
        }
        public override string ToString()
        {
            return String.Join(Environment.NewLine,
                new string[]{
                "step: "+Step,
                (Killed?"<KIA>":"")+Stack,
                Input,
                NodeId + (isForked()?" %FORK":"")+Shifted + Reduced.Join("") + (Recovered ? " recovering" : "")}.Where(it => it != null));
        }

    }
}
