using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using EFSecondLevelCache.Core.Contracts;

namespace EFSecondLevelCache.Core
{
    /// <summary>
    /// Ref. https://github.com/dotnet/corefx/ -> src/System.Linq.Expressions/src/System/Linq/Expressions/DebugViewWriter.cs
    /// </summary>
    public class EFQueryExpressionVisitor : ExpressionVisitor
    {
        private const int MaxColumn = 120;

        private const int Tab = 4;

        private readonly TextWriter _out;
        private readonly Stack<int> _stack = new Stack<int>();
        private readonly HashSet<string> _types = new HashSet<string>();
        private int _column;

        private Flow _flow;

        // Associate every unique anonymous LabelTarget in the tree with an integer.
        // The id is used to create a name for the anonymous LabelTarget.
        //
        private Dictionary<LabelTarget, int> _labelIds;

        // Associate every unique anonymous LambdaExpression in the tree with an integer.
        // The id is used to create a name for the anonymous lambda.
        //
        private Dictionary<LambdaExpression, int> _lambdaIds;

        // All the unique lambda expressions in the ET, will be used for displaying all
        // the lambda definitions.
        private Queue<LambdaExpression> _lambdas;
        // Associate every unique anonymous parameter or variable in the tree with an integer.
        // The id is used to create a name for the anonymous parameter or variable.
        //
        private Dictionary<ParameterExpression, int> _paramIds;
        private EFQueryExpressionVisitor(TextWriter file)
        {
            _out = file;
        }

        [Flags]
        private enum Flow
        {
            None,
            Space,
            NewLine,

            Break = 0x8000      // newline if column > MaxColumn
        }

        private int Base => _stack.Count > 0 ? _stack.Peek() : 0;

        private int Delta { get; set; }

        private int Depth => Base + Delta;

        /// <summary>
        /// Write out the given AST
        /// </summary>
        public static EFQueryDebugView GetDebugView(Expression node)
        {
            using (var writer = new StringWriter(CultureInfo.CurrentCulture))
            {
                var efQueryExpressionVisitor = new EFQueryExpressionVisitor(writer);
                efQueryExpressionVisitor.writeTo(node);
                var types = efQueryExpressionVisitor._types;
                return new EFQueryDebugView { DebugView = writer.ToString(), Types = types };
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.ArrayIndex)
            {
                parenthesizedVisit(node, node.Left);
                Out("[");
                Visit(node.Right);
                Out("]");
            }
            else
            {
                bool parenthesizeLeft = needsParentheses(node, node.Left);
                bool parenthesizeRight = needsParentheses(node, node.Right);

                string op;
                bool isChecked = false;
                Flow beforeOp = Flow.Space;
                switch (node.NodeType)
                {
                    case ExpressionType.Assign: op = "="; break;
                    case ExpressionType.Equal: op = "=="; break;
                    case ExpressionType.NotEqual: op = "!="; break;
                    case ExpressionType.AndAlso: op = "&&"; beforeOp = Flow.Break | Flow.Space; break;
                    case ExpressionType.OrElse: op = "||"; beforeOp = Flow.Break | Flow.Space; break;
                    case ExpressionType.GreaterThan: op = ">"; break;
                    case ExpressionType.LessThan: op = "<"; break;
                    case ExpressionType.GreaterThanOrEqual: op = ">="; break;
                    case ExpressionType.LessThanOrEqual: op = "<="; break;
                    case ExpressionType.Add: op = "+"; break;
                    case ExpressionType.AddAssign: op = "+="; break;
                    case ExpressionType.AddAssignChecked: op = "+="; isChecked = true; break;
                    case ExpressionType.AddChecked: op = "+"; isChecked = true; break;
                    case ExpressionType.Subtract: op = "-"; break;
                    case ExpressionType.SubtractAssign: op = "-="; break;
                    case ExpressionType.SubtractAssignChecked: op = "-="; isChecked = true; break;
                    case ExpressionType.SubtractChecked: op = "-"; isChecked = true; break;
                    case ExpressionType.Divide: op = "/"; break;
                    case ExpressionType.DivideAssign: op = "/="; break;
                    case ExpressionType.Modulo: op = "%"; break;
                    case ExpressionType.ModuloAssign: op = "%="; break;
                    case ExpressionType.Multiply: op = "*"; break;
                    case ExpressionType.MultiplyAssign: op = "*="; break;
                    case ExpressionType.MultiplyAssignChecked: op = "*="; isChecked = true; break;
                    case ExpressionType.MultiplyChecked: op = "*"; isChecked = true; break;
                    case ExpressionType.LeftShift: op = "<<"; break;
                    case ExpressionType.LeftShiftAssign: op = "<<="; break;
                    case ExpressionType.RightShift: op = ">>"; break;
                    case ExpressionType.RightShiftAssign: op = ">>="; break;
                    case ExpressionType.And: op = "&"; break;
                    case ExpressionType.AndAssign: op = "&="; break;
                    case ExpressionType.Or: op = "|"; break;
                    case ExpressionType.OrAssign: op = "|="; break;
                    case ExpressionType.ExclusiveOr: op = "^"; break;
                    case ExpressionType.ExclusiveOrAssign: op = "^="; break;
                    case ExpressionType.Power: op = "**"; break;
                    case ExpressionType.PowerAssign: op = "**="; break;
                    case ExpressionType.Coalesce: op = "??"; break;

                    default:
                        throw new InvalidOperationException();
                }

                if (parenthesizeLeft)
                {
                    Out("(", Flow.None);
                }

                Visit(node.Left);
                if (parenthesizeLeft)
                {
                    Out(Flow.None, ")", Flow.Break);
                }

                // prepend # to the operator to represent checked op
                if (isChecked)
                {
                    op = string.Format(
                            CultureInfo.CurrentCulture,
                            "#{0}",
                            op
                    );
                }
                Out(beforeOp, op, Flow.Space | Flow.Break);

                if (parenthesizeRight)
                {
                    Out("(", Flow.None);
                }
                Visit(node.Right);
                if (parenthesizeRight)
                {
                    Out(Flow.None, ")", Flow.Break);
                }
            }
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitBlock(BlockExpression node)
        {
            Out(".Block");

            Out(string.Format(CultureInfo.CurrentCulture, "«{0}»", node.Type));
            addType(node.Type);

            visitDeclarations(node.Variables);
            Out(" ");
            // Use ; to separate expressions in the block
            visitExpressions('{', ';', node.Expressions);

            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override CatchBlock VisitCatchBlock(CatchBlock node)
        {
            Out(Flow.NewLine, $"}} .Catch ({node.Test}");
            if (node.Variable != null)
            {
                Out(Flow.Space, "");
                VisitParameter(node.Variable);
            }
            if (node.Filter != null)
            {
                Out(") .If (", Flow.Break);
                Visit(node.Filter);
            }
            Out(") {", Flow.NewLine);
            indent();
            Visit(node.Body);
            dedent();
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitConditional(ConditionalExpression node)
        {
            if (isSimpleExpression(node.Test))
            {
                Out(".If (");
                Visit(node.Test);
                Out(") {", Flow.NewLine);
            }
            else
            {
                Out(".If (", Flow.NewLine);
                indent();
                Visit(node.Test);
                dedent();
                Out(Flow.NewLine, ") {", Flow.NewLine);
            }
            indent();
            Visit(node.IfTrue);
            dedent();
            Out(Flow.NewLine, "} .Else {", Flow.NewLine);
            indent();
            Visit(node.IfFalse);
            dedent();
            Out(Flow.NewLine, "}");
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitConstant(ConstantExpression node)
        {
            object value = node.Value;

            if (value == null)
            {
                Out("null");
            }
            else if ((value is string) && node.Type == typeof(string))
            {
                Out(string.Format(
                    CultureInfo.CurrentCulture,
                    "\"{0}\"",
                    value));
            }
            else if ((value is char) && node.Type == typeof(char))
            {
                Out(string.Format(
                    CultureInfo.CurrentCulture,
                    "'{0}'",
                    value));
            }
            else if ((value is int) && node.Type == typeof(int)
              || (value is bool) && node.Type == typeof(bool))
            {
                Out(value.ToString());
            }
            else
            {
                string suffix = getConstantValueSuffix(node.Type);
                if (suffix != null)
                {
                    Out(value.ToString());
                    Out(suffix);
                }
                else
                {
                    Out(string.Format(
                        CultureInfo.CurrentCulture,
                        ".Constant<{0}>({1})",
                        node.Type,
                        value));
                    addType(node.Type);
                }
            }
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitDebugInfo(DebugInfoExpression node)
        {
            Out(string.Format(
                CultureInfo.CurrentCulture,
                ".DebugInfo({0}: {1}, {2} - {3}, {4})",
                node.Document.FileName,
                node.StartLine,
                node.StartColumn,
                node.EndLine,
                node.EndColumn)
            );
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitDefault(DefaultExpression node)
        {
            Out($".Default({node.Type})");
            addType(node.Type);
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override ElementInit VisitElementInit(ElementInit node)
        {
            if (node.Arguments.Count == 1)
            {
                Visit(node.Arguments[0]);
            }
            else
            {
                visitExpressions('{', node.Arguments);
            }
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitExtension(Expression node)
        {
            Out(string.Format(CultureInfo.CurrentCulture, ".Extension<{0}>", node.GetType()));

            if (node.CanReduce)
            {
                Out(Flow.Space, "{", Flow.NewLine);
                indent();
                Visit(node.Reduce());
                dedent();
                Out(Flow.NewLine, "}");
            }

            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitGoto(GotoExpression node)
        {
            Out($".{node.Kind}", Flow.Space);
            Out(getLabelTargetName(node.Target), Flow.Space);
            Out("{", Flow.Space);
            Visit(node.Value);
            Out(Flow.Space, "}");
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitIndex(IndexExpression node)
        {
            if (node.Indexer != null)
            {
                outMember(node, node.Object, node.Indexer);
            }
            else
            {
                parenthesizedVisit(node, node.Object);
            }

            visitExpressions('[', node.Arguments);
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitInvocation(InvocationExpression node)
        {
            Out(".Invoke ");
            parenthesizedVisit(node, node.Expression);
            visitExpressions('(', node.Arguments);
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitLabel(LabelExpression node)
        {
            Out(".Label", Flow.NewLine);
            indent();
            Visit(node.DefaultValue);
            dedent();
            newLine();
            dumpLabel(node.Target);
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            Out(
                string.Format(CultureInfo.CurrentCulture,
                    "{0} {1}<{2}>",
                    ".Lambda",
                    getLambdaName(node),
                    node.Type
                )
            );
            addType(node.Type);

            if (_lambdas == null)
            {
                _lambdas = new Queue<LambdaExpression>();
            }

            // N^2 performance, for keeping the order of the lambdas.
            if (!_lambdas.Contains(node))
            {
                _lambdas.Enqueue(node);
            }

            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitListInit(ListInitExpression node)
        {
            Visit(node.NewExpression);
            visitExpressions('{', ',', node.Initializers, e => VisitElementInit(e));
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitLoop(LoopExpression node)
        {
            Out(".Loop", Flow.Space);
            if (node.ContinueLabel != null)
            {
                dumpLabel(node.ContinueLabel);
            }
            Out(" {", Flow.NewLine);
            indent();
            Visit(node.Body);
            dedent();
            Out(Flow.NewLine, "}");
            if (node.BreakLabel != null)
            {
                Out("", Flow.NewLine);
                dumpLabel(node.BreakLabel);
            }
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            outMember(node, node.Expression, node.Member);
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="assignment"></param>
        /// <returns></returns>
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
        {
            Out(assignment.Member.Name);
            Out(Flow.Space, "=", Flow.Space);
            Visit(assignment.Expression);
            return assignment;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            Visit(node.NewExpression);
            visitExpressions('{', ',', node.Bindings, e => VisitMemberBinding(e));
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="binding"></param>
        /// <returns></returns>
        protected override MemberListBinding VisitMemberListBinding(MemberListBinding binding)
        {
            Out(binding.Member.Name);
            Out(Flow.Space, "=", Flow.Space);
            visitExpressions('{', ',', binding.Initializers, e => VisitElementInit(e));
            return binding;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="binding"></param>
        /// <returns></returns>
        protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
        {
            Out(binding.Member.Name);
            Out(Flow.Space, "=", Flow.Space);
            visitExpressions('{', ',', binding.Bindings, e => VisitMemberBinding(e));
            return binding;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Out(".Call ");
            if (node.Object != null)
            {
                parenthesizedVisit(node, node.Object);
            }
            else if (node.Method.DeclaringType != null)
            {
                Out(node.Method.DeclaringType.ToString());
            }
            else
            {
                Out("<UnknownType>");
            }
            Out(".");
            Out(node.Method.Name);
            visitExpressions('(', node.Arguments);
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitNew(NewExpression node)
        {
            Out($".New {node.Type}");
            addType(node.Type);
            visitExpressions('(', node.Arguments);
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            if (node.NodeType == ExpressionType.NewArrayBounds)
            {
                // .NewArray MyType[expr1, expr2]
                Out($".NewArray {node.Type.GetElementType()}");
                addType(node.Type);
                visitExpressions('[', node.Expressions);
            }
            else
            {
                // .NewArray MyType {expr1, expr2}
                Out($".NewArray {node.Type}", Flow.Space);
                addType(node.Type);
                visitExpressions('{', node.Expressions);
            }
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            // Have '$' for the DebugView of ParameterExpressions
            Out("$");
            if (string.IsNullOrEmpty(node.Name))
            {
                // If no name if provided, generate a name as $var1, $var2.
                // No guarantee for not having name conflicts with user provided variable names.
                //
                int id = getParamId(node);
                Out($"var{id}");
            }
            else
            {
                Out(getDisplayName(node.Name));
            }
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
        {
            Out(".RuntimeVariables");
            visitExpressions('(', node.Variables);
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitSwitch(SwitchExpression node)
        {
            Out(".Switch ");
            Out("(");
            Visit(node.SwitchValue);
            Out(") {", Flow.NewLine);
            Visit(node.Cases, VisitSwitchCase);
            if (node.DefaultBody != null)
            {
                Out(".Default:", Flow.NewLine);
                indent(); indent();
                Visit(node.DefaultBody);
                dedent(); dedent();
                newLine();
            }
            Out("}");
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override SwitchCase VisitSwitchCase(SwitchCase node)
        {
            foreach (var test in node.TestValues)
            {
                Out(".Case (");
                Visit(test);
                Out("):", Flow.NewLine);
            }
            indent(); indent();
            Visit(node.Body);
            dedent(); dedent();
            newLine();
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitTry(TryExpression node)
        {
            Out(".Try {", Flow.NewLine);
            indent();
            Visit(node.Body);
            dedent();
            Visit(node.Handlers, VisitCatchBlock);
            if (node.Finally != null)
            {
                Out(Flow.NewLine, "} .Finally {", Flow.NewLine);
                indent();
                Visit(node.Finally);
                dedent();
            }
            else if (node.Fault != null)
            {
                Out(Flow.NewLine, "} .Fault {", Flow.NewLine);
                indent();
                Visit(node.Fault);
                dedent();
            }

            Out(Flow.NewLine, "}");
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            parenthesizedVisit(node, node.Expression);
            switch (node.NodeType)
            {
                case ExpressionType.TypeIs:
                    Out(Flow.Space, ".Is", Flow.Space);
                    break;
                case ExpressionType.TypeEqual:
                    Out(Flow.Space, ".TypeEqual", Flow.Space);
                    break;
            }
            Out(node.TypeOperand.ToString());
            return node;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitUnary(UnaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Convert:
                    Out($"({node.Type})");
                    addType(node.Type);
                    break;
                case ExpressionType.ConvertChecked:
                    Out($"#({node.Type})");
                    addType(node.Type);
                    break;
                case ExpressionType.TypeAs:
                    break;
                case ExpressionType.Not:
                    Out(node.Type == typeof(bool) ? "!" : "~");
                    break;
                case ExpressionType.OnesComplement:
                    Out("~");
                    break;
                case ExpressionType.Negate:
                    Out("-");
                    break;
                case ExpressionType.NegateChecked:
                    Out("#-");
                    break;
                case ExpressionType.UnaryPlus:
                    Out("+");
                    break;
                case ExpressionType.ArrayLength:
                    break;
                case ExpressionType.Quote:
                    Out("'");
                    break;
                case ExpressionType.Throw:
                    if (node.Operand == null)
                    {
                        Out(".Rethrow");
                    }
                    else
                    {
                        Out(".Throw", Flow.Space);
                    }
                    break;
                case ExpressionType.IsFalse:
                    Out(".IsFalse");
                    break;
                case ExpressionType.IsTrue:
                    Out(".IsTrue");
                    break;
                case ExpressionType.Decrement:
                    Out(".Decrement");
                    break;
                case ExpressionType.Increment:
                    Out(".Increment");
                    break;
                case ExpressionType.PreDecrementAssign:
                    Out("--");
                    break;
                case ExpressionType.PreIncrementAssign:
                    Out("++");
                    break;
                case ExpressionType.Unbox:
                    Out(".Unbox");
                    break;
            }

            parenthesizedVisit(node, node.Operand);

            switch (node.NodeType)
            {
                case ExpressionType.TypeAs:
                    Out(Flow.Space, ".As", Flow.Space | Flow.Break);
                    Out(node.Type.ToString());
                    addType(node.Type);
                    break;

                case ExpressionType.ArrayLength:
                    Out(".Length");
                    break;

                case ExpressionType.PostDecrementAssign:
                    Out("--");
                    break;

                case ExpressionType.PostIncrementAssign:
                    Out("++");
                    break;
            }
            return node;
        }

        /// <summary>
        /// Return true if the input string contains any whitespace character.
        /// Otherwise false.
        /// </summary>
        private static bool containsWhiteSpace(string name)
        {
            foreach (char c in name)
            {
                if (char.IsWhiteSpace(c))
                {
                    return true;
                }
            }
            return false;
        }

        private static string getConstantValueSuffix(Type type)
        {
            if (type == typeof(uint))
            {
                return "U";
            }
            if (type == typeof(long))
            {
                return "L";
            }
            if (type == typeof(ulong))
            {
                return "UL";
            }
            if (type == typeof(double))
            {
                return "D";
            }
            if (type == typeof(float))
            {
                return "F";
            }
            if (type == typeof(decimal))
            {
                return "M";
            }
            return null;
        }

        private static string getDisplayName(string name)
        {
            if (containsWhiteSpace(name))
            {
                // if name has whitespace in it, quote it
                return quoteName(name);
            }
            else
            {
                return name;
            }
        }

        private static int getId<T>(T e, ref Dictionary<T, int> ids)
        {
            if (ids == null)
            {
                ids = new Dictionary<T, int> { { e, 1 } };
                return 1;
            }
            else
            {
                int id;
                if (!ids.TryGetValue(e, out id))
                {
                    // e is met the first time
                    id = ids.Count + 1;
                    ids.Add(e, id);
                }
                return id;
            }
        }

        // the greater the higher

        private static int getOperatorPrecedence(Expression node)
        {
            // Roughly matches C# operator precedence, with some additional
            // operators. Also things which are not binary/unary expressions,
            // such as conditional and type testing, don't use this mechanism.
            switch (node.NodeType)
            {
                // Assignment
                case ExpressionType.Assign:
                case ExpressionType.ExclusiveOrAssign:
                case ExpressionType.AddAssign:
                case ExpressionType.AddAssignChecked:
                case ExpressionType.SubtractAssign:
                case ExpressionType.SubtractAssignChecked:
                case ExpressionType.DivideAssign:
                case ExpressionType.ModuloAssign:
                case ExpressionType.MultiplyAssign:
                case ExpressionType.MultiplyAssignChecked:
                case ExpressionType.LeftShiftAssign:
                case ExpressionType.RightShiftAssign:
                case ExpressionType.AndAssign:
                case ExpressionType.OrAssign:
                case ExpressionType.PowerAssign:
                case ExpressionType.Coalesce:
                    return 1;

                // Conditional (?:) would go here

                // Conditional OR
                case ExpressionType.OrElse:
                    return 2;

                // Conditional AND
                case ExpressionType.AndAlso:
                    return 3;

                // Logical OR
                case ExpressionType.Or:
                    return 4;

                // Logical XOR
                case ExpressionType.ExclusiveOr:
                    return 5;

                // Logical AND
                case ExpressionType.And:
                    return 6;

                // Equality
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    return 7;

                // Relational, type testing
                case ExpressionType.GreaterThan:
                case ExpressionType.LessThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.TypeAs:
                case ExpressionType.TypeIs:
                case ExpressionType.TypeEqual:
                    return 8;

                // Shift
                case ExpressionType.LeftShift:
                case ExpressionType.RightShift:
                    return 9;

                // Additive
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return 10;

                // Multiplicative
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return 11;

                // Unary
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.UnaryPlus:
                case ExpressionType.Not:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.PreIncrementAssign:
                case ExpressionType.PreDecrementAssign:
                case ExpressionType.OnesComplement:
                case ExpressionType.Increment:
                case ExpressionType.Decrement:
                case ExpressionType.IsTrue:
                case ExpressionType.IsFalse:
                case ExpressionType.Unbox:
                case ExpressionType.Throw:
                    return 12;

                // Power, which is not in C#
                // But VB/Python/Ruby put it here, above unary.
                case ExpressionType.Power:
                    return 13;

                // Primary, which includes all other node types:
                //   member access, calls, indexing, new.
                case ExpressionType.PostIncrementAssign:
                case ExpressionType.PostDecrementAssign:
                default:
                    return 14;

                // These aren't expressions, so never need parentheses:
                //   constants, variables
                case ExpressionType.Constant:
                case ExpressionType.Parameter:
                    return 15;
            }
        }

        private static bool isSimpleExpression(Expression node)
        {
            var binary = node as BinaryExpression;
            if (binary != null)
            {
                return !(binary.Left is BinaryExpression || binary.Right is BinaryExpression);
            }

            return false;
        }


        private static bool needsParentheses(Expression parent, Expression child)
        {
            Debug.Assert(parent != null);
            if (child == null)
            {
                return false;
            }

            // Some nodes always have parentheses because of how they are
            // displayed, for example: ".Unbox(obj.Foo)"
            switch (parent.NodeType)
            {
                case ExpressionType.Increment:
                case ExpressionType.Decrement:
                case ExpressionType.IsTrue:
                case ExpressionType.IsFalse:
                case ExpressionType.Unbox:
                    return true;
            }

            int childOpPrec = getOperatorPrecedence(child);
            int parentOpPrec = getOperatorPrecedence(parent);

            if (childOpPrec == parentOpPrec)
            {
                // When parent op and child op has the same precedence,
                // we want to be a little conservative to have more clarity.
                // Parentheses are not needed if
                // 1) Both ops are &&, ||, &, |, or ^, all of them are the only
                // op that has the precedence.
                // 2) Parent op is + or *, e.g. x + (y - z) can be simplified to
                // x + y - z.
                // 3) Parent op is -, / or %, and the child is the left operand.
                // In this case, if left and right operand are the same, we don't
                // remove parenthesis, e.g. (x + y) - (x + y)
                //
                switch (parent.NodeType)
                {
                    case ExpressionType.AndAlso:
                    case ExpressionType.OrElse:
                    case ExpressionType.And:
                    case ExpressionType.Or:
                    case ExpressionType.ExclusiveOr:
                        // Since these ops are the only ones on their precedence,
                        // the child op must be the same.
                        Debug.Assert(child.NodeType == parent.NodeType);
                        // We remove the parenthesis, e.g. x && y && z
                        return false;
                    case ExpressionType.Add:
                    case ExpressionType.AddChecked:
                    case ExpressionType.Multiply:
                    case ExpressionType.MultiplyChecked:
                        return false;
                    case ExpressionType.Subtract:
                    case ExpressionType.SubtractChecked:
                    case ExpressionType.Divide:
                    case ExpressionType.Modulo:
                        BinaryExpression binary = parent as BinaryExpression;
                        Debug.Assert(binary != null);
                        // Need to have parenthesis for the right operand.
                        return child == binary.Right;
                }
                return true;
            }

            // Special case: negate of a constant needs parentheses, to
            // disambiguate it from a negative constant.
            if (child.NodeType == ExpressionType.Constant &&
                (parent.NodeType == ExpressionType.Negate || parent.NodeType == ExpressionType.NegateChecked))
            {
                return true;
            }

            // If the parent op has higher precedence, need parentheses for the child.
            return childOpPrec < parentOpPrec;
        }

        private static string quoteName(string name)
        {
            return string.Format(CultureInfo.CurrentCulture, "'{0}'", name);
        }
        private Flow checkBreak(Flow flow)
        {
            if ((flow & Flow.Break) != 0)
            {
                if (_column > (MaxColumn + Depth))
                {
                    flow = Flow.NewLine;
                }
                else
                {
                    flow &= ~Flow.Break;
                }
            }
            return flow;
        }

        private void dedent()
        {
            Delta -= Tab;
        }

        private void dumpLabel(LabelTarget target)
        {
            Out(string.Format(CultureInfo.CurrentCulture, ".LabelTarget {0}:", getLabelTargetName(target)));
        }

        private Flow getFlow(Flow flow)
        {
            var last = checkBreak(_flow);
            flow = checkBreak(flow);

            // Get the biggest flow that is requested None < Space < NewLine
            return (Flow)Math.Max((int)last, (int)flow);
        }

        private int getLabelTargetId(LabelTarget target)
        {
            Debug.Assert(string.IsNullOrEmpty(target.Name));
            return getId(target, ref _labelIds);
        }

        private string getLabelTargetName(LabelTarget target)
        {
            if (string.IsNullOrEmpty(target.Name))
            {
                // Create the label target name as #Label1, #Label2, etc.
                return string.Format(CultureInfo.CurrentCulture, "#Label{0}", getLabelTargetId(target));
            }
            else
            {
                return getDisplayName(target.Name);
            }
        }

        private int getLambdaId(LambdaExpression le)
        {
            Debug.Assert(string.IsNullOrEmpty(le.Name));
            return getId(le, ref _lambdaIds);
        }

        private string getLambdaName(LambdaExpression lambda)
        {
            if (string.IsNullOrEmpty(lambda.Name))
            {
                return $"#Lambda{getLambdaId(lambda)}";
            }
            return getDisplayName(lambda.Name);
        }

        private int getParamId(ParameterExpression p)
        {
            Debug.Assert(string.IsNullOrEmpty(p.Name));
            return getId(p, ref _paramIds);
        }

        private void indent()
        {
            Delta += Tab;
        }
        private void newLine()
        {
            _flow = Flow.NewLine;
        }
        private void Out(string s)
        {
            Out(Flow.None, s, Flow.None);
        }

        private void Out(Flow before, string s)
        {
            Out(before, s, Flow.None);
        }

        private void Out(string s, Flow after)
        {
            Out(Flow.None, s, after);
        }

        private void Out(Flow before, string s, Flow after)
        {
            switch (getFlow(before))
            {
                case Flow.None:
                    break;
                case Flow.Space:
                    write(" ");
                    break;
                case Flow.NewLine:
                    writeLine();
                    write(new string(' ', Depth));
                    break;
            }
            write(s);
            _flow = after;
        }

        // Prints ".instanceField" or "declaringType.staticField"
        private void outMember(Expression node, Expression instance, MemberInfo member)
        {
            if (instance != null)
            {
                parenthesizedVisit(node, instance);
                Out($".{member.Name}");
            }
            else
            {
                // For static members, include the type name
                Out($"{member.DeclaringType}.{member.Name}");
            }
        }

        private void parenthesizedVisit(Expression parent, Expression nodeToVisit)
        {
            if (needsParentheses(parent, nodeToVisit))
            {
                Out("(");
                Visit(nodeToVisit);
                Out(")");
            }
            else
            {
                Visit(nodeToVisit);
            }
        }

        private void visitDeclarations(IList<ParameterExpression> expressions)
        {
            visitExpressions('(', ',', expressions, variable =>
            {
                Out(variable.Type.ToString());
                if (variable.IsByRef)
                {
                    Out("&");
                }
                Out(" ");
                VisitParameter(variable);
            });
        }

        private void visitExpressions<T>(char open, IList<T> expressions) where T : Expression
        {
            visitExpressions<T>(open, ',', expressions);
        }

        private void visitExpressions<T>(char open, char separator, IList<T> expressions) where T : Expression
        {
            visitExpressions(open, separator, expressions, e => Visit(e));
        }

        private void visitExpressions<T>(char open, char separator, IList<T> expressions, Action<T> visit)
        {
            Out(open.ToString());

            if (expressions != null)
            {
                indent();
                bool isFirst = true;
                foreach (T e in expressions)
                {
                    if (isFirst)
                    {
                        if (open == '{' || expressions.Count > 1)
                        {
                            newLine();
                        }
                        isFirst = false;
                    }
                    else
                    {
                        Out(separator.ToString(), Flow.NewLine);
                    }
                    visit(e);
                }
                dedent();
            }

            char close;
            switch (open)
            {
                case '(': close = ')'; break;
                case '{': close = '}'; break;
                case '[': close = ']'; break;
                case '<': close = '>'; break;
                default: throw new InvalidOperationException();
            }

            if (open == '{')
            {
                newLine();
            }
            Out(close.ToString(), Flow.Break);
        }

        private void write(string s)
        {
            _out.Write(s);
            _column += s.Length;
        }

        private void writeLambda(LambdaExpression lambda)
        {
            Out(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ".Lambda {0}<{1}>",
                    getLambdaName(lambda),
                    lambda.Type)
            );

            visitDeclarations(lambda.Parameters);

            Out(Flow.Space, "{", Flow.NewLine);
            indent();
            Visit(lambda.Body);
            dedent();
            Out(Flow.NewLine, "}");
            Debug.Assert(_stack.Count == 0);
        }

        private void writeLine()
        {
            _out.WriteLine();
            _column = 0;
        }

        private void writeTo(Expression node)
        {
            var lambda = node as LambdaExpression;
            if (lambda != null)
            {
                writeLambda(lambda);
            }
            else
            {
                Visit(node);
                Debug.Assert(_stack.Count == 0);
            }

            //
            // Output all lambda expression definitions.
            // in the order of their appearances in the tree.
            //
            while (_lambdas != null && _lambdas.Count > 0)
            {
                writeLine();
                writeLine();
                writeLambda(_lambdas.Dequeue());
            }
        }

        private void addType(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsGenericType)
            {
                foreach (var genericType in type.GetGenericArguments())
                {
                    var genericTypeInfo = genericType.GetTypeInfo();
                    if(genericTypeInfo.IsGenericType)
                    {
                        addType(genericType);
                    }
                    else
                    {
                        if (genericTypeInfo.IsClass)
                        {
                            _types.Add(genericType.ToString());
                        }
                    }
                }
            }
            else
            {
                if (typeInfo.IsClass)
                {
                    _types.Add(type.ToString());
                }
            }
        }
    }
}