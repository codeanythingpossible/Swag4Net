module Swag4Net.Core.v3.JsonParser

open Newtonsoft.Json.Linq
open System
open Swag4Net.Core.SpecificationModel
open Newtonsoft.Json

exception ParsingErrorException of string

let internal getTokenOrThrow (token:JToken) (tag: string) =
    token.SelectToken tag |> fun t -> if isNull t then raise (ParsingErrorException ((sprintf "'%s': field is missing" tag))) else t

let internal getOptionalToken (cb: JToken -> 'r) (token:JToken) (tag: string) =
    token.SelectToken tag |> fun t -> if isNull t then None else Some (cb t)
    
let internal parseInfos (spec:JToken) =
  let infoToken = spec.SelectToken "info"
  if isNull infoToken then raise (ParsingErrorException ("no info field provided"))
  { Description=    getOptionalToken (fun t -> string t) infoToken "description"
    Version=        getTokenOrThrow infoToken "version" |> string
    Title=          getTokenOrThrow infoToken "title" |> string
    TermsOfService= getOptionalToken (fun t -> string t) infoToken  "termsOfService"
    Contact=        getOptionalToken (fun t -> Email (string t)) infoToken "contact.email"
    License=        getOptionalToken (fun t -> t.ToObject<License>()) infoToken "license" }

let internal parseStandard (jsonSpec:JToken) =
    {
        Name="openapi"
        Version= jsonSpec.SelectToken "openapi" |> string }

let parse (content:string) =
    try
        let jsonSpec = JObject.Parse content
        Ok { 
            Standard= parseStandard jsonSpec
            Infos= parseInfos jsonSpec
            Servers=None
            Paths=Map.empty
            Components=None
            Security=None
            Tags=None
            ExternalDocs=None }

    with
        | :? ArgumentNullException -> Error "provided specification content is empty"
        | :? JsonReaderException as ex -> Error (sprintf "provided specification is not valid json: %s" ex.Message)
        | ParsingErrorException(msg) -> Error (sprintf "parsing failed: %s" msg)
