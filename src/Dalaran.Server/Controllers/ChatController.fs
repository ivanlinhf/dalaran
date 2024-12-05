namespace Dalaran.Server.Controllers

open System.ComponentModel.DataAnnotations
open System.Threading

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open Microsoft.SemanticKernel

open Dalaran.Server.Services

[<ApiController>]
[<Route("[controller]")>]
type ChatController(logger: ILogger<ChatController>, service: IChatService) =
    inherit ControllerBase()

    let _logger = logger
    let _service = service

    [<HttpPost>]
    [<Route("[action]")>]
    member _.Chat([<Required>] contents: KernelContent seq, token: CancellationToken) = _service.Chat contents token

    [<HttpPost>]
    [<Route("[action]")>]
    member _.Upload([<MinLength(1)>] files: IFormFileCollection, token: CancellationToken) = _service.Upload files token
