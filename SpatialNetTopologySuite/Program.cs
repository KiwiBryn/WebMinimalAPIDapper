//---------------------------------------------------------------------------------
// Copyright (c) Feb 2024, devMobile Software
//
// Licensed under the Apache License, Version 2.0 see http://www.apache.org/licenses/LICENSE-2.0
//
// 
//---------------------------------------------------------------------------------
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

using devMobile.Dapper;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<IDapperContext>(s => new DapperContext(builder.Configuration));

builder.Services.ConfigureHttpJsonOptions(options =>
{
   options.SerializerOptions.IgnoreReadOnlyProperties = true;
   options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
   options.SerializerOptions.WriteIndented = true;
   options.SerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
   app.UseSwagger();
   app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/Listing/Search/Ado", async (double latitude, double longitude, int distance, [FromServices] IDapperContext dapperContext) =>
{
   var origin = new Point(longitude, latitude) { SRID = 4326 };

   using (SqlConnection connection = (SqlConnection)dapperContext.ConnectionCreate())
   {
      await connection.OpenAsync();

      var geographyWriter = new SqlServerBytesWriter { IsGeography = true };

      using (SqlCommand command = connection.CreateCommand())
      {
         command.CommandText = "ListingsSpatialNearbyGeography";
         command.CommandType = CommandType.StoredProcedure;

         var originParameter = command.CreateParameter();
         originParameter.ParameterName = "Origin";
         originParameter.Value = new SqlBytes(geographyWriter.Write(origin));
         originParameter.SqlDbType = SqlDbType.Udt;
         originParameter.UdtTypeName = "GEOGRAPHY";
         command.Parameters.Add(originParameter);

         var distanceParameter = command.CreateParameter();
         distanceParameter.ParameterName = "Distance";
         distanceParameter.Value = distance;
         distanceParameter.DbType = DbType.Int32;
         command.Parameters.Add(distanceParameter);

         var geographyReader = new SqlServerBytesReader { IsGeography = true };

         using (var dbDataReader = await command.ExecuteReaderAsync())
         {
            List<Model.ListingNearbyListGeographyDto> listings = new List<Model.ListingNearbyListGeographyDto>();

            int listingUIDColumn = dbDataReader.GetOrdinal("ListingUID");
            int nameColumn = dbDataReader.GetOrdinal("Name");
            int listingUrlColumn = dbDataReader.GetOrdinal("ListingUrl");
            int distanceColumn = dbDataReader.GetOrdinal("Distance");
            int LocationColumn = dbDataReader.GetOrdinal("Location");

            while (await dbDataReader.ReadAsync())
            {
               listings.Add(new Model.ListingNearbyListGeographyDto
               {
                  ListingUID = dbDataReader.GetGuid(listingUIDColumn),
                  Name = dbDataReader.GetString(nameColumn),
                  ListingUrl = dbDataReader.GetString(listingUrlColumn),
                  Distance = dbDataReader.GetDouble(distanceColumn),
                  Location = (Point)geographyReader.Read(dbDataReader.GetSqlBytes(LocationColumn).Value)
               });
            }

            return listings;
         }
      }
   }
})
.Produces<IList<Model.ListingNearbyListGeographyDto>>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.WithOpenApi();


app.MapGet("/Listing/Search/AdoSerialize", async (double latitude, double longitude, int distance, [FromServices] IDapperContext dapperContext) =>
{
   var origin = new Point(longitude, latitude) { SRID = 4326 };

   using (SqlConnection connection = (SqlConnection)dapperContext.ConnectionCreate())
   {
      await connection.OpenAsync();

      var geographyWriter = new SqlServerBytesWriter { IsGeography = true };

      using (SqlCommand command = connection.CreateCommand())
      {
         command.CommandText = "ListingsSpatialNearbyGeographySerialize";
         command.CommandType = CommandType.StoredProcedure;

         var originParameter = command.CreateParameter();
         originParameter.ParameterName = "Origin";
         originParameter.Value = new SqlBytes(geographyWriter.Write(origin));
         originParameter.SqlDbType = SqlDbType.Udt;
         originParameter.UdtTypeName = "GEOGRAPHY";
         command.Parameters.Add(originParameter);

         var distanceParameter = command.CreateParameter();
         distanceParameter.ParameterName = "Distance";
         distanceParameter.Value = distance;
         distanceParameter.DbType = DbType.Int32;
         command.Parameters.Add(distanceParameter);

         var geographyReader = new SqlServerBytesReader { IsGeography = true };

         using (var dbDataReader = await command.ExecuteReaderAsync())
         {
            List<Model.ListingNearbyListGeographyDto> listings = new List<Model.ListingNearbyListGeographyDto>();

            int listingUIDColumn = dbDataReader.GetOrdinal("ListingUID");
            int nameColumn = dbDataReader.GetOrdinal("Name");
            int listingUrlColumn = dbDataReader.GetOrdinal("ListingUrl");
            int distanceColumn = dbDataReader.GetOrdinal("Distance");
            int LocationColumn = dbDataReader.GetOrdinal("Location");

            while (await dbDataReader.ReadAsync())
            {
               listings.Add(new Model.ListingNearbyListGeographyDto
               {
                  ListingUID = dbDataReader.GetGuid(listingUIDColumn),
                  Name = dbDataReader.GetString(nameColumn),
                  ListingUrl = dbDataReader.GetString(listingUrlColumn),
                  Distance = dbDataReader.GetDouble(distanceColumn),
                  Location = (Point)geographyReader.Read(dbDataReader.GetSqlBytes(LocationColumn).Value)
               });
            }

            return listings;
         }
      }
   }
})
.Produces<IList<Model.ListingNearbyListGeographyDto>>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.WithOpenApi();

app.Run();



namespace Model
{
   internal record ListingNearbyListGeographyDto
   {
      public Guid ListingUID { get; set; }
      public string? Name { get; set; }
      public string? ListingUrl { get; set; }
      public double Distance { get; set; }
      public Point? Location { get; set; }
   }
}