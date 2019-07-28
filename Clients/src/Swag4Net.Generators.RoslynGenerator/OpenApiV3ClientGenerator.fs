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

[<RequireQualifiedAccess>]
module OpenApiV3ClientGenerator =

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

  let getClrType (source:DataTypeDescription<Schema>) = 
    let rec getTypeName (t:DataTypeDescription<Schema>) =
        match t with
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
            | DataType.Array (Inlined s) -> s |> getTypeName |> sprintf "IEnumerable<%s>"
            | DataType.Array (Referenced _) -> "object"
            | DataType.Object -> "object"
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
                    //SyntaxFactory.PropertyDeclaration(clr, ucFirst name)
                    //    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    //    .AddAccessorListAccessors(
                    //        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    //        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    //    ).AddAttributeLists(jsonPropertyAttribute name)
                  //yield propertyDeclaration :> MemberDeclarationSyntax
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

  let generateClasses logError doc (schemas:(string* Schema InlinedOrReferenced) seq) =
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
                    yield t
                    for t in ts do
                      yield t
            | _, Some anyOf -> 
              let! r = generateAnyOf logError doc k anyOf
              match r with
              | Error e -> logError e
              | Ok (t,ts) ->
                  yield t
                  for t in ts do
                    yield t
            | None, None -> 
                let! r = generateBasicDto logError doc k schema
                match r with
                | Error e -> logError e
                | Ok t -> yield t

        | Referenced s ->
            failwith "not yiet implemented"
    }

  let generateDtos logError (settings:GenerationSettings) (doc:Documentation) : string =
    async {
      let classes = 
        doc.Components
        |> Option.bind (fun c -> c.Schemas)
        |> Option.defaultValue Map.empty
        |> Map.toSeq
        |> generateClasses logError doc
        |> AsyncSeq.toArray

      //let routesDtos = 
      //  doc.Paths
      //  |> Seq.choose (fun p -> p.Value.Patch |> Option.map (fun o -> o.Responses))
      //  |> Seq.collect (fun rs -> rs.Responses)
      //  |> Seq.map (fun kv -> kv.Value)
      //  |> Seq.choose (function | Inlined r -> Some r | _ -> None)
      //  |> Seq.choose (fun rs -> rs.Content)
      //  |> Seq.collect (fun v -> v |> Map.toArray |> Array.map snd)
      //  |> Seq.map (fun v -> "object", v.Schema)
      //  |> generateClasses logError doc
      //  |> AsyncSeq.toArray

      let routesDtos = 
        doc.Paths
        |> Seq.choose (fun p -> p.Value.Patch |> Option.bind (fun o -> o.RequestBody))
        |> Seq.choose (function | Inlined r -> Some r | _ -> None)
        |> Seq.map (fun r -> r.Content)
        |> Seq.collect (fun v -> v |> Map.toArray |> Array.map snd)
        |> Seq.map (fun v -> "object", v.Schema)
        |> generateClasses logError doc
        |> AsyncSeq.toArray

      let declaredClasses = 
        Array.concat [classes; routesDtos]
        |> Array.distinctBy (fun c -> c.Identifier.ValueText)
        |> Array.map (fun c -> c :> MemberDeclarationSyntax)

      let ns = SyntaxFactory.NamespaceDeclaration(parseName settings.Namespace).NormalizeWhitespace().AddMembers(declaredClasses)
      let syntaxFactory =
        SyntaxFactory.CompilationUnit()
        |> addUsings [
            "System"
            "Newtonsoft.Json" ]
        |> addMembers ns
      return syntaxFactory.NormalizeWhitespace().ToFullString()
    } |> Async.RunSynchronously

