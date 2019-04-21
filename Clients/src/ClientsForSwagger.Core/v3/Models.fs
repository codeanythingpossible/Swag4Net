namespace ClientsForSwagger.Core.v3

open System.Net

module Models =
  type TypeName = string
  type Documentation =
    { OpenApi:string
      Infos:Infos
      Servers:Server list
      Paths:Map<string, Path>
      Components:Components
      Security:SecurityRequirement list
      Tags:Tag list
      ExternalDocs:ExternalDocumentation }
  and Infos =
    { Description:string
      Version:string
      Title:string
      TermsOfService:string
      Contact:Contact option
      License:License option }
  and Contact = 
    | Email of string
    | Url of string
  and License = 
    { Name:string; Url:string }
  and Server = 
    {
      Url:string
      Description:string
      Variables:Map<string, ServerVariable> }
  and ServerVariable = 
    {
      Enum: string list
      Default: string
      Description: string }
  and Path =
    {
      Reference: string
      Summary:string
      Description:string
      Get:Operation
      Put:Operation
      Post:Operation
      Delete:Operation
      Options:Operation
      Head:Operation
      Patch:Operation
      Trace:Operation
      Servers:Server list
      Parameters:ParameterOrReference list
      }
  and ParameterOrReference =
      | P of Parameter
      | R of Reference
  and Parameter =
    {
      Name: string
      In: string
      Description: string
      Required: bool
      Deprecated: bool
      AllowEmptyValue: bool
      Style: string
      Explode: bool
      AllowReserved: bool
      Schema: SchemaOrReference
      Examples: Map<string, ExampleOrReference>
      Content: Map<string, MediaType>}
  and SchemaOrReference =
      | S of Schema
      | R of Reference
  and ExampleOrReference =
      | E of Example
      | R of Reference
  and Reference = string
  and MediaType =
    {
      Schema: SchemaOrReference
      Examples: Map<string, ExampleOrReference>
      Encoding: Map<string, Encoding> }
  and Schema =
