open System
open System.IO

open System.Net.Http
open Argu
open Swag4Net.Core
open Swag4Net.Core.v2
open Swag4Net.Core.Domain
open SharedKernel
open SwaggerSpecification
open Swag4Net.Generators.RoslynGenerator
open Swag4Net.Core.Document

let (/>) a b =
  Path.Combine(a, b)

type CLIArguments =
  | [<Mandatory>] SpecFile of path:string
  | Namespace of string
  | ClientName of string
  | [<Mandatory>] OutputFolder of string
with
  interface IArgParserTemplate with
    member s.Usage =
      match s with
      | SpecFile _ -> "specify a Swagger spec file."
      | Namespace _ -> "specify namespace of generated code."
      | ClientName _ -> "specify client name."
      | OutputFolder _ -> "specifyoOutput folder."

let getRawSpec (path:string) =
  try
    let uri = Uri path
    if uri.IsFile
    then File.ReadAllText path
    elif uri.IsAbsoluteUri
    then
      use http = new HttpClient()
      http.GetStringAsync uri |> fun t -> t.GetAwaiter().GetResult()
    else File.ReadAllText path
  with | _ -> 
    path |> Path.GetFullPath |> File.ReadAllText

module Loaders =
  let loadOpenApiComponent<'t> (http:HttpClient) kind (mapper:string -> OpenApiSpecification.Components -> 't InlinedOrReferenced option) (ctx:ResourceProviderContext<OpenApiSpecification.Documentation>) =
    match ctx.Reference with
    | ExternalUrl(uri, a) ->
      async {
          let! content = uri |> http.GetStringAsync |> Async.AwaitTask
          let v = content |> SwaggerParser.loadDocument
          let name =
            match a with
            | Some (Anchor l) -> SwaggerParser.resolveRefName l
            | _ -> uri.Segments |> Seq.last
          //return Ok { Name=name; Content=v }
          return Error "not impl"
      }
     | RelativePath(p, a) ->
      async {
          let content = File.ReadAllText p
          let v = content |> SwaggerParser.loadDocument
          let name =
            match a with
            | Some (Anchor l) -> SwaggerParser.resolveRefName l
            | _ -> SwaggerParser.resolveRefName p
          //return Ok { Name=name; Content=v }
          return Error "not impl"
      }
     | InnerReference (Anchor a) -> 
      async {
          match a.Split([|'/'|], StringSplitOptions.RemoveEmptyEntries) |> Array.toList with
          | "components" :: k :: [name] when k = kind ->
              let c =
                ctx.Document.Components
                |> Option.bind (mapper name)
              return
                match c with
                | None -> Error "path not found"
                | Some (Inlined v) -> 
                    Ok { Name=name; Content=v }
                | Some (Referenced _) -> 
                    Error "referenced schema are not allowed during reference resolution"

          | _ -> return Error "path not found"
      }

  let loadOpenApiSchema http : ResourceProvider<OpenApiSpecification.Documentation, OpenApiSpecification.Schema> =
    fun ctx ->
      let mapper : string -> OpenApiSpecification.Components -> OpenApiSpecification.Schema InlinedOrReferenced option = 
        fun name c ->
          c.Schemas
          |> Option.bind (
            fun s -> 
              match s.TryGetValue name with
              | false,_ -> None
              | true,s -> Some s
            )
      loadOpenApiComponent<OpenApiSpecification.Schema> http "schemas" mapper ctx

  let loadOpenApiResponse http : ResourceProvider<OpenApiSpecification.Documentation, OpenApiSpecification.Response> =
    fun ctx ->
      let mapper : string -> OpenApiSpecification.Components -> OpenApiSpecification.Response InlinedOrReferenced option = 
        fun name c ->
          c.Responses
          |> Option.bind (
            fun s -> 
              match s.TryGetValue name with
              | false,_ -> None
              | true,s -> Some s
            )
      loadOpenApiComponent<OpenApiSpecification.Response> http "responses" mapper ctx

  let loadOpenApiParameters http : ResourceProvider<OpenApiSpecification.Documentation, OpenApiSpecification.Parameter> =
    fun ctx ->
      let mapper : string -> OpenApiSpecification.Components -> OpenApiSpecification.Parameter InlinedOrReferenced option = 
        fun name c ->
          c.Parameters
          |> Option.bind (
            fun s -> 
              match s.TryGetValue name with
              | false,_ -> None
              | true,s -> Some s
            )
      loadOpenApiComponent<OpenApiSpecification.Parameter> http "parameters" mapper ctx


[<EntryPoint>]
let main argv =
  
  let parser = ArgumentParser.Create<CLIArguments>()
  try
    let results = parser.ParseCommandLine(inputs = argv, raiseOnUsage = true)
    let specFile = results.GetResult <@ SpecFile @>
    let ns = results.GetResult(<@ Namespace @>, defaultValue="GeneratedClient")
    let clientName = results.GetResult(<@ ClientName @>, defaultValue="ApiClient")
    let outputFolder = results.GetResult <@ OutputFolder @>
    
    let http = new HttpClient()
    
    let loadSwaggerReference : ResourceProvider<Value, Value> =
      fun ctx ->
        match ctx.Reference with
        | ExternalUrl(uri, a) ->
          async {
              let! content = uri |> http.GetStringAsync |> Async.AwaitTask
              let v = content |> SwaggerParser.loadDocument
              let name =
                match a with
                | Some (Anchor l) -> SwaggerParser.resolveRefName l
                | _ -> uri.Segments |> Seq.last
              return Ok { Name=name; Content=v }
          }
         | RelativePath(p, a) ->
          async {
              let content = File.ReadAllText p
              let v = content |> SwaggerParser.loadDocument
              let name =
                match a with
                | Some (Anchor l) -> SwaggerParser.resolveRefName l
                | _ -> SwaggerParser.resolveRefName p
              return Ok { Name=name; Content=v }
          }
         | InnerReference (Anchor a) -> 
          async {
              let p = (a.Trim '/').Replace('/', '.')
              let token = ctx.Document |> Document.selectToken p
              return
                match token with
                | None -> Error "path not found"
                | Some v -> 
                    let name = SwaggerParser.resolveRefName a
                    Ok { Name=name; Content=v }
          }

    let logger = printfn "- %s"

    let providers : OpenApiV3ClientGenerator.ResourceProviders =
      { SchemaProvider = Loaders.loadOpenApiSchema http
        ResponseProvider = Loaders.loadOpenApiResponse http
        ParameterProvider = Loaders.loadOpenApiParameters http }

    match specFile |> getRawSpec |> Parser.parse loadSwaggerReference with
    | Ok doc ->  
        let settings =
          { Namespace=ns }
    
        let client,dtos = 
          match doc with
          | Parser.Swagger spec ->
              SwaggerClientGenerator.generateClients settings spec clientName,
              SwaggerClientGenerator.generateDtos settings spec.Definitions
          | Parser.OpenApi spec ->
              OpenApiV3ClientGenerator.generateClients logger settings spec providers clientName,
              OpenApiV3ClientGenerator.generateDtos logger settings spec providers
        
        outputFolder |> Directory.CreateDirectory |> ignore
    
        let csFilePath = outputFolder /> "Dtos.cs"
        File.WriteAllText(csFilePath, dtos)
    
        let csFilePath = outputFolder /> "Client.cs"
        File.WriteAllText(csFilePath, client)
        0

    | Error error -> 
        printfn "%s" error
        -1

  with e ->
      printfn "%s" e.Message
      -1

