# ClientsForSwagger

This repository contains a preview version of a .Net code generator for API consuming documented by Swagger or Open API.

## Build project

```cmd
fake build
```

## Command line generator

Usage:

When your swagger spec is stored in a local file:

```cmd
*.exe --specfile ..\Files\petstore.json --outputfolder ../ClientsForSwagger.Sample/Generated --namespace  ClientsForSwagger.Sample.Generated --clientname PetstoreClient
``` 

When your swagger spec is downloadable from an endpoint:

```cmd
*.exe --specfile http://localhost:50464/swagger/v1/swagger.json --outputfolder ../ClientsForSwagger.Sample/Generated --namespace  ClientsForSwagger.Sample.Generated --clientname PetstoreClient
``` 

## Programming mindset

In following points, I will describe the main motivations of this project.

### Generate less code as possible

All REST clients inherit from a client base maintained in another assembly.

Generated code have to be simplest !

### Avoid exceptions with ROP (Railway oriented programming)

Generated client's methods return a `Result`.

#### REST API returning a simple OK status code.

Imagine an use case with messages API.

A generated method for message sending should be:

```C#
public Task<Result> Send(SendMessageRequest messageRequest, CancellationToken cancellationToken = default(CancellationToken))
{
    var request = new HttpRequestMessage(HttpMethod.Post, "/api/Messages/Send");
    base.AddBodyParameter(request, "messageRequest", messageRequest);
    return this.Execute(request, cancellationToken);
}
```
and definition of `SendMessageRequest` should be:

```C#
public class SendMessageRequest
{
    [JsonProperty("toUserId")]
    public string ToUserId { get; set; }

    [JsonProperty("content")]
    public string Content { get; set; }
}
```

Code sending a message should be:

```C#
var result = await client.Send(new SendMessageRequest{ ToUserId="1234", Content="Hello" });
if (result.IsSuccess)
{
    // do something
}
else
    logger.Error(result.ErrorMessage);
```

#### REST API returning a OK status code with a payload.

Imagine an use case with cars store API.

A generated method for requesting brands list should be:

```C#
public Task<Result<IEnumerable<Manufacturer>>> Brands(CancellationToken cancellationToken = default(CancellationToken))
{
    var request = new HttpRequestMessage(HttpMethod.Get, "/api/Cars/brands");
    return this.Execute<IEnumerable<Manufacturer>>(request, cancellationToken);
}
```

As you can see, this version of `Result` is a generic type.

Getting manufacturers should be like :

```C#
var result = await client.Brands();
if (result.IsSuccess)
{
    var manufacturers = result.Value.
    // do something
}
else
    logger.Error(result.ErrorMessage);
```

Or

```C#
var result = await client.Brands();
result
    .OnSuccess(manufacturers =>
        {
            // do something
            return Task.CompletedTask;
        })
        .OnError(error =>
        {
            logger.Error(error);
            return Task.CompletedTask;
        });
```

#### Chaining calls

Imagine an API crawling flow as:
- Calling brands.
- Requesting car models for each brands (if previous call succeeded).
- Gettings cars offers for each models (if previous call succeeded).

Code should be like:

```C#
var result = await client.Brands()
    .ThenMany(b => client.GetBrandModels(b.Id)) // not executed if brands failed
    .ThenMany(m => client.GetOffers(m.Manufacturer.Id, m.Id));  // not executed if models failed.

if (result.IsSuccess)
    var allOffers = result.Value;

```

### Handling success results with multiples payload types possibilities

Some APIs can return differents schemas for differents status codes.

Imagine a route with possible success responses:
- 200: `string`
- 204: empty (represented by `Nothing`)
- 206: `StrangeDto1`

Client's method will return a discriminated union.

Code using this route should be:

```C#
Result<DiscriminatedUnion<string, Nothing, StrangeDto1>> result = await client.Get(15);

if (!result.IsSuccess)
{
    logger.Error(error);
    return;
}
string value = result.Value
    .Match(
        s => s.ToUpper(),//executed if status code id 200
        nothing => "", //executed if status code id 204
        dto1 => dto1.Message //executed if status code id 206
    );
// ...
```


