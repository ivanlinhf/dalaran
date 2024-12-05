namespace Dalaran.Server.Services

open System
open System.Threading

open Microsoft.AspNetCore.Http
open Microsoft.Azure.Cosmos
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Microsoft.SemanticKernel
open Microsoft.SemanticKernel.ChatCompletion

open Azure.Storage.Blobs
open FSharp.Control

open Dalaran.Server.Models

module ChatHelper =
    let AppendSAS (uri: Uri) (targetUri: Uri) sas =
        if uri.Host = targetUri.Host then
            let builder = UriBuilder uri
            builder.Query <- sas
            builder.Uri
        else
            uri

    let GetChatResponse (content: StreamingChatMessageContent) =
        let found, value = content.Metadata.TryGetValue "FinishReason"

        { Content = content.Content
          IsFinished = found && value = "Stop" }

type ChatService
    (
        logger: ILogger<ChatService>,
        config: IConfiguration,
        chatCompletion: IChatCompletionService,
        kernel: Kernel,
        blobServiceClient: BlobServiceClient,
        cosmosClient: CosmosClient
    ) =

    let _logger = logger
    let _chatCompletion = chatCompletion
    let _kernel = kernel
    let _blobServiceClient = blobServiceClient
    let _blobContainer = config["AzureBlob_Container_Name"]
    let _blobContainerSAS = config.["AzureBlob_Container_SAS"]
    let _cosmosClient = cosmosClient
    let _cosmosDatabase = config["AzureCosmos_Database_Name"]
    let _cosmosContainer = config["AzureCosmos_Container_Name"]

    let _Upload (file: IFormFile) (token: CancellationToken) =
        async {
            token.ThrowIfCancellationRequested()

            let filename = Guid.NewGuid().ToString()
            let containerClient = _blobServiceClient.GetBlobContainerClient _blobContainer
            let blobClient = containerClient.GetBlobClient filename

            use stream = file.OpenReadStream()
            let! _ = blobClient.UploadAsync(stream, true, token) |> Async.AwaitTask

            return blobClient.Uri
        }

    interface IChatService with
        member _.Chat contents token =
            let itemCollection = ChatMessageContentItemCollection()

            contents
            |> Seq.map (fun x ->
                match x with
                | :? ImageContent as image ->
                    image.Uri <- ChatHelper.AppendSAS image.Uri _blobServiceClient.Uri _blobContainerSAS
                    image :> KernelContent
                | _ -> x)
            |> Seq.iter (itemCollection.Add)

            let messageContent = ChatMessageContent(AuthorRole.User, itemCollection)
            let history = ChatHistory [ messageContent ]

            let executionSettings =
                PromptExecutionSettings(FunctionChoiceBehavior = FunctionChoiceBehavior.Auto())

            _chatCompletion.GetStreamingChatMessageContentsAsync(history, executionSettings, _kernel, token)
            |> TaskSeq.choose (fun x ->
                let resp = ChatHelper.GetChatResponse x

                if not (String.IsNullOrEmpty resp.Content) || resp.IsFinished then
                    Some resp
                else
                    None)

        member _.Upload files token =
            async {
                let! uris = files |> Seq.map (fun file -> _Upload file token) |> Async.Sequential

                return { Uris = uris }
            }
