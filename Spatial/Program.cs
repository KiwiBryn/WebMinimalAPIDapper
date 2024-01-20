//---------------------------------------------------------------------------------
// Copyright (c) Jan 2024, devMobile Software
//
// Licensed under the Apache License, Version 2.0 see http://www.apache.org/licenses/LICENSE-2.0
//
//---------------------------------------------------------------------------------
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;

using devMobile.Azure.DapperTransient;
using devMobile.Dapper;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<IDapperContext>(s => new DapperContext(builder.Configuration));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
   app.UseSwagger();
   app.UseSwaggerUI();
}

app.UseHttpsRedirection();


const string ListingNeighbourhoodSQL = @"SELECT Id, neighbourhoodUID, name, neighbourhood_url FROM Neighbourhood ORDER BY Name";

const string ListingInNeighbourhoodSQL = @"SELECT Id, neighbourhoodUID, name, neighbourhood_url FROM Neighbourhood WHERE Neighbourhood.Boundary.STContains(geography::Point(@Latitude, @Longitude, 4326)) = 1";

const string ListingnearbySQL = @"DECLARE @Origin AS GEOGRAPHY = geography::Point(@Latitude, @Longitude, 4326); 
                                  DECLARE @Circle AS GEOGRAPHY = @Origin.STBuffer(@distance); 

                                 SELECT ID, name, description, @Origin.STDistance(Listing.Location) as Distance 
                                 FROM [listing] 
                                 WHERE Listing.Location.STWithin(@Circle) = 1 ORDER BY Distance";


app.MapGet("/Spatial/Neighbourhoods", async ( [FromServices] IDapperContext dappperContext) =>
{ 
   using (var connection = dappperContext.ConnectionCreate())
   {
      return await connection.QueryWithRetryAsync<Model.NeightbourhoodListDto>(ListingNeighbourhoodSQL );
   }
})
.Produces<IList<Model.NeightbourhoodListDto>>(StatusCodes.Status200OK)
.WithOpenApi();

app.MapGet("/Spatial/Neighbourhood", async (double latitude, double longitude, [FromServices] IDapperContext dappperContext) =>
{
   using (var connection = dappperContext.ConnectionCreate())
   {
      return await connection.QuerySingleOrDefaultWithRetryAsync<Model.NeightbourhoodSearchDto>(ListingInNeighbourhoodSQL, new { latitude, longitude });
   }
})
.Produces<IList<Model.NeightbourhoodListDto>>(StatusCodes.Status200OK)
.WithOpenApi();

app.MapGet("/Spatial/Nearby", async (double latitude, double longitude, double distance, [FromServices] IDapperContext dappperContext) =>
{
   using (var connection = dappperContext.ConnectionCreate())
   {
      return await connection.QueryWithRetryAsync<Model.ListingNearbyListDto>(ListingnearbySQL, new { latitude, longitude, distance });
   }
})
.Produces<IList<Model.ListingNearbyListDto>>(StatusCodes.Status200OK)
.WithOpenApi();

app.Run();

namespace Model
{
   internal record NeightbourhoodListDto
   {
      public ulong Id { get; set; }
      public Guid NeighbourhoodUID { get; set; }
      [Required]
      public string? Name { get; set; }
      [Required]
      public string? Neighbourhood_url { get; set; }
   };

   internal record NeightbourhoodSearchDto
   {
      public ulong Id { get; set; }
      public Guid NeighbourhoodUID { get; set; }
      [Required]
      public string? Name { get; set; }
      [Required]
      public string? Neighbourhood_url { get; set; }
   };

   internal record ListingNearbyListDto
   {
      public Guid NeighbourhoodUID { get; set; }
      public ulong Id { get; set; }
      [Required]
      public string? Name { get; set; }
      [Required]
      public double Distance { get; set; }
      [Required]
      public string? Neighbourhood_url { get; set; }
   };
}