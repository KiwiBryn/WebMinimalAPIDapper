//---------------------------------------------------------------------------------
// Copyright (c) Jan 2024, devMobile Software
//
// Licensed under the Apache License, Version 2.0 see http://www.apache.org/licenses/LICENSE-2.0
//
//---------------------------------------------------------------------------------
using System.ComponentModel.DataAnnotations;
using System.Data;

using Microsoft.AspNetCore.Mvc;

using NetTopologySuite.IO;
using NetTopologySuite.Geometries;

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

const string ListingsNearbySQL = @"DECLARE @Origin AS GEOGRAPHY = geography::Point(@Latitude, @Longitude, 4326); 
                                  DECLARE @Circle AS GEOGRAPHY = @Origin.STBuffer(@distance); 
                                  SELECT TOP(200) ID, uid as ListingUID, Name, listing_url as ListingUrl, @Origin.STDistance(Listing.Location) as Distance 
                                  FROM [listing] 
                                  WHERE Listing.Location.STWithin(@Circle) = 1 ORDER BY Distance";


app.MapGet("/Spatial/Neighbourhoods", async ( [FromServices] IDapperContext dapperContext) =>
{ 
   using (var connection = dapperContext.ConnectionCreate())
   {
      return await connection.QueryWithRetryAsync<Model.NeighbourhoodListDto>(ListingNeighbourhoodSQL);
   }
})
.Produces<IList<Model.NeighbourhoodListDto>>(StatusCodes.Status200OK)
.WithOpenApi();


app.MapGet("/Spatial/Neighbourhood", async (double latitude, double longitude, [FromServices] IDapperContext dapperContext) =>
{
   Model.NeighbourhoodSearchDto neighbourhood;

   using (var connection = dapperContext.ConnectionCreate())
   {
      neighbourhood = await connection.QuerySingleOrDefaultWithRetryAsync<Model.NeighbourhoodSearchDto>(ListingInNeighbourhoodSQL, new { latitude, longitude });
   }

   if (neighbourhood is null)
   {
      return Results.Problem($"Neighbourhood for Latitude:{latitude} Longitude:{longitude} not found", statusCode: StatusCodes.Status404NotFound);
   }

   return Results.Ok(neighbourhood);
})
.Produces<IList<Model.NeighbourhoodSearchDto>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound )
.WithOpenApi();


app.MapGet("/Spatial/NearbyText", async (double latitude, double longitude, double distance, [FromServices] IDapperContext dapperContext) =>
{
   using (var connection = dapperContext.ConnectionCreate())
   {
      return await connection.QueryWithRetryAsync<Model.ListingNearbyListDto>(ListingsNearbySQL, new { latitude, longitude, distance });
   }
})
.Produces<IList<Model.ListingNearbyListDto>>(StatusCodes.Status200OK)
.WithOpenApi();


app.MapGet("/Spatial/NearbyPoint", async (double latitude, double longitude, double distance, [FromServices] IDapperContext dapperContext) =>
{
   var location = new Point(longitude, latitude) { SRID = 4326 };
   var locationWriter = new SqlServerBytesWriter() { IsGeography = true };

   SqlServerBytesReader reader = new SqlServerBytesReader()
   { IsGeography = true };

   using (var connection = dapperContext.ConnectionCreate())
   {
      return await connection.QueryWithRetryAsync<Model.ListingNearbyListDto>("ListingsNearbyGeography", new { location = locationWriter.Write(location), distance }, commandType: CommandType.StoredProcedure);
   }
})
.Produces<IList<Model.ListingNearbyListDto>>(StatusCodes.Status200OK)
.WithOpenApi();

app.Run();



namespace Model
{
   internal record NeighbourhoodListDto
   {
      public ulong Id { get; set; }
      public Guid NeighbourhoodUID { get; set; }
      [Required]
      public string? Name { get; set; }
      [Required]
      public string? Neighbourhood_url { get; set; }
   };

   internal record NeighbourhoodSearchDto
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
      public ulong Id { get; set; }
      public Guid ListingUID { get;}
      [Required]
      public string? Name { get; set; }
      [Required]
      public string? ListingUrl { get; set; }
      [Required]
      public double Distance { get; set; }
   };
}