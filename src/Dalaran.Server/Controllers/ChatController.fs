namespace Dalaran.Server.Controllers

open System.ComponentModel.DataAnnotations
open System.Threading

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open Microsoft.SemanticKernel
open SixLabors.ImageSharp

open Dalaran.Server.Services

[<ApiController>]
[<Route("[controller]")>]
type ChatController(logger: ILogger<ChatController>, service: IChatService) =
    inherit ControllerBase()

    let _logger = logger
    let _service = service

    [<HttpPost>]
    member _.Create() = _service.Create()

    [<HttpPost>]
    [<Route("{id}")>]
    member _.AddMessages(id: string, [<Required>] contents: ChatMessageContent seq, token: CancellationToken) =
        _service.AddMessages id contents token

    [<HttpGet>]
    [<Route("{id}")>]
    member _.GetMessages(id: string, token: CancellationToken) = _service.GetMessages id token

    [<HttpPost>]
    [<Route("{id}/run")>]
    member _.Run(id: string, token: CancellationToken) = _service.Run id token

    [<HttpPost>]
    [<Route("{id}/images")>]
    member this.UploadImages(id: string, [<MinLength(1)>] images: IFormFileCollection, token: CancellationToken) =
        let hasError =
            images
            |> Seq.map (fun x ->
                async {
                    use stream = x.OpenReadStream()

                    try
                        let! _ = Image.DetectFormatAsync(stream, token) |> Async.AwaitTask
                        return false
                    with _ ->
                        return true
                }
                |> Async.RunSynchronously)
            |> Seq.contains true

        if hasError then
            this.BadRequest({| ErrorMessage = "One or more invalid image(s)." |}) :> IActionResult
        else
            this.Ok(_service.UploadImages id images token |> Async.RunSynchronously)
