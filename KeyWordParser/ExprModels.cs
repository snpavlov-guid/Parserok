using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KeyWordParser
{
    public enum Operator
    {
        Nop,
        Eq,
        Neq,
        Contains,
        Starts,
        Ends,
        Lt,
        LtEq,
        Gt,
        GtEq,
    }

    public enum Logic
    {
        None,
        And,
        Or,
        Not,
    }

    public abstract class ExprEntryBase
    {
        public ExprLogic Parent { get; set; }

        public Logic EntryLogic { get; set; }

        public Logic JoinLogic { get; set; }

        public Logic ChildEntryLogic { get; set; }

        public abstract bool IsLogic { get; }

        public abstract bool IsExpression { get; }

    }

    public class ExprEntry : ExprEntryBase
    {
        public string Keyword { get; set; }
        public Operator Operator { get; set; }
        public string Value { get; set; }

        public override bool IsLogic => false;
        public override bool IsExpression => true;

        public static ExprEntry Create(ExprLogic parent, string keyword = "")
        {
            return new ExprEntry() { Parent = parent, Keyword = keyword, EntryLogic = parent.ChildEntryLogic, Operator=Operator.Nop, Value="" };
        }

        public override string ToString()
        {
            var els = EntryLogic != Logic.None ? $"[{EntryLogic}]" : "";
            var jls = JoinLogic != Logic.None ? $"[{JoinLogic}]" : "";

            return $"{els} {Keyword} {Operator} <{Value}> {jls}";
        }

    }


    public class ExprLogic : ExprEntryBase
    {
        public List<ExprEntryBase> ExprEntries { get; set; }

        public override bool IsLogic => true;
        public override bool IsExpression => false;

        public ExprEntry ActiveExpr { get { return ExprEntries.Any() ? (ExprEntry)ExprEntries.Last(p => p is ExprEntry) : null;  } }

        public ExprEntryBase ActiveEntry { get { return ExprEntries.Any() ? ExprEntries.Last() : null; } }

        public ExprEntryBase AddEntry(ExprEntryBase entry)
        {
            ExprEntries.Add(entry);
            return entry;
        }

        public ExprLogic AddGroup(ExprLogic group) {
            return (ExprLogic)AddEntry(group);
        }

        public static ExprLogic CreateGroup(ExprLogic parent)
        {
            return new ExprLogic() {
                Parent = parent,
                EntryLogic = parent != null ? parent.ChildEntryLogic : Logic.None,
                ExprEntries = new List<ExprEntryBase>()
            };
        }

        public override string ToString()
        {
            var els = EntryLogic != Logic.None ? $"[{EntryLogic}]" : "";
            var jls = JoinLogic != Logic.None ? $"[{JoinLogic}]" : "";

            return $"{els} Count:{ExprEntries.Count} {jls}";
        }


    }



}
