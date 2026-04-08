var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.SeattleHotelAgent_Hosted_Agent>("hotel-agent")
    .WithExternalHttpEndpoints();

builder.Build().Run();
