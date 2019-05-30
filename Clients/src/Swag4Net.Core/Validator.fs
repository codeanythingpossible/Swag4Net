module Swag4Net.Core.Validator

open System.IO
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Newtonsoft.Json.Schema
open System.Reflection

#if !INTERACTIVE
let internal ReadEmbedded(path: string) =
    Assembly.GetExecutingAssembly().GetManifestResourceStream(path)
#endif

let validateV3' (jsonSchema:string) (content:string) =
    let schema = JSchema.Parse jsonSchema
    let json = JObject.Parse content
    let errors = ref []
    json.Validate(schema, fun _ evtArg -> errors := evtArg.Message :: !errors)
    if !errors |> List.isEmpty |> not then
        Error(!errors)
    else
        Ok()

#if !INTERACTIVE

let validateV3 =
  use stream = "Swag4Net.Core.v3.openapi-jsonschema.json" |> ReadEmbedded
  use reader = new StreamReader(stream)
  let schema = reader.ReadToEnd()
  validateV3' schema

#endif
