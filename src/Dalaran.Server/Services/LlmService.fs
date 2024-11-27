namespace Dalaran.Server.Services

open System
open System.Collections.Generic
open System.Threading

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Microsoft.SemanticKernel
open Microsoft.SemanticKernel.ChatCompletion

open Azure.Storage.Blobs
open FSharp.Control

type StreamingChatResponse = {
    Content: string
    IsFinished: bool
}

type ILlmService =
    abstract Chat: KernelContent seq -> CancellationToken -> IAsyncEnumerable<StreamingChatResponse>

module LlmHelper =
    let AppendSAS (uri: Uri) (targetUri: Uri) sas =
        if uri.Host = targetUri.Host then
            let builder = UriBuilder uri
            builder.Query <- sas
            builder.Uri
        else
            uri

    let GetChatResponse (content: StreamingChatMessageContent) =
        let found, value = content.Metadata.TryGetValue "FinishReason"
        {
            Content = content.Content
            IsFinished = found && value = "Stop"
        }

type LlmService (
    logger: ILogger<LlmService>,
    config: IConfiguration,
    chatCompletion: IChatCompletionService,
    kernel: Kernel,
    blobServiceClient: BlobServiceClient) =

    let _logger = logger
    let _chatCompletion = chatCompletion
    let _kernel = kernel
    let _blobServiceClient = blobServiceClient
    let _blobContainerSAS = config.["AzureBlob_Container_SAS"]

    interface ILlmService with
        member _.Chat contents token =
            let itemCollection = ChatMessageContentItemCollection ()
            contents
            |> Seq.map (fun x ->
                match x with
                | :? ImageContent as image -> 
                    image.Uri <- LlmHelper.AppendSAS image.Uri _blobServiceClient.Uri _blobContainerSAS
                    image :> KernelContent
                | _ -> x
                )
            |> Seq.iter (fun x -> itemCollection.Add x)

            let messageContent = ChatMessageContent (AuthorRole.User, itemCollection)
            let history = ChatHistory [messageContent]

            let executionSettings = PromptExecutionSettings (FunctionChoiceBehavior = FunctionChoiceBehavior.Auto())

            _chatCompletion.GetStreamingChatMessageContentsAsync (history, executionSettings, _kernel, token)
            |> TaskSeq.choose (fun x ->
                let resp = LlmHelper.GetChatResponse x
                if not (String.IsNullOrEmpty resp.Content) || resp.IsFinished then
                    Some resp
                else
                    None
                )
