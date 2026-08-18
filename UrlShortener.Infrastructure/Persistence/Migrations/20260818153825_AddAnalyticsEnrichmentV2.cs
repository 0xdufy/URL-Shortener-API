using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrlShortener.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsEnrichmentV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferrerKind",
                table: "ShortUrlAccessLogs",
                type: "varchar(16)",
                unicode: false,
                maxLength: 16,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [ShortUrlAccessLogs]
                SET [ReferrerKind] = CASE
                    WHEN [Referer] IS NULL OR LTRIM(RTRIM([Referer])) = '' THEN 'direct'
                    ELSE 'host'
                END
                WHERE [ReferrerKind] IS NULL;

                INSERT INTO [ShortUrlAnalyticsAggregates]
                    ([ShortUrlId], [BucketStartUtc], [Granularity], [Dimension],
                     [DimensionSchemaVersion], [DimensionValue], [ClickCount],
                     [UniqueVisitorCount], [UpdatedAtUtc])
                SELECT
                    [ShortUrlId], [BucketStartUtc], [Granularity], [Dimension],
                    2, [DimensionValue], [ClickCount], [UniqueVisitorCount], [UpdatedAtUtc]
                FROM [ShortUrlAnalyticsAggregates]
                WHERE [DimensionSchemaVersion] = 1
                  AND [Dimension] IN (0, 2, 3, 4);

                INSERT INTO [ShortUrlAnalyticsAggregates]
                    ([ShortUrlId], [BucketStartUtc], [Granularity], [Dimension],
                     [DimensionSchemaVersion], [DimensionValue], [ClickCount],
                     [UniqueVisitorCount], [UpdatedAtUtc])
                SELECT
                    source.[ShortUrlId], source.[BucketStartUtc], source.[Granularity], 1, 2,
                    source.[SourceLabel], SUM(source.[ClickCount]), 0, MAX(source.[UpdatedAtUtc])
                FROM
                (
                    SELECT
                        existing.[ShortUrlId], existing.[BucketStartUtc], existing.[Granularity],
                        existing.[ClickCount], existing.[UpdatedAtUtc],
                        CASE
                            WHEN existing.[DimensionValue] = 'Direct' THEN 'Direct'
                            WHEN existing.[DimensionValue] = 'Unknown' THEN 'Unknown'
                            WHEN existing.[DimensionValue] = 'google.com'
                              OR existing.[DimensionValue] LIKE '%.google.com' THEN 'Google'
                            WHEN existing.[DimensionValue] = 'bing.com'
                              OR existing.[DimensionValue] LIKE '%.bing.com' THEN 'Bing'
                            WHEN existing.[DimensionValue] = 'duckduckgo.com'
                              OR existing.[DimensionValue] LIKE '%.duckduckgo.com' THEN 'DuckDuckGo'
                            WHEN existing.[DimensionValue] = 'yahoo.com'
                              OR existing.[DimensionValue] LIKE '%.yahoo.com' THEN 'Yahoo'
                            WHEN existing.[DimensionValue] = 'facebook.com'
                              OR existing.[DimensionValue] LIKE '%.facebook.com'
                              OR existing.[DimensionValue] = 'fb.com'
                              OR existing.[DimensionValue] LIKE '%.fb.com' THEN 'Facebook'
                            WHEN existing.[DimensionValue] = 'instagram.com'
                              OR existing.[DimensionValue] LIKE '%.instagram.com' THEN 'Instagram'
                            WHEN existing.[DimensionValue] = 'x.com'
                              OR existing.[DimensionValue] LIKE '%.x.com'
                              OR existing.[DimensionValue] = 'twitter.com'
                              OR existing.[DimensionValue] LIKE '%.twitter.com'
                              OR existing.[DimensionValue] = 't.co'
                              OR existing.[DimensionValue] LIKE '%.t.co' THEN 'X / Twitter'
                            WHEN existing.[DimensionValue] = 'linkedin.com'
                              OR existing.[DimensionValue] LIKE '%.linkedin.com'
                              OR existing.[DimensionValue] = 'lnkd.in'
                              OR existing.[DimensionValue] LIKE '%.lnkd.in' THEN 'LinkedIn'
                            WHEN existing.[DimensionValue] = 'reddit.com'
                              OR existing.[DimensionValue] LIKE '%.reddit.com' THEN 'Reddit'
                            WHEN existing.[DimensionValue] = 'youtube.com'
                              OR existing.[DimensionValue] LIKE '%.youtube.com'
                              OR existing.[DimensionValue] = 'youtu.be'
                              OR existing.[DimensionValue] LIKE '%.youtu.be' THEN 'YouTube'
                            ELSE 'Other'
                        END AS [SourceLabel]
                    FROM [ShortUrlAnalyticsAggregates] AS existing
                    WHERE existing.[DimensionSchemaVersion] = 1
                      AND existing.[Dimension] = 1
                ) AS source
                GROUP BY
                    source.[ShortUrlId], source.[BucketStartUtc], source.[Granularity], source.[SourceLabel];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM [ShortUrlAnalyticsAggregates]
                WHERE [DimensionSchemaVersion] = 2;
                """);

            migrationBuilder.DropColumn(
                name: "ReferrerKind",
                table: "ShortUrlAccessLogs");
        }
    }
}
