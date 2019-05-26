#r "netstandard"
#r "../../packages/newtonsoft.json/12.0.2/lib/netstandard2.0/newtonsoft.json.dll"
#r "../../packages/newtonsoft.json.schema/3.0.11/lib/netstandard2.0/newtonsoft.json.schema.dll"
//#r "../../packages/YamlDotNet/6.0.0/lib/netstandard1.3/YamlDotNet.dll"
//#r "System.Net.Http.dll"

#load "SpecificationModel.fs"
//#load "v3/JsonParser.fs"
//#load "Parser.fs"
#load "Validator.fs"

//open YamlDotNet.Serialization
open System.IO
open Swag4Net.Core

//open Newtonsoft.Json

//E:\Users\zeric_000\Documents\dev\src\Swag4Net\Clients\tests\IntegrationTests\GeneratedClientTests\playground\schemas\psd2-api_1.3.3_20190412.json

let (/>) a b = Path.Combine(a, b)

//let specv2File = __SOURCE_DIRECTORY__ /> ".." /> ".." /> "tests" /> "Assets" /> "petstore.yaml"
//let specv3File = __SOURCE_DIRECTORY__ /> ".." /> ".." /> "tests" /> "Assets" /> "openapiV3" /> "petstoreV3.yaml"
let specv3File = __SOURCE_DIRECTORY__ /> ".." /> ".." /> "tests" /> "IntegrationTests" /> "GeneratedClientTests" /> "playground" /> "schemas" /> "psd2-api_1.3.3_20190412.json"

//let readYamlAsJson =
//  File.ReadAllText
//  >> Deserializer().Deserialize<obj>
//  >> JsonConvert.SerializeObject

//let http = new HttpClient()

//let specv2 = specv2File |> readYamlAsJson |> Swag4Net.Core.v2.JsonParser.parseSwagger http
//specv2.Routes |> List.find(fun r -> r.Path = "/pet/{petId}") |> fun r -> r.Responses

//let specv3 = specv3File |> readYamlAsJson |> JsonParser.parseOpenApiV3 http
//specv3.Routes |> List.find(fun r -> r.Path = "/pets/{petId}") |> fun r -> r.Responses

specv3File |> Validator.validateV3
//specv3.Routes |> List.find(fun r -> r.Path = "/pets/{petId}") |> fun r -> r.Responses
