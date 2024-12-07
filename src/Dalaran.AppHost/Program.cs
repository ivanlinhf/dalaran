using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var azureBlobConn = builder.AddConnectionString("AzureBlob");
var azureBlobContainer = builder.AddParameter("AzureBlob-ContainerName");

var azureCosmosConn = builder.AddConnectionString("AzureCosmos");
var azureCosmosNoSqlDatabase = builder.AddParameter("AzureCosmos-DatabaseName");
var azureCosmosNoSqlContainer = builder.AddParameter("AzureCosmos-ContainerName");

var bingApiKey = builder.AddParameter("Bing-ApiKey");

var openAiConn = builder.AddConnectionString("OpenAI");
var openAiApiKey = builder.AddParameter("OpenAI-ApiKey");
var openAiDeploymentName = builder.AddParameter("OpenAI-DeploymentName");

var server =
    builder.AddProject<Dalaran_Server>("server")
        .WithReference(azureBlobConn)
        .WithEnvironment("AzureBlob_Container_Name", azureBlobContainer)
        .WithReference(azureCosmosConn)
        .WithEnvironment("AzureCosmos_Database_Name", azureCosmosNoSqlDatabase)
        .WithEnvironment("AzureCosmos_Container_Name", azureCosmosNoSqlContainer)
        .WithEnvironment("Bing_Api_Key", bingApiKey)
        .WithReference(openAiConn)
        .WithEnvironment("OpenAI_Api_Key", openAiApiKey)
        .WithEnvironment("OpenAI_Deployment_Name", openAiDeploymentName);

builder.AddNpmApp("web", "../dalaran-web")
    .WithReference(server)
    .WaitFor(server)
    .WithEnvironment("BROWSER", "none")
    .WithEnvironment("VITE_SERVER_URL", server.GetEndpoint("http"))
    .WithHttpEndpoint(targetPort: 5173)
    .WithExternalHttpEndpoints();

builder.Build().Run();
