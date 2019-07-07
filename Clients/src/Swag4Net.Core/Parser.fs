namespace Swag4Net.Core

//https://github.com/OAI/OpenAPI-Specification/blob/master/versions/3.0.0.md#parameterIn
//https://swagger.io/docs/specification/data-models/data-types/

open System
open Document
open Swag4Net.Core.v2.SwaggerParser

open Swag4Net.Core.Domain
open SharedKernel
open SwaggerSpecification

module Parser =
    
    let parseOpenApi (content:string) = 
      let doc =  content |> loadDocument
      match Swag4Net.Core.v3.Parser.parseOpenApiDocument doc with
      | Error e -> failwithf "%A" e
      | Ok spec -> spec
            //let contact =
            //    spec.Infos.Contact |> Option.bind (fun c -> c.Email |> Option.map Email)
            //let infos =
            //  { Description = spec.Infos.Description |> Option.defaultValue ""
            //    Version = spec.Infos.Version
            //    Title = spec.Infos.Title
            //    TermsOfService = spec.Infos.TermsOfService |> Option.defaultValue ""
            //    Contact = contact
            //    License = spec.Infos.License |> Option.map (fun l -> { Name = l.Name; Url = l.Url |> Option.defaultValue "" } )
            //  }
            //let routes =
            //  spec.Paths
            //  |> Seq.collect(
            //        fun kv ->
            //          [ mapOperation "Get" kv.Key kv.Value.Get
            //            mapOperation "Delete" kv.Key kv.Value.Delete
            //            mapOperation "Options" kv.Key kv.Value.Options
            //            mapOperation "Post" kv.Key kv.Value.Post
            //            mapOperation "Put" kv.Key kv.Value.Put
            //            mapOperation "Head" kv.Key kv.Value.Head
            //            mapOperation "Patch" kv.Key kv.Value.Patch
            //            mapOperation "Trace" kv.Key kv.Value.Trace ]
            //      )
            //  |> Seq.choose Async.RunSynchronously
            //  |> Seq.toList
            //{ Infos = infos
            //  Host = ""
            //  BasePath = ""
            //  Schemes = []
            //  Routes = routes
            //  ExternalDocs = Map []
            //  Definitions = [] }
    
    let private byStandard (callback:string -> Result<'T,string>) (content:string) =
        let doc =  content |> loadDocument
        let openapi = doc |> Parsing.readStringOption "openapi"
        let swagger = doc |> Parsing.readStringOption "swagger"
        match swagger, openapi with
        | None, None -> Error "unable to determine file format"
        | Some _, Some _ -> Error "invalid specification: duplicated standard tag"
        | Some _, None -> callback "swagger"
        | None, Some _ -> callback "openapi"

    let getStandard (content:string) =
        byStandard (fun t -> Ok t) content

    type ApiDocumentation =
      | Swagger of SwaggerSpecification.Documentation
      | OpenApi of OpenApiSpecification.Documentation

    let parse (content:string) =
      let provider = fun _ -> async { return Error "not implemented" }
      let selectParser standard =
          match standard with
          | "openapi" -> Ok (OpenApi <| parseOpenApi content)
          | "swagger" -> Ok (Swagger<| parseSwagger provider content)
          | _ -> Error "unhandled standard"
      byStandard selectParser content
