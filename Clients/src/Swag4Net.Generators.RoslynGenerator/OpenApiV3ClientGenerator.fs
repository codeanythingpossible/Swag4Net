namespace Swag4Net.Generators.RoslynGenerator

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open Swag4Net.Core
open Swag4Net.Core.Domain
open SharedKernel
open OpenApiSpecification
open RoslynDsl
open FSharp.Control
open System.Collections.Generic

[<RequireQualifiedAccess>]
module OpenApiV3ClientGenerator =

  type ResourceProviders =
    { SchemaProvider : ResourceProvider<Documentation, Schema>
      ParameterProvider : ResourceProvider<Documentation, Parameter>
      ResponseProvider : ResourceProvider<Documentation, Response> }

  let getSchema (doc:Documentation) (schema:Schema InlinedOrReferenced) =
    async {
      match schema with
      | Inlined s -> return Ok (s.Type, s)
      | Referenced (InnerReference ref) ->
          match Anchor.split ref with
          | "components" :: "schemas" :: name :: _ -> 
            match doc.Components with
            | None -> return Error (sprintf "Cannot find reference %A" ref)
            | Some c ->
                match c.Schemas with
                | None -> return Error (sprintf "Cannot find reference %A" ref)
                | Some s -> 
                    match s.TryGetValue name with
                    | false, _ -> return Error (sprintf "Cannot find reference %A" ref)
                    | true, n ->
                        match n with
                        | Inlined s -> return Ok (name, s)
                        | _ -> return Error (sprintf "Cannot find reference %A" ref)
          | _ -> return Error (sprintf "Cannot find reference %A" ref)
      | _ -> return Error (sprintf "Cannot find reference %A" ref)
    }

  let getResponse (doc:Documentation) (query:Response InlinedOrReferenced) =
    async {
      match query with
      | Inlined s -> return Ok s
      | Referenced (InnerReference ref) ->
          match Anchor.split ref with
          | "components" :: "responses" :: name :: _ -> 
            match doc.Components with
            | None -> return Error (sprintf "Cannot find reference %A" ref)
            | Some c ->
                match c.Responses with
                | None -> return Error (sprintf "Cannot find reference %A" ref)
                | Some s -> 
                    match s.TryGetValue name with
                    | false, _ -> return Error (sprintf "Cannot find reference %A" ref)
                    | true, n ->
                        match n with
                        | Inlined rs -> return Ok rs
                        | _ -> return Error (sprintf "Cannot find reference %A" ref)
          | _ -> return Error (sprintf "Cannot find reference %A" ref)
      | _ -> return Error (sprintf "Cannot find reference %A" ref)
    }

  let nameSchema (s:Schema InlinedOrReferenced) =
    match s with
    | Referenced (InnerReference (Anchor a)) -> 
        let name = a.Split '/' |> Seq.last
        name, s
    | Referenced(ExternalUrl (_, Some (Anchor a))) ->
        let name = a.Split '/' |> Seq.last
        name, s
    | Referenced(ExternalUrl (uri, None)) ->
        let name = uri.Segments |> Seq.last
        name, s
    | Referenced(RelativePath (_, Some (Anchor a))) ->
        let name = a.Split '/' |> Seq.last
        name, s
    | Referenced(RelativePath (path, None)) ->
        let name = path.Split '/' |> Seq.last
        name, s
    | Inlined v ->
        v.Title, s

  let toDataTypeDescription doc (source:Schema) =
    let rec transorm (schema:Schema) =
      async {
        match schema.Type, schema.Format with
        | "integer", None -> return DataType.Integer |> PrimaryType |> Ok
        | "integer", Some "int32" -> return DataType.Integer |> PrimaryType |> Ok
        | "integer", Some "int64" -> return DataType.Integer64 |> PrimaryType |> Ok
        | "string", Some "date-time" -> return DataType.String (Some StringFormat.DateTime) |> PrimaryType |> Ok
        | "string", Some "date" -> return DataType.String (Some StringFormat.Date) |> PrimaryType |> Ok
        | "string", Some "password" -> return DataType.String (Some StringFormat.Password) |> PrimaryType |> Ok
        | "string", Some "byte" -> return DataType.String (Some StringFormat.Base64Encoded) |> PrimaryType |> Ok
        | "string", Some "binary" -> return DataType.String (Some StringFormat.Binary) |> PrimaryType |> Ok
        | "string", _ -> return DataType.String None |> PrimaryType |> Ok
        | "boolean", _ -> return DataType.Boolean |> PrimaryType |> Ok
        | "object", _ -> return DataType.Object |> PrimaryType |> Ok
        | "array",_ -> 
            match schema.Items with
            | None ->
              return 
                DataType.Object
                |> PrimaryType
                |> Inlined
                |> DataType<Schema>.Array
                |> PrimaryType
                |> Ok
            | Some ref -> 
                let! s = getSchema doc ref
                match s with
                | Ok (_,s) ->
                    let! r = transorm s
                    return 
                      r |> Result.map (
                          fun i ->
                            i
                            |> Inlined
                            |> DataType<Schema>.Array
                            |> PrimaryType
                          )
                | Error e -> return Error e
        | _ -> 
            let message = 
              match schema.Format with
              | None -> sprintf "cannot reconize type '%s'" schema.Type
              | Some format -> sprintf "cannot reconize type '%s' with format '%s'" schema.Type format
            return Error message
      }
    transorm source

  let getClrType (* resourceProvider:ResourceProvider<Documentation, Schema> *) (source:DataTypeDescription<Schema>) = 
    let rec getTypeName (t:DataTypeDescription<Schema>) =
        match t with
        | PrimaryType dataType -> 
            match dataType with
            | DataType.String (Some StringFormat.Date) -> "DateTime"
            | DataType.String (Some StringFormat.DateTime) -> "DateTime"
            | DataType.String (Some StringFormat.Base64Encoded) -> "string"
            | DataType.String (Some StringFormat.Binary) -> "byte[]"
            | DataType.String (Some StringFormat.Password) -> "string"
            | DataType.String _ -> "string"
            | DataType.Number -> "float"
            | DataType.Integer -> "int"
            | DataType.Integer64 -> "long"
            | DataType.Boolean -> "bool"
            | DataType.Array (Inlined s) -> s |> getTypeName |> sprintf "IEnumerable<%s>"
            | DataType.Array (Referenced r) -> 
                "object"
            | DataType.Object -> 
                "object"
        | ComplexType s -> s.Type
    source |> getTypeName |> SyntaxFactory.ParseTypeName

  let generateBasicDto logError doc (name:string) (schema:Schema) =
    async {
      let props =
        schema.Properties
        |> Option.defaultValue Map.empty
        |> Map.toSeq
      
      let members = 
        asyncSeq {
          for (name,ps) in props do
            let! ps = ps |> getSchema doc
            match ps with
            | Ok (_,v) -> 
              let! typ = toDataTypeDescription doc v
              match typ with
              | Ok typ ->
                  let clrType = getClrType typ
                  yield autoProperty clrType name :> MemberDeclarationSyntax
              | Error e -> logError e
            | Error e -> logError e
        } |> AsyncSeq.toArray

      let code = publicClass (cleanTypeName name) members

      match schema.Type, schema.Items with
      | "array", Some items ->
          let! items = items |> getSchema doc
          match items with
          | Error e -> return Error e
          | Ok (name,_) ->
              let clr = SyntaxFactory.ParseTypeName (if System.String.IsNullOrWhiteSpace name then "object" else name)
              let enumerable = SyntaxFactory.GenericName(SyntaxFactory.Identifier "IEnumerable")
              return Ok (code.AddBaseListTypes(SyntaxFactory.SimpleBaseType (enumerable.AddTypeArgumentListArguments clr)))
      | _ ->
          return Ok code
  }

  let generateOneOf logError doc (name:string) (schemas:Schema InlinedOrReferenced list) =
    async {
      let code = publicClass name Array.empty
      
      let! types = 
        asyncSeq {
          for i in 0 .. schemas.Length-1 do
            let! schema = schemas.Item i |> getSchema doc
            match schema with
            | Error e -> logError e
            | Ok (n,schema) ->
                let! dto = generateBasicDto logError doc n schema
                match dto with
                | Error e -> logError e
                | Ok c ->
                    if c.Identifier.ValueText.Equals("object", System.StringComparison.InvariantCultureIgnoreCase)
                    then yield c.WithIdentifier (SyntaxFactory.Identifier (sprintf "%sChoice%d" name i))
                    else yield c
        } |> AsyncSeq.toArrayAsync
      
      let genericArgs = 
        types |> Array.map (fun t -> SyntaxFactory.ParseTypeName t.Identifier.ValueText)
        
      let discriminatedUnion = 
        SyntaxFactory
          .GenericName(SyntaxFactory.Identifier "DiscriminatedUnion")
          .AddTypeArgumentListArguments genericArgs
      
      let c = code.AddBaseListTypes(SyntaxFactory.SimpleBaseType discriminatedUnion)

      return Ok (c, types)
    }

  let generateAnyOf logError doc (name:string) (schemas:Schema InlinedOrReferenced list) =
    async {
      let code = publicClass name Array.empty
      
      let! props = 
        asyncSeq {
          for i in 0 .. schemas.Length-1 do
            let! schema = schemas.Item i |> getSchema doc
            match schema with
            | Error e -> logError e
            | Ok (n,schema) ->
                let! dto = generateBasicDto logError doc n schema
                match dto with
                | Error e -> logError e
                | Ok c ->
                    if c.Identifier.ValueText.Equals("object", System.StringComparison.InvariantCultureIgnoreCase)
                    then yield n,c.WithIdentifier (SyntaxFactory.Identifier (sprintf "%sChoice%d" name i))
                    else yield n,c
        } |> AsyncSeq.toArrayAsync
        
      let properties =
        seq {
          for (name, p) in props |> Seq.distinctBy fst do
            let clrType = SyntaxFactory.ParseTypeName p.Identifier.ValueText
            yield autoProperty clrType name :> MemberDeclarationSyntax
        } |> Seq.toArray

      let c = code.AddMembers properties

      return Ok (c, props |> Array.map snd)
    }

  let generateClasses logError doc (providers : ResourceProviders) (schemas:(string* Schema InlinedOrReferenced) seq) =
    asyncSeq {
      for k,schema in schemas do
        match schema with
        | Inlined schema ->

            match schema.OneOf, schema.AnyOf with
            | Some oneOf, _ -> 
                let! r = generateOneOf logError doc k oneOf
                match r with
                | Error e -> logError e
                | Ok (t,ts) ->
                    yield schema,t
                    for t in ts do
                      yield schema,t
            | _, Some anyOf -> 
              let! r = generateAnyOf logError doc k anyOf
              match r with
              | Error e -> logError e
              | Ok (t,ts) ->
                  yield schema,t
                  for t in ts do
                    yield schema,t
            | None, None -> 
                let! r = generateBasicDto logError doc k schema
                match r with
                | Error e -> logError e
                | Ok t -> yield schema,t

        | Referenced ref ->
            let! r = ResourceProviderContext.Create doc ref |> providers.SchemaProvider
            match r with
            | Error e -> logError e
            | Ok content ->
                let schema = 
                  match content.Content.Title with 
                  | n when n.Equals("object", System.StringComparison.OrdinalIgnoreCase) -> { content.Content with Title=content.Name }
                  | n when System.String.IsNullOrWhiteSpace n -> { content.Content with Title=content.Name }
                  | _ -> content.Content

                let! c = generateBasicDto logError doc k schema
                match c with
                | Error e -> logError e
                | Ok t -> yield schema,t
    }

  let buildSchemasClasses logError (settings:GenerationSettings) (doc:Documentation) (providers : ResourceProviders) =
    async {
      let schemas = 
        doc.Components
        |> Option.bind (fun c -> c.Schemas)
        |> Option.defaultValue Map.empty
        |> Map.toSeq
        |> generateClasses logError doc providers
        |> AsyncSeq.toList

      let responses = 
        asyncSeq {
          let tasks = 
            doc.Components
            |> Option.bind (fun c -> c.Responses)
            |> Option.defaultValue Map.empty
            |> Map.toSeq
            |> Seq.map (fun (k,v) -> k, getResponse doc v)
          
          for (name,t) in tasks do
            let! r = t
            match r with
            | Error e -> logError e
            | Ok rs -> 
              for (_,v) in rs.Content |> Map.toSeq do
                yield name,v.Schema
        } |> AsyncSeq.toBlockingSeq
          |> generateClasses logError doc providers
          |> AsyncSeq.toList

      let classes = schemas @ responses |> List.toArray

      let payload (f : KeyValuePair<string, Path> -> Operation option) =
        let rq = 
          doc.Paths
          |> Seq.choose (fun p -> f p |> Option.bind (fun o -> o.RequestBody))
          |> Seq.choose (
               function
               | Inlined r -> r.Content |> Map.toArray |> Array.map snd |> Some
               | _ -> None
             )
          |> Seq.collect id
          |> Seq.toList

        let d =
          doc.Paths
          |> Seq.choose (fun p -> f p |> Option.bind (fun o -> o.Responses.Default))
          |> Seq.choose (function | Inlined r -> Some r | _ -> None)
          |> Seq.map (fun r -> r.Content |> Map.toArray |> Array.map snd)
          |> Seq.collect id
          |> Seq.toList

        rq @ d
        
      let payloads = 
        [ payload (fun p -> p.Value.Delete)
          payload (fun p -> p.Value.Get)
          payload (fun p -> p.Value.Head)
          payload (fun p -> p.Value.Options)
          payload (fun p -> p.Value.Patch)
          payload (fun p -> p.Value.Post)
          payload (fun p -> p.Value.Put)
          payload (fun p -> p.Value.Trace) ] |> Seq.collect id |> Seq.toList

      let routesDtos = 
        payloads
        |> Seq.map (fun v -> nameSchema v.Schema)
        |> generateClasses logError doc providers
        |> AsyncSeq.toArray

      return Array.concat [classes; routesDtos] |> dict
    }

  let generateDtos logError (settings:GenerationSettings) (doc:Documentation) (providers : ResourceProviders) : string =
    async {
      let! builtSchemas = buildSchemasClasses logError settings doc providers
      
      let declaredClasses = 
          builtSchemas.Values
          |> Seq.distinctBy (fun c -> c.Identifier.ValueText)
          |> Seq.map (fun c -> c :> MemberDeclarationSyntax)
          |> Seq.toArray

      let ns = SyntaxFactory.NamespaceDeclaration(parseName settings.Namespace).NormalizeWhitespace().AddMembers(declaredClasses)
      let syntaxFactory =
        SyntaxFactory.CompilationUnit()
        |> addUsings [
            "System"
            "Newtonsoft.Json" ]
        |> addMembers ns
      return syntaxFactory.NormalizeWhitespace().ToFullString()
    } |> Async.RunSynchronously


  let rec rawTypeIdentifier : DataTypeDescription<Schema> -> string =
    function
    | PrimaryType dataType -> 
        match dataType with
        | DataType.String _ -> "string"
        | DataType.Number -> "float"
        | DataType.Integer -> "int"
        | DataType.Integer64 -> "long"
        | DataType.Boolean -> "bool"
        | DataType.Array (Inlined propType) -> propType |> rawTypeIdentifier |> sprintf "IEnumerable<%s>"
        | DataType.Array (Referenced ref) -> "object"
        | DataType.Object -> "object"
    | ComplexType s -> s.Title

  let isSuccess c =
    c >= 200 && c < 300

  let resolveRouteSuccessReponseType (route:Operation) doc (providers : ResourceProviders) =
    let responses = 
      //route.Responses.Default // TODO: default response
      route.Responses.Responses
      |> Seq.filter(fun i -> i.Key |> isSuccess)
      |> Seq.choose (
            fun i -> 
              match i.Value with
              | Inlined s -> 
                  let name = 
                    s.Content
                    |> Seq.tryHead
                    |> Option.bind (
                        fun s -> 
                          match s.Value.Schema with
                          | Inlined schema ->
                              schema |> ComplexType |> rawTypeIdentifier |> Some
                          | Referenced ref ->
                              let r = ResourceProviderContext.Create doc ref |> providers.SchemaProvider |> Async.RunSynchronously
                              match r with
                              | Ok schema -> Some schema.Name
                              | Error _ -> None
                          )
                  match name with
                  | None -> Some "Nothing"
                  | Some n -> Some n
              | Referenced ref ->
                  let getName a = a |> Anchor.split |> List.last |> cleanTypeName |> Some
                  match ref with
                  | ExternalUrl (_, Some a) -> getName a
                  | RelativePath (_, Some a) -> getName a
                  | InnerReference a -> 
                      getName a
                  | _ -> None
        )
      |> Seq.distinct
      |> Seq.toList

    match responses with
    | [] -> false, ""
    | ["Nothing"] -> false, ""
    | [rs] -> false, rs
    | types ->
        let g = System.String.Join(',', types)
        true,  sprintf "DiscriminatedUnion<%s>" g

  let generateRestMethod doc generateBody verb (path:string) (route:Operation) (builtSchemas:IDictionary<Schema, ClassDeclarationSyntax>) (providers : ResourceProviders) (pathParams:Parameter InlinedOrReferenced list) =
    let discriminated,dtoType = resolveRouteSuccessReponseType route doc providers
    
    let request =
      declareVariableWithValue "request"
        (instanciate "HttpRequestMessage"
           [ memberAccess "HttpMethod" (ucFirst verb)
             literalExpression SyntaxKind.StringLiteralExpression (SyntaxFactory.Literal path) ])
  
    let (|NameFromRef|_|) ref = 
      let getName a = 
        a |> Anchor.split |> List.last |> cleanTypeName  |> Some
      match ref with
      | ExternalUrl (_, Some a) -> a |> getName
      | RelativePath (_, Some a) -> getName a
      | InnerReference a -> getName a
      | _ -> None

    let types =
      if discriminated
      then
        let d = declareVariableWithValue "types" (instanciate "Dictionary<int, Type>" [])
        let codesAndTypes : (int * string) list =
          route.Responses.Responses
            |> Map.toList
            |> List.filter (fun (code,r) -> code |> isSuccess)
            |> List.map (
                 fun (code,r) ->
                  match r with
                  | Inlined rs ->
                      // TODO: manage multiples mimetypes
                      // rawTypeIdentifier
                      let media = rs.Content |> Map.toArray |> Array.map snd |> Array.head
                      match media.Schema with
                      | Inlined schema -> 
                          code, schema.Title
                      | Referenced (NameFromRef name) -> code, name
                      | _ -> code, "Nothing"
                  | Referenced (NameFromRef name) -> code, name
                  | _ -> code, "Nothing"
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
      
    let callQueryParam (param:Parameter) =
      let name = param.Name |> cleanVarName
      let varName = identifierName name
      let methodName = (* if param.ParamType.IsArray() then "AddQueryParameters" else *) "AddQueryParameter"
      SyntaxFactory.ExpressionStatement(
        memberAccess "base" methodName
          |> invokeMember [
                argument (identifierName "request")
                literalExpression SyntaxKind.StringLiteralExpression (SyntaxFactory.Literal name) |> SyntaxFactory.Argument
                argument varName
              ]
        )

    let callParamMethodName methodName (p:Parameter) =
      let name = p.Name |> cleanVarName
      let varName = identifierName name
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
    let callHeaderParam = callParamMethodName "AddHeaderParameter"
    let callFormDataParam = callParamMethodName "AddFormDataParameter"
    
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
          SyntaxFactory.InvocationExpression(
              SyntaxFactory.MemberAccessExpression(
                  SyntaxKind.SimpleMemberAccessExpression,
                  SyntaxFactory.IdentifierName("this"),
                  execMethod)
          ).WithArgumentList(SyntaxFactory.ArgumentList(args))
      )

    let handleParamInBody (p:Parameter) =
      match p.In with
      | InQuery -> Some (callQueryParam p :> StatementSyntax)
      | InPath -> Some (callPathParam p :> StatementSyntax)
      | InBody _ -> Some (callBodyParam p :> StatementSyntax)
      | InCookie -> Some (callCookieParam p :> StatementSyntax)
      | InHeader -> Some (callHeaderParam p :> StatementSyntax)
      | InFormData -> Some (callFormDataParam p :> StatementSyntax)

    let queryParams =
      pathParams @ route.Parameters
      |> List.choose(
            fun p -> 
              match p with
              | Inlined p -> handleParamInBody p
              | Referenced ref ->
                  let r = ResourceProviderContext.Create doc ref |> providers.ParameterProvider |> Async.RunSynchronously
                  match r with
                  | Ok content -> handleParamInBody content.Content
                  | Error _ -> None
          )
    let block = 
      SyntaxFactory.Block(
          SyntaxList<StatementSyntax>()
            .Add(request)
            .AddRange(queryParams)
            .AddRange(types)
            .Add(retResponse)
      )
      
    let handleParam (p:Parameter) = 
      match p.Schema with
      | Some (Inlined s) when builtSchemas.ContainsKey s ->
          let c = builtSchemas.Item s
          (parameterNamed p.Name).WithType(c.Identifier.ValueText |> parseTypeName) |> Some
      | Some (Inlined s) ->
          match toDataTypeDescription doc s |> Async.RunSynchronously with
          | Ok r ->
              (parameterNamed p.Name).WithType(getClrType r) |> Some
          | Error e -> None
      | _ -> None

    let methodArgs =
      pathParams @ route.Parameters
        |> Seq.choose (
              fun p -> 
                match p with
                | Inlined p -> handleParam p
                | Referenced ref ->
                    let r = ResourceProviderContext.Create doc ref |> providers.ParameterProvider |> Async.RunSynchronously
                    match r with
                    | Ok content -> handleParam content.Content
                    | Error _ -> None
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

  let generateClientClass (settings:GenerationSettings) (tag:string option) (doc:Documentation) name (builtSchemas:IDictionary<Schema, ClassDeclarationSyntax>) (providers : ResourceProviders) =
    let constructors =
      [|
        callBaseConstructor "baseUrl" "string" name
        callBaseConstructor "baseUrl" "Uri" name
        callBaseConstructor "client" "HttpClient" name
      |]
    
    let operations =
      doc.Paths
      |> Seq.collect
           (fun r ->
              [
                r.Value.Get |> Option.map (fun o -> r.Key, "GET", o, r.Value.Parameters)
                r.Value.Post |> Option.map (fun o -> r.Key,"POST", o, r.Value.Parameters)
                r.Value.Delete |> Option.map (fun o -> r.Key,"DELETE", o, r.Value.Parameters)
                r.Value.Head |> Option.map (fun o -> r.Key, "HEAD", o, r.Value.Parameters)
                r.Value.Put |> Option.map (fun o -> r.Key, "PUT", o, r.Value.Parameters)
                r.Value.Patch |> Option.map (fun o -> r.Key,"PATCH", o, r.Value.Parameters)
                r.Value.Options |> Option.map (fun o -> r.Key, "OPTIONS", o, r.Value.Parameters)
              ] |> Seq.choose id
           )
      |> Seq.choose (
            fun i ->
              match tag, i with
              | Some tag, (_,_,op,_) -> 
                  if op.Tags |> List.contains tag then Some i else None
              | None, (_,_,op,_) -> 
                  if op.Tags |> List.isEmpty then Some i else None
              | _ -> None
              )
    let methods =
      operations
      |> Seq.map (fun (path,verb,operation,pathParams) -> generateRestMethod doc true verb path operation builtSchemas providers pathParams)
      |> Seq.cast<MemberDeclarationSyntax>
      |> Seq.toArray

    let interfaceName = sprintf "I%s" name
    SyntaxFactory.ClassDeclaration(name)
          .AddModifiers(SyntaxFactory.Token SyntaxKind.PublicKeyword)
          .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName "RestApiClientBase"))
          .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName interfaceName))
          .AddMembers(constructors)
          .AddMembers(methods)

  let isEmptyClass (c:ClassDeclarationSyntax) =
      c.Members
      |> Seq.choose (
          function
          | :? MethodDeclarationSyntax as m when m.Modifiers |> Seq.exists(fun t -> t.IsKind SyntaxKind.PublicKeyword) -> 
              SyntaxFactory
                .MethodDeclaration(m.ReturnType, m.Identifier)
                .WithConstraintClauses(m.ConstraintClauses)
                .WithParameterList(m.ParameterList)
                .WithSemicolonToken(SyntaxFactory.Token SyntaxKind.SemicolonToken)
              |> Some
          | _ -> None
        )
      |> Seq.cast<MemberDeclarationSyntax>
      |> Seq.isEmpty

  let extractInterface (c:ClassDeclarationSyntax) =
    let methods = 
      c.Members
      |> Seq.choose (
          function
          | :? MethodDeclarationSyntax as m when m.Modifiers |> Seq.exists(fun t -> t.IsKind SyntaxKind.PublicKeyword) -> 
              SyntaxFactory
                .MethodDeclaration(m.ReturnType, m.Identifier)
                .WithConstraintClauses(m.ConstraintClauses)
                .WithParameterList(m.ParameterList)
                .WithSemicolonToken(SyntaxFactory.Token SyntaxKind.SemicolonToken)
              |> Some
          | _ -> None
        )
      |> Seq.cast<MemberDeclarationSyntax>
      |> Seq.toArray
    let interfaceName = sprintf "I%s" (c.Identifier.ToFullString())
    SyntaxFactory
      .InterfaceDeclaration(interfaceName)
      .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token SyntaxKind.PublicKeyword))
      .AddMembers(methods)
    

  let generateClientsClasses (settings:GenerationSettings) (doc:Documentation) name logError (providers : ResourceProviders) =
    async {
      let! builtSchemas = buildSchemasClasses logError settings doc providers

      let routes = doc.Paths |> Map.toList
    
      let createTaggedClients (fs : (Path -> Operation option) list) =
        fs
        |> List.collect (fun f -> routes |> List.choose (fun (_,r) -> f r))
        |> List.collect (fun op -> op.Tags)
        |> List.distinct
        |> List.collect (
               fun tag ->
                 let className = cleanTypeName <| sprintf "%s%s" tag name
                 let c = generateClientClass settings (Some tag) doc className builtSchemas providers
                 let i = extractInterface c
                 [c:> MemberDeclarationSyntax; i:> MemberDeclarationSyntax]
             )

      let createNotTaggedClients () =

        let className = cleanTypeName name
        let c = generateClientClass settings None doc className builtSchemas providers
        if isEmptyClass c
        then
          List.empty
        else
          let i = extractInterface c
          [c:> MemberDeclarationSyntax; i:> MemberDeclarationSyntax]

      let clients =
        createTaggedClients [
          fun p -> p.Get
          fun p -> p.Post
          fun p -> p.Delete
          fun p -> p.Head
          fun p -> p.Put
          fun p -> p.Patch
          fun p -> p.Options
        ]
        
      let clientNotTagged = createNotTaggedClients ()

      let code = SyntaxFactory
                  .NamespaceDeclaration(parseName settings.Namespace)
                  .NormalizeWhitespace()
                  .AddMembers(clientNotTagged @ clients |> List.toArray)
      return code
    }
    
  let generateClients logError (settings:GenerationSettings) (doc:Documentation) (providers : ResourceProviders) name : string =
    async {
      let! ns = generateClientsClasses settings doc (ucFirst name) logError providers
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
      
      return syntaxFactory.NormalizeWhitespace().ToFullString()
    } |> Async.RunSynchronously


