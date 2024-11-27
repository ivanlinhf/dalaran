using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var azureBlobConn = builder.AddConnectionString("AzureBlob");
var azureBlobContainer = builder.AddParameter("AzureBlob-ContainerName");
var azureBlobContainerSAS = builder.AddParameter("AzureBlob-ContainerSAS");

var bingApiKey = builder.AddParameter("Bing-ApiKey");

var openAiConn = builder.AddConnectionString("OpenAI");
var openAiApiKey = builder.AddParameter("OpenAI-ApiKey");
var openAiDeploymentName = builder.AddParameter("OpenAI-DeploymentName");

var server =
    builder.AddProject<Dalaran_Server>("server")
        .WithReference(azureBlobConn)
        .WithEnvironment("AzureBlob_Container_Name", azureBlobContainer)
        .WithEnvironment("AzureBlob_Container_SAS", azureBlobContainerSAS)
        .WithEnvironment("Bing_Api_Key", bingApiKey)
        .WithReference(openAiConn)
        .WithEnvironment("OpenAI_Api_Key", openAiApiKey)
        .WithEnvironment("OpenAI_Deployment_Name", openAiDeploymentName);

builder.AddNpmApp("web", "../dalaran-web")
    .WithReference(server)
    .WaitFor(server)
    .WithEnvironment("BROWSER", "none")
    .WithHttpEndpoint(targetPort: 5173)
    .WithExternalHttpEndpoints();

builder.Build().Run();
