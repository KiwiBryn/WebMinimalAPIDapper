//---------------------------------------------------------------------------------
// Copyright (c) Jan 2024, devMobile Software
//
// Licensed under the Apache License, Version 2.0 see http://www.apache.org/licenses/LICENSE-2.0
//
//---------------------------------------------------------------------------------
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;

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

// Embedded SQL so queries easy to tweak
const string LookupByIdSql = @"SELECT Id, [Name], Listing_URL AS ListingURL
                     FROM ListingsHosts
                     WHERE id = @Id";

const string SearchCountSql = @"SELECT COUNT(*) FROM ListingsHosts WHERE [Name] LIKE N'%' + @SearchText + N'%'";

const string SearchPaginatedIdSql = @"SELECT Id, [Name], Listing_URL AS ListingURL
                     FROM ListingsHosts
                     WHERE[Name] LIKE N'%' + @SearchText + N'%'
                     ORDER By[Name] 
                     OFFSET @PageSize *(@PageNumber - 1) ROWS FETCH NEXT @PageSize ROWS ONLY";


// The MapGets start here (including ones that don't work so well
app.MapGet("/Listing/Search/Sync", (string q, int pageNumber, int pageSize, [FromServices] IDapperContext dapperContext) =>
{
   using (var connection = dapperContext.ConnectionCreate())
   {
      return connection.QueryWithRetry(SearchPaginatedIdSql, new { searchText = q , pageNumber, pageSize }); // IEnumerable<Dynamic>
   }
})
.WithOpenApi();


app.MapGet("/Listing/Search/Async", async (string q, int pageNumber, int pageSize, [FromServices] IDapperContext dapperContext) =>
{
   using (var connection = dapperContext.ConnectionCreate())
   {
      return await connection.QueryWithRetryAsync(SearchPaginatedIdSql, new { searchText = q, pageNumber, pageSize }); // IEnumerable<Dynamic>
   }
})
.WithOpenApi();


app.MapGet("/Listing/Search/Typed", async Task<IEnumerable<Model.ListingListDto>> (string q, int pageNumber, int pageSize, [FromServices] IDapperContext dapperContext) =>
{
   using (var connection = dapperContext.ConnectionCreate())
   {
      return await connection.QueryWithRetryAsync<Model.ListingListDto>(SearchPaginatedIdSql, new { searchText = q, pageNumber, pageSize }); // IEnumerable<ListingListDto>
   }
})
.WithOpenApi();


app.MapGet("/Listing/Search/Produces", async (string q, int pageNumber, int pageSize, [FromServices] IDapperContext dapperContext) =>
{
   using (var connection = dapperContext.ConnectionCreate())
   {
      return await connection.QueryWithRetryAsync<Model.ListingListDto>(SearchPaginatedIdSql, new { searchText = q, pageNumber, pageSize });
   }
})
.Produces<IList<Model.ListingListDto>>(StatusCodes.Status200OK)
.WithOpenApi();


app.MapGet("/Listing/Count", async Task<int> (string q, [FromServices] IDapperContext dapperContext) =>
{
   using (var connection = dapperContext.ConnectionCreate())
   {
      return await connection.ExecuteScalarWithRetryAsync<int>(SearchCountSql, new { searchText = q});
   }
})
.WithOpenApi();


app.MapGet("/Listing/Produces/{id:long}", async (long id, IDapperContext dapperContext) =>
{
   using (var connection = dapperContext.ConnectionCreate())
   {
      Model.ListingLookupDto result = await connection.QuerySingleOrDefaultWithRetryAsync<Model.ListingLookupDto>(LookupByIdSql, new { id });
      if (result is null)
      {
         return Results.Problem($"Listing {id} not found", statusCode: StatusCodes.Status404NotFound);
      }

      return Results.Ok(result);
   }
})
.Produces<Model.ListingLookupDto>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status404NotFound)
.WithOpenApi();


app.MapGet("/Listing/Results/{id:long}", async Task<Results<Ok<Model.ListingLookupDto>, NotFound>> (long id, IDapperContext dapperContext) =>
{
   using (var connection = dapperContext.ConnectionCreate())
   {
      Model.ListingLookupDto searchResultSet = await connection.QuerySingleOrDefaultWithRetryAsync<Model.ListingLookupDto>(LookupByIdSql, new { id });
      if (searchResultSet is null)
      {
         return TypedResults.NotFound();
      }

      return TypedResults.Ok(searchResultSet);
   }
})
.WithOpenApi();


app.MapGet("/Listing/Search/ValidatedQuery", async (
   //[FromQuery,Required, MinLength(Constants.SearchTextMinimumLength, ErrorMessage = "SearchTextMaximumLength"), MaxLength(Constants.SearchTextMaximumLength, ErrorMessage = "SearchTextMaximumLength")]
   [FromQuery, StringLength(Constants.SearchTextMaximumLength, ErrorMessage = "SearchTextMaximumLength SearchTextMaximumLength",MinimumLength=Constants.SearchTextMinimumLength)]
   string q,
   [FromQuery, Range(Constants.PageNumberMinimum, Constants.PageNumberMaximum, ErrorMessage = "PageNumberMinimum PageNumberMaximum")]
   int pageNumber,
   [FromQuery, Range(Constants.PageSizeMinimum, Constants.PageSizeMaximum, ErrorMessage = "PageSizeMinimum PageSizeMaximum")]
   int pageSize,
   [FromServices] IDapperContext dapperContext) =>
{
   using (var connection = dapperContext.ConnectionCreate())
   {
      return await connection.QueryWithRetryAsync<Model.ListingListDto>(SearchPaginatedIdSql, new { searchText = q, pageNumber, pageSize });
   }
})
.Produces<IList<Model.ListingListDto>>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.WithOpenApi();


app.MapGet("/Listing/Search/AsParameters", async ([AsParameters] SearchParameters searchParameters,
   [FromServices] IDapperContext dapperContext) =>
{
   using (var connection = dapperContext.ConnectionCreate())
   {
      return await connection.QueryWithRetryAsync<Model.ListingListDto>(SearchPaginatedIdSql, new { searchText = searchParameters.Q, searchParameters.PageNumber, searchParameters.PageSize });
   }
})
.Produces<IList<Model.ListingListDto>>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.WithOpenApi();


app.MapGet("/Listing/Search/DocumentedInCode", async (
   [FromQuery, Required, MinLength(Constants.SearchTextMinimumLength, ErrorMessage = "SearchTextMinimumLength"), MaxLength(Constants.SearchTextMaximumLength, ErrorMessage = "SearchTextMaximumLength")]
   string q,
   [Range(Constants.PageNumberMinimum, Constants.PageNumberMaximum, ErrorMessage = "PageNumberMinimum PageNumberMaximum")]
   int pageNumber,
   [Range(Constants.PageSizeMinimum, Constants.PageSizeMaximum, ErrorMessage = "PageSizeMinimum PageSizeMaximum")]
   int pageSize,
   [FromServices] IDapperContext dapperContext) =>
{
   using (var connection = dapperContext.ConnectionCreate())
   {
      return await connection.QueryWithRetryAsync<Model.ListingListDto>(SearchPaginatedIdSql, new { searchText = q, pageNumber, pageSize });
   }
})
.Produces<IList<Model.ListingListDto>>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.WithName("GetListingsValidated")
.WithOpenApi(operation => new(operation)
{
   Summary = "This is a summary",
   Description = "This is a description"
})
.WithOpenApi(generatedOperation =>
{
   var searchTextParameter = generatedOperation.Parameters[0];
   searchTextParameter.Description = "This is the search text";

   var pageNumberParameter = generatedOperation.Parameters[1];
   pageNumberParameter.Description = "This is the pageNumber parameter";

   var pageSizeParameter = generatedOperation.Parameters[2];
   pageSizeParameter.Description = "This is the pageNumber parameter";

   return generatedOperation;
});


app.MapGet("/Listing/Search/AdoSync", ([AsParameters] SearchParameters searchParameters, [FromServices] IDapperContext dapperContext) =>
{
   using (DbConnection connection = (DbConnection)dapperContext.ConnectionCreate())
   {
      connection.Open();

      using (DbCommand command = connection.CreateCommand())
      {
         command.CommandText = SearchPaginatedIdSql;

         var searchTextParameter = command.CreateParameter();
         searchTextParameter.ParameterName = "SearchText";
         searchTextParameter.Value = searchParameters.Q;
         searchTextParameter.DbType = DbType.String;
         command.Parameters.Add(searchTextParameter);

         var pageNumberParameter = command.CreateParameter();
         pageNumberParameter.ParameterName = "PageNumber";
         pageNumberParameter.Value = searchParameters.PageNumber;
         pageNumberParameter.DbType = DbType.Byte;
         command.Parameters.Add(pageNumberParameter);

         var pageSizeParameter = command.CreateParameter();
         pageSizeParameter.ParameterName = "PageSize";
         pageSizeParameter.Value = searchParameters.PageSize;
         pageSizeParameter.DbType = DbType.Byte;
         command.Parameters.Add(pageSizeParameter);

         using (var dbDataReader = command.ExecuteReader())
         {
            List<Model.ListingListDto> listings = new List<Model.ListingListDto>();

            int idColumn = dbDataReader.GetOrdinal("Id");
            int nameColumn = dbDataReader.GetOrdinal("Name");
            int listingUrlColumn = dbDataReader.GetOrdinal("ListingURL");

            while (dbDataReader.Read())
            {
               listings.Add(new Model.ListingListDto
               {
                  Id = (ulong)dbDataReader.GetInt64(idColumn),
                  Name = dbDataReader.GetString(nameColumn),
                  ListingURL = dbDataReader.GetString(listingUrlColumn),
               });
            }

            return listings;
         }
      }
   }
})
.Produces<IList<Model.ListingListDto>>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.WithOpenApi();


app.MapGet("/Listing/Search/AdoAsync", async ([AsParameters] SearchParameters searchParameters, [FromServices] IDapperContext dapperContext) =>
{
   using (DbConnection connection = (DbConnection)dapperContext.ConnectionCreate())
   {
      await connection.OpenAsync();

      using (DbCommand command = connection.CreateCommand())
      {
         command.CommandText = SearchPaginatedIdSql;

         var searchTextParameter = command.CreateParameter();
         searchTextParameter.ParameterName = "SearchText";
         searchTextParameter.Value = searchParameters.Q;
         searchTextParameter.DbType = DbType.String;
         command.Parameters.Add(searchTextParameter);

         var pageNumberParameter = command.CreateParameter();
         pageNumberParameter.ParameterName = "PageNumber";
         pageNumberParameter.Value = searchParameters.PageNumber;
         pageNumberParameter.DbType = DbType.Byte;
         command.Parameters.Add(pageNumberParameter);

         var pageSizeParameter = command.CreateParameter();
         pageSizeParameter.ParameterName = "PageSize";
         pageSizeParameter.Value = searchParameters.PageSize;
         pageSizeParameter.DbType = DbType.Byte;
         command.Parameters.Add(pageSizeParameter);

         using (var dbDataReader = await command.ExecuteReaderAsync())
         {
            List<Model.ListingListDto> listings = new List<Model.ListingListDto>();

            int idColumn = dbDataReader.GetOrdinal("Id");
            int nameColumn = dbDataReader.GetOrdinal("Name");
            int listingUrlColumn = dbDataReader.GetOrdinal("ListingURL");

            while (await dbDataReader.ReadAsync())
            {
               listings.Add(new Model.ListingListDto
               {
                  Id = (ulong)dbDataReader.GetInt64(idColumn),
                  Name = dbDataReader.GetString(nameColumn),
                  ListingURL = dbDataReader.GetString(listingUrlColumn),
               });
            }

            return listings;
         }
      }
   }
})
.Produces<IList<Model.ListingListDto>>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.WithOpenApi();

app.MapGet("/Listing/Search/AdoAsyncYield", AdoAsyncYield);

app.Run();


static async IAsyncEnumerable<Model.ListingListDto> AdoAsyncYield([AsParameters] SearchParameters searchParameters, [FromServices] IDapperContext dapperContext)
{
   using (DbConnection connection = (DbConnection)dapperContext.ConnectionCreate())
   {
      await connection.OpenAsync();

      using (DbCommand command = connection.CreateCommand())
      {
         command.CommandText = SearchPaginatedIdSql;

         var searchTextParameter = command.CreateParameter();
         searchTextParameter.ParameterName = "SearchText";
         searchTextParameter.Value = searchParameters.Q;
         searchTextParameter.DbType = DbType.String;
         command.Parameters.Add(searchTextParameter);

         var pageNumberParameter = command.CreateParameter();
         pageNumberParameter.ParameterName = "PageNumber";
         pageNumberParameter.Value = searchParameters.PageNumber;
         pageNumberParameter.DbType = DbType.Byte;
         command.Parameters.Add(pageNumberParameter);

         var pageSizeParameter = command.CreateParameter();
         pageSizeParameter.ParameterName = "PageSize";
         pageSizeParameter.Value = searchParameters.PageSize;
         pageSizeParameter.DbType = DbType.Byte;
         command.Parameters.Add(pageSizeParameter);

         using (var dbDataReader = await command.ExecuteReaderAsync())
         {
            List<Model.ListingListDto> listings = new List<Model.ListingListDto>();

            int idColumn = dbDataReader.GetOrdinal("Id");
            int nameColumn = dbDataReader.GetOrdinal("Name");
            int listingUrlColumn = dbDataReader.GetOrdinal("ListingURL");

            while (await dbDataReader.ReadAsync())
            {
               yield return new Model.ListingListDto
               {
                  Id = (ulong)dbDataReader.GetInt64(idColumn),
                  Name = dbDataReader.GetString(nameColumn),
                  ListingURL = dbDataReader.GetString(listingUrlColumn),
               };
            }
         }
      }
   }
}


public record Constants
{
   public const byte SearchTextMinimumLength = 3;
   public const byte SearchTextMaximumLength = 20;
   public const byte PageNumberMinimum = 1;
   public const byte PageNumberMaximum = 100;
   public const byte PageSizeMinimum = 5;
   public const byte PageSizeMaximum = 50;
}


public record SearchParameters
{
   // https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/2658 possibly related?

   //[FromQuery, Required, MinLength(Constants.SearchTextMinimumLength, ErrorMessage = "SearchTextMinimumLength"), MaxLength(Constants.SearchTextMaximumLength, ErrorMessage = "SearchTextMaximumLength")]
   //[Required, MinLength(Constants.SearchTextMinimumLength, ErrorMessage = "SearchTextMinimumLength"), MaxLength(Constants.SearchTextMaximumLength, ErrorMessage = "SearchTextMaximumLength")]
   //[MinLength(Constants.SearchTextMinimumLength, ErrorMessage = "SearchTextMinimumLength"), MaxLength(Constants.SearchTextMaximumLength, ErrorMessage = "SearchTextMaximumLength")]
   [StringLength(Constants.SearchTextMaximumLength, ErrorMessage = "SearchTextMaximumLength SearchTextMaximumLength", MinimumLength = Constants.SearchTextMinimumLength)]
   public string Q { get; set; }

   //[FromQuery, Range(Constants.PageNumberMinimum, Constants.PageNumberMaximum, ErrorMessage = "PageNumberMinimum PageNumberMaximum")]
   //[Required, Range(Constants.PageNumberMinimum, Constants.PageNumberMaximum, ErrorMessage = "PageNumberMinimum PageNumberMaximum")]
   [Range(Constants.PageNumberMinimum, Constants.PageNumberMaximum, ErrorMessage = "PageNumberMinimum PageNumberMaximum")]
   public int PageNumber { get; set; }

   [Range(Constants.PageSizeMinimum, Constants.PageSizeMaximum, ErrorMessage = "PageSizeMinimum PageSizeMaximum")]
   public int PageSize { get; set; }
}

namespace Model
{
   public record ListingListDto
   {
      public ulong Id { get; set; }
      [Required]
      public string? Name { get; set; }
      [Required]
      public string? ListingURL { get; set; }
   };

   public record ListingLookupDto
   {
      public long Id { get; set; }
      [Required]
      public string? Name { get; set; }
      [Required]
      public string? ListingURL { get; set; }
   };
}