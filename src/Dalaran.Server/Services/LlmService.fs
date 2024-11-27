namespace Dalaran.Server.Services

open System
open System.Collections.Generic
open System.Threading

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Microsoft.SemanticKernel
open Microsoft.SemanticKernel.ChatCompletion

open Azure.Storage.Blobs

type ILlmService =
    abstract Chat: KernelContent seq -> CancellationToken -> IAsyncEnumerable<StreamingChatMessageContent>

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

    let _AppendSAS (uri: Uri) =
        if uri.Host = _blobServiceClient.Uri.Host then
            let builder = UriBuilder uri
            builder.Query <- _blobContainerSAS
            builder.Uri
        else
            uri

    interface ILlmService with
        member _.Chat contents token =
            let itemCollection = ChatMessageContentItemCollection ()
            contents
            |> Seq.map (fun x ->
                match x with
                | :? ImageContent as image -> 
                    image.Uri <- _AppendSAS image.Uri
                    image :> KernelContent
                | _ -> x
                )
            |> Seq.iter (fun x -> itemCollection.Add x)

            let messageContent = ChatMessageContent (AuthorRole.User, itemCollection)
            let history = ChatHistory [messageContent]

            let executionSettings = PromptExecutionSettings (FunctionChoiceBehavior = FunctionChoiceBehavior.Auto())

            _chatCompletion.GetStreamingChatMessageContentsAsync (history, executionSettings, _kernel, token)
