namespace Dalaran.Server.Services

open System.Collections.Generic
open System.Threading

open Microsoft.AspNetCore.Http
open Microsoft.SemanticKernel

open Dalaran.Server.Models

type IChatService =
    abstract Chat: KernelContent seq -> CancellationToken -> IAsyncEnumerable<StreamingChatResponse>
    abstract Upload: IFormFileCollection -> CancellationToken -> Async<UploadResult>
