using System;
using jackCompiler.AST;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Tests.Jack
{
    public class ASTNodeTests
    {
        [Test]
        public void SimpleLetStatementAsAST_HasCorrectStringOutput()
        {
            //let x = 100;
            var xID = new IdentiferNode("x");
            var const100 = new IntNode(100);
            var assign = new BinaryExpressionNode(xID, const100, Operators.Operator.assign);
            var letStatment = new LetStatementNode(assign);

            Console.WriteLine(letStatment);
        }

        [Test]
        public void IndexedLetStatementAsAST_HasCorrectStringOutput()
        {
            //let x[0] = 100;
            var xID = new IndexedIdentifierNode("x", new IntNode(0));
            var const100 = new IntNode(100);
            var assign = new BinaryExpressionNode(xID, const100, Operators.Operator.assign);
            var letStatment = new LetStatementNode(assign);

            Console.WriteLine(letStatment);
        }

        [Test]
        public void SimpleClassDeclaration_StringOut()
        {
            /*class Main{
                function void main(){

                }
            }
            */

            var funcBody = new SubroutineBodyNode(new List<VarDeclNode>(), new List<StatementNode>());
            var function = new SubroutineDeclNode(SubroutineType.function, new IdentiferNode("main"), typeof(void), new List<VarDeclNode>(), funcBody);
            var classnode = new ClassDeclNode("Main", new List<ClassVarDeclNode>(), new List<SubroutineDeclNode>() { function });

            Console.WriteLine(classnode);
        }

        [Test]
        public void FunctionWithParams_StringOut()
        {
            /*{
                function int add(x int, y int){
                    return x + y
                }
            */

            var xdecl = new VarDeclNode(new IdentiferNode("x", typeof(Int32)));
            var ydecl = new VarDeclNode(new IdentiferNode("y", typeof(Int32)));
            var funcBody = new SubroutineBodyNode(new List<VarDeclNode>() { },
            new List<StatementNode>() { new ReturnStatementNode(new BinaryExpressionNode(new IdentiferNode("x"), new IdentiferNode("y"), Operators.Operator.add)) });

            var function = new SubroutineDeclNode(SubroutineType.function, new IdentiferNode("add"), typeof(Int32), new List<VarDeclNode>() { xdecl, ydecl }, funcBody);

            Console.WriteLine(function);
        }

        [Test]
        public void ClassDeclarationWithFunction_StringOut()
        {
            /*class Main{
                function void main(){
                var x int
                let x = 100
                }
            }
            */
            var xID = new IdentiferNode("x");
            var const100 = new IntNode(100);
            var assign = new BinaryExpressionNode(xID, const100, Operators.Operator.assign);
            var letStatment = new LetStatementNode(assign);

            var funcBody = new SubroutineBodyNode(new List<VarDeclNode>() { new VarDeclNode(new IdentiferNode("x", typeof(Int32))) }, new List<StatementNode>() { letStatment });
            var function = new SubroutineDeclNode(SubroutineType.function, new IdentiferNode("main"), typeof(void), new List<VarDeclNode>(), funcBody);
            var classnode = new ClassDeclNode("Main", new List<ClassVarDeclNode>(), new List<SubroutineDeclNode>() { function });

            Console.WriteLine(classnode);
        }

    }
}