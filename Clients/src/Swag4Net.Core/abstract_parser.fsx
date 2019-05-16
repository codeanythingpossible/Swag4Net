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

let (|ReadWhile|_|) f (chars:char list) =
  let m = chars |> List.takeWhile f
  if m |> List.isEmpty
  then None
  else
    let n = min m.Length (chars.Length)
    let r = chars |> List.skip n
    let txt = m |> Seq.toArray |> System.String
    Some (txt, r)

let (|ReadTo|_|) (search:char list) (chars:char list) =
  let f i = search |> List.contains i |> not
  (|ReadWhile|_|) f (chars:char list)

let betweenChars a b (buffer:char list) =
  if buffer.Length <= 0 || buffer.Head <> a
  then None
  else
    match buffer |> List.skip 1 |> List.tryFindIndex (fun c -> c = b) with
    | None -> None
    | Some i ->
        let result = 
          buffer
          |> List.skip 1
          |> List.take i
        let rest = buffer |> List.skip (result.Length + 2)
        Some(result, rest)

let (|BetweenChars|_|) a b (buffer:char list) =
  betweenChars a b buffer

let parseInstructions (path:string) =
  let rec loop acc text =
    match text with
    | ReadTo ['.'; '['; ']'] (name, r) -> 
        loop ((SelectMember (string name)) :: acc) r
    | '.' :: r ->
        loop acc r
    | BetweenChars '[' ']' (nums, r) ->
        if nums |> Seq.exists (Char.IsDigit >> not) 
        then Error "offset can be only digit"
        else
          let o = nums |> List.toArray |> System.String |> string |> Int32.Parse
          loop ((SelectOffset o) :: acc) r
    | [] -> acc |> List.rev |> Ok
    | r -> 
      let o = path.Length - r.Length
      Error <| sprintf "Invalid syntax '%c' at char %d" r.Head o
  path |> Seq.toList |> loop []

parseInstructions "popo.lala.mamam[0].ages[35]"
parseInstructions "lklk"
parseInstructions "popo.lala.mamam[0dz].ages[35]"

o

// let parsePath (path:string) : PathSelect =
//   let rec loop acc chars =
//     let m = chars |> Array.takeWhile (fun c -> Char.IsLetterOrDigit c || c = '_' || c = '-')
//     let p = System.String m
//     let r = (SelectMember p) :: acc
//     if chars.Length > m.Length
//     then
//       //let sep = chars.[m.Length]
//       chars |> Array.skip (m.Length + 1) |> loop r
//     else
//       r
//   path.ToCharArray() |> loop [] |> List.rev

// parsePath "popo"
// parsePath "popo.tata"


