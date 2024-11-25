namespace Dalaran.Server.Controllers

open System.Threading

open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open Microsoft.SemanticKernel

open Dalaran.Server.Services

[<ApiController>]
[<Route("[controller]")>]
type LlmController (logger : ILogger<LlmController>, service: ILlmService) =
    inherit ControllerBase ()

    let _logger = logger
    let _service = service

    [<HttpPost>]
    [<Route("[action]")>]
    member _.Chat ([<FromBody>] contents: KernelContent seq, token: CancellationToken) =
        _service.Chat(contents, token)
