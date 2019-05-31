using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KeyWordParser
{
    public enum Expectation
    {
        Blank,
        NotBlank,
        Whitespace,
        OpenGroup,
        CloseGroup,
        Keyword,
        Operator,
        Value,
        JoinLogic,
        EntryLogic,

    }



    public delegate (bool, int, ExprLogic) GraphHandler(int pos, string source, ExprLogic exprLogic);

    public static class GraphMeta
    {
        public static readonly Dictionary<Operator, string> Operators = new Dictionary<Operator, string>()
        {
            { Operator.Eq, "=" },
            { Operator.Neq, "<>" },
            { Operator.Contains, ":" },
            { Operator.Starts, "^" },
            { Operator.Ends, "$" },
            { Operator.Lt, "<" },
            { Operator.LtEq, "<=" },
            { Operator.Gt, ">" },
            { Operator.GtEq, ">=" },
        };

        public static readonly Dictionary<Logic, List<string>> Logics = new Dictionary<Logic, List<string>>()
        {
            { Logic.And, new List<string>() { "AND", "&&", "&" } },
            { Logic.Or, new List<string>() { "OR", "||", "|" } },
        };

        public static readonly Dictionary<Logic, List<string>> Unaries = new Dictionary<Logic, List<string>>()
        {
            { Logic.Not, new List<string>() { "NOT", "!" } },
        };
    }


    public class GraphNode : IEnumerable

    {
        public GraphNode(string nodeName, params Expectation[] expects) {
            NodeName = nodeName;
            _nodeItems = expects.Select(p => new GraphNodeItem(p)).ToDictionary(p=> p.Expect);
        }

        protected string NodeName { get; }
        protected Dictionary<Expectation, GraphNodeItem> _nodeItems;

        public GraphNodeItem this[Expectation expect]
        {
            get { return _nodeItems[expect];  }
        }

        public IEnumerator GetEnumerator()
        {
            foreach(Expectation key in _nodeItems.Keys)
            {
                yield return _nodeItems[key];
            }
        }

    }

    public class GraphNodeItem
    {
        public GraphNodeItem(Expectation expect)
        {
            Expect = expect;
        }

        public Expectation Expect { get; }
        public GraphNode Next { get; protected set; }
        public GraphHandler Handler { get; protected set; }

        public Func<(GraphNode, GraphHandler)> Link
        {
            set { (Next, Handler) = value.Invoke(); }
        }



    }
}
