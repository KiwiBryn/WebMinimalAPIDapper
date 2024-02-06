//---------------------------------------------------------------------------------
// Copyright (c) Jan 2024, devMobile Software
//
// Licensed under the Apache License, Version 2.0 see http://www.apache.org/licenses/LICENSE-2.0
//
//---------------------------------------------------------------------------------
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


const string ListingNeighbourhoodSQL = @"SELECT neighbourhoodUID, name, neighbourhood_url as neighbourhoodUrl FROM Neighbourhood ORDER BY Name";

const string ListingInNeighbourhoodSQL = @"SELECT neighbourhoodUID, name, neighbourhood_url as neighbourhoodUrl FROM Neighbourhood WHERE Neighbourhood.Boundary.STContains(geography::Point(@Latitude, @Longitude, 4326)) = 1";

const string ListingsNearbySQL = @"DECLARE @Origin AS GEOGRAPHY = geography::Point(@Latitude, @Longitude, 4326); 
                                  DECLARE @Circle AS GEOGRAPHY = @Origin.STBuffer(@distance); 
                                  SELECT uid as ListingUID, Name, listing_url as ListingUrl, @Origin.STDistance(Listing.Location) as Distance 
                                  FROM [listing] 
                                  WHERE @Circle.STContains(Listing.Location) = 1 ORDER BY Distance
                                  --WHERE Listing.Location.STWithin(@Circle) = 1 ORDER BY Distance";

const string ListingsNearbyLatitudeLongitudeSQL = @"DECLARE @Location AS GEOGRAPHY = geography::Point(@Latitude, @longitude,4326)
                                 DECLARE @Circle AS GEOGRAPHY = @Location.STBuffer(@distance);
                                 SELECT UID as ListingUID
	                              ,[Name]
	                              ,listing_url as ListingUrl
	                              ,Listing.Location.STDistance(@Location) as Distance
	                              ,latitude
                                 ,longitude
                                 FROM [listing]
                                 WHERE Listing.Location.STWithin(@Circle) = 1
                                 ORDER BY Distance";


app.MapGet("/Spatial/Neighbourhoods", async ([FromServices] IDapperContext dapperContext) =>
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
.Produces(StatusCodes.Status404NotFound)
.WithOpenApi();


app.MapGet("/Spatial/Nearby", async (double latitude, double longitude, double distance, [FromServices] IDapperContext dapperContext) =>
{
   using (var connection = dapperContext.ConnectionCreate())
   {
      return await connection.QueryWithRetryAsync<Model.ListingNearbyListDto>(ListingsNearbySQL, new { latitude, longitude, distance });
   }
})
.Produces<IList<Model.ListingNearbyListDto>>(StatusCodes.Status200OK)
.WithOpenApi();


app.MapGet("/Spatial/NearbyLatitudeLongitude", async (double latitude, double longitude, double distance, [FromServices] IDapperContext dapperContext) =>
{
   using (var connection = dapperContext.ConnectionCreate())
   {
      return await connection.QueryWithRetryAsync<Model.ListingNearbyListLatitudeLongitudeDto>(ListingsNearbyLatitudeLongitudeSQL, new { latitude, longitude, distance });
   }
})
.Produces<IList<Model.ListingNearbyListLatitudeLongitudeDto>>(StatusCodes.Status200OK)
.WithOpenApi();

app.Run();



namespace Model
{
   internal record NeighbourhoodListDto
   {
      public Guid NeighbourhoodUID { get; set; }
      public string Name { get; set; }
      public string NeighbourhoodUrl { get; set; }
   };

   internal record NeighbourhoodSearchDto
   {
      public Guid NeighbourhoodUID { get; set; }
      public string Name { get; set; }
      public string NeighbourhoodUrl { get; set; }
   };

   internal record ListingNearbyListDto
   {
      public Guid ListingUID { get; }
      public string Name { get; set; }
      public string ListingUrl { get; set; }
      public int Distance { get; set; }
   };

   internal record ListingNearbyListLatitudeLongitudeDto
   {
      public Guid ListingUID { get; }
      public string Name { get; set; }
      public string ListingUrl { get; set; }
      public int Distance { get; set; }
      public double Latitude { get; set; }
      public double Longitude { get; set; }
   };
}