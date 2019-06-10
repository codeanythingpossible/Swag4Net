//#I "../../packages/netstandard.library/2.0.0/build/netstandard2.0/ref"
#r "netstandard"
#r "../../packages/newtonsoft.json/12.0.1/lib/netstandard2.0/newtonsoft.json.dll"
#r "../../packages/YamlDotNet/6.0.0/lib/netstandard1.3/YamlDotNet.dll"
#r "System.Net.Http.dll"

#load "Document.fs"
#load "Parsing.fs"
#load "v3/SpecificationDocument.fs"
#load "v3/Parser.fs"

open System
open System.IO
open Swag4Net.Core
open Document
open Swag4Net.Core.v3.SpecificationDocument
open Swag4Net.Core.v3.Parser
open Parsing

let (/>) a b = Path.Combine(a, b)

let specv3File = __SOURCE_DIRECTORY__ /> ".." /> ".." /> "tests" /> "Assets" /> "openapiV3" /> "petstoreV3.yaml" |> File.ReadAllText

let doc = fromYaml specv3File


let spec = parseOpenApiDocument doc


spec |> Result.map (fun r -> r.Paths.Item "/pets")


spec
 |> Result.map (
    fun r -> 
      r.Paths
      |> Map.toList
      |> List.filter (fun (k,v) -> v.Get.Value.OperationId = "showPetById")
    )

