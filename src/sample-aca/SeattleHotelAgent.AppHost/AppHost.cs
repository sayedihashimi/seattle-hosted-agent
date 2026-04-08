var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.SeattleHotelAgent_Api>("hotel-api")
    .WithExternalHttpEndpoints();

builder.Build().Run();
