namespace Swag4Net.Core

//https://github.com/OAI/OpenAPI-Specification/blob/master/versions/3.0.0.md#parameterIn
//https://swagger.io/docs/specification/data-models/data-types/

open Swag4Net.Core.SpecificationModel
open System
open Swag4Net.Core.Document

module DocumentModel =

  type Anchor = Anchor of string
 
  type ReferencePath =
      | ExternalUrl of Uri * Anchor option
      | RelativePath of string * Anchor option
      | InnerReference of Anchor

module Parser =

    open DocumentModel

    type ResourceProvider = ResourceProviderContext -> Result<ReferenceContent, string> Async
    and ResourceProviderContext = 
      { Document:Value
        Reference:ReferencePath }
    and ReferenceContent =
      { Name:string
        Content:Value }

    let private readString name (token:Value) =
      match token |> selectToken name with
      | Some (RawValue v) -> string v
      | _ -> String.Empty // TODO: check format

    let private readBool name (token:Value) =
      match token |> selectToken name with
      | Some (RawValue v) ->
          match v with
          | :? Boolean as b -> b
          | :? String as s -> Boolean.TryParse s |> snd
          | _ -> false // TODO: check format
      | _ -> false // TODO: check format

    let private readRefItem (provider:ResourceProvider) (doc:Value) rp =
      match rp with
      | Ok p ->
          { Document=doc; Reference=p } |> provider
      | Error e -> async { return Error e }

    //let private getRefItem provider doc (path:string) =
    //  path
    //  |> parseReference
    //  |> readRefItem provider doc
  
    let private (|IsRawValue|_|) (v:'t) =
      function
      | RawValue o ->
          match o with
          | :? 't as rv when rv = v -> Some ()
          | _ -> None
      | _ -> None

    let private parseReference (ref:string) : Result<ReferencePath, string> =
      match ref with
      | _ when String.IsNullOrWhiteSpace ref ->
          Error "ref cannot be empty"
      | _ when ref.StartsWith "#" ->
          ref.Substring 1 |> Anchor |> InnerReference |> Ok
      | _ when Uri.IsWellFormedUriString(ref, UriKind.Absolute) ->
          let uri = Uri ref
          let a = if String.IsNullOrWhiteSpace uri.Fragment then None else Some(Anchor uri.Fragment)
          ExternalUrl(Uri uri.AbsoluteUri, a) |> Ok
      | _ -> 
          match ref.IndexOf '#' with
          | -1 -> RelativePath(ref, None) |> Ok
          | i -> 
            let a = ref.Substring i
            RelativePath(ref, Some (Anchor a)) |> Ok
  
    let private parseParameterLocation (v:Value) =
      match v with
      | IsRawValue "body" -> Ok InBody
      | IsRawValue "cookie" -> Ok InCookie
      | IsRawValue "header" -> Ok InHeader
      | IsRawValue "path" -> Ok InPath
      | IsRawValue "query" -> Ok InQuery
      | IsRawValue "formData" -> Ok InFormData
      | s -> Error <| sprintf "Not supported parameter location '%A'" s

    let private parseParameterLocation' (v:Value option) =
      v |> Option.map parseParameterLocation

    type DataTypeProvider = Value -> Result<DataTypeDescription,string>

    let private parseSchema (parseDataType:DataTypeProvider) (d:Value) name =
      match d |> selectToken "properties" with
      | Some (SObject o) ->
          let properties =
            o |> Seq.choose (                  
                fun (name,value) ->
                  let enumValues =
                    match value |> selectToken "enum" with
                    | Some (SCollection a) ->
                        a
                        |> Seq.map (
                              function
                              | RawValue v when v |> isNull -> "null"
                              | RawValue v -> v.ToString()
                              | v -> v.ToString()
                            )
                        |> Seq.toList
                        |> Some
                    | _ -> None
                  match parseDataType value with
                  | Ok t ->
                      Some
                        { Name=name
                          Type=t
                          Enums=enumValues }
                  | Error e -> 
                      printfn "error: %A" e
                      None
                )
            |> Seq.toList
          Ok { Name=name
               Properties=properties }
      | _ -> Error (sprintf "invalid properties for schema %s" name)

    let rec parseDataType (spec:Value) provider (o:Value) =
  
      let (|Ref|_|) (token:Value) =
        match token |> readString "$ref" with
        | r when System.String.IsNullOrWhiteSpace r -> None
        | r -> r |> parseReference |> Some
  
      let (|IsType|_|) name (token:Value) =
        match token |> readString "type" with
        | s when s.Equals(name, StringComparison.InvariantCultureIgnoreCase) -> Some ()
        | _ -> None

      let (|IsFormat|_|) name (token:Value) =
        match token |> readString "format" with
        | s when s.Equals(name, StringComparison.InvariantCultureIgnoreCase) -> Some ()
        | _ -> None
  
      match o with
      | IsType "integer" & IsFormat "int32" -> DataType.Integer |> PrimaryType |> Ok
      | IsType "integer" & IsFormat "int64" -> DataType.Integer64 |> PrimaryType |> Ok
      | IsType "string" & IsFormat "date-time" -> DataType.String (Some StringFormat.DateTime) |> PrimaryType |> Ok
      | IsType "string" & IsFormat "date" -> DataType.String (Some StringFormat.Date) |> PrimaryType |> Ok
      | IsType "string" & IsFormat "password" -> DataType.String (Some StringFormat.Password) |> PrimaryType |> Ok
      | IsType "string" & IsFormat "byte" -> DataType.String (Some StringFormat.Base64Encoded) |> PrimaryType |> Ok
      | IsType "string" & IsFormat "binary" -> DataType.String (Some StringFormat.Binary) |> PrimaryType |> Ok
      | IsType "string" -> DataType.String None |> PrimaryType |> Ok
      | IsType "boolean" -> DataType.Boolean |> PrimaryType |> Ok
      | IsType "array" -> 
          match o |> selectToken "items" with
          | None -> Error "Could not resolve array items"
          | Some v -> v |> parseDataType spec provider |> Result.map (DataType.Array >> PrimaryType)
      | Ref ref -> 
          let r = 
            ref 
            |> readRefItem provider spec 
            |> Async.RunSynchronously
          match r with
          | Ok c ->
              let provider = parseDataType spec provider
              parseSchema provider c.Content c.Name |> Result.map ComplexType
          | Error e -> Error e
      | a -> 
          printfn "a: %A" a
          Error "Could not resolve data type"

    let private readStringList =
      function
      | Some (SCollection values) ->
          values
          |> List.choose (
               function
               | RawValue v when isNull v -> None
               | RawValue v -> Some (string v)
               | _ -> None
               )
      | _ -> []

    let private readLicense =
      Option.map (
        fun v ->
          { Name = readString "name" v
            Url = readString "url" v }
        )

    let private parseInfos model =
      let contact = model |> readString "info.contact.email"
      let license = model |> selectToken "info.license" |> readLicense
      { Description = model |> readString "info.description"
        Version = model |> readString "info.version"
        Title = model |> readString "info.title"
        TermsOfService = model |> readString "info.termsOfService"
        Contact = Some (Email contact)
        License = license }

    let private isJson (content:string) =
      if content |> String.IsNullOrWhiteSpace |> not
      then content.TrimStart().StartsWith "{"
      else false

    let private loadDocument content = if content |> isJson then fromJson content else fromYaml content

    let parseSchemas (spec:Value) http (SObject o) =
      o
      |> Seq.map (
          fun (name,v) ->
            let p = parseDataType spec http
            parseSchema p v name
        )
      |> Seq.toList

    let parseResponses (spec:Value) http (token:Value) =
      match token with
      | SObject props ->
          props
          |> Seq.choose (
              fun (name, v) ->
                let rsType = 
                  match v |> selectToken "schema" with
                  | None -> 
                      match v |> selectToken "content" with
                      | Some (SObject content) ->
                          content
                          |> Seq.choose (
                               fun (mimetype, c) ->
                                  match c |> selectToken "schema" with
                                  | Some cv -> Some (parseDataType spec http cv)
                                  | _ -> None
                             )
                          |> Seq.tryHead
                      | _ -> None
                  | Some s -> s |> parseDataType spec http |> Some
                let rsType' = match rsType with | Some (Ok v) -> Some v | _ -> None 
                match Enum.TryParse<System.Net.HttpStatusCode> name with
                | false, _ when name |> String.IsNullOrEmpty |> not && name.Equals("default", StringComparison.InvariantCultureIgnoreCase) -> 
                    Some 
                      { Code = AnyStatusCode
                        Description = v |> readString "description"
                        Type = rsType' }
                | false, _ -> None
                | true, code -> 
                    Some 
                      { Code = StatusCode code
                        Description = v |> readString "description"
                        Type = rsType' }
            ) |> Seq.toList
      | _ -> []

    let parseParameter (spec:Value) http (props:SProperty list) =
      let c = SObject props
      let typ =
        match c |> selectToken "schema" with
        | None -> parseDataType spec http c
        | Some t -> parseDataType spec http t
      let l =
        match c |> selectToken "in" |> parseParameterLocation' with
        | Some (Ok v) -> v
        | _ -> InQuery
      match typ with
      | Ok t ->
          Some
            { Location = l
              Name = c |> readString "name"
              Description = c |> readString "description"
              Deprecated = c |> readBool "deprecated"
              AllowEmptyValue = c |> readBool "allowEmptyValue"
              ParamType = t
              Required = c |> readBool "required" }
      | _ -> None

    let parseParameters (spec:Value) http (token:Value) =
      match token with
      | SObject props ->
          props
            |> Seq.choose (
                fun (n,c) ->
                  let typ =
                    match c |> selectToken "schema" with
                    | None -> parseDataType spec http c
                    | Some t -> parseDataType spec http t
                  let l =
                    match c |> selectToken "in" |> parseParameterLocation' with
                    | Some (Ok v) -> v
                    | _ -> InQuery
                  match typ with
                  | Ok t ->
                      Some
                        { Location = l
                          Name = c |> readString "name"
                          Description = c |> readString "description"
                          Deprecated = c |> readBool "deprecated"
                          AllowEmptyValue = c |> readBool "allowEmptyValue"
                          ParamType = t
                          Required = c |> readBool "required" }
                  | _ -> None
               )
            |> Seq.toList
      | SCollection items -> 
          items |> List.choose (
              function 
              | SObject props -> parseParameter spec http props
              | _ -> None
            )
      | _ -> []

    let parseRoutes provider (spec:Value) =
      spec
      |> selectToken "paths"
      |> someProperties
      |> Seq.collect(
           fun (path,value) ->
            value
            |> properties
            |> Seq.choose (
                fun (n,route) ->
                  let verb = n.ToLowerInvariant()
                  if ["get";"post";"put";"patch";"head";"delete";"options";"trace"] |> List.contains verb
                  then
                    Some
                      { Verb = verb
                        Path = path
                        Tags = route |> selectToken "tags" |> readStringList
                        Summary = route |> readString "summary"
                        Description = route |> readString "description"
                        OperationId = route |> readString "operationId"
                        Consumes = route |> selectToken "consumes" |> readStringList
                        Produces = route |> selectToken "produces" |> readStringList
                        Responses = match route |> selectToken "responses" with | Some t -> parseResponses spec provider t | None -> []
                        Parameters = match route |> selectToken "parameters" with | Some t -> parseParameters spec provider t | None -> []
                      }
                  else None
                )
         )
      |> Seq.toList

    let parseSwagger provider (content:string) =
      let spec =  content |> loadDocument
      let infos = parseInfos spec
      let definitions =
        match spec |> selectToken "definitions" with
        | Some (SObject o) ->
            parseSchemas spec provider (SObject o)
            |> List.choose (function | Ok s -> Some s | _ -> None)
        | _ -> []
  
      let routes = parseRoutes provider spec
      { Infos = infos
        Host = spec |> readString "host"
        BasePath = spec |> readString "basePath"
        Schemes = spec |> selectToken "schemes" |> readStringList
        ExternalDocs = Map.empty
        Routes = routes
        Definitions = definitions }

    let parseOpenApiV3 provider (content:string) =
      let spec = content |> fromJson
      let infos = parseInfos spec
      let routes = parseRoutes provider spec

      { Infos = infos
        Host = spec |> readString "host"
        BasePath = spec |> readString "basePath"
        Schemes = spec |> selectToken "schemes" |> readStringList
        ExternalDocs = Map.empty
        Routes = routes
        Definitions = [] }

    open Newtonsoft.Json.Linq
    open Swag4Net.Core.v3.SpecificationDocument

    let parseV2 (content:string) = 
        Ok { 
            Standard= {
                Name="swagger"
                Version="2.0" }
            Infos={
                License=None
                Contact=None
                TermsOfService=None
                Description=None
                Title="title" 
                Version="1.0" }
            Servers=None
            Paths=Map.empty
            Components=None
            Security=None
            Tags=None
            ExternalDocs=None }

    let parseV3 (content:string) = Swag4Net.Core.v3.JsonParser.parse content

    let private byStandard (callback:string -> Result<'T,string>) (content:string) =
        let json = JObject.Parse content
        if json.ContainsKey "openapi" 
            then
                if json.ContainsKey "swagger"
                    then Error "invalid specification: duplicated standard tag"
                    else callback "openapi"
            else if json.ContainsKey "swagger"
                then callback "swagger"
                else Error "unable to determine file format"

    let getStandard (content:string) =
        byStandard (fun t -> Ok t) content

    let parse (content:string) =
        let selectParser standard =
            match standard with
            | "openapi" -> parseV3 content
            | "swagger" -> parseV2 content
            | _ -> Error "unhandled standard"
        byStandard selectParser content
