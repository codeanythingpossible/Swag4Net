open YamlDotNet.Core.Tokens
open System
#r "../../packages/newtonsoft.json/12.0.1/lib/netstandard2.0/newtonsoft.json.dll"
#r "../../packages/YamlDotNet/6.0.0/lib/netstandard1.3/YamlDotNet.dll"
#r "netstandard"
#r "System.Net.Http.dll"

#load "Models.fs"
#load "JsonParser.fs"
#load "YamlParser.fs"

open Swag4Net.Core
open YamlDotNet
open YamlDotNet.Serialization
open YamlDotNet.Serialization.NamingConventions
open Models
open System
open System.Net
open System.Net.Http
open System.IO
open Newtonsoft.Json
open Newtonsoft.Json.Linq


let (/>) a b = Path.Combine(a, b)

let specFile = __SOURCE_DIRECTORY__ /> ".." /> ".." /> "tests" /> "Assets"  /> "openapiV3" /> "petstoreV3.yaml"
let content = File.ReadAllText specFile

let deserializer = Deserializer()
let spec = deserializer.Deserialize<obj>(content)
let json = spec |> JsonConvert.SerializeObject |> JObject.Parse

//json.SelectToken "components"

type Anchor = Anchor of string
type ReferencePath =
  | ExternalUrl of Uri * Anchor option
  | RelativePath of string * Anchor option
  | InnerReference of Anchor

let parseReference (ref:string) : Result<ReferencePath, string> =
  match ref with
  | _ when String.IsNullOrWhiteSpace ref ->
      Error "ref cannot be empty"
  | _ when ref.StartsWith "#" ->
      ref |> Anchor |> InnerReference |> Ok
  | _ when Uri.IsWellFormedUriString(ref, UriKind.Absolute) ->
      let uri = Uri ref
      let a = if String.IsNullOrWhiteSpace uri.Fragment then None else Some(Anchor uri.Fragment)
      ExternalUrl(Uri uri.AbsoluteUri, a) |> Ok
  | _ -> 
      match ref.IndexOf '#' with
      | -1 -> RelativePath(ref, None) |> Ok
      | i -> 
        let a = ref.Substring i
        RelativePath(ref, Some (Anchor a)) |> Ok

parseReference "http://local/popo#lala/mlml"
parseReference "#lala/mlml"
parseReference "//local/popo#lala/mlml"
parseReference "../local/popo"

let getRefItem (http:HttpClient) (path:string) =
  path
  |> parseReference
  |> Result.map
      (function
        | ExternalUrl(uri, a) ->
            async {
              let! content = http.GetStringAsync(uri) |> Async.AwaitTask
              return content //todo: parse ...
            }
        | RelativePath(p, a) ->
            async {
              let content = File.ReadAllText(p)
              return content //todo: parse ...
            }
        | InnerReference a -> 
            async {return ""}
      )
  // match parseReference path with
  // | ExternalUrl(uri, Some (Anchor a)) ->

  //     0
  // | RelativePath of string * Anchor option
  // | InnerReference of Anchor

let path = "#components/schemas/Pet"


