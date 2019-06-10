open System

let ofOption error = function Some s -> Ok s | None -> Error error

type ResultBuilder() =
    member __.Return(x) = Ok x

    member __.ReturnFrom(m: Result<_, _>) = m

    member __.Bind(m, f) = Result.bind f m
    member __.Bind((m, error): (Option<'T> * 'E), f) = m |> ofOption error |> Result.bind f

    member __.Zero() = None

    member __.Combine(m, f) = Result.bind f m

    member __.Delay(f: unit -> _) = f

    member __.Run(f) = f()

    member __.TryWith(m, h) =
        try __.ReturnFrom(m)
        with e -> h e

    member __.TryFinally(m, compensation) =
        try __.ReturnFrom(m)
        finally compensation()

    member __.Using(res:#IDisposable, body) =
        __.TryFinally(body res, fun () -> match res with null -> () | disp -> disp.Dispose())

    member __.While(guard, f) =
        if not (guard()) then Ok () else
        do f() |> ignore
        __.While(guard, f)

    member __.For(sequence:seq<_>, body) =
        __.Using(sequence.GetEnumerator(), fun enum -> __.While(enum.MoveNext, __.Delay(fun () -> body enum.Current)))

let result = new ResultBuilder()

type MyErr = Err1 | Err2

let aa = // : Result<string, MyErr> = 
    result {
      let! (a: string) = Ok "a string"
      printfn "A: %A" a
      let! b = Error Err2
      printfn "B: %A" b
      let! c = (Some "c string", Err1)
    //   let! c = (None, Err1)
      printfn "C: %A" c
      let d = if true then a else c
      printfn "D: %A" d
      return d
    }

type TraceBuilder() =
  member this.Bind(m:'t option, f:'t -> 'u option) = 
      match m with 
      | None -> 
          printfn "Binding with None. Exiting."
      | Some a -> 
          printfn "Binding with Some(%A). Continuing" a
      Option.bind f m

  member this.Bind(m:Result<'t, 'e>, f:'t -> 'u option) = 
    None

  member this.Return(x) = 
      printfn "Returning a unwrapped %A as an option" x
      Some x

let trace = new TraceBuilder()

let cc n : Result<int,string> = Ok n

let rr:int option = 
  trace {
    let! a = Some 45
    let! b = Some 98
    let! c = Ok 98

    return 54
  }


type ParsingState<'TSuccess> = 
  { Result: Result<'TSuccess, ParsingError>
    Warnings: string list }
and ParsingError =
  | InvalidFormat of Message
  | UnhandledException of Exception
and Message = string

module State =
  let ofResult r = 
    { Result = r
      Warnings = List.empty }

  let bindResult (v:Result<'t, ParsingError>) (next:'t -> Result<'t, ParsingError>) =
    v |> Result.bind next |> ofResult
  
  let bind (binder:'T -> ParsingState<'U>) (v:ParsingState<'T>) : ParsingState<'U> =
    try
      match v.Result with
      | Ok v -> binder v
      | Error e -> 
          let state:ParsingState<'U> = { Result=Error e; Warnings=v.Warnings }
          state
    with e -> { Result=Error(UnhandledException e); Warnings=v.Warnings }

let r1 = Ok 2
let r2:Result<int,string> = Result.bind (fun v -> Ok (v+1)) r1


type ParsingWorkflowBuilder() =
    
  member this.Bind(m, f) = 
    m |> State.bind f
  
  member this.Bind(m, f) = 
    let state = m |> State.ofResult
    this.Bind(state, f)
  
  member this.Return(x) = 
    x |> Ok |> State.ofResult

  [<CustomOperation("warn",MaintainsVariableSpaceUsingBind=true)>]
  member this.Warn (state:ParsingState<'T>, text : string) = 
      { state with Warnings=text :: state.Warnings }

  member this.ReturnFrom(m) = 
    m

  member this.Yield(x:unit) = 
    1 |> Ok |> State.ofResult

  member this.Yield(x:Result<'s,ParsingError>) = 
    x |> State.ofResult

  member this.Yield(x:'t) = 
    x |> Ok |> State.ofResult

  member this.Yield(x:ParsingState<'s>) = 
    x

  member __.For(state:ParsingState<'T>, f : unit -> ParsingState<'U>) =
    let state2 = f()
    { state2 with Warnings = state2.Warnings @ state.Warnings }


let parsing = new ParsingWorkflowBuilder()


let r : ParsingState<int*string> = 
  parsing {
    let! a = Ok 8
    //let! b = Error "no"
    let! c = Ok "coucou"
    let! d = Ok true
    let e = "eee"

    let! f = parsing {
      let! a2 = Ok (a+1)
      return 10 + a2
    }

    warn "fail"
    warn "haha"

    //failwith "critical error"

   // printfn "results : %A" (a,c)

    return (4+f,c)
  }



type PackageDefinition = 
  {
      id : string
      version : string
  }
let defaultPackageDefinition = {id=""; version=""}

type NugetBuilder() = 

  member __.Yield (item:'a) : PackageDefinition =
    printfn "Yield %A" (item,typeof<'a>)
    defaultPackageDefinition

  [<CustomOperation("id")>] 
  member __.Id (spec, x) = {spec with id = x }

  [<CustomOperation("version")>] 
  member __.Version (spec : PackageDefinition, x : string) = 
      {spec with version = x}

let nuget = new NugetBuilder()

let nugetDef = 
  nuget {
      id "coucou"
      version "1.1"
  }

