using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static jackCompiler.AST.Operators;


namespace jackCompiler.AST
{

    public static class ASTBuilder
    {

        public static ASTNode buildKeyWordNode(string token)
        {
            if (token == "true")
            {
                return new BooleanNode(true);
            }
            if (token == "false")
            {
                return new BooleanNode(false);
            }
            if (token == "this")
            {
                return new ThisNode();
            }
            if (token == "null")
            {
                return new NullNode();
            }
            throw new Exception("unknown keyword token");
        }

        public static Operator OperatorFromString(string token)
        {
            var matchingOp = Operators.OpMap.Where(x => x.Value == token);
            if (matchingOp.Count() != 0)
            {
                return matchingOp.FirstOrDefault().Key;
            }
            throw new Exception($"could not find matching operator for token {token} ");
        }

        public static UnaryOperator UnaryOperatorFromString(string token)
        {
            var matchingOp = Operators.UnOpMap.Where(x => x.Value == token);
            if (matchingOp.Count() != 0)
            {
                return matchingOp.FirstOrDefault().Key;
            }
            throw new Exception($"could not find matching operator for token {token} ");
        }
        public static LetStatementNode GenerateLetStatementAST(string varName, ASTNode rhs)
        {
            //let x = 100;
            var xID = new IdentiferNode(varName);
            var assign = new BinaryExpressionNode(xID, rhs, Operators.Operator.assign);
            var letStatement = new LetStatementNode(assign);

            return letStatement;
        }

        public static DoStatementNode GenerateDoStatementAST(string targetName, string functionName, IEnumerable<ASTNode> argumentList)
        {
            var doStatementNode = new DoStatementNode(new SubroutineCallNode(
                targetName != null ? new IdentiferNode(targetName) : null, new IdentiferNode(functionName), argumentList));

            return doStatementNode;
        }

        public static StatementNode GenerateIndexedLetStatement(string varName, int index, ASTNode rhs)
        {
            //let x[0] = 100;
            var xID = new IndexedIdentifierNode(varName, new IntNode(index));
            var assign = new BinaryExpressionNode(xID, rhs, Operators.Operator.assign);
            var letStatment = new LetStatementNode(assign);

            return letStatment;
        }

        public static VarDeclNode GenerateVarDeclaration(string name, JackType type = null)
        {
            return new VarDeclNode(new IdentiferNode(name, type));
        }

        public static SubroutineDeclNode GenerateFunctionDeclaration(string functionName, IEnumerable<string> names, IEnumerable<JackType> types, JackType returnType, SubroutineBodyNode functionBody)
        {
            /*{
                function int add(x int, y int){
                    return x + y
                }
            */
            var parameters = names.Select((x, index) => { return new VarDeclNode(new IdentiferNode(x, types.ElementAt(index))); });
            var function = new SubroutineDeclNode(SubroutineType.function, new IdentiferNode(functionName), returnType, parameters, functionBody);

            return function;
        }

    }


    public static class Operators
    {
        public static Dictionary<Operator, string> OpMap;
        public static Dictionary<UnaryOperator, string> UnOpMap;
        static Operators()
        {
            OpMap = new Dictionary<Operator, string>();

            OpMap.Add(Operator.add, "+");
            OpMap.Add(Operator.sub, "-");
            OpMap.Add(Operator.mult, "*");
            OpMap.Add(Operator.divide, "/");
            OpMap.Add(Operator.and, "&");
            OpMap.Add(Operator.or, "|");
            OpMap.Add(Operator.less, "<");
            OpMap.Add(Operator.greater, ">");
            OpMap.Add(Operator.assign, "=");
            OpMap.Add(Operator.equal, "==");

            UnOpMap = new Dictionary<UnaryOperator, string>();
            UnOpMap.Add(UnaryOperator.neg, "-");
            UnOpMap.Add(UnaryOperator.not, "~");
        }


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


            if (op is UnaryOperator)
            {
                return Operators.UnOpMap[(UnaryOperator)op];
            }
            else if (op is Operator)
            {
                return Operators.OpMap[(Operator)op];
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

    public class JackType
    {
        public String Name { get; set; }
        public JackType(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }


    //TODO implement equality comparison?
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
        public JackType Type { get; set; }
        public IdentiferNode(string ident, JackType type = null)
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

        //TODO may need special identifer token for this.
        public ClassVarDeclNode() : base(new IdentiferNode("CONSTRUCTION_IN_PROGRESS"))
        {

        }

        public override string ToString()
        {
            return $"{(IsStatic == true ? "static" : "field")} {base.Identifer}";
        }

    }

    public class VarDeclNode : ASTNode
    {
        public IdentiferNode Identifer { get; set; }

        public VarDeclNode(IdentiferNode identifer)
        {
            this.Identifer = identifer;
            /*// TODO should we have a typedIdentifierNode?
            if (this.Identifer.Type == null)
            {
                throw new Exception("you cannot declare a variable without a type.");
            }*/
        }
        public VarDeclNode()
        {

        }

        public override IEnumerable<ASTNode> Children()
        {
            return new ASTNode[] { Identifer };
        }

        public override string ToString()
        {
            return $"var {Identifer};";
        }

    }

    public class SubroutineDeclNode : ASTNode
    {
        public SubroutineType FunctionType { get; set; }
        public IdentiferNode FunctionName { get; set; }
        public JackType ReturnType { get; set; }
        //TODO consider argumentListNode.
        public IEnumerable<VarDeclNode> ParameterList { get; set; }
        public SubroutineBodyNode FunctionBody { get; set; }

        public SubroutineDeclNode()
        {

        }

        public SubroutineDeclNode(SubroutineType functionType, IdentiferNode functionName, JackType returnType, IEnumerable<VarDeclNode> parameterList, SubroutineBodyNode functionBody)
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
            var output = $"{FunctionType} {ReturnType.Name} {FunctionName.Value}({String.Join(",", ParameterList.Select(x => x.Identifer.ToString()))})";
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
        public SubroutineBodyNode()
        {

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

    public class SubroutineCallNode : ASTNode
    {

        /// <summary>
        /// what to call the function on
        /// </summary>
        /// <value></value>
        public IdentiferNode Target { get; set; }
        /// <summary>
        /// name of the function to call
        /// </summary>
        /// <value></value>
        public IdentiferNode FunctionName { get; set; }
        /// <summary>
        /// List of arguments
        /// </summary>
        /// <value></value>

        /// <summary>
        /// List of expressions we pass as arguments to the function.
        /// </summary>
        /// <value></value>
        public IEnumerable<ASTNode> ArgumentList { get; set; }

        public SubroutineCallNode(IdentiferNode target,
         IdentiferNode functionName,
          IEnumerable<ASTNode> argumentList)
        {
            Target = target;
            FunctionName = functionName;
            ArgumentList = argumentList;

        }

        public SubroutineCallNode()
        {

        }

        public override string ToString()
        {
            var targetString = Target != null ? "." : string.Empty;
            return $"{Target}{targetString }{FunctionName }({String.Join(",", ArgumentList.ToList().Select(x => x.ToString()))})";
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
        public BinaryExpressionNode()
        {
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

    public class UnaryExpressionNode : ASTNode
    {
        public UnaryOperator Operator { get; set; }

        public ASTNode Rhs { get; set; }

        public UnaryExpressionNode(ASTNode rhs, UnaryOperator op)
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
            return $"let {this.Assignment};";
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
            return $"return {this.Expression};";
        }
    }

    public class DoStatementNode : StatementNode
    {
        public SubroutineCallNode FunctionCall { get; set; }

        public DoStatementNode(SubroutineCallNode functionCall)
        {
            FunctionCall = functionCall;
        }
        public DoStatementNode()
        {

        }

        public override string ToString()
        {
            return $"do {FunctionCall};";
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

        public ClassDeclNode()
        {

        }

    }


    #region primitiveNodes

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
    public class StringNode : ASTNode
    {
        public string Value { get; set; }

        public StringNode(string value)
        {
            this.Value = value;
        }

        public override string ToString()
        {
            return Value;
        }

    }

    public class BooleanNode : ASTNode
    {
        public bool Value { get; set; }

        public BooleanNode(bool value)
        {
            this.Value = value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

    }

    public class NullNode : ASTNode
    {

        public NullNode()
        { }

        public override string ToString()
        {
            return "null";
        }
    }

    public class ThisNode : ASTNode
    {

        public ThisNode()
        { }

        public override string ToString()
        {
            return "this";
        }

    }
    #endregion
}