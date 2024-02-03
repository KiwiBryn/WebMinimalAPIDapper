//---------------------------------------------------------------------------------
// Copyright (c) Jan 2024, devMobile Software
//
// Licensed under the Apache License, Version 2.0 see http://www.apache.org/licenses/LICENSE-2.0
//
// 
//---------------------------------------------------------------------------------
// NETTOPOLOGY_SUITE_LOCATION NETTOPOLOGY_SUITE_SERIALIZE NETTOPOLOGY_SUITE_WKB NETTOPOLOGY_SUITE_WKT
//#define NETTOPOLOGY_SUITE_LOCATION
//#define NETTOPOLOGY_SUITE_SERIALIZE
//#define NETTOPOLOGY_SUITE_WKB
//#define NETTOPOLOGY_SUITE_WKT

using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

using Dapper;

using devMobile.Dapper;

//https://stackoverflow.com/questions/58980355/microsoft-sqlserver-types-14-0-to-access-its-geographical-capabitlies
#if NETTOPOLOGY_SUITE_SERIALIZE
   SqlMapper.AddTypeHandler(new PointHandlerSerialise());
#endif
#if NETTOPOLOGY_SUITE_WKT
   SqlMapper.AddTypeHandler(new PointHandlerWkt());
#endif
#if NETTOPOLOGY_SUITE_WKB
   SqlMapper.AddTypeHandler(new PointHandlerWkb());
#endif
#if NETTOPOLOGY_SUITE_WKT
   SqlMapper.AddTypeHandler(new PointHandlerWkt());
#endif

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


#if NETTOPOLOGY_SUITE_LOCATION
app.MapGet("/Spatial/NearbyGeography", async (double latitude, double longitude, int distance, [FromServices] IDapperContext dapperContext) =>
{
   var origin = new Point(longitude, latitude) { SRID = 4326 };

   using (var connection = dapperContext.ConnectionCreate())
   {
      var results = await connection.QueryAsync<Model.ListingNearbyListGeographyDto>("ListingsSpatialNearbyGeography", new { origin, distance }, commandType: CommandType.StoredProcedure);

      return results;
   }
})
.Produces<IList<Model.ListingNearbyListGeographyDto>>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.WithOpenApi();
#endif


#if NETTOPOLOGY_SUITE_SERIALIZE
app.MapGet("/Spatial/NearbyGeographySerialize", async (double latitude, double longitude, int distance, [FromServices] IDapperContext dapperContext) =>
{
   var origin = new Point(longitude, latitude) { SRID = 4326 };

   using (var connection = dapperContext.ConnectionCreate())
   {
      var results = await connection.QueryAsync<Model.ListingNearbyListGeographyDto>("ListingsSpatialNearbyGeographySerialize", new { origin, distance }, commandType: CommandType.StoredProcedure);

      return results;
   }
})
.Produces<IList<Model.ListingNearbyListGeographyDto>>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.WithOpenApi();
#endif


#if NETTOPOLOGY_SUITE_WKB
app.MapGet("/Spatial/NearbyGeographyWkb", async (double latitude, double longitude, int distance, [FromServices] IDapperContext dapperContext) =>
{
   var origin = new Point(longitude, latitude) { SRID = 4326 };

   using (var connection = dapperContext.ConnectionCreate())
   {
      var results = await connection.QueryAsync<Model.ListingNearbyListGeographyDto>("ListingsSpatialNearbyGeographyWkb", new { origin, distance }, commandType: CommandType.StoredProcedure);

      return results;
   }
})
.Produces<IList<Model.ListingNearbyListGeographyDto>>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.WithOpenApi();
#endif


#if NETTOPOLOGY_SUITE_WKT
app.MapGet("/Spatial/NearbyGeographyWkt", async (double latitude, double longitude, int distance, [FromServices] IDapperContext dapperContext) =>
{
   var origin = new Point(longitude, latitude) { SRID = 4326 };

   using (var connection = dapperContext.ConnectionCreate())
   {
      var results = await connection.QueryAsync<Model.ListingNearbyListGeographyDto>("ListingsSpatialNearbyGeographyWkt", new { origin, distance }, commandType: CommandType.StoredProcedure);

      return results;
   }
})
.Produces<IList<Model.ListingNearbyListGeographyDto>>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.WithOpenApi();
#endif


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


// Inspired by
//  https://github.com/NetTopologySuite/NetTopologySuite.IO.SqlServerBytes
//  https://blog.marcgravell.com/2014/07/dapper-gets-type-handlers-and-learns.html
//
#if NETTOPOLOGY_SUITE_WKB
class PointHandlerWkb : SqlMapper.TypeHandler<Point>
{
   public override Point Parse(object value)
   {
      var reader = new WKBReader();

      return (Point)reader.Read((byte[])value);
   }
      

   public override void SetValue(IDbDataParameter parameter, Point? value)
   {
      ((SqlParameter)parameter).SqlDbType = SqlDbType.Udt;  // @Origin parameter?
      ((SqlParameter)parameter).UdtTypeName = "GEOGRAPHY";

      var geometryWriter = new SqlServerBytesWriter { IsGeography = true };

      parameter.Value = geometryWriter.Write(value);
   }
}
#endif

#if NETTOPOLOGY_SUITE_WKT
class PointHandlerWkt : SqlMapper.TypeHandler<Point>
{
   public override Point Parse(object value)
   {
      var reader = new SqlServerBytesReader { IsGeography = true };

      return (Point)reader.Read((byte[])value);
   }

   public override void SetValue(IDbDataParameter parameter, Point? value)
   {
      ((SqlParameter)parameter).SqlDbType = SqlDbType.Udt;  // @Origin parameter?
      ((SqlParameter)parameter).UdtTypeName = "GEOGRAPHY";

      parameter.Value = new SqlServerBytesWriter() { IsGeography = true }.Write(value);
   }
}
#endif

// Inspired some more by
//  https://github.com/bricelam
//
#if NETTOPOLOGY_SUITE_SERIALIZE
class PointHandlerSerialise : SqlMapper.TypeHandler<Point>
{
   public override Point Parse(object value)
   {
      var reader = new SqlServerBytesReader { IsGeography = true };

      return (Point)reader.Read((byte[])value);
   }

   public override void SetValue(IDbDataParameter parameter, Point? value)
   {
      ((SqlParameter)parameter).SqlDbType = SqlDbType.Udt;  // @Origin parameter?
      ((SqlParameter)parameter).UdtTypeName = "GEOGRAPHY";

      var writer = new SqlServerBytesWriter { IsGeography = true };

      parameter.Value = writer.Write(value);
   }
}
#endif


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