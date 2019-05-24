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

// let specv2File = __SOURCE_DIRECTORY__ /> ".." /> ".." /> "tests" /> "Assets" /> "petstore.yaml"
// let specv3File = __SOURCE_DIRECTORY__ /> ".." /> ".." /> "tests" /> "Assets" /> "openapiV3" /> "petstoreV3.yaml"

let http = new HttpClient()

let route = 
           """{
           "paths": {
            "/pet": {
               "post": {
                    "tags": [
                      "pet"
                    ],
                    "summary": "Add a new pet to the store",
                    "description": "this is cool",
                    "operationId": "addPet",
                    "consumes": [
                      "application/json",
                      "application/xml"
                    ],
                    "produces": [
                      "application/xml",
                      "application/json"
                    ],
                    "parameters": [
                      {
                        "in": "query",
                        "name": "body",
                        "description": "Pet object that needs to be added to the store",
                        "required": true,
                        "type": "string"
                      }
                    ],
                    "responses": {
                      "405": {
                        "description": "Invalid input"
                      }
                    }
                  }
                }
              },
            "definitions": {
              "Pet": {
                "type": "object",
                "properties": {
                  "id": {
                    "type": "integer",
                    "format": "int64"
                  },
                  "name": {
                    "type": "string",
                    "example": "doggie"
                  }
                }
              }
            }
           }""" |> Document.fromJson |> JsonParser.parseRoutes http |> Seq.head

