module Swag4Net.Core.v3.Parser

open System
open Swag4Net.Core
open Swag4Net.Core.Domain
open SharedKernel
open OpenApiSpecification
open Parsing
open Document

let parseLicense doc =
  parsing {
    match doc |> selectToken "info.license" with
    | None -> return None
    | Some node ->
        let! licenseName = doc |> readString "info.license.name"
        return Some 
          { Name = licenseName
            Url = node |> readStringOption "url" }
  }

let parseInfos doc : ParsingState<Infos> = 
  parsing {

    let contact =
      doc
      |> selectToken "info.contact"
      |> Option.map (
           fun node ->
             { Name = node |> readStringOption "name"
               Url = node |> readStringOption "url"
               Email = node |> readStringOption "email" })
    
    let! license = parseLicense doc
    let! version = doc |> readString "info.version"
    let! title = doc |> readString "info.title"

    return 
      { Description = doc |> readStringOption "info.description"
        Version = version
        Title = title
        TermsOfService = doc |> readStringOption "info.termsOfService"
        Contact = contact
        License = license }
  }

let readStringArray =
  function
  | SCollection values -> 
      values
      |> List.choose (
          fun item ->
            match item with
            | RawValue v -> Some (v.ToString()) 
            | _ -> None
        )
      |> Some
  | _ -> None

let (|IsEnum|_|) =
  function
  | SObject props -> 
        props
        |> List.choose (
              function
              | ("enum", v) -> readStringArray v
              | _ -> None
            )
        |> List.tryHead
  | _ -> None

let serverVariable node : ParsingState<ServerVariable> =
  parsing {
    let description = node |> readStringOption "description" |> Option.defaultValue ""
    let! defaultValue = node |> readString "default"
    
    let (variable:ServerVariable) =
      match node with
      | IsEnum enumValues -> 
          { Enum = Some enumValues
            Default = defaultValue
            Description = description }
      | _ -> 
          { Enum = None
            Default = defaultValue
            Description = description }

    return variable
  }

let serverVariables node : ParsingState<Map<string, ServerVariable> option> =
  parsing {
    let variables = 
      node 
      |> selectToken "variables"
      |> Option.bind (
            function
            | SObject props -> 
                props
                |> List.choose (
                     fun (name, value) -> 
                       let v = serverVariable value
                       match v.Result with
                       | Ok o -> Some (name, o)
                       | _ -> None
                   )
                |> Map
                |> fun m -> if m |> Map.isEmpty then None else Some m
            | _ -> None
          )
    return variables
  }

let parseServer node : ParsingState<Server> =
  parsing {
    let! url = node |> readString "url"
    let description = node |> readStringOption "description"
    let! variables = node |> serverVariables

    return {
      Url=url
      Description=description
      Variables=variables }
  }

let parseServers node : ParsingState<Server list> =
  parsing {
    let! servers =
      match node |> selectToken "servers" with
      | Some (SCollection items) -> items |> List.map parseServer
      | o -> [sprintf "invalid %A" o |> InvalidFormat |> ParsingState.FailureOf]
    
    return servers
  }

let parseInlinedOrReferenced f node : ParsingState<'T InlinedOrReferenced> =
  parsing {
    let! r =
        match node with
        | SObject ["$ref", RawValue link] ->
            match link |> string |> ReferencePath.parseReference with
            | Ok r -> ParsingState.success (Referenced r)
            | Error e -> ParsingState.FailureOf  <| InvalidFormat e
        | SObject _ as v -> 
            v |> f |> ParsingState.map Inlined
        | _ -> ParsingState.FailureOf <| InvalidFormat "Could not determine if value is $ref or object"
    return r
  }

let parseOptionalInlinedOrReferenced f node  : ParsingState<'T InlinedOrReferenced option> =
  match node with
  | Some (SObject ["$ref", RawValue link]) ->
      let result =
        if link |> isNull
        then None
        else
          match link |> string |> ReferencePath.parseReference with
          | Ok r -> Some (Referenced r)
          | Error _ -> None
      ParsingState.success result
  | Some v -> v |> f |> ParsingState.map (Inlined >> Some)
  | _ -> None |> ParsingState.success

let rec parseSchema node =
  parsing {
    let! r =
        match node with
        | XObject _ as o ->
            parsing {
              let typ = o |> readStringOption "type" |> Option.defaultValue ""

              let! properties =
                parsing {
                  match o |> selectToken "properties" with
                  | Some (SObject props) -> 
                      let! r =
                        props
                        |> List.map (
                            fun (name, v) -> 
                              let schema = parseInlinedOrReferenced parseSchema v
                              schema |> ParsingState.map (fun s -> name,s)
                            )
                      return Some (Map r)
                  | _ -> return None
                }

              let parseInheritance name =
                match node |> selectToken name with
                | Some(SObject _  as n) ->
                    n |> parseInlinedOrReferenced parseSchema |> ParsingState.map (fun s -> Some [s])
                | Some(SCollection items) ->
                    parsing {
                      let! b = items |> List.map (parseInlinedOrReferenced parseSchema)
                      return Some b
                    }
                | _ -> ParsingState.success None
                
              let! items =
                node |> selectToken "items" |> parseOptionalInlinedOrReferenced parseSchema

              let! allOf = parseInheritance "allOf"
              let! oneOf = parseInheritance "oneOf"
              let! anyOf = parseInheritance "anyOf"
              let! ``not`` = parseInheritance "not"
              let! multipleOf = parseInheritance "multipleOf"

              let! externalDocs = 
                parsing {
                  match o |> selectToken "externalDocs" with
                  | None -> return None
                  | Some _ ->
                      let! u = o |> readString "externalDocs.url"
                      return 
                        Some
                          { Description = o |> readStringOption "externalDocs.description"
                            Url=u }
                  }

              let! discriminator =
                parsing {
                  match o |> selectToken "discriminator" with
                  | None -> return None
                  | Some d -> 
                      let! pn = d |> readString "propertyName"
                      let mapping =
                        d
                        |> selectToken "mapping" 
                        |> Option.bind (
                            function
                            | SObject props ->
                                props
                                |> List.choose (
                                    function
                                    | name, RawValue v -> Some (name, v.ToString())
                                    | _ -> None
                                   )
                                |> Map
                                |> Some
                            | _ -> None
                           )
                      return Some { PropertyName=pn; Mapping = mapping }
                }

              let required = o |> selectToken "required" |> Option.bind readStringArray
              let title = o |> readStringOption "title"

              return 
                 { Title = title |> Option.defaultValue ""
                   Type = typ
                   AllOf = allOf
                   OneOf = oneOf
                   AnyOf = anyOf
                   Not = ``not``
                   MultipleOf = multipleOf
                   Items = items
                   Maximum = o |> readIntOption "maximum"
                   ExclusiveMaximum = o |> readIntOption "exclusiveMaximum"
                   Minimum = o |> readIntOption "minimum"
                   ExclusiveMinimum = o |> readIntOption "exclusiveMinimum"
                   MaxLength = o |> readIntOption "maxLength"
                   MinLength = o |> readIntOption "minLength"
                   Pattern = o |> readStringOption "pattern"
                   MaxItems = o |> readIntOption "maxItems"
                   MinItems = o |> readIntOption "minItems"
                   UniqueItems = None
                   MaxProperties = o |> readIntOption "maxProperties"
                   MinProperties = o |> readIntOption "minProperties"
                   Properties = properties
                   AdditionalProperties = None
                   Required = required
                   Nullable = o |> readBool "nullable"
                   Enum = None
                   Format = o |> readStringOption "format"
                   Discriminator = discriminator
                   Readonly = o |> readBool "readOnly"
                   WriteOnly = o |> readBool "writeOnly"
                   Xml = None
                   ExternalDocs = externalDocs
                   Example = None
                   Deprecated = o |> readBool "deprecated"
                 }
              }
        | _ ->
            let message = sprintf "Invalid schema format %A" node
            ParsingState.FailureOf <| InvalidFormat message
    return r
  }

let parseSchemaRef node : ParsingState<Schema InlinedOrReferenced option> =
  parsing {
    let! r = 
      node
      |> selectToken "schema"
      |> parseOptionalInlinedOrReferenced parseSchema
    return r
  }

let parseReference s : ParsingState<ReferencePath> =
  match ReferencePath.parseReference s with
  | Ok f -> ParsingState.success <| f
  | Error e -> ParsingState.FailureOf <| InvalidFormat e

let parseParameter node : ParsingState<Parameter InlinedOrReferenced> =
  parsing {
    match node |> selectToken "$ref" with
    | Some (RawValue s) ->
        let! r = s.ToString() |> parseReference |> ParsingState.map Referenced
        return r
    | _ ->
        let! name = node |> readString "name"
        let! ``in`` = node |> readString "in"
        let! location = ``in`` |> Helpers.parseParameterLocation |> Result.mapError (fun e -> InvalidFormat e) |> ParsingState.ofResult
        let! schema = node |> parseSchemaRef

        return 
          Inlined
            {
              Name = name
              In = location
              Description = node |> readStringOption "description"
              Required = node |> readBoolWithDefault "required" true
              Deprecated = node |> readBool "required"
              AllowEmptyValue = node |> readBool "required"
              Style = node |> readStringOption "style"
              Explode = node |> readBool "explode"
              AllowReserved = node |> readBool "allowReserved"
              Schema = schema
              Example = node |> readStringOption "example"
              Examples = None
              Content = None }
  }

let parseContent node : Map<MimeType, MediaType> option ParsingState =
  parsing {

    match node with
    | SObject props -> 
        let! r = 
          props
          |> List.map (
              fun (mimetype, n) ->
                let schema = 
                  parseSchemaRef n
                  |> ParsingState.map (
                        fun s -> 
                          match s with
                          | Some o ->
                              let d = 
                                { Schema = o
                                  Examples = Map.empty //Map<string, Example InlinedOrReferenced>
                                  Encoding = Map.empty } //Map<string, Encoding>
                              Some (mimetype, d)
                          | None -> None
                      )
                schema
             )
        
        return r |> List.choose id |> Map |> Some
    | _ -> 
      return None
  }

let parseResponse node : Response ParsingState =
  parsing {
    let! description = node |> readString "description"
    let! content = 
      match node |> selectToken "content" with
      | Some (SObject _ as o) -> o |> parseContent
      | _ -> None |> ParsingState.success

    return
      { Description = description
        Headers = Map.empty
        Content = content |> Option.defaultValue Map.empty
        Links = Map.empty }
  }

let parseResponses node =
  parsing {
    match node |> selectToken "responses" with
    | Some(SObject props) -> 
        
        let! def = 
          node
          |> selectToken "responses.default"
          |> parseOptionalInlinedOrReferenced parseResponse

        let! responsesDefinitions = 
          props
          |> List.filter (fun i -> i |> fst <> "default")
          |> List.distinctBy fst
          |> List.map (
               fun (code, n) -> 
                (parseOptionalInlinedOrReferenced parseResponse) (Some n) |> ParsingState.map (fun r -> code,r)
             )

        let responses =
          responsesDefinitions
          |> List.choose (fun (code,r) -> r |> Option.map (fun v -> int code,v) )
          |> Map

        return 
          {
            Responses = responses
            Default = def
          }
    | _ -> return { Responses = Map []; Default = None }
  }

let parseParameters node =
  match node |> selectToken "parameters" with
  | Some (SCollection items) -> 
      items
      |> List.map parseParameter
      |> List.fold (fun state i -> i |> ParsingState.combine' state) ( [] |> ParsingState.success)
      |> ParsingState.bindWith (fun p -> Some p)
  | _ -> None |> ParsingState.success

let operation template verb node =
  parsing {
    let summary = node |> readStringOption "summary"
    let description = node |> readStringOption "description"
    let! id = node |> readString "operationId"
    
    let! externalDocs = 
      parsing {
        match node |> selectToken "externalDocs.url" with
        | Some n -> 
            let! url = n |> readString "url"
            return Some
              { Description=n |> readStringOption "description"
                Url=url }
        | None -> return None
      }

    let! rs = parseResponses node
    let! parameters = parseParameters node

    return
      template,
      verb,
        {
          Tags = node |> selectToken "tags" |> Option.bind (fun tags -> readStringArray tags) |> Option.defaultValue List.empty
          Summary = summary
          Description = description
          ExternalDocs = externalDocs
          OperationId = id
          Parameters = parameters
          RequestBody = None
          Responses = rs
          Callbacks = None // TODO
          Deprecated = node |> readBool "deprecated"
          Security = None // TODO
          Servers = node |> selectToken "servers" |> Option.bind (fun n -> parseServers n |> ParsingState.toOption)
        }
  }

let parsePaths doc : ParsingState<Map<string,Path>> =
  let httpVerbs = ["get";"post";"put";"patch";"head";"delete";"options";"trace"]
  parsing {
    let! operations = 
      match doc |> selectToken "paths" with
      | Some (SObject p) ->
          p
          |> List.choose (
               function
               | (template, (XObject(path, props) as node)) -> 
                    let methods = 
                      props 
                      |> List.filter(fun (verb,_) -> httpVerbs |> List.contains verb)
                      |> List.map (fun (verb, r) -> operation (template,node) verb r)
                    Some methods
               | _ -> None
             )
          |> List.collect id
      | _ -> List.empty
    
    let r = 
      operations
      |> List.groupBy (fun (k, _, _) -> k)
      |> List.map (
          fun ((template, node), ops) -> 
            let getVerb m =
              ops
              |> List.tryFind (fun (_,k,_) -> k.Equals(m, StringComparison.InvariantCultureIgnoreCase))
              |> Option.map (fun (_,_,o) -> o)
            
            let servers = node |> parseServers |> ParsingState.toOption
            let parameters = 
              match node |> parseParameters |> fun i -> i.Result with
              | Ok v -> v
              | _ -> None

            template,
              { Reference = ""
                Summary = ""
                Description = ""
                Get = getVerb "get"
                Put = getVerb "put"
                Post = getVerb "post"
                Delete = getVerb "delete"
                Options = getVerb "options"
                Head = getVerb "head"
                Patch = getVerb "patch"
                Trace = getVerb "trace"
                Servers = servers
                Parameters = parameters
              }

         ) |> Map

    return r
  }

let parseStandard doc =
  parsing {
    let! version = doc |> readString "openapi"
    return { Name = "openapi"
             Version = version }
  }


let parseComponents doc =
  parsing {
    let parseComponentSection path parser (f: (string*'U) -> 'V) =
      match doc |> selectToken path with
      | Some (SObject props) -> 
            parsing {
              let! r =
                props
                |> List.map (
                    fun (name, s) -> 
                      s
                      |> parser
                      |> ParsingState.map (fun p -> f (name,p))
                    )
              return Some (Map r)
            }
      | _ -> ParsingState.success None

    let! schemas = 
      parseComponentSection "components.schemas" parseSchema (fun (name,p) -> name,Inlined p)

    let! parameters = 
      parseComponentSection "components.parameters" parseParameter (fun (name,p) -> name,p)     

    let! responses = 
      parseComponentSection "components.responses" parseResponse (fun (name,p) -> name,Inlined p)     

    return 
        { Schemas=schemas
          Responses=responses
          Parameters=parameters
          Examples=None
          RequestBodies=None
          Headers=None
          SecuritySchemes=None
          Links=None
          Callbacks=None }
  }

let parseOpenApiDocument doc =
  parsing {
    let! infos = parseInfos doc
    let! standard = parseStandard doc
    let! paths = parsePaths doc
    let! servers = parseServers doc
    let! components = 
      match doc |> selectToken "components" with
      | Some (SObject _) -> parseComponents doc |> ParsingState.map Some
      | _ -> ParsingState.success None

    return
      { Standard = standard
        Infos = infos
        Servers = servers
        Paths = paths
        Components = components
        Security = None
        Tags = None
        ExternalDocs = None }
  } |> fun p -> p.Result

