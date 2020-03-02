namespace Swag4Net.Core.v2

module SwaggerParser =

  open System
  open Swag4Net.Core
  open Document
  open Swag4Net.Core.Domain
  open SharedKernel
  open SwaggerSpecification

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

  let private readRefItem (provider:ResourceProvider<Value,Value>) (doc:Value) rp =
    match rp with
    | Ok p ->
        { Document=doc; Reference=p } |> provider
    | Error e -> async { return Error e }

  let private parseParameterLocation' (v:Value option) =
    v |> Option.map Helpers.readParameterLocation

  type DataTypeProvider = Value -> Result<DataTypeDescription<Schema>,string>

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
                | Error e -> None
              )
          |> Seq.toList
        Ok { Name=name
             Properties=properties }
    | _ -> Error (sprintf "invalid properties for schema %s" name)

  let resolveRefName (path:string) =
    path.Split '/' |> Seq.last

  let rec parseDataType (spec:Value) provider (o:Value) : Result<DataTypeDescription<Schema>,string> =

    let (|Ref|_|) (token:Value) =
      match token |> readString "$ref" with
      | r when System.String.IsNullOrWhiteSpace r -> None
      | r -> r |> ReferencePath.parseReference |> Some

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
        | Some v -> v |> parseDataType spec provider |> Result.map (fun o -> DataType<Schema>.Array (Inlined o) |> PrimaryType)
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
    | a -> Error "Could not resolve data type"

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

  let parseParameter (spec:Value) http (node:Value) =
    let typ =
      match node |> selectToken "schema" with
      | None -> parseDataType spec http node
      | Some t -> parseDataType spec http t
    let l =
      match node |> selectToken "in" |> parseParameterLocation' with
      | Some (Ok v) -> v
      | _ -> InQuery
    match typ with
    | Ok t ->
        Some
          { Location = l
            Name = node |> readString "name"
            Description = node |> readString "description"
            Deprecated = node |> readBool "deprecated"
            AllowEmptyValue = node |> readBool "allowEmptyValue"
            ParamType = t
            Required = node |> readBool "required" }
    | _ -> None

  let parseParameters (spec:Value) http (token:Value) =
    match token with
    | SObject props ->
        props
          |> Seq.choose (
              fun (_,c) ->
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
            | XObject _ as o -> parseParameter spec http o
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

  let loadDocument content = if content |> isJson then fromJson content else fromYaml content

  let parseSwagger provider (content:string) =
    let spec =  content |> loadDocument
    let infos = parseInfos spec
    let definitions =
      match spec |> selectToken "definitions" with
      | Some (XObject _ as o) ->
          parseSchemas spec provider o
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
  
