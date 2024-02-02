//---------------------------------------------------------------------------------
// Copyright (c) Jan 2024, devMobile Software
//
// Licensed under the Apache License, Version 2.0 see http://www.apache.org/licenses/LICENSE-2.0
//
// Demo Dapper spatial project built with Geo-A geospatial library for .NET(https://github.com/sibartlett/Geo)
// 
//---------------------------------------------------------------------------------
using System.Data;

using Microsoft.AspNetCore.Mvc;

using Geo.IO.Wkb;
using Geo.IO.Wkt;
using Geo.Geometries;

using Dapper;

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


app.MapGet("/Spatial/NearbyWkb", async (double latitude, double longitude, int distance, [FromServices] IDapperContext dapperContext) =>
{
   var origin = new Point(latitude, longitude).ToWkbBinary();

   using (var connection = dapperContext.ConnectionCreate())
   {
      var results = await connection.QueryAsync<Model.ListingNearbyListWkbDto>("[ListingsSpatialNearbyGeoWkb]", new { origin, distance }, commandType: CommandType.StoredProcedure);

      return results;
   }
})
.Produces<IList<Model.ListingNearbyListWkbDto>>(StatusCodes.Status200OK)
.WithOpenApi();


app.MapGet("/Spatial/NearbyWkt", async (double latitude, double longitude, int distance, [FromServices] IDapperContext dapperContext) =>
{
   var origin = new Point(latitude, longitude).ToWktString();

   using (var connection = dapperContext.ConnectionCreate())
   {
      var results = await connection.QueryAsync<Model.ListingNearbyListWktDto>("[ListingsSpatialNearbyGeoWkt]", new { origin, distance }, commandType: CommandType.StoredProcedure);

      return results;
   }
})
.Produces<IList<Model.ListingNearbyListWktDto>>(StatusCodes.Status200OK)
.WithOpenApi();

app.UseHttpsRedirection();

app.Run();


namespace Model
{
   internal record ListingNearbyListWkbDto
   {
      public Guid ListingUID { get; }
      public string? Name { get; set; }
      public string? ListingUrl { get; set; }
      public double Distance { get; set; }
      private byte[]? Wkb { get; set; }

      public Point Location => (Point)new WkbReader().Read(Wkb);
   }

   internal record ListingNearbyListWktDto
   {
      public Guid ListingUID { get; }
      public string? Name { get; set; }
      public string? ListingUrl { get; set; }
      public double Distance { get; set; }
      private string? Wkt { get; set; }

      public Point Location => (Point)new WktReader().Read(Wkt);
   }
}