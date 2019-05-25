//#I "../../packages/netstandard.library/2.0.0/build/netstandard2.0/ref"
#r "netstandard"
#r "../../packages/newtonsoft.json/12.0.1/lib/netstandard2.0/newtonsoft.json.dll"
#r "../../packages/YamlDotNet/6.0.0/lib/netstandard1.3/YamlDotNet.dll"
#r "System.Net.Http.dll"

open System.IO
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open YamlDotNet
open YamlDotNet.RepresentationModel
open YamlDotNet.Serialization
open YamlDotNet.Serialization.NamingConventions

#load "Document.fs"
open Swag4Net.Core
open Document


let yamlToJson (content:string) =
  let deserializer = Deserializer()
  let spec = deserializer.Deserialize(content)
  JsonConvert.SerializeObject spec

let yaml = 
  """
openapi: "3.0.0"
info: test
version: 2
title: Swagger Petstore
license:
  name: MIT
servers:
  - url: http://petstore.swagger.io/v1
  - url2: http://petstore.swagger.io/v2
/pets/{petId}:
  get:
    summary: Info for a specific pet
    operationId: showPetById
    tags:
      - pets
    parameters:
      - name: petId
        in: path
        required: true
        description: The id of the pet to retrieve
        schema:
          type: string
    responses:
      '200':
        description: Expected response to a valid request
        content:
          application/json:
            schema:
              $ref: "#/components/schemas/Pets"
      default:
        description: unexpected error
        content:
          application/json:
            schema:
              $ref: "#/components/schemas/Error"
"""

let json = yamlToJson yaml

let doc1 = fromYaml yaml

let doc2 = fromJson json

doc1 = doc2
