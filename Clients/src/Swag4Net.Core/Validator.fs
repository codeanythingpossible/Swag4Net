module Swag4Net.Core.Validator

open System.IO
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Newtonsoft.Json.Schema
open System.Reflection

let internal ReadEmbedded(path: string) =
    Assembly.GetExecutingAssembly().GetManifestResourceStream(path)

let validateV3 (content:string) =
    use s = new StreamReader(ReadEmbedded("Swag4Net.Core.v3.openapi-jsonschema.json"))
    use reader = new JsonTextReader(s)
    let schema = JSchema.Load(reader)
    let json = JObject.Parse content
    let errors = ref []
    json.Validate(schema, fun _ evtArg -> errors := evtArg.Message :: !errors)
    if !errors |> List.isEmpty |> not then
        Error(!errors)
    else
        Ok()
