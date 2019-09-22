#r "netstandard"

#r "../../packages/Microsoft.CodeAnalysis.common/2.3.1/lib/netstandard1.3/Microsoft.CodeAnalysis.dll"
#r "../../packages/Microsoft.CodeAnalysis.CSharp/2.3.1/lib/netstandard1.3/Microsoft.CodeAnalysis.CSharp.dll"
#r "../../packages/microsoft.csharp/4.4.0/lib/netstandard2.0/Microsoft.CSharp.dll"
#r "../../packages/microsoft.codeanalysis.analyzers/2.6.1/analyzers/dotnet/cs/Microsoft.CodeAnalysis.Analyzers.dll"
#r "../../packages/microsoft.codeanalysis.analyzers/2.6.1/analyzers/dotnet/cs/Microsoft.CodeAnalysis.CSharp.Analyzers.dll"

#r "System.Net.Http.dll"
#r "System.Text.Encoding"
#r "System.Collections.Immutable"

#load "C:\dev\Swag4Net-1\Clients\src\Swag4Net.Generators.RoslynGenerator\RoslynDsl.fs"

open System
open System.IO
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax


let (/>) a b = Path.Combine(a, b)

let code =
  """public class PetsApiClient : RestApiClientBase, IPetsApiClient
{
    public PetsApiClient(string baseUrl): base(baseUrl)
    {
    }

    public PetsApiClient(Uri baseUrl): base(baseUrl)
    {
    }

    public PetsApiClient(HttpClient client): base(client)
    {
    }

    public Task<Result<Pets>> ListPets(int limit, CancellationToken cancellationToken = default(CancellationToken))
    {
        var request = new HttpRequestMessage(HttpMethod.GET, "/pets");
        base.AddQueryParameter(request, "limit", limit);
        return this.Execute<Pets>(request, cancellationToken);
    }

    public Task<Result> CreatePets(CancellationToken cancellationToken = default(CancellationToken))
    {
        var request = new HttpRequestMessage(HttpMethod.POST, "/pets");
        return this.Execute(request, cancellationToken);
    }

    public Task<Result> UpdatePet(CancellationToken cancellationToken = default(CancellationToken))
    {
        var request = new HttpRequestMessage(HttpMethod.PATCH, "/pets");
        return this.Execute(request, cancellationToken);
    }

    public Task<Result<Pets>> ShowPetById(string petId, CancellationToken cancellationToken = default(CancellationToken))
    {
        var request = new HttpRequestMessage(HttpMethod.GET, "/pets/{petId}");
        base.AddPathParameter(request, "petId", petId);
        return this.Execute<Pets>(request, cancellationToken);
    }

    public Task<Result> UserById(CancellationToken cancellationToken = default(CancellationToken))
    {
        var request = new HttpRequestMessage(HttpMethod.GET, "/users/{id}");
        return this.Execute(request, cancellationToken);
    }

    public T GenericThing<T, K>(T arg1)
    {
      return default(K);
    }
}"""


let tree = CSharpSyntaxTree.ParseText code
let root = tree.GetCompilationUnitRoot()

let c = 
  root.Members 
  //|> Seq.map (fun m -> m.GetType().Name)
  |> Seq.choose (function | :? ClassDeclarationSyntax as c -> Some c | _ -> None)
  |> Seq.head

let methods = 
  c.Members
  |> Seq.choose (
      function
      | :? MethodDeclarationSyntax as m when m.Modifiers |> Seq.exists(fun t -> t.IsKind SyntaxKind.PublicKeyword) -> 
          SyntaxFactory
            .MethodDeclaration(m.ReturnType, m.Identifier)
            .WithConstraintClauses(m.ConstraintClauses)
            .WithParameterList(m.ParameterList)
            .WithSemicolonToken(SyntaxFactory.Token SyntaxKind.SemicolonToken)
          |> Some
      | _ -> None
    )
  |> Seq.cast<MemberDeclarationSyntax>
  |> Seq.toArray
  //|> List.map(fun m -> m.ToFullString())

let interfaceName = sprintf "I%s" (c.Identifier.ToFullString())
let ``interface`` = 
  SyntaxFactory
    .InterfaceDeclaration(interfaceName)
    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token SyntaxKind.PublicKeyword))
    .AddMembers(methods)

``interface``.NormalizeWhitespace().ToFullString()
