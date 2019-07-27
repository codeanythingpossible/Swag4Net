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
        | "string", None -> return DataType.String None |> PrimaryType |> Ok
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

  let jsonPropertyAttribute(propName:string) = 
    let name = parseName "JsonProperty"
    let arguments = SyntaxFactory.ParseAttributeArgumentList("(\"" + propName + "\")")
    let attribute = SyntaxFactory.Attribute(name, arguments)
    let attributeList = (new SeparatedSyntaxList<AttributeSyntax>()).Add(attribute)
    SyntaxFactory.AttributeList(attributeList)

  let generateClasses logError doc (schemas:Map<string, Schema InlinedOrReferenced>) =
    asyncSeq {
      for k,schema in schemas |> Map.toSeq do
        match schema with
        | Inlined schema ->
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
                        let clr = getClrType typ
                        let propertyDeclaration = 
                          SyntaxFactory.PropertyDeclaration(clr, ucFirst name)
                              .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                              .AddAccessorListAccessors(
                                  SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                                  SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                              ).AddAttributeLists(jsonPropertyAttribute name)
                        yield propertyDeclaration :> MemberDeclarationSyntax
                    | Error e -> logError e
                  | Error e -> logError e
              } |> AsyncSeq.toArray

            let code = 
              SyntaxFactory.ClassDeclaration(k)
                .AddModifiers(SyntaxFactory.Token SyntaxKind.PublicKeyword)
                .AddMembers(members)

            match schema.Type, schema.Items with
            | "array", Some items ->
                let! items = items |> getSchema doc
                match items with
                | Error e -> logError e
                | Ok (name,_) ->
                    let clr = SyntaxFactory.ParseTypeName name
                    let enumerable = SyntaxFactory.GenericName(SyntaxFactory.Identifier "IEnumerable")
                    yield code.AddBaseListTypes(SyntaxFactory.SimpleBaseType (enumerable.AddTypeArgumentListArguments clr)) :> MemberDeclarationSyntax
            | _ ->
                yield code :> MemberDeclarationSyntax
        | Referenced s ->
            failwith "not yiet implemented"
        ()
    }

  let generateDtos logError (settings:GenerationSettings) (doc:Documentation) : string =
    let classes = 
      doc.Components
      |> Option.bind (fun c -> c.Schemas)
      |> Option.defaultValue Map.empty
      |> generateClasses logError doc
      |> AsyncSeq.toArray

    let ns = SyntaxFactory.NamespaceDeclaration(parseName settings.Namespace).NormalizeWhitespace().AddMembers(classes)
    let syntaxFactory =
      SyntaxFactory.CompilationUnit()
      |> addUsings [
          "System"
          "Newtonsoft.Json" ]
      |> addMembers ns
    syntaxFactory.NormalizeWhitespace().ToFullString()

