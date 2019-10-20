using NUnit.Framework;
using System.IO;
using System;
using jackCompiler.AST;
using System.Collections.Generic;
namespace Tests.Jack
{

    public class Coco_R_JackParserTests
    {

        [Test]
        public void simpleClassWithOneFunction()
        {
            var testClassCode =
            @"  class Main{
                function void main(){
                var int x ;
                let x = 100;
                }
            }";
            var path = Path.GetTempFileName();
            File.WriteAllText(path, testClassCode);

            var scanner = new Scanner(path);
            var jackParser = new Parser(scanner);
            jackParser.Parse();
            Console.WriteLine(jackParser.root.ToString());

            //generate an AST to test against.

            var variables = new List<VarDeclNode>(){
                ASTBuilder.GenerateVarDeclaration("x",new JackType("int")),
                };
            var statements = new List<StatementNode>(){
                                ASTBuilder.GenerateLetStatementAST("x",new IntNode(100))
                };
            var mainFuncBody = new SubroutineBodyNode(variables, statements);

            var mainFunc = ASTBuilder.GenerateFunctionDeclaration("main",
            new List<string>(),
            new List<JackType>(),
            new JackType("void"),
            mainFuncBody);

            var classRoot = new ClassDeclNode(
                "Main",
            new List<ClassVarDeclNode>(),
            new List<SubroutineDeclNode>() { mainFunc }
            );

            Assert.AreEqual(jackParser.root.ToString(), classRoot.ToString());


        }

        [Test]
        public void classFields()
        {
            var testClassCode =
            @"  class Main{
                field int coolVar;
            }";
            var path = Path.GetTempFileName();
            File.WriteAllText(path, testClassCode);

            var scanner = new Scanner(path);
            var jackParser = new Parser(scanner);
            jackParser.Parse();
            Console.WriteLine(jackParser.root.ToString());

            var classRoot = new ClassDeclNode(
                "Main",
            new List<ClassVarDeclNode>() { new ClassVarDeclNode(new IdentiferNode("coolVar", new JackType("int")), false, true) },
            new List<SubroutineDeclNode>()
            );
            Assert.AreEqual(jackParser.root.ToString(), classRoot.ToString());

        }

        [Test]
        public void classstatic()
        {
            var testClassCode =
            @"  class Main{
                static int coolVar;
            }";
            var path = Path.GetTempFileName();
            File.WriteAllText(path, testClassCode);

            var scanner = new Scanner(path);
            var jackParser = new Parser(scanner);
            jackParser.Parse();
            Console.WriteLine(jackParser.root.ToString());

            var classRoot = new ClassDeclNode(
               "Main",
           new List<ClassVarDeclNode>() { new ClassVarDeclNode(new IdentiferNode("coolVar", new JackType("int")), true, false) },
           new List<SubroutineDeclNode>()
           );
            Assert.AreEqual(jackParser.root.ToString(), classRoot.ToString());
        }

        [Test]
        public void multiVariables()
        {
            var testClassCode =
            @"  class Main{
                static int coolVar;
                static int coolVar1;
                static int coolVar2;
                field boolean coolVar3;
                field boolean coolVar4;
                field boolean coolVar5;
            }";
            var path = Path.GetTempFileName();
            File.WriteAllText(path, testClassCode);

            var scanner = new Scanner(path);
            var jackParser = new Parser(scanner);
            jackParser.Parse();
            Console.WriteLine(jackParser.root.ToString());

            var classRoot = new ClassDeclNode(
            "Main",
        new List<ClassVarDeclNode>() {
            new ClassVarDeclNode(new IdentiferNode("coolVar", new JackType("int")), true, false),
            new ClassVarDeclNode(new IdentiferNode("coolVar1", new JackType("int")), true, false),
            new ClassVarDeclNode(new IdentiferNode("coolVar2", new JackType("int")), true, false),
            new ClassVarDeclNode(new IdentiferNode("coolVar3", new JackType("boolean")), false, true),
            new ClassVarDeclNode(new IdentiferNode("coolVar4", new JackType("boolean")), false, true),
            new ClassVarDeclNode(new IdentiferNode("coolVar5", new JackType("boolean")), false, true)
         },
        new List<SubroutineDeclNode>()
        );
            Assert.AreEqual(jackParser.root.ToString(), classRoot.ToString());
        }

        [Test]
        public void functionCall()
        {
            var testClassCode =
           @"  class Main{
               function int ten(){
                   return 10;
               }

                function void main(){
              do ten();
                }
            }";
            var path = Path.GetTempFileName();
            File.WriteAllText(path, testClassCode);

            var scanner = new Scanner(path);
            var jackParser = new Parser(scanner);
            jackParser.Parse();
            Console.WriteLine(jackParser.root.ToString());

            //generate an AST to test against.


            var variables = new List<VarDeclNode>() { };
            var statements = new List<StatementNode>(){
                                ASTBuilder.GenerateDoStatementAST(null,"ten",new List<ASTNode>())
                };
            var mainFuncBody = new SubroutineBodyNode(variables, statements);

            var mainFunc = ASTBuilder.GenerateFunctionDeclaration("main",
            new List<string>(),
            new List<JackType>(),
            new JackType("void"),
            mainFuncBody);

            var tenStatments = new List<StatementNode>() { new ReturnStatementNode(new IntNode(10)) };
            var tenFuncBody = new SubroutineBodyNode(variables, tenStatments);

            var tenFunc = ASTBuilder.GenerateFunctionDeclaration("ten",
           new List<string>(),
           new List<JackType>(),
           new JackType("int"),
           tenFuncBody);

            var classRoot = new ClassDeclNode(
                "Main",
            new List<ClassVarDeclNode>(),
            new List<SubroutineDeclNode>() { tenFunc, mainFunc }
            );

            Assert.AreEqual(jackParser.root.ToString(), classRoot.ToString());

        }

        [Test]
        public void functionCallWithParameter()
        {
            var testClassCode =
           @"  class Main{
               function int plus10(int x){
                   return x +10;
               }

                function void main(){
                do plus10(10);
                }
            }";
            var path = Path.GetTempFileName();
            File.WriteAllText(path, testClassCode);

            var scanner = new Scanner(path);
            var jackParser = new Parser(scanner);
            jackParser.Parse();
            Console.WriteLine(jackParser.root.ToString());

            //generate an AST to test against.


            var variables = new List<VarDeclNode>() { };
            var statements = new List<StatementNode>(){
                                ASTBuilder.GenerateDoStatementAST(null,"plus10",new List<ASTNode>(){new IntNode(10)})
                };
            var mainFuncBody = new SubroutineBodyNode(variables, statements);

            var mainFunc = ASTBuilder.GenerateFunctionDeclaration("main",
            new List<string>(),
            new List<JackType>(),
            new JackType("void"),
            mainFuncBody);

            var tenStatments = new List<StatementNode>()
                { new ReturnStatementNode(
                    new BinaryExpressionNode(new IdentiferNode("x"), new IntNode(10), Operators.Operator.add)) };
            var tenFuncBody = new SubroutineBodyNode(variables, tenStatments);

            var tenFunc = ASTBuilder.GenerateFunctionDeclaration("plus10",
           new List<string>() { "x" },
           new List<JackType>() { new JackType("int") },
           new JackType("int"),
           tenFuncBody);

            var classRoot = new ClassDeclNode(
                "Main",
            new List<ClassVarDeclNode>(),
            new List<SubroutineDeclNode>() { tenFunc, mainFunc }
            );

            Assert.AreEqual(jackParser.root.ToString(), classRoot.ToString());
        }

    }


}