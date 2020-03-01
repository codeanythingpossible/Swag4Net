module Swag4Net.Code.Tests.v3.ParsingTests

open Expecto
open Expecto.Logging
open Expecto.Logging.Message
open Swag4Net.Core

let logger = Log.create "OpenApi parsing"

let tests =
    testList "OpenApi specification parsing tests" [
        test "Parsing only standard tag" {
            let result = 
                """{
                "openapi": "3.0.1"
                }""" |> Document.fromJson |> Swag4Net.Core.v3.Parser.parseOpenApiDocument
            Expect.isError result "parsing should fail as specification is not complete"
        }
        test "Parsing only info empty item" {
            let result = 
                """{
                "openapi": "3.0.1"
                ,"info": {}
                }""" |> Document.fromJson |> Swag4Net.Core.v3.Parser.parseOpenApiDocument
            match result with
            | Result.Error error -> logger.info(eventX (sprintf "%A" error))
            | Ok _ -> ()
            Expect.isError result "parsing should fail as specification is not complete"
            //match result with
            //| Ok specification -> 
            //      Expect.equal specification.Standard.Name "openapi" "unexpected standard version"
            //      Expect.equal specification.Standard.Version "3.0.1" "unexpected standard version"
            //| Error error -> ()
        }
    ]