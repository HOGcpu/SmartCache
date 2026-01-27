using Azure.Storage.Blobs;

var builder = WebApplication.CreateBuilder(args);

// Orleans builder
builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();

    // Azure Blob Storage provider for grain persistence
    //siloBuilder.AddAzureBlobGrainStorage(
    //    name: "AzureBlobStore",
    //    configureOptions: options =>
    //    {
    //        // 
    //        options.BlobServiceClient = new BlobServiceClient(
    //            builder.Configuration["AzureStorage:ConnectionString"]
    //        );
    //    });

    // local testing for now
    siloBuilder.AddMemoryGrainStorage("AzureBlobStore");

});

// Orleans API client 
//builder.Services.AddOrleansClient(client =>
//{
//    client.UseLocalhostClustering();
//});

builder.Services.AddControllers();

// Swagger  /  OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartCache API v1");
    });
}

app.MapControllers();

app.Run();
