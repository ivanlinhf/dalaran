namespace Dalaran.Server

open System.Text.Json

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Net.Http.Headers
open Microsoft.SemanticKernel
open Microsoft.SemanticKernel.Plugins.Web
open Microsoft.SemanticKernel.Plugins.Web.Bing

open Dalaran.Server.Services

module Program =
    let exitCode = 0

    [<EntryPoint>]
    let main args =

        let builder = WebApplication.CreateBuilder args

        builder.AddServiceDefaults() |> ignore

        builder.AddAzureBlobClient("AzureBlob") |> ignore

        builder.AddAzureCosmosClient(
            "AzureCosmos",
            configureClientOptions = fun x -> x.UseSystemTextJsonSerializerWithOptions <- JsonSerializerOptions()
        )
        |> ignore

        builder.AddAzureOpenAIClient("OpenAI", fun cs -> cs.Key <- builder.Configuration["OpenAI_Api_Key"])

        builder.Services.AddSingleton<WebSearchEnginePlugin>(fun _ ->
            let apiKey = builder.Configuration["Bing_Api_Key"]
            let connector = BingConnector apiKey
            WebSearchEnginePlugin connector)
        |> ignore

        builder.Services.AddSingleton<KernelPluginCollection>(fun sp ->
            let plugins =
                seq {
                    ()
                    |> sp.GetRequiredService<WebSearchEnginePlugin>
                    |> KernelPluginFactory.CreateFromObject
                }

            KernelPluginCollection plugins)
        |> ignore

        builder.Services.AddTransient<Kernel>(fun sp ->
            let pluginCollection = sp.GetRequiredService<KernelPluginCollection>()
            Kernel(sp, pluginCollection))
        |> ignore

        builder.Services.AddAzureOpenAIChatCompletion(builder.Configuration["OpenAI_Deployment_Name"])
        |> ignore

        builder.Services.AddTransient<IChatService, ChatService>() |> ignore

        builder.Services.AddControllers() |> ignore
        builder.Services.AddOpenApi() |> ignore

        builder.Services.AddCors(fun o ->
            o.AddPolicy(
                "AllowAll",
                fun b ->
                    b
                        .AllowAnyMethod()
                        .SetIsOriginAllowed(fun _ -> true)
                        .WithHeaders(HeaderNames.ContentType, "Access-Control-Allow-Origin")
                        .AllowAnyHeader()
                        .AllowAnyOrigin()
                    |> ignore
            )
            |> ignore)
        |> ignore

        let app = builder.Build()

        app.MapDefaultEndpoints() |> ignore
        app.MapControllers() |> ignore
        app.MapOpenApi() |> ignore

        app.UseCors("AllowAll") |> ignore

        app.Run()

        exitCode
