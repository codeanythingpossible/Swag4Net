namespace Swag4Net.Core.v3

//open System.Net

//module Models =
//  type TypeName = string
//  type Documentation =
//    { OpenApi:string
//      Infos:Infos
//      Servers:Server list option
//      Paths:Map<string, Path>
//      Components:Components option
//      Security:SecurityRequirement list option
//      Tags:Tag list option
//      ExternalDocs:ExternalDocumentation option }
//  and Infos =
//    { Description:string
//      Version:string
//      Title:string
//      TermsOfService:string
//      Contact:Contact option
//      License:License option }
//  and Component =
//    {
//      Schemas: Map<string, SchemaOrReference> option
//      Responses: Map<string, ResponseOrReference> option
//      Parameters: Map<string, ParameterOrReference> option
//      Examples: Map<string, ExampleOrReference> option
//      RequestBodies: Map<string, RequestOrReference> option
//      Headers: Map<string, HeaderOrReference> option
//      SecuritySchemes: Map<string, SecuritySchemeOrReference> option
//      Links: Map<string, LinkOrReference> option
//      Callbacks: Map<string, CallbackOrReference> option }
//  and Contact = 
//    | Email of string
//    | Url of string
//  and License = 
//    { Name:string; Url:string }
//  and Server = 
//    {
//      Url:string
//      Description:string
//      Variables:Map<string, ServerVariable> }
//  and ServerVariable = 
//    {
//      Enum: string list
//      Default: string
//      Description: string }
//  and Path =
//    {
//      Reference: string
//      Summary:string
//      Description:string
//      Get:Operation
//      Put:Operation
//      Post:Operation
//      Delete:Operation
//      Options:Operation
//      Head:Operation
//      Patch:Operation
//      Trace:Operation
//      Servers:Server list
//      Parameters:ParameterOrReference list
//      }
//  and Operation =
//    {
//      Tags: string list option
//      Summary: string option
//      Description: string option
//      ExternalDocs: ExternalDocumentation option
//      OperationId: string 
//      Parameters: ParameterOrReference list option
//      RequestBody: RequestOrReference option
//      Responses: Responses
//      Callbacks: Map<string, CallbackOrReference> option
//      Deprecated: bool option
//      Security: SecurityRequirement list option
//      Servers: Server lsit option }
//  and Responses =
//  and ParameterOrReference =
//      | P of Parameter
//      | R of Reference
//  and Parameter =
//    {
//      Name: string
//      In: string
//      Description: string
//      Required: bool
//      Deprecated: bool
//      AllowEmptyValue: bool
//      Style: string
//      Explode: bool
//      AllowReserved: bool
//      Schema: SchemaOrReference
//      Example: Any
//      Examples: Map<string, ExampleOrReference>
//      Content: Map<string, MediaType>}
//  and SchemaOrReference =
//      | S of Schema
//      | R of Reference
//  and ExampleOrReference =
//      | E of Example
//      | R of Reference
//  and Reference = string
//  and MediaType =
//    {
//      Schema: SchemaOrReference
//      Examples: Map<string, ExampleOrReference>
//      Encoding: Map<string, Encoding> }
//  and Schema =
//    {
//      Title:string
//      AllOf: SchemaOrReference
//      OneOf: SchemaOrReference
//      AnyOf: SchemaOrReference
//      Not: SchemaOrReference
//      MultipleOf: SchemaOrReference
//      Items: ItemType
//      Maximum: int
//      ExclusiveMaximum: int
//      Minimum: int
//      ExclusiveMinimum: int
//      MaxLength: int
//      MinLength: int
//      Pattern: RegularExpression
//      MaxItems: int
//      MinItems: int
//      UniqueItems: bool
//      MaxProperties: int
//      MinProperties: int
//      Properties: Map<string, Schema>
//      AdditionalProperties: AdditionalProperties
//      Required: bool
//      Nullable: bool
//      Enum: Any list
//      Format: Format
//      Discriminator: Discriminator
//      Readonly: bool
//      WriteOnly: bool
//      Xml: Xml
//      ExternalDocs:ExternalDocumentation
//      Example: Any
//      Deprecated:bool
//    }
//  and AdditionalProperties =
//    | B of bool
//    | M of Map<string, Schema>
//  and ItemType =
//    | T of TypeName
//    | S of Schema
//    | R of Reference
  
