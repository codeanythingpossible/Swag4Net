#r "netstandard"
#r "../../packages/newtonsoft.json/12.0.1/lib/netstandard2.0/newtonsoft.json.dll"
#r "../../packages/YamlDotNet/6.0.0/lib/netstandard1.3/YamlDotNet.dll"
#r "System.Net.Http.dll"

#load "Models.fs"
#load "JsonParser.fs"
#load "YamlParser.fs"

open Swag4Net.Core
open YamlDotNet
open YamlDotNet.Serialization
open YamlDotNet.Serialization.NamingConventions
open System
open System.Net
open System.Net.Http
open System.IO
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open YamlDotNet.Core.Tokens
open DocumentModel
open Models

let (/>) a b = Path.Combine(a, b)

let specv2File = __SOURCE_DIRECTORY__ /> ".." /> ".." /> "tests" /> "Assets" /> "petstore.yaml"
let specv3File = __SOURCE_DIRECTORY__ /> ".." /> ".." /> "tests" /> "Assets" /> "openapiV3" /> "petstoreV3.yaml"

let deserializer = Deserializer()

let readYamlAsJson file =
  file
  |> File.ReadAllText |> deserializer.Deserialize<obj>
  |> JsonConvert.SerializeObject

//let readSpec file =
//  file
//  |> readYamlAsJson
//  |> JObject.Parse

//let docv2 = specv2File |> readSpec
//let docv3 = specv3File |> readSpec

let http = new HttpClient()

let specv2 = specv2File |> readYamlAsJson |> JsonParser.parseSwagger http
specv2.Routes |> List.find(fun r -> r.Path = "/pet/{petId}") |> fun r -> r.Responses

let json = specv2File |> readYamlAsJson |> JObject.Parse
let d = json.SelectToken "definitions.Category"
d.ToString()

let schemas = 
  json.Descendants()
  |> Seq.filter(fun t -> t.Type = JTokenType.Property && (t :?> JProperty).Name = "schema")
  |> Seq.toList

//schemas.Head.GetType()

let specv3 = specv3File |> readYamlAsJson |> JsonParser.parseOpenApiV3 http
specv3.Routes |> List.find(fun r -> r.Path = "/pets/{petId}") |> fun r -> r.Responses

//parseReference "http://local/popo#lala/mlml"
//parseReference "#lala/mlml"
//parseReference "//local/popo#lala/mlml"
//parseReference "../local/popo"

//let path = "#components/schemas/Pet"

//let schema =
//    match JsonParser.getRefItem http json path with
//    | Ok f -> f |> Async.RunSynchronously |> fun t -> t.ToString()
//    | Error message -> ""


