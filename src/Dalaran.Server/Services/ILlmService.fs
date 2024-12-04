namespace Dalaran.Server.Services

open System.Collections.Generic
open System.Threading

open Microsoft.SemanticKernel

open Dalaran.Server.Models

type ILlmService =
    abstract Chat: KernelContent seq -> CancellationToken -> IAsyncEnumerable<StreamingChatResponse>
