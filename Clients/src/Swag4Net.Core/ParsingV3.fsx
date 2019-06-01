//#I "../../packages/netstandard.library/2.0.0/build/netstandard2.0/ref"
#r "netstandard"
#r "../../packages/newtonsoft.json/12.0.1/lib/netstandard2.0/newtonsoft.json.dll"
#r "../../packages/YamlDotNet/6.0.0/lib/netstandard1.3/YamlDotNet.dll"
#r "System.Net.Http.dll"

#load "v3/SpecificationDocument.fs"
#load "Document.fs"

open System
open System.IO
open Swag4Net.Core
open Document
open Swag4Net.Core.v3.SpecificationDocument


let (/>) a b = Path.Combine(a, b)

type ParsingState<'TSuccess> = 
  { Result: Result<'TSuccess, ParsingError>
    Warnings: string list }
and ParsingError =
  | InvalidFormat of Message
  | UnhandledException of Exception
and Message = string

module ParsingState =
  let ofResult r = 
    { Result = r
      Warnings = List.empty }

  let success o = 
    { Result = Ok o
      Warnings = List.empty }

  let map f state = 
    { Result = state.Result |> Result.map f
      Warnings = state.Warnings }

  let FailureOf e =
    { Result = Error e
      Warnings = List.empty }

  let Empty = 
    { Result = Ok ()
      Warnings = List.empty }

  let bindResult (v:Result<'t, ParsingError>) (next:'t -> Result<'t, ParsingError>) =
    v |> Result.bind next |> ofResult
  
  let toOption (v:ParsingState<'T>) =
    match v.Result with
    | Ok r -> Some r
    | _ -> None


  let bind (binder:'T -> ParsingState<'U>) (v:ParsingState<'T>) : ParsingState<'U> =
    try
      match v.Result with
      | Ok v -> binder v
      | Error e -> 
          let state:ParsingState<'U> = { Result=Error e; Warnings=v.Warnings }
          state
    with e -> { Result=Error(UnhandledException e); Warnings=v.Warnings }

  let bindWith (binder:'T -> 'U) (v:ParsingState<'T>) : ParsingState<'U> =
    try
      match v.Result with
      | Ok v' -> 
          let r = binder v'
          { Result=Ok r ; Warnings = v.Warnings}
      | Error e -> 
          let state:ParsingState<'U> = { Result=Error e; Warnings=v.Warnings }
          state
    with e -> { Result=Error(UnhandledException e); Warnings=v.Warnings }

  let combine (a:ParsingState<'T>) (b:ParsingState<'T>) : ParsingState<'T list> =
    match a.Result, b.Result with
    | Ok v1, Ok v2 -> { Result=Ok[v1;v2]; Warnings=a.Warnings @ b.Warnings }
    | Error e , _-> { Result=Error e; Warnings=a.Warnings @ b.Warnings }
    | _, Error e -> { Result=Error e; Warnings=a.Warnings @ b.Warnings }

  let combine' (a:ParsingState<'T list>) (b:ParsingState<'T>) : ParsingState<'T list> =
    match a.Result, b.Result with
    | Ok v1, Ok v2 -> { Result=Ok( v2 :: v1 ); Warnings=a.Warnings @ b.Warnings }
    | Error e , _-> { Result=Error e; Warnings=a.Warnings @ b.Warnings }
    | _, Error e -> { Result=Error e; Warnings=a.Warnings @ b.Warnings }


type ParsingWorkflowBuilder() =
    
  member this.Bind(m:ParsingState<'T>, f:'T -> ParsingState<'U>) = 
    m |> ParsingState.bind f
  
  member this.Bind(m:Result<'T, ParsingError>, f:'T -> ParsingState<'U>) = 
    let state = m |> ParsingState.ofResult
    this.Bind(state, f)
  
  member this.Bind(error:ParsingError, f:'T -> ParsingState<'U>) = 
    let m = Error error
    let state = m |> ParsingState.ofResult
    this.Bind(state, f)
  
  member this.Bind(results:Result<ParsingState<'T> list, ParsingError>, f:'T list -> ParsingState<'U>) = 
    match results with
    | Ok states ->
        match states with
        //| [] when typeof<System.Collections.IEnumerable>.IsAssignableFrom typeof<'U> ->
        //    InvalidFormat "HAHAHA" |> Error |> ParsingState.ofResult |> ParsingState.bind f
        | [] -> 
            f []
            //InvalidFormat "Missing elements" |> Error |> ParsingState.ofResult |> ParsingState.bind f
        | h :: t -> 
            let r = match h.Result with Ok i -> Ok [i] | Error e -> Error e
            let head : ParsingState<'T list> = { Result=r; Warnings=h.Warnings }
            let state = t |> List.fold (fun acc s -> s |> ParsingState.combine' acc) head
            state |> ParsingState.bind f
    | Error e -> Error e |> ParsingState.ofResult

  member this.Bind(states:ParsingState<'T> list, f:'T list -> ParsingState<'U> ) : ParsingState<'U> = 
    match states with
    | [] when typeof<System.Collections.IEnumerable>.IsAssignableFrom typeof<'U> ->
        InvalidFormat "HAHAHA" |> Error |> ParsingState.ofResult |> ParsingState.bind f
    | [] -> 
        f []
        //InvalidFormat "Missing elements 2" |> Error |> ParsingState.ofResult |> ParsingState.bind f
    | h :: t -> 
        let r = match h.Result with Ok i -> Ok [i] | Error e -> Error e
        let head : ParsingState<'T list> = { Result=r; Warnings=h.Warnings }
        let state = t |> List.fold (fun acc s -> s |> ParsingState.combine' acc) head
        state |> ParsingState.bind f

  member this.Return(x) = 
    x |> Ok |> ParsingState.ofResult

  [<CustomOperation("warn",MaintainsVariableSpaceUsingBind=true)>]
  member this.Warn (state:ParsingState<'T>, text : string) = 
      { state with Warnings=text :: state.Warnings }

  member this.ReturnFrom(error:ParsingError) = 
    let m = Error error
    m |> ParsingState.ofResult

  member this.ReturnFrom(r) = 
    let m = Ok r
    m |> ParsingState.ofResult

  member this.Yield(x:unit) = 
    1 |> Ok |> ParsingState.ofResult

  member this.Yield(x:Result<'s,ParsingError>) = 
    x |> ParsingState.ofResult

  member this.Yield(x:'t) = 
    x |> Ok |> ParsingState.ofResult

  member this.Yield(x:ParsingState<'s>) = 
    x

  //member __.Zero() = () |> Ok |> ParsingState.ofResult
  
  member __.Comine (a,b) = 
    ParsingState.combine a b

  member __.For(state:ParsingState<'T>, f : unit -> ParsingState<'U>) =
    let state2 = f()
    { state2 with Warnings = state2.Warnings @ state.Warnings }

let parsing = new ParsingWorkflowBuilder()

let readString name (token:Value) =
  match token |> selectToken name with
  | Some (RawValue v) -> Ok(string v)
  | _ -> Error (InvalidFormat <| sprintf "Missing field '%s'" name)

let readStringOption name (token:Value) =
  match token |> selectToken name with
  | Some (RawValue v) -> Some (string v)
  | _ -> None

let readBoolWithDefault name defaultValue (token:Value) =
  match token |> selectToken name with
  | Some (RawValue v) ->
      match v with
      | :? Boolean as b -> b
      | :? String as s -> Boolean.TryParse s |> snd
      | _ -> defaultValue
  | _ -> defaultValue

let readBool name token =
  readBoolWithDefault name false token

let specv3File = __SOURCE_DIRECTORY__ /> ".." /> ".." /> "tests" /> "Assets" /> "openapiV3" /> "petstoreV3.yaml" |> File.ReadAllText
let doc = fromYaml specv3File

let (license : ParsingState<License option>) =
  parsing {
    match doc |> selectToken "info.license" with
    | None -> return None
    | Some node ->
        let! licenseName = doc |> readString "info.license.name"
        return Some 
          { Name = licenseName
            Url = node |> readStringOption "url" }
  }

let infos : ParsingState<Infos> = 
  parsing {

    let contact =
      doc
      |> selectToken "info.contact"
      |> Option.map (
           fun node ->
             { Name = node |> readStringOption "name"
               Url = node |> readStringOption "url"
               Email = node |> readStringOption "email" })
    
    let! license = license
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
              | ("enum", v) ->
                  readStringArray v
                  //match v with
                  //| SCollection values -> 
                  //    values
                  //    |> List.choose (
                  //        fun item ->
                  //          match item with
                  //          | RawValue v -> Some (v.ToString()) 
                  //          | _ -> None
                  //      )
                  //    |> Some
                  //| _ -> None
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

let servers node : ParsingState<Server list> =
  parsing {
    let! servers =
      match node |> selectToken "servers" with
      | Some (SCollection items) -> items |> List.map parseServer
      | o -> [sprintf "invalid %A" o |> InvalidFormat |> ParsingState.FailureOf]
    
    return servers
  }

let parseInlinedOrReferenced f node =
  match node with
  | Some (SObject ["$ref", RawValue link]) ->
      if link |> isNull
      then None
      else Some (Referenced (string link))
      |> ParsingState.success
  | Some v -> v |> f |> ParsingState.map (fun r -> Some (Inlined r))
  | _ -> None |> ParsingState.success

let parseSchema node =
  parsing {
    let! r =
        match node with
        | SObject props ->
            let o = SObject props
            parsing {
              let! typ = o |> readString "type"
              return 
                 { Title = o |> readStringOption "title"
                   Type = typ
                   AllOf = None
                   OneOf = None
                   AnyOf = None
                   Not = None
                   MultipleOf = None
                   Items = None
                   Maximum = None
                   ExclusiveMaximum = None
                   Minimum = None
                   ExclusiveMinimum = None
                   MaxLength = None
                   MinLength = None
                   Pattern = None
                   MaxItems = None
                   MinItems = None
                   UniqueItems = None
                   MaxProperties = None
                   MinProperties = None
                   Properties = None
                   AdditionalProperties = None
                   Required = None
                   Nullable = None
                   Enum = None
                   Format = o |> readStringOption "format"
                   Discriminator = None
                   Readonly = None
                   WriteOnly = None
                   Xml = None
                   ExternalDocs = None
                   Example = None
                   Deprecated=false
                 }
              }
        | _ -> ParsingState.FailureOf <| InvalidFormat "Invalid schema format"
    return r
  }

let parseSchemaRef node : ParsingState<Schema InlinedOrReferenced option> =
  parsing {
    let! r =
        match node |> selectToken "schema" with
        | Some (SObject ["$ref", RawValue link]) ->
            if link |> isNull
            then None
            else Some (Referenced (string link))
            |> ParsingState.success
        | Some v -> 
            v |> parseSchema |> ParsingState.map (fun s -> Some <| Inlined s)
        | _ -> None |> ParsingState.success
    return r
  }

let parseParameter node : ParsingState<Parameter> =
  parsing {
    let! name = node |> readString "name"
    let! ``in`` = node |> readString "in"
    let! schema = node |> parseSchemaRef

    return 
      {
        Name = name
        In = ``in``
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

let parseResponse node : Response ParsingState =
  parsing {
    let! description = node |> readString "description"

    return
      { Description = description
        Headers = None
        Content = None
        Links = None }
  }

let parseResponses node =
  parsing {
    match node |> selectToken "responses" with
    | Some n -> 
        
        let! def = 
          node
          |> selectToken "responses.default"
          |> parseInlinedOrReferenced parseResponse

        return 
          {
            Responses = Map []
            Default = def
          }
    | None -> return { Responses = Map []; Default = None }
  }

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

    let! (parameters : Parameter list InlinedOrReferenced option) =
        match node |> selectToken "parameters" with
        | Some (SObject ["$ref", RawValue link]) ->
            if link |> isNull
            then None
            else Some (Referenced (string link))
            |> ParsingState.success
        | Some (SCollection items) -> 
            items
            |> List.map parseParameter
            |> List.fold (fun state i -> i |> ParsingState.combine' state) ( [] |> ParsingState.success)
            |> ParsingState.bindWith (fun p -> Some <| Inlined p)
        | _ -> None |> ParsingState.success

    let! rs = parseResponses node

    return
      template,
      verb,
        {
          Tags = node |> selectToken "tags" |> Option.bind (fun tags -> readStringArray tags)
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
          Servers = node |> selectToken "servers" |> Option.bind (fun n -> servers n |> ParsingState.toOption)
        }
  }

let (paths : ParsingState<Map<string,Path>>) =
  parsing {
    let! (operations : (Name * Name * Operation) list) = 
      match doc |> selectToken "paths" with
      | Some (SObject p) ->
          p
          |> List.choose (
               function
               | (template, SObject props) -> 
                    let methods = props |> List.map (fun (verb, r) -> operation template verb r)
                    Some (template, methods)
               | _ -> None
             )
          |> List.map (fun (_,i) -> i)
          |> List.collect id
      | _ -> List.empty
    
    let r = 
      operations
      |> List.groupBy (fun (template, _, _) -> template)
      |> List.map (
          fun (template, ops) -> 
            let getVerb m =
              ops
              |> List.tryFind (fun (_,k,_) -> k.Equals(m, StringComparison.InvariantCultureIgnoreCase))
              |> Option.map (fun (_,_,o) -> o)
            
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
                Servers = None // TODO
                Parameters = None // TODO
              }

         ) |> Map

    return r
  }

