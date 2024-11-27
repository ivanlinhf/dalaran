namespace Dalaran.Server.Services

open System.Collections.Generic
open System.Threading

open Microsoft.Extensions.Logging
open Microsoft.SemanticKernel
open Microsoft.SemanticKernel.ChatCompletion

type ILlmService =
    abstract Chat: KernelContent seq -> CancellationToken -> IAsyncEnumerable<StreamingChatMessageContent>

type LlmService (logger: ILogger<LlmService>, chatCompletion: IChatCompletionService, kernel: Kernel) =
    let _logger = logger
    let _chatCompletion = chatCompletion
    let _kernel = kernel

    interface ILlmService with
        member _.Chat contents token =
            let itemCollection = ChatMessageContentItemCollection ()
            contents |> Seq.iter (fun x -> itemCollection.Add x)

            let messageContent = ChatMessageContent (AuthorRole.User, itemCollection)
            let history = ChatHistory [messageContent]

            let executionSettings = PromptExecutionSettings (FunctionChoiceBehavior = FunctionChoiceBehavior.Auto())

            _chatCompletion.GetStreamingChatMessageContentsAsync (history, executionSettings, _kernel, token)
