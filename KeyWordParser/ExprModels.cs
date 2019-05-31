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
            return new ExprEntry() { Parent = parent, Keyword = keyword, Operator=Operator.Nop, Value="" };
        }

        public override string ToString()
        {
            return $"{Keyword} {Operator} <{Value}>";
        }

    }


    public class ExprLogic : ExprEntryBase
    {
        public string Logic { get; set; }

        public List<ExprEntryBase> ExprEntries { get; set; }

        public override bool IsLogic => true;
        public override bool IsExpression => false;

        public ExprEntry ActiveExpr { get { return ExprEntries.Any() ? (ExprEntry)ExprEntries.Last(p => p is ExprEntry) : null;  } }

        public ExprEntryBase AddEntry(ExprEntryBase entry)
        {
            ExprEntries.Add(entry);
            return entry;
        }

        public ExprLogic AddGroup(ExprLogic group) {
            return (ExprLogic)AddEntry(group);
        }

        public static ExprLogic CreateGroup(ExprLogic parent, string logic = "and")
        {
           return new ExprLogic() { Parent = parent, Logic = logic, ExprEntries = new List<ExprEntryBase>() };
        }

        public override string ToString()
        {
            return $"{Logic} Items:{ExprEntries.Count}";
        }


    }



}
