namespace Swag4Net.Core

//https://github.com/OAI/OpenAPI-Specification/blob/master/versions/3.0.0.md#parameterIn
//https://swagger.io/docs/specification/data-models/data-types/

open Swag4Net.Core.SpecificationModel
open System
open Document
open Swag4Net.Core.v2.SwaggerParser

module Parser =
    open System

    let parseOpenApi (content:string) : Documentation = 
      let doc =  content |> loadDocument
      match Swag4Net.Core.v3.Parser.parseOpenApiDocument doc with
      | Error e -> failwithf "%A" e
      | Ok spec ->
            let contact =
                spec.Infos.Contact |> Option.bind (fun c -> c.Email |> Option.map Email)
            
            let infos =
              { Description = spec.Infos.Description |> Option.defaultValue ""
                Version = spec.Infos.Version
                Title = spec.Infos.Title
                TermsOfService = spec.Infos.TermsOfService |> Option.defaultValue ""
                Contact = contact
                License = spec.Infos.License |> Option.map (fun l -> { Name = l.Name; Url = l.Url |> Option.defaultValue "" } )
              }
            
            { Infos = infos
              Host = ""
              BasePath = ""
              Schemes = []
              Routes = []
              ExternalDocs = Map []
              Definitions = [] }
    
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

    let parse (content:string) =
      let provider = fun _ -> async { return Error "not implemented" }
      let selectParser standard =
          match standard with
          | "openapi" -> Ok (parseOpenApi content)
          | "swagger" -> Ok (parseSwagger provider content)
          | _ -> Error "unhandled standard"
      byStandard selectParser content
