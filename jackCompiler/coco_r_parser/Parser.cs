/*----------------------------------------------------------------------
Compiler Generator Coco/R,
Copyright (c) 1990, 2004 Hanspeter Moessenboeck, University of Linz
extended by M. Loeberbauer & A. Woess, Univ. of Linz
with improvements by Pat Terry, Rhodes University

This program is free software; you can redistribute it and/or modify it 
under the terms of the GNU General Public License as published by the 
Free Software Foundation; either version 2, or (at your option) any 
later version.

This program is distributed in the hope that it will be useful, but 
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License 
for more details.

You should have received a copy of the GNU General Public License along 
with this program; if not, write to the Free Software Foundation, Inc., 
59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.

As an exception, it is allowed to write an extension of Coco/R that is
used as a plugin in non-free software.

If not otherwise stated, any source code generated by Coco/R (other than 
Coco/R itself) does not fall under the GNU General Public License.
-----------------------------------------------------------------------*/

//THIS FILE WAS GENERATED WITH COCO/R. 
//AS STATED ABOVE IT IS LICENSED UNDER THE LICENSE OF THE REPO
//NOT GNU GPL.

using System.Linq;
using static jackCompiler.AST.Operators;
using System.Collections.Generic;



using System;



public class Parser {
	public const int _EOF = 0;
	public const int _add = 1;
	public const int _sub = 2;
	public const int _mult = 3;
	public const int _div = 4;
	public const int _and = 5;
	public const int _or = 6;
	public const int _lt = 7;
	public const int _gt = 8;
	public const int _eq = 9;
	public const int _assign = 10;
	public const int _not = 11;
	public const int _ident = 12;
	public const int _integerConstant = 13;
	public const int _stringConstant = 14;
	public const int _true = 15;
	public const int _false = 16;
	public const int _null = 17;
	public const int _this = 18;
	public const int _static = 19;
	public const int _field = 20;
	public const int _kw_constructor = 21;
	public const int _kw_method = 22;
	public const int _kw_function = 23;
	public const int _kw_class = 24;
	public const int _kw_let = 25;
	public const int _kw_return = 26;
	public const int maxT = 35;

	const bool T = true;
	const bool x = false;
	const int minErrDist = 2;
	
	public Scanner scanner;
	public Errors  errors;

	public Token t;    // last recognized token
	public Token la;   // lookahead token
	int errDist = minErrDist;

public jackCompiler.AST.ASTNode root { get; set; }




	public Parser(Scanner scanner) {
		this.scanner = scanner;
		errors = new Errors();
	}

	void SynErr (int n) {
		if (errDist >= minErrDist) errors.SynErr(la.line, la.col, n);
		errDist = 0;
	}

	public void SemErr (string msg) {
		if (errDist >= minErrDist) errors.SemErr(t.line, t.col, msg);
		errDist = 0;
	}
	
	void Get () {
		for (;;) {
			t = la;
			la = scanner.Scan();
			if (la.kind <= maxT) { ++errDist; break; }

			la = t;
		}
	}
	
	void Expect (int n) {
		if (la.kind==n) Get(); else { SynErr(n); }
	}
	
	bool StartOf (int s) {
		return set[s, la.kind];
	}
	
	void ExpectWeak (int n, int follow) {
		if (la.kind == n) Get();
		else {
			SynErr(n);
			while (!StartOf(follow)) Get();
		}
	}


	bool WeakSeparator(int n, int syFol, int repFol) {
		int kind = la.kind;
		if (kind == n) {Get(); return true;}
		else if (StartOf(repFol)) {return false;}
		else {
			SynErr(n);
			while (!(set[syFol, kind] || set[repFol, kind] || set[0, kind])) {
				Get();
				kind = la.kind;
			}
			return StartOf(syFol);
		}
	}

	
	void jack() {
		jackCompiler.AST.ClassDeclNode classNode;
		ClassDeclaration(out classNode);
		root = classNode; 
	}

	void ClassDeclaration(out jackCompiler.AST.ClassDeclNode currentClass ) {
		currentClass = new jackCompiler.AST.ClassDeclNode(); 
		Expect(24);
		Expect(12);
		currentClass.ClassName = t.val; 
		Expect(29);
		List<jackCompiler.AST.ClassVarDeclNode> varnodeList;
		List<jackCompiler.AST.SubroutineDeclNode> funcnodelist;
		ClassVarDeclarationList(out varnodeList);
		currentClass.ClassVariables = varnodeList; 
		SubroutineDeclarationList(out funcnodelist);
		currentClass.ClassSubroutines = funcnodelist; 
		Expect(30);
	}

	void Identifer(out jackCompiler.AST.IdentiferNode node ) {
		Expect(12);
		node = new jackCompiler.AST.IdentiferNode(t.val);  
	}

	void IndexedIdentifier(out jackCompiler.AST.IdentiferNode identNode) {
		jackCompiler.AST.ASTNode indexExpression;
		Expect(12);
		var name = t.val; 
		Expect(27);
		Expression(out indexExpression);
		Expect(28);
		identNode = new jackCompiler.AST.IndexedIdentifierNode(name,indexExpression); 
	}

	void Expression(out jackCompiler.AST.ASTNode expressionNode) {
		jackCompiler.AST.ASTNode term1Node; 
		Term(out term1Node);
		var currentTerm = term1Node;
		if (StartOf(1)) {
			while (StartOf(1)) {
				jackCompiler.AST.Operators.Operator opNode;
				jackCompiler.AST.ASTNode otherTerm;
				Op(out opNode);
				Term(out otherTerm);
				var currentBinaryExpression = new jackCompiler.AST.BinaryExpressionNode(currentTerm,otherTerm,opNode); 
				currentTerm = currentBinaryExpression;  
			}
		}
		expressionNode = currentTerm; 
	}

	void ClassVarDeclarationList(out List<jackCompiler.AST.ClassVarDeclNode> varlist) {
		varlist =  new List<jackCompiler.AST.ClassVarDeclNode>();
		jackCompiler.AST.ClassVarDeclNode node;
		while (la.kind == 19 || la.kind == 20) {
			ClassVarDeclaration(out node);
			varlist.Add(node);
		}
	}

	void SubroutineDeclarationList(out List<jackCompiler.AST.SubroutineDeclNode> varlist) {
		varlist =  new List<jackCompiler.AST.SubroutineDeclNode>();
		jackCompiler.AST.SubroutineDeclNode node;
		while (la.kind == 21 || la.kind == 22 || la.kind == 23) {
			SubroutineDeclaration(out node);
			varlist.Add(node);
		}
	}

	void ClassVarDeclaration(out jackCompiler.AST.ClassVarDeclNode currentClassVarDec ) {
		currentClassVarDec = new jackCompiler.AST.ClassVarDeclNode(); Type classType; 
		if (la.kind == 19) {
			Get();
		} else if (la.kind == 20) {
			Get();
		} else SynErr(36);
		if(t.val == "static"){
		currentClassVarDec.IsStatic = true;
		currentClassVarDec.IsField = false;
		                }
		else{
		   currentClassVarDec.IsStatic = false;
		   currentClassVarDec.IsField = true;
		}
		                
		Expect(12);
		classType = Type.GetType(t.val); 
		jackCompiler.AST.IdentiferNode varident;
		Identifer(out varident);
		varident.Type = classType;
		currentClassVarDec.Identifer = varident; 
		Expect(31);
	}

	void SubroutineDeclaration(out jackCompiler.AST.SubroutineDeclNode currentSubDec ) {
		currentSubDec = new jackCompiler.AST.SubroutineDeclNode(); Type returnType; 
		if (la.kind == 21) {
			Get();
		} else if (la.kind == 22) {
			Get();
		} else if (la.kind == 23) {
			Get();
		} else SynErr(37);
		if(t.val == "constructor"){
		currentSubDec.FunctionType = jackCompiler.AST.SubroutineType.constructor;
		}
		else if(t.val == "method"){
		currentSubDec.FunctionType = jackCompiler.AST.SubroutineType.method;
		}
		else{
		currentSubDec.FunctionType = jackCompiler.AST.SubroutineType.function;
		}
		
		Expect(12);
		returnType = Type.GetType(t.val); 
		jackCompiler.AST.IdentiferNode typeIdent;
		Identifer(out typeIdent);
		typeIdent.Type = returnType;
		currentSubDec.FunctionName = typeIdent; 
		currentSubDec.ReturnType = returnType; 
		Expect(32);
		List<jackCompiler.AST.VarDeclNode> paramList;
		ParameterList(out paramList);
		currentSubDec.ParameterList =  paramList;
		jackCompiler.AST.SubroutineBodyNode bodyNode;
		SubRoutineBody(out bodyNode);
		currentSubDec.FunctionBody = bodyNode;
		Expect(33);
	}

	void ParameterList(out List<jackCompiler.AST.VarDeclNode> varlist) {
		varlist =  new List<jackCompiler.AST.VarDeclNode>(); 
		if (la.kind == 12) {
			while (la.kind == 12) {
				Get();
				Type varType;
				jackCompiler.AST.IdentiferNode typeIdent;
				varType = Type.GetType(t.val); 
				Identifer(out typeIdent);
				typeIdent.Type = varType;
				varlist.Add(new jackCompiler.AST.VarDeclNode(typeIdent)); 
			}
		}
	}

	void SubRoutineBody(out jackCompiler.AST.SubroutineBodyNode bodyNode) {
		bodyNode = new jackCompiler.AST.SubroutineBodyNode(); 
		var varList = new List<jackCompiler.AST.VarDeclNode>(); 
		var statList = new List<jackCompiler.AST.StatementNode>(); 
		while (la.kind == 34) {
			VarDeclaration(out jackCompiler.AST.VarDeclNode VarDeclNode);
			varList.Add(VarDeclNode) ; 
		}
		bodyNode.Variables = varList; 
		while (la.kind == 25 || la.kind == 26) {
			Statement(out jackCompiler.AST.StatementNode statementNode);
			statList.Add(statementNode) ; 
		}
		bodyNode.Statements = statList; 
	}

	void VarDeclaration(out jackCompiler.AST.VarDeclNode vardeclNode) {
		Expect(34);
		vardeclNode = new jackCompiler.AST.VarDeclNode(); 
		Expect(12);
		var varType = Type.GetType(t.val); 
		jackCompiler.AST.IdentiferNode varident;
		Identifer(out varident);
		varident.Type = varType;
		vardeclNode.Identifer = varident; 
		Expect(31);
	}

	void Statement(out jackCompiler.AST.StatementNode statementNode) {
		statementNode = null;
		if (la.kind == 25) {
			LetStatement(out statementNode);
		} else if (la.kind == 26) {
			ReturnStatement(out statementNode);
		} else SynErr(38);
	}

	void LetStatement(out jackCompiler.AST.StatementNode letnode) {
		jackCompiler.AST.IdentiferNode identNode = null;
		Expect(25);
		if (la.kind == 12) {
			Identifer(out identNode);
		} else if (la.kind == 12) {
			IndexedIdentifier(out identNode);
		} else SynErr(39);
		Expect(10);
		jackCompiler.AST.ASTNode expressionNode;
		Expression(out expressionNode);
		var assignmentExpression = new jackCompiler.AST.BinaryExpressionNode(identNode,expressionNode,jackCompiler.AST.Operators.Operator.assign);
		letnode = new jackCompiler.AST.LetStatementNode(assignmentExpression);
		Expect(31);
	}

	void ReturnStatement(out jackCompiler.AST.StatementNode returnnode) {
		jackCompiler.AST.ASTNode expressionNode = null;
		Expect(26);
		if (StartOf(2)) {
			Expression(out expressionNode);
		}
		returnnode = new jackCompiler.AST.ReturnStatementNode(expressionNode); 
		Expect(31);
	}

	void Term(out jackCompiler.AST.ASTNode termNode) {
		jackCompiler.AST.IntNode intNode = null;
		jackCompiler.AST.StringNode stringNode = null;
		jackCompiler.AST.ASTNode keywordNode = null;
		jackCompiler.AST.IdentiferNode identNode = null;
		jackCompiler.AST.IdentiferNode identIndexNode = null;
		jackCompiler.AST.ASTNode subcallNode = null;
		jackCompiler.AST.ASTNode expressionNode = null;
		jackCompiler.AST.Operators.UnaryOperator opNode = 0;
		jackCompiler.AST.ASTNode unaryTermNode = null;
		termNode = null;
		if (la.kind == 13) {
			IntConstant(out intNode);
			termNode = intNode; 
		} else if (la.kind == 14) {
			StringConstant(out stringNode);
			termNode = stringNode; 
		} else if (StartOf(3)) {
			KeyWordConstant(out keywordNode);
			termNode = keywordNode; 
		} else if (la.kind == 12) {
			Identifer(out identNode);
			termNode = identNode; 
		} else if (la.kind == 12) {
			IndexedIdentifier(out identIndexNode);
			termNode = identIndexNode; 
		} else if (StartOf(4)) {
		} else if (la.kind == 32) {
			Get();
			Expression(out expressionNode);
			termNode = expressionNode; 
			Expect(33);
		} else if (la.kind == 2 || la.kind == 11) {
			UnaryOp(out opNode);
			Term(out unaryTermNode);
			termNode = new jackCompiler.AST.UnaryExpressionNode(unaryTermNode,opNode);
		} else SynErr(40);
	}

	void Op(out jackCompiler.AST.Operators.Operator op) {
		switch (la.kind) {
		case 1: {
			Get();
			break;
		}
		case 2: {
			Get();
			break;
		}
		case 3: {
			Get();
			break;
		}
		case 4: {
			Get();
			break;
		}
		case 5: {
			Get();
			break;
		}
		case 6: {
			Get();
			break;
		}
		case 7: {
			Get();
			break;
		}
		case 8: {
			Get();
			break;
		}
		case 9: {
			Get();
			break;
		}
		default: SynErr(41); break;
		}
		op = jackCompiler.AST.ASTBuilder.OperatorFromString(t.val); 
	}

	void IntConstant(out jackCompiler.AST.IntNode intNode) {
		Expect(13);
		intNode = new jackCompiler.AST.IntNode (Int32.Parse(t.val));
	}

	void StringConstant(out jackCompiler.AST.StringNode strNode) {
		Expect(14);
		strNode = new jackCompiler.AST.StringNode((t.val));
	}

	void KeyWordConstant(out jackCompiler.AST.ASTNode astNode) {
		if (la.kind == 15) {
			Get();
		} else if (la.kind == 16) {
			Get();
		} else if (la.kind == 17) {
			Get();
		} else if (la.kind == 18) {
			Get();
		} else SynErr(42);
		astNode = jackCompiler.AST.ASTBuilder.buildKeyWordNode(t.val);
	}

	void UnaryOp(out jackCompiler.AST.Operators.UnaryOperator op) {
		if (la.kind == 2) {
			Get();
		} else if (la.kind == 11) {
			Get();
		} else SynErr(43);
		op = jackCompiler.AST.ASTBuilder.UnaryOperatorFromString(t.val); 
	}



	public void Parse() {
		la = new Token();
		la.val = "";		
		Get();
		jack();
		Expect(0);

	}
	
	static readonly bool[,] set = {
		{T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,T,T, T,T,T,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,T,T, T,T,T,T, T,T,x,T, T,T,T,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,T,T, T,T,T,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,T, x,T,x,x, x}

	};
} // end Parser


public class Errors {
	public int count = 0;                                    // number of errors detected
	public System.IO.TextWriter errorStream = Console.Out;   // error messages go to this stream
	public string errMsgFormat = "-- line {0} col {1}: {2}"; // 0=line, 1=column, 2=text

	public virtual void SynErr (int line, int col, int n) {
		string s;
		switch (n) {
			case 0: s = "EOF expected"; break;
			case 1: s = "add expected"; break;
			case 2: s = "sub expected"; break;
			case 3: s = "mult expected"; break;
			case 4: s = "div expected"; break;
			case 5: s = "and expected"; break;
			case 6: s = "or expected"; break;
			case 7: s = "lt expected"; break;
			case 8: s = "gt expected"; break;
			case 9: s = "eq expected"; break;
			case 10: s = "assign expected"; break;
			case 11: s = "not expected"; break;
			case 12: s = "ident expected"; break;
			case 13: s = "integerConstant expected"; break;
			case 14: s = "stringConstant expected"; break;
			case 15: s = "true expected"; break;
			case 16: s = "false expected"; break;
			case 17: s = "null expected"; break;
			case 18: s = "this expected"; break;
			case 19: s = "static expected"; break;
			case 20: s = "field expected"; break;
			case 21: s = "kw_constructor expected"; break;
			case 22: s = "kw_method expected"; break;
			case 23: s = "kw_function expected"; break;
			case 24: s = "kw_class expected"; break;
			case 25: s = "kw_let expected"; break;
			case 26: s = "kw_return expected"; break;
			case 27: s = "\"[\" expected"; break;
			case 28: s = "\"]\" expected"; break;
			case 29: s = "\"{\" expected"; break;
			case 30: s = "\"}\" expected"; break;
			case 31: s = "\";\" expected"; break;
			case 32: s = "\"(\" expected"; break;
			case 33: s = "\")\" expected"; break;
			case 34: s = "\"var\" expected"; break;
			case 35: s = "??? expected"; break;
			case 36: s = "invalid ClassVarDeclaration"; break;
			case 37: s = "invalid SubroutineDeclaration"; break;
			case 38: s = "invalid Statement"; break;
			case 39: s = "invalid LetStatement"; break;
			case 40: s = "invalid Term"; break;
			case 41: s = "invalid Op"; break;
			case 42: s = "invalid KeyWordConstant"; break;
			case 43: s = "invalid UnaryOp"; break;

			default: s = "error " + n; break;
		}
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}

	public virtual void SemErr (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}
	
	public virtual void SemErr (string s) {
		errorStream.WriteLine(s);
		count++;
	}
	
	public virtual void Warning (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
	}
	
	public virtual void Warning(string s) {
		errorStream.WriteLine(s);
	}
} // Errors


public class FatalError: Exception {
	public FatalError(string m): base(m) {}
}
