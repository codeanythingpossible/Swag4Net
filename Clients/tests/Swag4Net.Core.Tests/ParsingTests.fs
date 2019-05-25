module ParsingTests

open System.IO
open Newtonsoft
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Swag4Net.Core
open Expecto
open System.Net
open System.Net.Http
open Models
open Swag4Net.Core
open System
open Expecto
open Swag4Net.Core

let (/>) a b = Path.Combine(a, b)

let http = new HttpClient()
let spec = AppDomain.CurrentDomain.BaseDirectory /> "petstore.json" |> File.ReadAllText |> Document.fromJson

let parseProps (json:string) = 
  match Document.fromJson json with
  | Document.SObject props -> props
  | _ -> failtest "json has not expected structure"

let tests =
  testList "spec parsing tests" [
    testList "route parsing tests" [
      
      test "Parsing one empty path" {
        let routes = 
           """{
           "paths": { 
             "/pet": {
                "post": { },
                "get": { },
                "put": { }
                }
              }
           }""" |> Document.fromJson |> JsonParser.parseRoutes http
        Expect.equal routes.Length 3 "routes count should match"
      }
      
      test "Parsing POST /pet" {
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
                        "in": "body",
                        "name": "body",
                        "description": "Pet object that needs to be added to the store",
                        "required": true,
                        "schema": {
                          "$ref": "#/definitions/Pet"
                        }
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
        
        Expect.equal route.Path "/pet" "path should be equal"
        Expect.equal route.Summary "Add a new pet to the store" "summary should be equal"
        Expect.equal route.OperationId "addPet" "operationId should be equal"
        Expect.equal route.Description "this is cool" "Description should be equal"
        
        Expect.sequenceEqual route.Tags ["pet"] "tags should be equal"
        Expect.sequenceEqual route.Consumes ["application/json"; "application/xml"] "consumes should be equal"
        Expect.sequenceEqual route.Produces ["application/xml";"application/json"] "produces should be equal"
        
        Expect.sequenceEqual route.Responses [{ Code=StatusCode HttpStatusCode.MethodNotAllowed; Description="Invalid input"; Type=None }] "responses should be equal"
        
        Expect.sequenceEqual route.Parameters
          [ { Location=InBody
              Name="body"
              Description="Pet object that needs to be added to the store"
              Deprecated=false
              AllowEmptyValue=false
              ParamType=ComplexType 
                { Name="Pet"
                  Properties=
                  [
                    { Name = "id"
                      Type = PrimaryType DataType.Integer64
                      Enums = None;
                    }
                    { Name = "name"
                      Type = PrimaryType (DataType.String None)
                      Enums = None } ]
                }
              Required=true } ]
          "parameters should be equal"
      }
      
      testList "parameters parsing tests" [
        
        test "Parsing int32 path param" {

          let parameter = 
            """{
              "name": "petId",
              "in": "path",
              "description": "ID of pet to return",
              "required": true,
              "type": "integer",
              "format": "int32"
            }""" |> parseProps |> JsonParser.parseParameter spec http
            
          Expect.equal parameter
              (Some { Location=InPath
                      Name="petId"
                      Description="ID of pet to return"
                      Deprecated=false
                      AllowEmptyValue=false
                      ParamType=PrimaryType DataType.Integer
                      Required=true })
            "parameters should be equal"
        }
        
        test "Parsing int64 path param" {
          let parameter = 
            """{
              "name": "petId",
              "in": "path",
              "description": "ID of pet to return",
              "required": true,
              "type": "integer",
              "format": "int64"
            }""" |> parseProps |> JsonParser.parseParameter spec http
            
          Expect.equal parameter
              (Some { Location=InPath
                      Name="petId"
                      Description="ID of pet to return"
                      Deprecated=false
                      AllowEmptyValue=false
                      ParamType=PrimaryType DataType.Integer64
                      Required=true })
            "parameters should be equal"
        }
        
        test "Parsing int64 data type" {
          let actual =
            """{ "type": "integer", "format": "int64" }"""
            |> Document.fromJson |> JsonParser.parseDataType spec http
          Expect.equal actual (Ok(PrimaryType DataType.Integer64)) "data type should be equal"
        }
        
        test "Parsing int32 data type" {
          let actual =
            """{ "type": "integer", "format": "int32" }"""
            |> Document.fromJson |> JsonParser.parseDataType spec http
          Expect.equal actual (Ok(PrimaryType DataType.Integer)) "data type should be equal"
        }
        
        test "Parsing boolean data type" {
          let actual =
            """{ "type": "boolean" }"""
            |> Document.fromJson |> JsonParser.parseDataType spec http
          Expect.equal actual (Ok(PrimaryType DataType.Boolean)) "data type should be equal"
        }    
        
        test "Parsing string data type" {
          let actual =
            """{ "type": "string" }"""
            |> Document.fromJson |> JsonParser.parseDataType spec http
          Expect.equal actual (Ok (PrimaryType (DataType.String None))) "data type should be equal"
        }
        
        test "Parsing string data type with invalid format should fallback to simple string" {
          let actual =
            """{ "type": "string", "format": "lalala" }"""
            |> Document.fromJson |> JsonParser.parseDataType spec http
          Expect.equal actual (Ok(PrimaryType (DataType.String None))) "data type should be equal"
        }
              
        test "Parsing string data type with date format" {
          let actual =
            """{ "type": "string", "format": "date" }"""
            |> Document.fromJson |> JsonParser.parseDataType spec http
          Expect.equal actual (Ok(PrimaryType (DataType.String (Some StringFormat.Date)))) "data type should be equal"
        }
  
        test "Parsing string data type with datetime format" {
          let actual =
            """{ "type": "string", "format": "date-time" }"""
            |> Document.fromJson |> JsonParser.parseDataType spec http
          Expect.equal actual (Ok(PrimaryType (DataType.String (Some StringFormat.DateTime)))) "data type should be equal"
        }
  
        test "Parsing string data type with password format" {
          let actual =
            """{ "type": "string", "format": "password" }"""
            |> Document.fromJson |> JsonParser.parseDataType spec http
          Expect.equal actual (Ok(PrimaryType (DataType.String (Some StringFormat.Password)))) "data type should be equal"
        }
        
        test "Parsing string data type with binary format" {
          let actual =
            """{ "type": "string", "format": "binary" }"""
            |> Document.fromJson |> JsonParser.parseDataType spec http
          Expect.equal actual (Ok(PrimaryType (DataType.String (Some StringFormat.Binary)))) "data type should be equal"
        }
        
        test "Parsing string data type with byte format" {
          let actual =
            """{ "type": "string", "format": "byte" }"""
            |> Document.fromJson |> JsonParser.parseDataType spec http
          Expect.equal actual (Ok(PrimaryType (DataType.String (Some StringFormat.Base64Encoded)))) "data type should be equal"
        }
        
      ]
      
      test "responses parsing tests" {
        let responses =
            """{
            "200": {
              "description": "successful operation",
              "schema": {
                "$ref": "#/definitions/Pet"
              }
            },
            "400": {
              "description": "Invalid ID supplied"
            },
            "404": {
              "description": "Pet not found"
            }
          }""" |> Document.fromJson |> JsonParser.parseResponses spec http
        Expect.sequenceEqual responses
            [
              {
                Code=StatusCode HttpStatusCode.OK
                Description="successful operation"
                Type= Some(ComplexType
                                { Name = "Pet";
                                  Properties =
                                   [
                                     { Name = "id"
                                       Type = PrimaryType DataType.Integer64
                                       Enums = None
                                     }
                                     { Name = "category"
                                       Type =
                                         ComplexType
                                            {  Name = "Category"
                                               Properties = 
                                                 [ { Name = "id"
                                                     Type = PrimaryType DataType.Integer64
                                                     Enums = None }
                                                   { Name = "name"
                                                     Type = PrimaryType (DataType.String None)
                                                     Enums = None }]
                                            }
                                       Enums = None }
                                     { Name = "name"
                                       Type = PrimaryType (DataType.String None)
                                       Enums = None }
                                     { Name = "photoUrls"
                                       Type = PrimaryType (DataType.Array (PrimaryType (DataType.String None)))
                                       Enums = None }
                                     { Name = "tags"
                                       Type = PrimaryType
                                               ( DataType.Array
                                                   ( ComplexType
                                                      { Name = "Tag"
                                                        Properties =
                                                          [ { Name = "id"
                                                              Type = PrimaryType DataType.Integer64
                                                              Enums = None }
                                                            { Name = "name"
                                                              Type = PrimaryType (DataType.String None)
                                                              Enums = None } ]
                                                      } ) )
                                       Enums = None
                                     }
                                     { Name = "status"
                                       Type = PrimaryType (DataType.String None)
                                       Enums = Some ["available"; "pending"; "sold"]
                                     }
                                   ]
                                }
                            )

              }
              { Code=StatusCode HttpStatusCode.BadRequest; Description="Invalid ID supplied"; Type=None }
              { Code=StatusCode HttpStatusCode.NotFound; Description="Pet not found"; Type=None }
            ] "responses should be equal"
      }
      
    ]
    
    testList "definitions parsing tests" [
      
      test "Parsing simple definition" {
        let actual =
          """{ "ApiResponse": {
              "type": "object",
              "properties": {
                "code": {
                  "type": "integer",
                  "format": "int32"
                },
                "type": {
                  "type": "string"
                },
                "message": {
                  "type": "string"
                }
              }
            } }"""
          |> Document.fromJson |> JsonParser.parseSchemas spec http |> Seq.head
        Expect.equal actual
            (Ok { Name="ApiResponse"
                  Properties=
                    [ { Name = "code"; Type = PrimaryType DataType.Integer; Enums=None }
                      { Name = "type"; Type = PrimaryType (DataType.String None); Enums=None }
                      { Name = "message"; Type = PrimaryType (DataType.String None); Enums=None } ]
                })
          "definition should be equal"
      }
      
      test "Parsing complex definition" {
        let actual =
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
              }"""
          |> Document.fromJson |> JsonParser.parseSchemas spec http |> Seq.head

        Expect.equal actual
          (Ok
            { Name = "Pet"
              Properties =
               [{ Name = "id"
                  Type = PrimaryType DataType.Integer64
                  Enums = None
                } 
                { Name = "category"
                  Type =
                   ComplexType
                     { Name = "Category"
                       Properties = [ { Name = "id"
                                        Type = PrimaryType DataType.Integer64
                                        Enums = None }
                                      { Name = "name"
                                        Type = PrimaryType (DataType.String None)
                                        Enums = None } ]
                     }
                  Enums = None }
                { Name = "name"
                  Type = PrimaryType (DataType.String None)
                  Enums = None }
                { Name = "photoUrls"
                  Type = PrimaryType (DataType.Array (PrimaryType (DataType.String None)))
                  Enums = None }
                { Name = "tags"
                  Type =
                    PrimaryType
                      (DataType.Array
                         (ComplexType
                            { Name = "Tag"
                              Properties = [
                               { Name = "id"
                                 Type = PrimaryType DataType.Integer64
                                 Enums = None }
                               { Name = "name"
                                 Type = PrimaryType (DataType.String None)
                                 Enums = None
                               } ]
                            }))
                  Enums = None
                }
                { Name = "status"
                  Type = PrimaryType (DataType.String None)
                  Enums = Some ["available"; "pending"; "sold"]
                }]
              })
          "definition should be equal"
      }
    ]
    
    testList "YAML parsing tests" [
      
      test "parsing yaml should give same result than json" {
        
        let json = AppDomain.CurrentDomain.BaseDirectory /> "petstore.json" |> File.ReadAllText
        let yaml = AppDomain.CurrentDomain.BaseDirectory /> "petstore.yaml" |> File.ReadAllText
        
        let yamlSpec = YamlParser.parseSwagger http yaml
        let jsonSpec = JsonParser.parseSwagger http json

        Expect.equal yamlSpec jsonSpec "yaml and json should give same spec"
      }
      
    ]
    
  ]
  