using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static jackCompiler.AST.Operators;

namespace jackCompiler.AST
{

    public static class Operators
    {


        public enum Operator
        {
            add,
            sub,
            mult,
            divide,
            and,
            or,
            less,
            greater,
            assign,
            equal,
        }

        public enum UnaryOperator
        {
            neg,
            not,

        }

        public static string OperatorToString(object op)
        {
            var opMap = new Dictionary<Operator, string>();

            opMap.Add(Operator.add, "+");
            opMap.Add(Operator.sub, "-");
            opMap.Add(Operator.mult, "*");
            opMap.Add(Operator.divide, "/");
            opMap.Add(Operator.and, "&");
            opMap.Add(Operator.or, "|");
            opMap.Add(Operator.less, "<");
            opMap.Add(Operator.greater, ">");
            opMap.Add(Operator.assign, "=");
            opMap.Add(Operator.equal, "==");

            var unopMap = new Dictionary<UnaryOperator, string>();
            unopMap.Add(UnaryOperator.neg, "-");
            unopMap.Add(UnaryOperator.not, "~");

            if (op is UnaryOperator)
            {
                return unopMap[(UnaryOperator)op];
            }
            else if (op is Operator)
            {
                return opMap[(Operator)op];
            }
            else
            {
                throw new ArgumentException("op must be operator or un op");
            }
        }
    }

    public enum SubroutineType
    {
        constructor, method, function
    }

    public abstract class ASTNode
    {
        public Guid ID { get; set; }
        public virtual IEnumerable<ASTNode> Children()
        {
            return Enumerable.Empty<ASTNode>();
        }

        public override string ToString()
        {
            return $"BASECLASSIMPL {this.GetType().Name}:{this.ID}";
        }

        public ASTNode() => this.ID = Guid.NewGuid();

    }

    public class IdentiferNode : ASTNode
    {
        public string Value { get; set; }

        //TODO use c# types for this?
        //should this property even exist here?
        public Type Type { get; set; }
        public IdentiferNode(string ident, Type type = null)
        {
            this.Value = ident;
            this.Type = type;
        }

        public override string ToString()
        {
            var typeString = string.Empty;
            if (Type != null)
            {
                typeString = $"{Type}";
            }
            return $"{typeString} {Value}";
        }
    }

    public class IndexedIdentifierNode : IdentiferNode
    {
        public ASTNode ExpressionInsideArrayIndex { get; set; }
        public IndexedIdentifierNode(string ident, ASTNode arrayIndexExpression) : base(ident)
        {
            this.ExpressionInsideArrayIndex = arrayIndexExpression;
        }

        public override string ToString()
        {
            return $"{this.Value}[{this.ExpressionInsideArrayIndex}]";
        }

        public override IEnumerable<ASTNode> Children()
        {
            return new List<ASTNode>() { ExpressionInsideArrayIndex };
        }
    }

    public class ClassVarDeclNode : VarDeclNode
    {
        public bool IsStatic { get; set; }
        public bool IsField { get; set; }

        public ClassVarDeclNode(IdentiferNode identifer, bool isStatic, bool isField) : base(identifer)
        {
            this.IsStatic = isStatic;
            this.IsField = isField;
        }

    }

    public class VarDeclNode : ASTNode
    {
        public IdentiferNode Identifer { get; set; }

        public VarDeclNode(IdentiferNode identifer)
        {
            this.Identifer = identifer;
            // TODO should we have a typedIdentifierNode?
            if (this.Identifer.Type == null)
            {
                throw new Exception("you cannot declare a variable without a type.");
            }
        }

        public override IEnumerable<ASTNode> Children()
        {
            return new ASTNode[] { Identifer };
        }

        public override string ToString()
        {
            return $"var {Identifer}";
        }

    }

    public class SubroutineDeclNode : ASTNode
    {
        public SubroutineType FunctionType { get; set; }
        public IdentiferNode FunctionName { get; set; }
        public Type ReturnType { get; set; }
        //TODO consider argumentListNode.
        public IEnumerable<VarDeclNode> ParameterList { get; set; }
        public SubroutineBodyNode FunctionBody { get; set; }

        public SubroutineDeclNode(SubroutineType functionType, IdentiferNode functionName, Type returnType, IEnumerable<VarDeclNode> parameterList, SubroutineBodyNode functionBody)
        {
            this.FunctionType = functionType;
            this.FunctionName = functionName;
            this.ReturnType = returnType;
            this.ParameterList = parameterList;
            this.FunctionBody = functionBody;
        }
        public override IEnumerable<ASTNode> Children()
        {
            return (new List<ASTNode>() { FunctionName }.AsEnumerable().Concat(ParameterList)).Concat(new List<ASTNode>() { FunctionBody });
        }

        public override string ToString()
        {
            var output = $"{FunctionType} {ReturnType} {FunctionName}({String.Join(",", ParameterList.Select(x => x.Identifer.ToString()))})";
            output += "{" + $"{Environment.NewLine} {FunctionBody} {Environment.NewLine}" + "}";
            return output;
        }
    }

    public class SubroutineBodyNode : ASTNode
    {
        public IEnumerable<VarDeclNode> Variables { get; set; }
        public IEnumerable<StatementNode> Statements { get; set; }

        public SubroutineBodyNode(IEnumerable<VarDeclNode> variables, IEnumerable<StatementNode> statements)
        {
            this.Variables = variables;
            this.Statements = statements;
        }

        public override string ToString()
        {
            return $"{string.Join(Environment.NewLine, Variables.Select(x => x.ToString()))}" +
                $"{Environment.NewLine} {string.Join(Environment.NewLine, Statements.Select(x => x.ToString())) }";
        }

        public override IEnumerable<ASTNode> Children()
        {
            return this.Variables.AsEnumerable<ASTNode>().Concat(this.Statements);
        }
    }

    public class StatementNode : ASTNode
    {

    }



    public class BinaryExpressionNode : ASTNode
    {

        public ASTNode Lhs { get; set; }
        public Operator Operator { get; set; }

        public ASTNode Rhs { get; set; }

        public BinaryExpressionNode(ASTNode lhs, ASTNode rhs, Operator op)
        {
            this.Lhs = lhs;
            this.Rhs = rhs;
            this.Operator = op;
        }

        public override string ToString()
        {
            return $"{Lhs} {Operators.OperatorToString(Operator)} {Rhs}";
        }

        public override IEnumerable<ASTNode> Children()
        {
            return new List<ASTNode>() { Lhs }.Concat(new List<ASTNode> { Rhs });
        }

    }

    public class UnaryExpression : ASTNode
    {
        public Operator Operator { get; set; }

        public ASTNode Rhs { get; set; }

        public UnaryExpression(ASTNode rhs, Operator op)
        {
            this.Rhs = rhs;
            this.Operator = op;
        }

        public override string ToString()
        {
            return $"{Operator} {Rhs}";
        }

        public override IEnumerable<ASTNode> Children()
        {
            return new List<ASTNode> { Rhs };
        }
    }

    public class LetStatementNode : StatementNode
    {
        public BinaryExpressionNode Assignment { get; set; }

        public LetStatementNode(BinaryExpressionNode assignment)
        {
            this.Assignment = assignment;
        }

        public override string ToString()
        {
            return $"let {this.Assignment}";
        }
    }

    public class ReturnStatementNode : StatementNode
    {
        public ASTNode Expression { get; set; }

        public ReturnStatementNode(ASTNode returnValue = null)
        {
            this.Expression = returnValue;
        }

        public override string ToString()
        {
            return $"return {this.Expression}";
        }
    }

    public class ClassDeclNode : ASTNode
    {
        public String ClassName { get; set; }
        public IEnumerable<ClassVarDeclNode> ClassVariables { get; set; }
        public IEnumerable<SubroutineDeclNode> ClassSubroutines { get; set; }

        public override string ToString()
        {
            var output = $"class {ClassName} {'{'} {Environment.NewLine} {String.Join(Environment.NewLine, ClassVariables.Select(x => x.ToString()))}";
            output += $"{Environment.NewLine}{String.Join(Environment.NewLine, ClassSubroutines.Select(x => x.ToString()))}";
            output += Environment.NewLine + '}';
            return output;
        }

        public override IEnumerable<ASTNode> Children()
        {
            return Enumerable.Empty<ASTNode>().Concat(this.ClassSubroutines).Concat(this.ClassVariables);
        }

        public ClassDeclNode(string className, IEnumerable<ClassVarDeclNode> classVariables, IEnumerable<SubroutineDeclNode> classSubroutines)
        {
            this.ClassName = className;
            this.ClassSubroutines = classSubroutines;
            this.ClassVariables = classVariables;
        }

    }

    public class IntNode : ASTNode
    {
        public Int32 Value { get; set; }

        public IntNode(Int32 value)
        {
            this.Value = value;
        }

        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }

    }

}