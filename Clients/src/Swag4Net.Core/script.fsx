#r "netstandard"
#r "../../packages/newtonsoft.json/12.0.1/lib/netstandard2.0/newtonsoft.json.dll"
#r "../../packages/YamlDotNet/6.0.0/lib/netstandard1.3/YamlDotNet.dll"
#r "System.Net.Http.dll"

#load "Document.fs"
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

//let specv2File = __SOURCE_DIRECTORY__ /> ".." /> ".." /> "tests" /> "Assets" /> "petstore.yaml"
// let specv3File = __SOURCE_DIRECTORY__ /> ".." /> ".." /> "tests" /> "Assets" /> "openapiV3" /> "petstoreV3.yaml"

let spec = __SOURCE_DIRECTORY__ /> ".." /> ".." /> "tests" /> "Assets" /> "petstore.json" |> File.ReadAllText |> Document.fromJson

let http = new HttpClient()

let schemas = 
           """{ "Pet": {
           "type": "object",
           "required": [
             "name",
             "photoUrls"
           ],
           "properties": {
             "id": {
               "type": "integer",
               "format": "int64"
             },
             "category": {
               "$ref": "#/definitions/Category"
             },
             "name": {
               "type": "string",
               "example": "doggie"
             },
             "photoUrls": {
               "type": "array",
               "xml": {
                 "name": "photoUrl",
                 "wrapped": true
               },
               "items": {
                 "type": "string"
               }
             },
             "tags": {
               "type": "array",
               "xml": {
                 "name": "tag",
                 "wrapped": true
               },
               "items": {
                 "$ref": "#/definitions/Tag"
               }
             },
             "status": {
               "type": "string",
               "description": "pet status in the store",
               "enum": [
                 "available",
                 "pending",
                 "sold"
               ]
             }
           },
           "xml": {
             "name": "Pet"
           }
                }
        }""" |> Document.fromJson |> JsonParser.parseSchemas spec http


