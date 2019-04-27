#r "../../packages/newtonsoft.json/12.0.1/lib/netstandard2.0/newtonsoft.json.dll"
#r "../../packages/YamlDotNet/6.0.0/lib/netstandard1.3/YamlDotNet.dll"
#r "netstandard"
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

let readSpec file =
  file
  |> readYamlAsJson
  |> JObject.Parse

//let docv2 = specv2File |> readSpec
//let docv3 = specv3File |> readSpec

let specv2 = specv2File |> readYamlAsJson |> JsonParser.parseSwagger
let specv3 = specv3File |> readYamlAsJson |> JsonParser.parseSwagger

//parseReference "http://local/popo#lala/mlml"
//parseReference "#lala/mlml"
//parseReference "//local/popo#lala/mlml"
//parseReference "../local/popo"

//let http = new HttpClient()

//let path = "#components/schemas/Pet"

//let schema =
//    match JsonParser.getRefItem http json path with
//    | Ok f -> f |> Async.RunSynchronously |> fun t -> t.ToString()
//    | Error message -> ""


