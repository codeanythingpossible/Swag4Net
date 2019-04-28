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

let readYamlAsJson =
  File.ReadAllText
  >> Deserializer().Deserialize<obj>
  >> JsonConvert.SerializeObject

let http = new HttpClient()

let specv2 = specv2File |> readYamlAsJson |> JsonParser.parseSwagger http
specv2.Routes |> List.find(fun r -> r.Path = "/pet/{petId}") |> fun r -> r.Responses

let specv3 = specv3File |> readYamlAsJson |> JsonParser.parseOpenApiV3 http
specv3.Routes |> List.find(fun r -> r.Path = "/pets/{petId}") |> fun r -> r.Responses

