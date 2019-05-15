#I "../../packages/netstandard.library/2.0.0/build/netstandard2.0/ref"
#r "netstandard.dll"
#r "../../packages/newtonsoft.json/12.0.1/lib/netstandard2.0/newtonsoft.json.dll"
#r "../../packages/YamlDotNet/6.0.0/lib/netstandard1.3/YamlDotNet.dll"
#r "System.Net.Http"

open YamlDotNet
open YamlDotNet.Serialization
open System
open System.Net
open System.Net.Http
open System.IO
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open YamlDotNet.Core.Tokens

type Name = string

type SProperty = Name * Value

and Value =
  | RawValue of obj
  | SObject of SProperty list
  | SCollection of Value list

type Document =
  { Content:SProperty list }

let rec parseProperties (o:JObject) =
    let rec toValue(token:JToken) =
        match token with
        | :? JValue as v -> RawValue v.Value
        | :? JObject as c -> parseProperties c
        | :? JArray as a -> 
            a |> Seq.map toValue |> Seq.toList |> SCollection
        | _ -> token.ToString() |> box |> RawValue
    
    let props = o.Properties() :> JProperty seq |> Seq.toList
    props |> Seq.map (
     fun (p:JProperty) ->
       let n = p.Name
       let v = p.Value |> toValue
       n,v
    ) |> Seq.toList |> SObject


let o = 
    """{
        "name":"toto",
        "age": 20,
        "infos": {
                "address": {
                    "city": "paris",
                    "postalCode":"75020"
                },
                "comments":
                    [
                        "coucou ça va ?",
                        "nickel",
                        123
                    ]
            }
    }""" |> JObject.Parse |> parseProperties

type PathInstruction =
  | SelectMember of string
  | SelectOffset of int32
and PathSelect = PathInstruction list

let parsePath (path:string) : PathSelect =
  let rec loop acc chars =
    let m = chars |> Array.takeWhile (fun c -> Char.IsLetterOrDigit c || c = '_' || c = '-')
    let p = System.String m
    let r = (SelectMember p) :: acc
    if chars.Length > m.Length
    then
      //let sep = chars.[m.Length]
      chars |> Array.skip (m.Length + 1) |> loop r
    else
      r
  path.ToCharArray() |> loop [] |> List.rev

parsePath "popo"
parsePath "popo.tata"

//[|'d';'r'|] |> System.String

