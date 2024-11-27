namespace Dalaran.Server.Services

open System
open System.Threading

open Azure.Storage.Blobs
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging

type UploadResult = { Uris: Uri seq }

type IStorageService =
    abstract Upload: IFormFileCollection -> CancellationToken -> Async<UploadResult>

type StorageService (logger: ILogger<StorageService>, config: IConfiguration, blobServiceClient: BlobServiceClient) =
    let _logger = logger
    let _blobServiceClient = blobServiceClient
    let _blobContainer = config["AzureBlob_Container_Name"]

    let _Upload (file: IFormFile) (token: CancellationToken) =
        async {
            token.ThrowIfCancellationRequested ()

            let filename  = Guid.NewGuid().ToString()
            let containerClient = _blobServiceClient.GetBlobContainerClient _blobContainer
            let blobClient = containerClient.GetBlobClient filename

            use stream = file.OpenReadStream ()
            let! _ = blobClient.UploadAsync (stream, true, token) |> Async.AwaitTask

            return blobClient.Uri
        }

    interface IStorageService with
        member _.Upload files token =
            async {
                let! uris = files
                            |> Seq.map (fun file -> _Upload file token)
                            |> Async.Sequential

                return { Uris = uris }
            }
