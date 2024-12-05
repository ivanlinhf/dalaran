namespace Dalaran.Server.Services

open System
open System.Linq

open Microsoft.Azure.Cosmos
open Microsoft.Azure.Cosmos.Linq
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Microsoft.SemanticKernel
open Microsoft.SemanticKernel.ChatCompletion

open Azure.Storage.Blobs
open FSharp.Control

open Dalaran.Server.Models

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

    interface IChatService with
        member _.Create() : ChatMeta =
            { ThreadId = Guid.NewGuid().ToString() }

        member _.AddMessages id contents token =
            async {
                let itemCollection = ChatMessageContentItemCollection()
                contents |> Seq.iter (itemCollection.Add)
                let messageContent = ChatMessageContent(AuthorRole.User, itemCollection)

                let message: ChatMessage =
                    { Id = Guid.NewGuid().ToString()
                      ThreadId = id
                      Content = messageContent
                      Timestamp = DateTime.UtcNow }

                let container = _cosmosClient.GetContainer(_cosmosDatabase, _cosmosContainer)

                let! _ =
                    container.CreateItemAsync<ChatMessage>(
                        message,
                        new PartitionKey(message.ThreadId),
                        cancellationToken = token
                    )
                    |> Async.AwaitTask

                return ()
            }

        member _.GetMessages id token =
            async {
                let container = _cosmosClient.GetContainer(_cosmosDatabase, _cosmosContainer)

                let query =
                    container.GetItemLinqQueryable<ChatMessage>().Where(fun x -> x.ThreadId = id).OrderBy(_.Timestamp)

                use iterator = query.ToFeedIterator()

                return!
                    taskSeq {
                        while iterator.HasMoreResults do
                            let! resp = iterator.ReadNextAsync(token)
                            yield! resp
                    }
                    |> TaskSeq.toListAsync
                    |> Async.AwaitTask
            }

        member this.Run id token =
            let messages =
                async { return! (this :> IChatService).GetMessages id token }
                |> Async.RunSynchronously

            let history = ChatHistory(messages |> List.map (_.Content))

            let executionSettings =
                PromptExecutionSettings(FunctionChoiceBehavior = FunctionChoiceBehavior.Auto())

            _chatCompletion.GetStreamingChatMessageContentsAsync(history, executionSettings, _kernel, token)
            |> TaskSeq.choose (fun x ->
                let found, value = x.Metadata.TryGetValue "FinishReason"

                let resp =
                    { Content = x.Content
                      IsFinished = found && value = "Stop" }

                if not (String.IsNullOrEmpty resp.Content) || resp.IsFinished then
                    Some resp
                else
                    None)

        member _.UploadImages id files token =
            async {
                let! uris =
                    files
                    |> Seq.map (fun x ->
                        async {
                            let filename = sprintf "%s_%s" id x.FileName
                            let containerClient = _blobServiceClient.GetBlobContainerClient _blobContainer
                            let blobClient = containerClient.GetBlobClient filename

                            use stream = x.OpenReadStream()
                            let! _ = blobClient.UploadAsync(stream, true, token) |> Async.AwaitTask

                            return blobClient.Uri
                        })
                    |> Async.Sequential

                return { Uris = uris }
            }
