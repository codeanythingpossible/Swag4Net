module Swag4Net.Core.Parser

open Newtonsoft.Json.Linq
open SpecificationModel

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

