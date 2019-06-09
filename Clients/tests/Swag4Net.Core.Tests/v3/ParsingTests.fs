module Swag4Net.Code.Tests.v3.ParsingTests

open Expecto
open Expecto.Logging
open Expecto.Logging.Message

let logger = Log.create "OpenApi parsing"

//let tests =
//    testList "OpenApi specification parsing tests" [
//        test "Parsing only standard tag" {
//            let result = 
//                """{
//                "openapi": "3.0.1"
//                }""" |> Swag4Net.Core.v3.Parser.parseOpenApiDocument
//            Expect.isError result "parsing should fail as specification is not complete"
//        }
//        test "Parsing only info empty item" {
//            let result = 
//                """{
//                "openapi": "3.0.1"
//                ,"info": {}
//                }""" |> Swag4Net.Core.v3.Parser.parse
//            match result with
//            | Result.Error error -> logger.info(eventX error)
//            | Ok _ -> ()
//            Expect.isError result "parsing should fail as specification is not complete"
//            //match result with
//            //| Ok specification -> 
//            //      Expect.equal specification.Standard.Name "openapi" "unexpected standard version"
//            //      Expect.equal specification.Standard.Version "3.0.1" "unexpected standard version"
//            //| Error error -> ()
//        }
//    ]