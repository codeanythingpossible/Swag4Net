#r "netstandard"
#r "../../packages/newtonsoft.json/12.0.2/lib/netstandard2.0/newtonsoft.json.dll"
#r "../../packages/newtonsoft.json.schema/3.0.11/lib/netstandard2.0/newtonsoft.json.schema.dll"

#load "SpecificationModel.fs"
#load "Validator.fs"

open System.IO
open Swag4Net.Core

let (/>) a b = Path.Combine(a, b)

let specv3File = __SOURCE_DIRECTORY__ /> ".." /> ".." /> "tests" /> "Assets" /> "openapiV3" /> "petstoreV3.json"

let stream = __SOURCE_DIRECTORY__ /> "v3" /> "openapi-jsonschema.json" |> File.OpenRead
let content = specv3File |> File.ReadAllText 

Validator.validateV3' stream content
