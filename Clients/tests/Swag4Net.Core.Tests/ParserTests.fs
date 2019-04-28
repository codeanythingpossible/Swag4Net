module Swag4Net.Code.Tests.ParserTests

open Expecto

let tests =
    testList "specification version selection tests" [
        test "Parsing openapi specification" {
          let result = 
             """{
             "openapi": "3.0.1"
             }""" |> Swag4Net.Core.Parser.Parse
          match result with
          | Ok specification -> 
                Expect.equal specification.Standard.Name "openapi" "unexpected standard version"
                Expect.equal specification.Standard.Version "3.0.1" "unexpected standard version"
          | Error error -> Expect.isOk result "parsing failed"
        } 
        test "Parsing swagger specification" {
          let result = 
             """{
             "swagger": "2.0"
             }""" |> Swag4Net.Core.Parser.Parse
          match result with
          | Ok specification -> 
                Expect.equal specification.Standard.Name "swagger" "unexpected standard version"
                Expect.equal specification.Standard.Version "2.0" "unexpected standard version"
          | Error error -> Expect.isOk result "parsing failed"
        } 
    ]