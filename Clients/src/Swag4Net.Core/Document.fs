namespace Swag4Net.Core

open System
open System.Net
open System.IO
open Newtonsoft.Json.Linq

type Name = string

type SProperty = Name * Value

and Value =
  | RawValue of string
  | SObject of SProperty list
  | SCollection of Value list

type Document =
  { Content:SProperty list }

