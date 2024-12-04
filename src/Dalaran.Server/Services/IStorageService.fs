namespace Dalaran.Server.Services

open System.Threading

open Microsoft.AspNetCore.Http

open Dalaran.Server.Models

type IStorageService =
    abstract Upload: IFormFileCollection -> CancellationToken -> Async<UploadResult>
