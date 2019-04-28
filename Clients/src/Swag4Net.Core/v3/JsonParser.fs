module Swag4Net.Core.v3.JsonParser

open Newtonsoft.Json.Linq
open System
open Swag4Net.Core.SpecificationModel

let parseStandard (token:JToken) =
    
    {
        Name="openapi"
        Version= token.SelectToken "openapi" |> string }

let parseSwagger (content:string) =
        let json = JObject.Parse content
        Ok { 
            Standard= parseStandard json
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
