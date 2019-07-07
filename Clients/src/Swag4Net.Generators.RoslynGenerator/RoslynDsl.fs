module Swag4Net.Generators.RoslynGenerator.RoslynDsl

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open Swag4Net.Core
open SpecificationModel

let parseName = SyntaxFactory.ParseName
let identifierName (n:string) = SyntaxFactory.IdentifierName n
let usingDirective = parseName >> SyntaxFactory.UsingDirective
let parseTypeName = SyntaxFactory.ParseTypeName

let getClrType (prop:Property) = 
  let rec getTypeName =
    function
      | Inlined (PrimaryType dataType) -> 
          match dataType with
          | DataType.String (Some StringFormat.Date) -> "DateTime"
          | DataType.String (Some StringFormat.DateTime) -> "DateTime"
          | DataType.String (Some StringFormat.Base64Encoded) -> "string" //TODO: create a base64 string type
          | DataType.String (Some StringFormat.Binary) -> "byte[]"
          | DataType.String (Some StringFormat.Password) -> "string"
          | DataType.String _ -> "string"
          | DataType.Number -> "float"
          | DataType.Integer -> "int"
          | DataType.Integer64 -> "long"
          | DataType.Boolean -> "bool"
          | DataType.Array s -> s |> getTypeName |> sprintf "IEnumerable<%s>"
          | DataType.Object -> "object"
      | Inlined(ComplexType s) -> s.Name
      | _ -> raise (System.NotImplementedException "Cannot resolve CLR type for now")
  prop.Type |> getTypeName |> SyntaxFactory.ParseTypeName

let ucFirst(text:string) =
  if System.String.IsNullOrWhiteSpace(text) || text.Length < 2
  then text
  else
    sprintf "%s%s" (text.Substring(0, 1).ToUpperInvariant()) (text.Substring(1))

let constructor (name:string) =
  SyntaxFactory.ConstructorDeclaration name

let inline withModifier<'t when 't :> BaseMethodDeclarationSyntax> (kind:SyntaxKind) (c:'t) =
  let m = c :> BaseMethodDeclarationSyntax
  m.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(kind))) :?> 't

let withParameters (parameters:ParameterSyntax list) (c:ConstructorDeclarationSyntax) =
  let paramList =
        parameters
        |> List.fold (fun (l:ParameterListSyntax) p -> l.AddParameters p) (SyntaxFactory.ParameterList())
  c.WithParameterList(paramList)

let parameter varName typeName =
  SyntaxFactory.Parameter(SyntaxFactory.Identifier varName).WithType(parseTypeName typeName)

let withBody (statements:StatementSyntax list) (c:ConstructorDeclarationSyntax) =
  SyntaxFactory.Block().AddStatements(statements |> Seq.toArray)
  |> c.WithBody

let argumentName (varName:string) =
  SyntaxFactory.Argument(SyntaxFactory.IdentifierName varName)

let withBaseConstructorInitializer (args:ArgumentSyntax list) (c:ConstructorDeclarationSyntax) =
  c.WithInitializer(
    SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer)
        .AddArgumentListArguments(args |> Seq.toArray)
  ) :> MemberDeclarationSyntax

let declareVariable typeName name =
  SyntaxFactory.LocalDeclarationStatement(
    SyntaxFactory.VariableDeclaration(
          (parseTypeName typeName),
          SyntaxFactory.SeparatedList().Add(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(name))))
  )

let declareVariableWithValue name value =
  SyntaxFactory.LocalDeclarationStatement(
    SyntaxFactory.VariableDeclaration(
      SyntaxFactory.IdentifierName("var"))
        .WithVariables(
            SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                SyntaxFactory.VariableDeclarator(
                    SyntaxFactory.Identifier name).WithInitializer(
                    SyntaxFactory.EqualsValueClause value))))

let argument exp =
  SyntaxFactory.Argument(exp)

let literalExpression kind value =
  SyntaxFactory.LiteralExpression(kind, value) :> ExpressionSyntax

let memberAccess idName memberName =
  SyntaxFactory.MemberAccessExpression(
      SyntaxKind.SimpleMemberAccessExpression,
      identifierName idName,
      identifierName memberName
    )

let argumentList (arguments:ArgumentSyntax list) =
  SyntaxFactory.ArgumentList().AddArguments(arguments |> Seq.toArray)

let instanciate className (arguments:ExpressionSyntax list) =
  let args = arguments |> List.map argument |> argumentList
  let classIdentifier = identifierName className
  SyntaxFactory.ObjectCreationExpression(classIdentifier, args, null)

let assignVariable objectCreationExpression variableIdentifier =
  let assignment = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, variableIdentifier, objectCreationExpression);
  SyntaxFactory.ExpressionStatement(assignment) :> StatementSyntax

let separatedArgumentList (arguments:ArgumentSyntax list) =
  let addArg a (s:SeparatedSyntaxList<ArgumentSyntax>) = s.Add(a)
  let state = SyntaxFactory.SeparatedList<ArgumentSyntax>()
  arguments
    |> List.fold (fun a b -> a |> addArg b) state
    |> SyntaxFactory.ArgumentList

let invokeMember (arguments:ArgumentSyntax list) exp =
  let args = arguments |> separatedArgumentList
  SyntaxFactory.InvocationExpression(exp).WithArgumentList args

let addUsings (usings:string list) (unit:CompilationUnitSyntax) =
   let us = usings |> List.toArray |> Array.map usingDirective
   unit.AddUsings us

let inline addMembers (m:#MemberDeclarationSyntax) (unit:CompilationUnitSyntax) =
   unit.AddMembers m

let addArg a (args:SeparatedSyntaxList<ArgumentSyntax>) =
  args.Add(a)



