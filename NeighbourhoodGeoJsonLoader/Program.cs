//---------------------------------------------------------------------------------
// Copyright (c) Jan 2024, devMobile Software
//
// Licensed under the Apache License, Version 2.0 see http://www.apache.org/licenses/LICENSE-2.0
//
//---------------------------------------------------------------------------------
/*
https://bertwagner.com/tag/geojson.html

https://datatracker.ietf.org/doc/html/rfc7946
3.1.6.  Polygon
To specify a constraint specific to Polygons, it is useful to introduce the concept of a linear ring:

   o A linear ring is a closed LineString with four or more positions.

   o  The first and last positions are equivalent, and they MUST contain
      identical values; their representation SHOULD also be identical.

   o  A linear ring is the boundary of a surface or the boundary of a
      hole in a surface.

   o  A linear ring MUST follow the right-hand rule with respect to the
      area it bounds, i.e., exterior rings are counterclockwise, and
      holes are clockwise.

https://learn.microsoft.com/en-us/sql/relational-databases/spatial/polygon?view=sql-server-ver16

he interior of the polygon in an ellipsoidal system is defined by the "left-hand rule": if you imagine yourself 
walking along the ring of a geography Polygon, following the points in the order in which they are listed, the 
area on the left is being treated as the interior of the Polygon, and the area on the right as the exterior of 
the Polygon.

*/
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;

using Microsoft.SqlServer.Types;

using Dapper;

string jsonString = File.ReadAllText("neighbourhoods.GeoJson");


using (SqlConnection connection = new SqlConnection("This is not the connection string you are looking for"))
{
   connection.Open();

   var neighbourHoods = JsonSerializer.Deserialize<GeoJSON.Text.Feature.FeatureCollection>(jsonString)!;

   Console.WriteLine($"Features:{neighbourHoods.Features.Count}");
   foreach (var feature in neighbourHoods.Features)
   {
      string neighbourhood = feature.Properties["neighbourhood"].ToString();

      Console.WriteLine($"Neighbourhood:{neighbourhood}");

      var geometry = (GeoJSON.Text.Geometry.MultiPolygon)feature.Geometry;

      var s = new SqlGeographyBuilder();

      s.SetSrid(4326);

      s.BeginGeography(OpenGisGeographyType.MultiPolygon);
      s.BeginGeography(OpenGisGeographyType.Polygon); // A
      
      Console.WriteLine($"Polygon coordinates:{geometry.Coordinates.Count}");
      foreach (var coordinates in geometry.Coordinates)
      {
        //s.BeginGeography(OpenGisGeographyType.Polygon); // B

         Console.WriteLine($"LineString coordinates:{coordinates.Coordinates.Count}");
         foreach (var c in coordinates.Coordinates)
         {
            Console.WriteLine($"Point coordinates:{c.Coordinates.Count}");

            s.BeginFigure(c.Coordinates[0].Latitude, c.Coordinates[0].Longitude, null, null);

            for (int i = 1; i < c.Coordinates.Count; i++)
            {
               s.AddLine(c.Coordinates[i].Latitude, c.Coordinates[i].Longitude);

               Console.Write('.');
            }
            Console.WriteLine();

            s.EndFigure();
         }
         //s.EndGeography(); //B
      }

      s.EndGeography(); //A
      s.EndGeography(); // OpenGisGeographyType.MultiPolygon

      connection.Execute("INSERT INTO Neighbourhood (Name, Boundary) VALUES(@Neighbourhood, geography::STMPolyFromText(@boundary, 4326))", new { neighbourhood, boundary = s.ConstructedGeography.ToString()});

      Console.WriteLine();
   }
}

Console.WriteLine("loaded press <enter> to exit");
Console.ReadLine();
