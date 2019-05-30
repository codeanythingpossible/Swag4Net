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

  let bindResult (v:Result<'t, ParsingError>) (next:'t -> Result<'t, ParsingError>) =
    v |> Result.bind next |> ofResult
  
  let bind (binder:'T -> ParsingState<'U>) (v:ParsingState<'T>) : ParsingState<'U> =
    try
      match v.Result with
      | Ok v -> binder v
      | Error e -> 
          let state:ParsingState<'U> = { Result=Error e; Warnings=v.Warnings }
          state
    with e -> { Result=Error(UnhandledException e); Warnings=v.Warnings }

type ParsingWorkflowBuilder() =
    
  member this.Bind(m, f) = 
    m |> ParsingState.bind f
  
  member this.Bind(m, f) = 
    let state = m |> ParsingState.ofResult
    this.Bind(state, f)
  
  member this.Return(x) = 
    x |> Ok |> ParsingState.ofResult

  [<CustomOperation("warn",MaintainsVariableSpaceUsingBind=true)>]
  member this.Warn (state:ParsingState<'T>, text : string) = 
      { state with Warnings=text :: state.Warnings }

  member this.ReturnFrom(m) = 
    m

  member this.Yield(x:unit) = 
    1 |> Ok |> ParsingState.ofResult

  member this.Yield(x:Result<'s,ParsingError>) = 
    x |> ParsingState.ofResult

  member this.Yield(x:'t) = 
    x |> Ok |> ParsingState.ofResult

  member this.Yield(x:ParsingState<'s>) = 
    x

  //member __.Zero() = () |> Ok |> ParsingState.ofResult

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

let readBool name (token:Value) =
  match token |> selectToken name with
  | Some (RawValue v) ->
      match v with
      | :? Boolean as b -> b
      | :? String as s -> Boolean.TryParse s |> snd
      | _ -> false // TODO: check format
  | _ -> false // TODO: check format


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



