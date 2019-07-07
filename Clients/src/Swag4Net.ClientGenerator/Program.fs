open System
open System.IO

open System.Net.Http
open Argu
open Swag4Net.Core
open Swag4Net.Core.v2
open Swag4Net.Core.SpecificationModel
open Swag4Net.Generators.RoslynGenerator
open CsharpGenerator

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
    
    let loadReference : SwaggerParser.ResourceProvider =
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


    let swagger = specFile |> getRawSpec |> SwaggerParser.parseSwagger loadReference
  
    let settings =
      { Namespace=ns }
    
    let client = generateClients settings swagger clientName
    
    let dtos =
      generateDtos settings swagger.Definitions
    
    outputFolder |> Directory.CreateDirectory |> ignore
    
    let csFilePath = outputFolder /> "Dtos.cs"
    File.WriteAllText(csFilePath, dtos)
    
    let csFilePath = outputFolder /> "Client.cs"
    File.WriteAllText(csFilePath, client)

  with e ->
      printfn "%s" e.Message

  0 // return an integer exit code
