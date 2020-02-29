namespace Swag4Net.Core

module Parsing =

  open System
  open System.IO
  open Document

  type ParsingState<'TSuccess> = 
    { Result: Result<'TSuccess, ParsingError>
      Warnings: string list }
  and ParsingError =
    | InvalidFormat of Message
    | UnhandledException of Exception
  and Message = string
  
  module ParsingState =
    let ofResult r = 
      { Result = r
        Warnings = List.empty }
  
    let success o = 
      { Result = Ok o
        Warnings = List.empty }
  
    let map f state = 
      { Result = state.Result |> Result.map f
        Warnings = state.Warnings }
  
    let FailureOf e =
      { Result = Error e
        Warnings = List.empty }
  
    let Empty = 
      { Result = Ok ()
        Warnings = List.empty }
  
    let bindResult (v:Result<'t, ParsingError>) (next:'t -> Result<'t, ParsingError>) =
      v |> Result.bind next |> ofResult
    
    let toOption (v:ParsingState<'T>) =
      match v.Result with
      | Ok r -> Some r
      | _ -> None
  
  
    let bind (binder:'T -> ParsingState<'U>) (v:ParsingState<'T>) : ParsingState<'U> =
      try
        match v.Result with
        | Ok v -> binder v
        | Error e -> 
            let state:ParsingState<'U> = { Result=Error e; Warnings=v.Warnings }
            state
      with e -> { Result=Error(UnhandledException e); Warnings=v.Warnings }
  
    let bindWith (binder:'T -> 'U) (v:ParsingState<'T>) : ParsingState<'U> =
      try
        match v.Result with
        | Ok v' -> 
            let r = binder v'
            { Result=Ok r ; Warnings = v.Warnings}
        | Error e -> 
            let state:ParsingState<'U> = { Result=Error e; Warnings=v.Warnings }
            state
      with e -> { Result=Error(UnhandledException e); Warnings=v.Warnings }
  
    let combine (a:ParsingState<'T>) (b:ParsingState<'T>) : ParsingState<'T list> =
      match a.Result, b.Result with
      | Ok v1, Ok v2 -> { Result=Ok[v1;v2]; Warnings=a.Warnings @ b.Warnings }
      | Error e , _-> { Result=Error e; Warnings=a.Warnings @ b.Warnings }
      | _, Error e -> { Result=Error e; Warnings=a.Warnings @ b.Warnings }
  
    let combine' (a:ParsingState<'T list>) (b:ParsingState<'T>) : ParsingState<'T list> =
      match a.Result, b.Result with
      | Ok v1, Ok v2 -> { Result=Ok( v2 :: v1 ); Warnings=a.Warnings @ b.Warnings }
      | Error e , _-> { Result=Error e; Warnings=a.Warnings @ b.Warnings }
      | _, Error e -> { Result=Error e; Warnings=a.Warnings @ b.Warnings }
  
  
  type ParsingWorkflowBuilder() =
      
    member this.Bind(m:ParsingState<'T>, f:'T -> ParsingState<'U>) = 
      m |> ParsingState.bind f
    
    member this.Bind(m:Result<'T, ParsingError>, f:'T -> ParsingState<'U>) = 
      let state = m |> ParsingState.ofResult
      this.Bind(state, f)
  
    member this.Bind(error:ParsingError, f:'T -> ParsingState<'U>) = 
      let m = Error error
      let state = m |> ParsingState.ofResult
      this.Bind(state, f)
    
    member this.Bind(results:Result<ParsingState<'T> list, ParsingError>, f:'T list -> ParsingState<'U>) = 
      match results with
      | Ok states ->
          match states with
          | [] -> f []
          | h :: t -> 
              let r = match h.Result with Ok i -> Ok [i] | Error e -> Error e
              let head : ParsingState<'T list> = { Result=r; Warnings=h.Warnings }
              let state = t |> List.fold (fun acc s -> s |> ParsingState.combine' acc) head
              state |> ParsingState.bind f
      | Error e -> Error e |> ParsingState.ofResult
  
    member this.Bind(states:ParsingState<'T> list, f:'T list -> ParsingState<'U> ) : ParsingState<'U> = 
      match states with
      | [] -> f []
      | h :: t -> 
          let r = match h.Result with Ok i -> Ok [i] | Error e -> Error e
          let head : ParsingState<'T list> = { Result=r; Warnings=h.Warnings }
          let state = t |> List.fold (fun acc s -> s |> ParsingState.combine' acc) head
          state |> ParsingState.bind f
  
    member this.Return(x) = 
      x |> Ok |> ParsingState.ofResult
  
    [<CustomOperation("warn",MaintainsVariableSpaceUsingBind=true)>]
    member this.Warn (state:ParsingState<'T>, text : string) = 
        { state with Warnings=text :: state.Warnings }
  
    member this.ReturnFrom(error:ParsingError) = 
      let m = Error error
      m |> ParsingState.ofResult
  
    member this.ReturnFrom(r) = 
      let m = Ok r
      m |> ParsingState.ofResult
  
    member this.Yield(x:unit) = 
      1 |> Ok |> ParsingState.ofResult
  
    member this.Yield(x:Result<'s,ParsingError>) = 
      x |> ParsingState.ofResult
  
    member this.Yield(x:'t) = 
      x |> Ok |> ParsingState.ofResult
  
    member this.Yield(x:ParsingState<'s>) = 
      x
  
    //member __.Zero() = () |> Ok |> ParsingState.ofResult
    
    member __.Comine (a,b) = 
      ParsingState.combine a b
  
    member __.For(state:ParsingState<'T>, f : unit -> ParsingState<'U>) =
      let state2 = f()
      { state2 with Warnings = state2.Warnings @ state.Warnings }
  
  let parsing = new ParsingWorkflowBuilder()
  
  let readString name (token:Value) =
    match token |> selectToken name with
    | Some (RawValue v) -> Ok(string v)
    | _ -> Error (InvalidFormat <| sprintf "Missing field '%s' in %A" name token)
  
  let readStringOption name (token:Value) =
    match token |> selectToken name with
    | Some (RawValue v) -> Some (string v)
    | _ -> None
  
  let readIntOption name (token:Value) =
    match token |> selectToken name with
    | Some (RawValue v) -> 
        match v with
        | :? Int32 as i -> Some (i)
        | :? Int16 as i -> Some (int i)
        | :? Int64 as i -> Some (int i)
        | :? String as i -> Some (int i)
        | _ -> None
    | _ -> None
  
  let readBoolWithDefault name defaultValue (token:Value) =
    match token |> selectToken name with
    | Some (RawValue v) ->
        match v with
        | :? Boolean as b -> b
        | :? String as s -> Boolean.TryParse s |> snd
        | _ -> defaultValue
    | _ -> defaultValue
  
  let readBool name token =
    readBoolWithDefault name false token


