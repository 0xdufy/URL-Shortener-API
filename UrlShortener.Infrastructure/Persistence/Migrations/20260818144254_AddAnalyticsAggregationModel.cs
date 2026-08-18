using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrlShortener.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsAggregationModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShortUrlAnalyticsAggregates",
                columns: table => new
                {
                    ShortUrlId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BucketStartUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    Granularity = table.Column<byte>(type: "tinyint", nullable: false),
                    Dimension = table.Column<byte>(type: "tinyint", nullable: false),
                    DimensionSchemaVersion = table.Column<short>(type: "smallint", nullable: false),
                    DimensionValue = table.Column<string>(type: "varchar(253)", unicode: false, maxLength: 253, nullable: false, collation: "Latin1_General_100_BIN2"),
                    ClickCount = table.Column<long>(type: "bigint", nullable: false),
                    UniqueVisitorCount = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShortUrlAnalyticsAggregates", x => new { x.ShortUrlId, x.Granularity, x.Dimension, x.DimensionSchemaVersion, x.BucketStartUtc, x.DimensionValue });
                    table.ForeignKey(
                        name: "FK_ShortUrlAnalyticsAggregates_ShortUrls_ShortUrlId",
                        column: x => x.ShortUrlId,
                        principalTable: "ShortUrls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShortUrlAnalyticsDailyVisitors",
                columns: table => new
                {
                    ShortUrlId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentityPeriodUtc = table.Column<DateOnly>(type: "date", nullable: false),
                    PseudonymousVisitorId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    IdentityScheme = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    FirstSeenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShortUrlAnalyticsDailyVisitors", x => new { x.ShortUrlId, x.IdentityPeriodUtc, x.PseudonymousVisitorId });
                    table.ForeignKey(
                        name: "FK_ShortUrlAnalyticsDailyVisitors_ShortUrls_ShortUrlId",
                        column: x => x.ShortUrlId,
                        principalTable: "ShortUrls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsAggregates_Link_Query",
                table: "ShortUrlAnalyticsAggregates",
                columns: new[] { "ShortUrlId", "Granularity", "Dimension", "DimensionSchemaVersion", "BucketStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsDailyVisitors_Period_Link",
                table: "ShortUrlAnalyticsDailyVisitors",
                columns: new[] { "IdentityPeriodUtc", "ShortUrlId" });

            migrationBuilder.Sql("""
                INSERT INTO [ShortUrlAnalyticsDailyVisitors]
                    ([ShortUrlId], [IdentityPeriodUtc], [PseudonymousVisitorId],
                     [IdentityScheme], [FirstSeenAtUtc])
                SELECT
                    [ShortUrlId],
                    [VisitorIdentityPeriodUtc],
                    [PseudonymousVisitorId],
                    MAX([VisitorIdentityScheme]),
                    MIN([AccessedAtUtc])
                FROM [ShortUrlAccessLogs]
                WHERE [VisitorIdentityPeriodUtc] IS NOT NULL
                  AND [PseudonymousVisitorId] IS NOT NULL
                  AND [VisitorIdentityScheme] IS NOT NULL
                GROUP BY [ShortUrlId], [VisitorIdentityPeriodUtc], [PseudonymousVisitorId];

                INSERT INTO [ShortUrlAnalyticsAggregates]
                    ([ShortUrlId], [BucketStartUtc], [Granularity], [Dimension],
                     [DimensionSchemaVersion], [DimensionValue], [ClickCount],
                     [UniqueVisitorCount], [UpdatedAtUtc])
                SELECT
                    [ShortUrlId],
                    DATEADD(
                        HOUR,
                        DATEDIFF(HOUR, CAST('2000-01-01' AS datetime2), [AccessedAtUtc]),
                        CAST('2000-01-01' AS datetime2)),
                    1, 0, 1, 'All', COUNT_BIG(*), 0, SYSUTCDATETIME()
                FROM [ShortUrlAccessLogs]
                GROUP BY
                    [ShortUrlId],
                    DATEADD(
                        HOUR,
                        DATEDIFF(HOUR, CAST('2000-01-01' AS datetime2), [AccessedAtUtc]),
                        CAST('2000-01-01' AS datetime2));

                INSERT INTO [ShortUrlAnalyticsAggregates]
                    ([ShortUrlId], [BucketStartUtc], [Granularity], [Dimension],
                     [DimensionSchemaVersion], [DimensionValue], [ClickCount],
                     [UniqueVisitorCount], [UpdatedAtUtc])
                SELECT
                    [ShortUrlId], CAST(CAST([AccessedAtUtc] AS date) AS datetime2),
                    2, 0, 1, 'All', COUNT_BIG(*),
                    COUNT_BIG(DISTINCT [PseudonymousVisitorId]), SYSUTCDATETIME()
                FROM [ShortUrlAccessLogs]
                GROUP BY [ShortUrlId], CAST([AccessedAtUtc] AS date);

                INSERT INTO [ShortUrlAnalyticsAggregates]
                    ([ShortUrlId], [BucketStartUtc], [Granularity], [Dimension],
                     [DimensionSchemaVersion], [DimensionValue], [ClickCount],
                     [UniqueVisitorCount], [UpdatedAtUtc])
                SELECT
                    logs.[ShortUrlId], CAST(CAST(logs.[AccessedAtUtc] AS date) AS datetime2),
                    2, 1, 1, dimensions.[DimensionValue], COUNT_BIG(*), 0, SYSUTCDATETIME()
                FROM [ShortUrlAccessLogs] AS logs
                CROSS APPLY
                (
                    SELECT CASE
                        WHEN logs.[Referer] IS NULL OR LTRIM(RTRIM(logs.[Referer])) = '' THEN 'Direct'
                        WHEN LEN(logs.[Referer]) > 253
                          OR logs.[Referer] LIKE '%/%'
                          OR logs.[Referer] LIKE '% %'
                          OR logs.[Referer] LIKE '%:%' THEN 'Unknown'
                        ELSE LOWER(logs.[Referer])
                    END AS [DimensionValue]
                ) AS dimensions
                GROUP BY logs.[ShortUrlId], CAST(logs.[AccessedAtUtc] AS date), dimensions.[DimensionValue];

                INSERT INTO [ShortUrlAnalyticsAggregates]
                    ([ShortUrlId], [BucketStartUtc], [Granularity], [Dimension],
                     [DimensionSchemaVersion], [DimensionValue], [ClickCount],
                     [UniqueVisitorCount], [UpdatedAtUtc])
                SELECT
                    logs.[ShortUrlId], CAST(CAST(logs.[AccessedAtUtc] AS date) AS datetime2),
                    2, 2, 1, dimensions.[DimensionValue], COUNT_BIG(*), 0, SYSUTCDATETIME()
                FROM [ShortUrlAccessLogs] AS logs
                CROSS APPLY
                (
                    SELECT CASE
                        WHEN logs.[UserAgent] IS NULL OR LTRIM(RTRIM(logs.[UserAgent])) = ''
                          OR CHARINDEX(CHAR(9), logs.[UserAgent]) > 0
                          OR CHARINDEX(CHAR(10), logs.[UserAgent]) > 0
                          OR CHARINDEX(CHAR(13), logs.[UserAgent]) > 0 THEN 'Unknown'
                        WHEN LOWER(logs.[UserAgent]) LIKE '%bot%'
                          OR LOWER(logs.[UserAgent]) LIKE '%spider%'
                          OR LOWER(logs.[UserAgent]) LIKE '%crawler%'
                          OR LOWER(logs.[UserAgent]) LIKE '%slurp%'
                          OR LOWER(logs.[UserAgent]) LIKE '%headless%' THEN 'Bot'
                        WHEN LOWER(logs.[UserAgent]) LIKE '%ipad%'
                          OR LOWER(logs.[UserAgent]) LIKE '%tablet%'
                          OR LOWER(logs.[UserAgent]) LIKE '%kindle%'
                          OR LOWER(logs.[UserAgent]) LIKE '%silk/%' THEN 'Tablet'
                        WHEN LOWER(logs.[UserAgent]) LIKE '%iphone%'
                          OR LOWER(logs.[UserAgent]) LIKE '%ipod%'
                          OR LOWER(logs.[UserAgent]) LIKE '%mobile%'
                          OR LOWER(logs.[UserAgent]) LIKE '%android%' THEN 'Mobile'
                        WHEN LOWER(logs.[UserAgent]) LIKE '%windows%'
                          OR LOWER(logs.[UserAgent]) LIKE '%macintosh%'
                          OR LOWER(logs.[UserAgent]) LIKE '%x11%'
                          OR LOWER(logs.[UserAgent]) LIKE '%cros%'
                          OR LOWER(logs.[UserAgent]) LIKE '%linux%' THEN 'Desktop'
                        ELSE 'Other'
                    END AS [DimensionValue]
                ) AS dimensions
                GROUP BY logs.[ShortUrlId], CAST(logs.[AccessedAtUtc] AS date), dimensions.[DimensionValue];

                INSERT INTO [ShortUrlAnalyticsAggregates]
                    ([ShortUrlId], [BucketStartUtc], [Granularity], [Dimension],
                     [DimensionSchemaVersion], [DimensionValue], [ClickCount],
                     [UniqueVisitorCount], [UpdatedAtUtc])
                SELECT
                    logs.[ShortUrlId], CAST(CAST(logs.[AccessedAtUtc] AS date) AS datetime2),
                    2, 3, 1, dimensions.[DimensionValue], COUNT_BIG(*), 0, SYSUTCDATETIME()
                FROM [ShortUrlAccessLogs] AS logs
                CROSS APPLY
                (
                    SELECT CASE
                        WHEN logs.[UserAgent] IS NULL OR LTRIM(RTRIM(logs.[UserAgent])) = ''
                          OR CHARINDEX(CHAR(9), logs.[UserAgent]) > 0
                          OR CHARINDEX(CHAR(10), logs.[UserAgent]) > 0
                          OR CHARINDEX(CHAR(13), logs.[UserAgent]) > 0 THEN 'Unknown'
                        WHEN LOWER(logs.[UserAgent]) LIKE '%edg/%'
                          OR LOWER(logs.[UserAgent]) LIKE '%edga/%'
                          OR LOWER(logs.[UserAgent]) LIKE '%edgios/%' THEN 'Edge'
                        WHEN LOWER(logs.[UserAgent]) LIKE '%opr/%' THEN 'Opera'
                        WHEN LOWER(logs.[UserAgent]) LIKE '%chrome/%'
                          OR LOWER(logs.[UserAgent]) LIKE '%crios/%' THEN 'Chrome'
                        WHEN LOWER(logs.[UserAgent]) LIKE '%firefox/%'
                          OR LOWER(logs.[UserAgent]) LIKE '%fxios/%' THEN 'Firefox'
                        WHEN LOWER(logs.[UserAgent]) LIKE '%safari/%'
                          AND LOWER(logs.[UserAgent]) LIKE '%version/%' THEN 'Safari'
                        WHEN LOWER(logs.[UserAgent]) LIKE '%msie %'
                          OR LOWER(logs.[UserAgent]) LIKE '%trident/%' THEN 'Internet Explorer'
                        ELSE 'Other'
                    END AS [DimensionValue]
                ) AS dimensions
                GROUP BY logs.[ShortUrlId], CAST(logs.[AccessedAtUtc] AS date), dimensions.[DimensionValue];

                INSERT INTO [ShortUrlAnalyticsAggregates]
                    ([ShortUrlId], [BucketStartUtc], [Granularity], [Dimension],
                     [DimensionSchemaVersion], [DimensionValue], [ClickCount],
                     [UniqueVisitorCount], [UpdatedAtUtc])
                SELECT
                    logs.[ShortUrlId], CAST(CAST(logs.[AccessedAtUtc] AS date) AS datetime2),
                    2, 4, 1, dimensions.[DimensionValue], COUNT_BIG(*), 0, SYSUTCDATETIME()
                FROM [ShortUrlAccessLogs] AS logs
                CROSS APPLY
                (
                    SELECT CASE
                        WHEN logs.[UserAgent] IS NULL OR LTRIM(RTRIM(logs.[UserAgent])) = ''
                          OR CHARINDEX(CHAR(9), logs.[UserAgent]) > 0
                          OR CHARINDEX(CHAR(10), logs.[UserAgent]) > 0
                          OR CHARINDEX(CHAR(13), logs.[UserAgent]) > 0 THEN 'Unknown'
                        WHEN LOWER(logs.[UserAgent]) LIKE '%iphone%'
                          OR LOWER(logs.[UserAgent]) LIKE '%ipad%'
                          OR LOWER(logs.[UserAgent]) LIKE '%ipod%' THEN 'iOS'
                        WHEN LOWER(logs.[UserAgent]) LIKE '%android%' THEN 'Android'
                        WHEN LOWER(logs.[UserAgent]) LIKE '%windows%' THEN 'Windows'
                        WHEN LOWER(logs.[UserAgent]) LIKE '%macintosh%'
                          OR LOWER(logs.[UserAgent]) LIKE '%mac os x%' THEN 'macOS'
                        WHEN LOWER(logs.[UserAgent]) LIKE '%linux%'
                          OR LOWER(logs.[UserAgent]) LIKE '%x11%' THEN 'Linux'
                        ELSE 'Other'
                    END AS [DimensionValue]
                ) AS dimensions
                GROUP BY logs.[ShortUrlId], CAST(logs.[AccessedAtUtc] AS date), dimensions.[DimensionValue];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShortUrlAnalyticsAggregates");

            migrationBuilder.DropTable(
                name: "ShortUrlAnalyticsDailyVisitors");
        }
    }
}
