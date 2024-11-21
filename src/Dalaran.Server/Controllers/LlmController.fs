namespace Dalaran.Server.Controllers

open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open Microsoft.SemanticKernel.ChatCompletion

[<ApiController>]
[<Route("[controller]")>]
type LlmController (logger : ILogger<LlmController>) =
    inherit ControllerBase ()

    [<HttpGet>]
    [<Route("[action]")>]
    member this.Chat (chatService: IChatCompletionService) =
        raise (System.NotImplementedException ())
