namespace Dalaran.Server.Services

open System.Collections.Generic
open System.Threading

open Microsoft.AspNetCore.Http
open Microsoft.SemanticKernel

open Dalaran.Server.Models

type IChatService =
    abstract Create: unit -> ChatMeta
    abstract AddMessages: string -> ChatMessageContent seq -> CancellationToken -> Async<unit>
    abstract GetMessages: string -> CancellationToken -> Async<ChatMessage list>
    abstract Run: string -> CancellationToken -> IAsyncEnumerable<StreamingChatResponse>
    abstract UploadImages: string -> IFormFileCollection -> CancellationToken -> Async<UploadImagesResult>
