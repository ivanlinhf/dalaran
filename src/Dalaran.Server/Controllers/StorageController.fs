namespace Dalaran.Server.Controllers

open System.ComponentModel.DataAnnotations
open System.Threading

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging

open Dalaran.Server.Services

[<ApiController>]
[<Route("[controller]")>]
type StorageController (logger: ILogger<StorageController>, service: IStorageService) =
    inherit ControllerBase ()

    let _logger = logger
    let _service = service

    [<HttpPost>]
    [<Route("[action]")>]
    member _.Upload ([<MinLength(1)>] files: IFormFileCollection, token: CancellationToken) =
        _service.Upload files token
