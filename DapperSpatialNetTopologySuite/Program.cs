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
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

using devMobile.Dapper;
using devMobile.Azure.DapperTransient;

//https://stackoverflow.com/questions/58980355/microsoft-sqlserver-types-14-0-to-access-its-geographical-capabitlies
#if NETTOPOLOGY_SUITE_LOCATION
   SqlMapper.AddTypeHandler(new PointHandler());
#endif
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


app.MapGet("/Spatial/NearbyPoint", async (double latitude, double longitude, double distance, [FromServices] IDapperContext dapperContext) =>
{
   var origin = new Point(longitude, latitude) { SRID = 4326 };
   var locationWriter = new SqlServerBytesWriter() { IsGeography = true };

   SqlServerBytesReader reader = new SqlServerBytesReader()
   { IsGeography = true };

   using (var connection = dapperContext.ConnectionCreate())
   {
      return await connection.QueryWithRetryAsync<Model.ListingNearbyListDto>("ListingsNearbyGeography", new { Origin = locationWriter.Write(origin), distance }, commandType: CommandType.StoredProcedure);
   }
})
.Produces<IList<Model.ListingNearbyListDto>>(StatusCodes.Status200OK)
.WithOpenApi();


#if NETTOPOLOGY_SUITE_LOCATION
app.MapGet("/Spatial/NearbyGeography", async (double latitude, double longitude, int distance, [FromServices] IDapperContext dapperContext) =>
{
   var origin = new Point(longitude, latitude) { SRID = 4326 };

   using (var connection = dapperContext.ConnectionCreate())
   {
      var results = await connection.QueryWithRetryAsync<Model.ListingNearbyListGeographyDto>("ListingsSpatialNearbyGeography", new { origin, distance }, commandType: CommandType.StoredProcedure);

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
      var results = await connection.QueryWithRetryAsync<Model.ListingNearbyListGeographyDto>("ListingsSpatialNearbyGeographySerialize", new { origin, distance }, commandType: CommandType.StoredProcedure);

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
      var results = await connection.QueryWithRetryAsync<Model.ListingNearbyListGeographyDto>("ListingsSpatialNearbyGeographyWkb", new { origin, distance }, commandType: CommandType.StoredProcedure);

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
      var results = await connection.QueryWithRetryAsync<Model.ListingNearbyListGeographyDto>("ListingsSpatialNearbyGeographyWkt", new { origin, distance }, commandType: CommandType.StoredProcedure);

      return results;
   }
})
.Produces<IList<Model.ListingNearbyListGeographyDto>>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.WithOpenApi();
#endif

app.Run();


// Inspired by
//  https://github.com/NetTopologySuite/NetTopologySuite.IO.SqlServerBytes
//  https://blog.marcgravell.com/2014/07/dapper-gets-type-handlers-and-learns.html
//
#if NETTOPOLOGY_SUITE_LOCATION
class PointHandler : SqlMapper.TypeHandler<Point>
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

      var geometryWriter = new SqlServerBytesWriter { IsGeography = true };

      parameter.Value = geometryWriter.Write(value);
   }
}
#endif


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
   internal record ListingNearbyListDto
   {
      public Guid ListingUID { get; }
      [Required]
      public string? Name { get; set; }
      [Required]
      public string? ListingUrl { get; set; }
      [Required]
      public double Distance { get; set; }
   };

   internal record ListingNearbyListGeographyDto
   {
      public Guid ListingUID { get; set; }
      public string? Name { get; set; }
      public string? ListingUrl { get; set; }
      public double Distance { get; set; }
      public Point? Location { get; set; }
   }
}