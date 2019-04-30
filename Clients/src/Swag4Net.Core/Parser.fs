module Swag4Net.Core.Parser

open Newtonsoft.Json.Linq
open SpecificationModel

let ParseV2 (content:string) = 
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
    
let ParseV3 (content:string) = Swag4Net.Core.v3.JsonParser.parseSwagger content

let Parse (content:string) =
    let json = JObject.Parse content
    if json.ContainsKey "openapi" 
        then
            if json.ContainsKey "swagger"
                then Error "invalid specification: duplicated standard tag"
                else ParseV3 content
        else if json.ContainsKey "swagger"
            then ParseV2 content
            else Error "unable to determine file format"
   //json.SelectToken "openapi" 
   //     |> fun t -> if isNull t |> not
   //                     then t.Value |> string
   //                     else if json.SelectToken "swagger" |> isnull
   //                                 then "??" 
   //                                 else t.Value |> string

