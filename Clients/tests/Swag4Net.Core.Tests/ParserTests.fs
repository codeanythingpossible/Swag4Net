module Swag4Net.Code.Tests.ParserTests

open Expecto

let tests =
    testList "specification standard selection tests" [
        test "unknown api standard returns an error" {
          let result = 
             """{
             "unknown-standard": "3.0.1"
             }""" |> Swag4Net.Core.Parser.getStandard
          Expect.isError result "unknown standard should return an error"
        } 
        test "nested token should not be matched as standard" {
          let result = 
             """{
             "anObject": { "openapi": "3.0.1", "swagger": "2.0" }
             }""" |> Swag4Net.Core.Parser.getStandard
          Expect.isError result "unknown standard should return an error"
        } 
        test "duplicated standard field should raise an error" {
          let result = 
             """{
                "openapi": "3.0.1"
                , "swagger": "2.0"
             }""" |> Swag4Net.Core.Parser.getStandard
          Expect.isError result "unknown standard should return an error"
        } 
        test "Parsing openapi specification" {
          let result = 
             """{
             "openapi": "3.0.1"
             }""" |> Swag4Net.Core.Parser.getStandard
          Expect.isOk result "parsing failed"
          match result with
          | Ok specification -> 
                Expect.equal specification "openapi" "unexpected standard version"
          | Error error -> ()
        } 
        test "Parsing swagger specification" {
          let result = 
             """{
             "swagger": "2.0"
             }""" |> Swag4Net.Core.Parser.getStandard
          Expect.isOk result "parsing failed"
          match result with
          | Ok specification -> 
                Expect.equal specification "swagger" "unexpected standard version"
          | Error error -> ()
        } 
    ]

