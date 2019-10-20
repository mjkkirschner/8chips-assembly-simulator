using System;
using jackCompiler.AST;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Tests.Jack
{
    public class ASTTestUtils
    {




        [Test]
        public void FunctionCall()
        {
            /*class Main{
                function void main(){
               do coolfunc(3);
                }
                function int coolfunc(int x){
                return x;
                }
            }
            */
            var const3 = new IntNode(3);


            var returnStatement = new ReturnStatementNode(new IdentiferNode("x"));
            var mainBody = new SubroutineBodyNode(
                new List<VarDeclNode>(),
             new List<StatementNode>() { new DoStatementNode(new SubroutineCallNode(null, new IdentiferNode("coolfunc"), new List<ASTNode>() { const3 })) });
            var mainDecl = new SubroutineDeclNode(SubroutineType.function, new IdentiferNode("main"), new JackType("void"), new List<VarDeclNode>(), mainBody);

            var coolfuncBody = new SubroutineBodyNode(
                new List<VarDeclNode>(),
             new List<StatementNode>() { returnStatement });

            var coolFuncDecl = new SubroutineDeclNode(SubroutineType.function, new IdentiferNode("coolfunc"),
             new JackType("int"), new List<VarDeclNode>() { new VarDeclNode(new IdentiferNode("x", new JackType("int"))) }, coolfuncBody);

            var classnode = new ClassDeclNode("Main", new List<ClassVarDeclNode>(), new List<SubroutineDeclNode>() { coolFuncDecl, mainDecl });

            Console.WriteLine(classnode);
        }

    }
}