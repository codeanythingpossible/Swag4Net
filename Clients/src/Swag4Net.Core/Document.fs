namespace Swag4Net.Core

open System
open System.IO

module Document =
  
  type Name = string
  type DocPath = string
  
  type SProperty = Name * Value
  
  and Value =
    | Litteral of DocPath * obj
    | XObject of DocPath * SProperty list
    | Collection of DocPath * Value list
  
  let (|SObject|_|) =
    function
    | XObject(_, props) -> Some props
    | _ -> None
  
  let (|SCollection|_|) =
    function
    | Collection(_, items) -> Some items
    | _ -> None

  let (|RawValue|_|) =
    function
    | Litteral(_, v) -> Some v
    | _ -> None


  type Document =
    { Content:SProperty list }
  
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
  
  let selectToken (path:string) (o:Value) = 
    let rec loop current (l:PathInstruction list) =
      match current,l with
      | SObject props, [SelectMember m] -> 
          props |> List.tryFind (fun (name,_) -> name = m) |> Option.map snd
      | SObject props, SelectMember m :: r -> 
          props
          |> List.tryFind (fun (name,_) -> name = m)
          |> Option.bind (fun (_,v) -> loop v r)
      | SCollection values, [SelectOffset offset] -> 
          if values.Length < offset
          then None
          else Some (values.Item offset)
      | SCollection values, SelectOffset offset :: r -> 
          if values.Length < offset
          then None
          else loop (values.Item offset) r
      | _ -> None
    match path |> parseInstructions with
    | Error e -> None
    | Ok p -> loop o p 

  let properties =
    function
    | SObject props -> props
    | _ -> []
    
  let someProperties =
    function
    | Some p -> properties p
    | None -> []

  let private (+>) path name =
    if String.IsNullOrWhiteSpace path
    then name
    else sprintf "%s.%s" path name

  open Newtonsoft.Json.Linq

  let fromJson (json:string) =
    let rec parseProperties path (o:JObject) =
      let rec toValue path (token:JToken) =
          match token with
          | :? JValue as v -> Litteral (path,v.Value)
          | :? JObject as c -> 
              parseProperties path c
          | :? JArray as a -> 
              let items = 
                a 
                |> Seq.mapi (
                    fun i item -> 
                      let np = sprintf "%s[%d]" path i
                      toValue np item
                   )
                |> Seq.toList
              Collection(path, items)
          | _ -> 
              let v = token.ToString() |> box
              Litteral (path,v)
      
      let props =
        o.Properties()
          |> Seq.map (
              fun (p:JProperty) ->
                let n = p.Name
                let v = p.Value |> toValue (path +> n)
                n,v
             ) |> Seq.toList
      XObject(path, props)
    json |> JObject.Parse |> parseProperties ""

  open YamlDotNet.RepresentationModel

  let fromYaml (content:string) =
    let rec analyze path (node:YamlNode) = 
      match node with
      | :? YamlMappingNode as o -> 
          let props = 
            o.Children
            |> Seq.map (
                  fun p -> 
                    let name = p.Key.ToString()
                    let np = path +> name
                    let value = analyze np p.Value
                    name, value
                )
            |> Seq.toList
          XObject(path, props)
      | :? YamlSequenceNode as s -> 
          s.Children
          |> Seq.mapi (
              fun i item -> 
                let np = sprintf "%s[%d]" path i
                analyze np item
              )
          |> Seq.toList
          |> fun r -> Collection(path, r)
      | :? YamlScalarNode as s ->
          Litteral(path, s.Value)
      | _ -> failwithf "node of type %A not supported" (node.GetType().Name)
    let yaml = YamlStream()
    use reader = new StringReader(content)
    yaml.Load(reader)
    let doc = yaml.Documents |> Seq.head
    analyze "" doc.RootNode
    
