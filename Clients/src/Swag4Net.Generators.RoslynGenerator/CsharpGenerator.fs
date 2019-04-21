namespace Swag4Net.Generators.RoslynGenerator

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open System.Net

open Swag4Net.Core.v2
open Models
open Microsoft.CodeAnalysis.CSharp.Syntax
open Microsoft.CodeAnalysis.CSharp

module CodeGeneration =

  type GenerationSettings =
    { Namespace:string }

module RoslynDsl =
  let parseName = SyntaxFactory.ParseName
  let identifierName (n:string) = SyntaxFactory.IdentifierName n
  let usingDirective = parseName >> SyntaxFactory.UsingDirective
  let parseTypeName = SyntaxFactory.ParseTypeName

  let getClrType (prop:Property) = 
    let rec getTypeName =
      function
        | PrimaryType dataType -> 
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
        | ComplexType name -> name
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

//[<RequireQualifiedAccess>]
module CsharpGenerator =

  open CodeGeneration
  open RoslynDsl

  let jsonPropertyAttribute(propName:string) = 
    let name = parseName "JsonProperty"
    let arguments = SyntaxFactory.ParseAttributeArgumentList("(\"" + propName + "\")")
    let attribute = SyntaxFactory.Attribute(name, arguments)
    let attributeList = (new SeparatedSyntaxList<AttributeSyntax>()).Add(attribute)
    SyntaxFactory.AttributeList(attributeList)

  let callBaseConstructor varName typeName (clientName:string) =
    
    constructor clientName
    |> withModifier SyntaxKind.PublicKeyword
    |> withParameters [ parameter varName typeName ]
    |> withBody []
    |> withBaseConstructorInitializer [ argument (identifierName varName) ]

  let rec rawTypeIdentifier =
    function
    | PrimaryType dataType -> 
        match dataType with
        | DataType.String _ -> "string"
        | DataType.Number -> "float"
        | DataType.Integer -> "int"
        | DataType.Integer64 -> "long"
        | DataType.Boolean -> "bool"
        | DataType.Array propType -> 
            propType |> rawTypeIdentifier |> sprintf "IEnumerable<%s>"
        | DataType.Object -> "object"
    | ComplexType typeName -> typeName

  let isSuccess (code:HttpStatusCode) =
    let c = int32 code
    c >= 200 && c < 300

  let resolveRouteSuccessReponseType route =
    let successResponses = route.Responses |> List.filter (fun r -> r.Code |> isSuccess)
    match successResponses with
    | [] -> false, ""
    | [rs] -> 
        match rs.Type with
        | Some t -> false, rawTypeIdentifier t
        | None -> false, ""
    | responses ->
        let types =
          responses
          |> Seq.map(
               fun r ->
                match r.Type with
                | Some t -> rawTypeIdentifier t
                | None -> "Nothing" 
            )
          |> Seq.distinct
          |> Seq.toArray
        let g = System.String.Join(',', types)
        true,  sprintf "DiscriminatedUnion<%s>" g

  let generateRestMethod generateBody route =
    let discriminated,dtoType = resolveRouteSuccessReponseType route

    let request =
      declareVariableWithValue "request"
        (instanciate "HttpRequestMessage"
           [ memberAccess "HttpMethod" (ucFirst route.Verb)
             literalExpression SyntaxKind.StringLiteralExpression (SyntaxFactory.Literal route.Path) ])
    
    let types =
      if discriminated
      then
        let d = declareVariableWithValue "types" (instanciate "Dictionary<int, Type>" [])
        let codesAndTypes =
          route.Responses
            |> List.filter (fun r -> r.Code |> isSuccess)
            |> List.map(
                 fun r ->
                  let t = 
                    match r.Type with
                    | Some t -> rawTypeIdentifier t
                    | None -> "Nothing"
                  (int r.Code), t
              )
            |> List.distinctBy fst

        let dicValues =
          codesAndTypes
          |> List.map (
              fun (code, t) ->
                SyntaxFactory.ExpressionStatement(
                  memberAccess "types" "Add"
                    |> invokeMember [
                          argument (literalExpression SyntaxKind.NumericLiteralExpression (SyntaxFactory.Literal code))
                          argument (identifierName (sprintf "typeof(%s)" t))
                        ]
                  ) :> StatementSyntax
          )
        (d :> StatementSyntax) :: dicValues
      else []

    let callQueryParam (p:Parameter) =
      let name = p.Name
      let varName = identifierName p.Name
      let methodName =
        if p.ParamType.IsArray() then "AddQueryParameters" else "AddQueryParameter" 
      SyntaxFactory.ExpressionStatement(
        memberAccess "base" methodName
          |> invokeMember [
                argument (identifierName "request")
                literalExpression SyntaxKind.StringLiteralExpression (SyntaxFactory.Literal name) |> SyntaxFactory.Argument
                argument varName
              ]
        )

    let callParamMethodName methodName (p:Parameter) =
      let name = p.Name
      let varName = identifierName p.Name
      SyntaxFactory.ExpressionStatement(
        memberAccess "base" methodName
          |> invokeMember [
                argument (identifierName "request")
                literalExpression SyntaxKind.StringLiteralExpression (SyntaxFactory.Literal name) |> SyntaxFactory.Argument
                argument varName
              ]
        )
    let callPathParam = callParamMethodName "AddPathParameter"
    let callBodyParam = callParamMethodName "AddBodyParameter"
    let callCookieParam = callParamMethodName "AddCookieParameter"
    
    let execMethod =
      match dtoType with
      | _ when System.String.IsNullOrWhiteSpace dtoType ->
          identifierName "Execute" :> SimpleNameSyntax
      | _ ->
        let methodName = if discriminated then "ExecuteDiscriminated" else "Execute"
        SyntaxFactory.GenericName(SyntaxFactory.Identifier methodName)
          .WithTypeArgumentList(
            SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                    identifierName dtoType))) :> SimpleNameSyntax
      
    let retResponse =
      let args = 
        SyntaxFactory.SeparatedList<ArgumentSyntax>()
        |> addArg (SyntaxFactory.Argument(identifierName "request"))
        |> fun a -> 
             if discriminated
             then a |> addArg (SyntaxFactory.Argument(identifierName "types"))
             else a
        |> addArg (SyntaxFactory.Argument(identifierName "cancellationToken"))

      SyntaxFactory.ReturnStatement(
//        SyntaxFactory.AwaitExpression(
          SyntaxFactory.InvocationExpression(
              SyntaxFactory.MemberAccessExpression(
                  SyntaxKind.SimpleMemberAccessExpression,
                  SyntaxFactory.IdentifierName("this"),
                  execMethod)
          ).WithArgumentList(SyntaxFactory.ArgumentList(args))
          //)
      )

    let queryParams =
      route.Parameters
      |> List.choose(
           fun p -> 
            match p.Location with
            | InQuery -> Some (callQueryParam p :> StatementSyntax)
            | InPath -> Some (callPathParam p :> StatementSyntax)
            | InBody -> Some (callBodyParam p :> StatementSyntax)
            | InCookie -> Some (callCookieParam p :> StatementSyntax)
            | _ -> None
         )
    let block = 
      SyntaxFactory.Block(
          SyntaxList<StatementSyntax>()
            .Add(request)
            .AddRange(queryParams)
            .AddRange(types)
            .Add(retResponse)
      )
    
    let methodArgs =
      route.Parameters
        |> Seq.map(
             fun p -> 
                SyntaxFactory.Parameter(SyntaxFactory.Identifier p.Name).WithType(p.ParamType |> rawTypeIdentifier |> parseTypeName)
           )
        |> Seq.toArray

    let taskType = if System.String.IsNullOrWhiteSpace dtoType then "Task<Result>" else sprintf "Task<Result<%s>>" dtoType 
    let method =
      SyntaxFactory.MethodDeclaration(parseTypeName taskType, ucFirst route.OperationId)
        .WithParameterList(
          SyntaxFactory.ParameterList()
            .AddParameters(methodArgs)
            .AddParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier "cancellationToken")
              .WithType(parseTypeName "CancellationToken")
              .WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.DefaultExpression(parseTypeName "CancellationToken")))
            )
        )
    if generateBody
    then
      method
        .WithModifiers(SyntaxFactory.TokenList((SyntaxFactory.Token(SyntaxKind.PublicKeyword))))
        .WithBody(block)
    else
      method.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
  
  let generateClass (settings:GenerationSettings) (def:TypeDefinition) =
    let members = 
      def.Properties
      |> List.map (
          fun (prop:Property) ->
            let propertyDeclaration = 
              SyntaxFactory.PropertyDeclaration(getClrType prop, ucFirst prop.Name)
                  .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                  .AddAccessorListAccessors(
                      SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                      SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                  ).AddAttributeLists(jsonPropertyAttribute prop.Name)
            propertyDeclaration :> MemberDeclarationSyntax
          )
      |> List.toArray
    SyntaxFactory.ClassDeclaration(def.Name)
        .AddModifiers(SyntaxFactory.Token SyntaxKind.PublicKeyword)
        .AddMembers(members)

  let generateTagInterface (settings:GenerationSettings) (swagger:Documentation) (tag:string option) (name:string) =
    let methods = 
      swagger.Routes
      |> Seq.filter
           (fun r ->
              match tag with
              | None -> r.Tags |> List.isEmpty
              | Some t -> r.Tags |> List.contains t
           )
      |> Seq.map (generateRestMethod false)
      |> Seq.cast<MemberDeclarationSyntax>
      |> Seq.toArray
    SyntaxFactory
      .InterfaceDeclaration(name)
      .AddModifiers(SyntaxFactory.Token SyntaxKind.PublicKeyword)
      .AddMembers(methods) :> MemberDeclarationSyntax
  
  let generateClientClass (settings:GenerationSettings) (swagger:Documentation) (tag:string option) name =
    let constructors =
      [|
        callBaseConstructor "baseUrl" "string" name
        callBaseConstructor "baseUrl" "Uri" name
        callBaseConstructor "client" "HttpClient" name
      |]
    let methods = 
      swagger.Routes
      |> Seq.filter
           (fun r ->
              match tag with
              | None -> r.Tags |> List.isEmpty
              | Some t -> r.Tags |> List.contains t
           )
      |> Seq.map (generateRestMethod true)
      |> Seq.cast<MemberDeclarationSyntax>
      |> Seq.toArray
    let interfaceName = sprintf "I%s" name
    SyntaxFactory.ClassDeclaration(name)
          .AddModifiers(SyntaxFactory.Token SyntaxKind.PublicKeyword)
          .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName "RestApiClientBase"))
          .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName interfaceName))
          .AddMembers(constructors)
          .AddMembers(methods) :> MemberDeclarationSyntax
  
  let generateClientsClasses (settings:GenerationSettings) (swagger:Documentation) name =
    let clients =
      swagger.Routes
      |> List.collect (fun r -> r.Tags)
      |> List.distinct
      |> List.collect (
             fun tag ->
               let className = sprintf "%s%s" tag name
               let interfaceName = sprintf "I%s" className
               let c = generateClientClass settings swagger (Some tag) className
               let i = generateTagInterface settings swagger (Some tag) interfaceName
               [c;i]
           )
    let notTaggedRoutes =
      swagger.Routes
      |> List.filter (fun r -> r.Tags |> List.isEmpty)
    
    let clientNotTagged =
      generateClientClass settings {swagger with Routes=notTaggedRoutes} None name

    let interfaceNotTagged =
      generateTagInterface settings swagger None (sprintf "I%s" name)
      
    SyntaxFactory
        .NamespaceDeclaration(parseName settings.Namespace)
        .NormalizeWhitespace()
        .AddMembers(interfaceNotTagged :: clientNotTagged :: clients |> List.toArray)

  let generateClients (settings:GenerationSettings) (swagger:Documentation) name =
    let ns = generateClientsClasses settings swagger name
    let syntaxFactory =
      SyntaxFactory.CompilationUnit()
      |> addUsings [
          "System"
          "System.Net"
          "System.Net.Http"
          "System.Threading"
          "System.Threading.Tasks"
          "System.Collections.Generic"
          "Newtonsoft.Json"
          settings.Namespace
          "Swag4Net.RestClient.Results.DiscriminatedUnions"
          "Swag4Net.RestClient.Results"
          "Swag4Net.RestClient" ]
      |> addMembers ns
    syntaxFactory.NormalizeWhitespace().ToFullString()

  let generateDtos (settings:GenerationSettings) (defs:TypeDefinition list) =
    let classes = defs |> Seq.map (generateClass settings) |> Seq.cast<MemberDeclarationSyntax> |> Seq.toArray
    let ns = SyntaxFactory.NamespaceDeclaration(parseName settings.Namespace).NormalizeWhitespace().AddMembers(classes)
    let syntaxFactory =
      SyntaxFactory.CompilationUnit()
      |> addUsings [
          "System"
          "Newtonsoft.Json" ]
      |> addMembers ns
    syntaxFactory.NormalizeWhitespace().ToFullString()


