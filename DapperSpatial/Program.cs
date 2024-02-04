//---------------------------------------------------------------------------------
// Copyright (c) Jan 2024, devMobile Software
//
// Licensed under the Apache License, Version 2.0 see http://www.apache.org/licenses/LICENSE-2.0
//
// Big thanks to https://github.com/bricelam
// 
//---------------------------------------------------------------------------------
using System.ComponentModel.DataAnnotations;
using System.Data;

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

app.MapGet("/Spatial/NearbyLatitudeLongitude", async (double latitude, double longitude, double distance, [FromServices] IDapperContext dapperContext) =>
{
   using (var connection = dapperContext.ConnectionCreate())
   {
      return await connection.QueryWithRetryAsync<Model.ListingNearbyListLatitudeLongitudeDto>("ListingsSpatialNearbyLatitudeLongitude", new { latitude, longitude, distance }, commandType: CommandType.StoredProcedure);
   }
})
.Produces<IList<Model.ListingNearbyListLatitudeLongitudeDto>>(StatusCodes.Status200OK)
.WithOpenApi();

app.Run();


namespace Model
{
   internal record ListingNearbyListLatitudeLongitudeDto
   {
      public Guid ListingUID { get; }
      [Required]
      public string? Name { get; set; }
      [Required]
      public string? ListingUrl { get; set; }
      public double Distance { get; set; }
      public double Latitude { get; set; }
      public double Longitude { get; set; }
   };
}