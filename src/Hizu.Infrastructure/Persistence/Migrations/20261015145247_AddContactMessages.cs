using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hizu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContactMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""ContactMessages"" (
                    ""Id"" uuid NOT NULL,
                    ""Name"" character varying(150) NOT NULL,
                    ""Email"" character varying(200) NOT NULL,
                    ""PhoneNumber"" character varying(30),
                    ""Subject"" character varying(250),
                    ""Message"" character varying(4000) NOT NULL,
                    ""IsRead"" boolean NOT NULL DEFAULT FALSE,
                    ""AdminNotes"" character varying(3000),
                    ""RepliedAt"" timestamp with time zone,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    ""UpdatedAt"" timestamp with time zone,
                    CONSTRAINT ""PK_ContactMessages"" PRIMARY KEY (""Id"")
                );
                CREATE INDEX IF NOT EXISTS ""IX_ContactMessages_CreatedAt"" ON ""ContactMessages"" (""CreatedAt"");
                CREATE INDEX IF NOT EXISTS ""IX_ContactMessages_IsRead"" ON ""ContactMessages"" (""IsRead"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""ContactMessages"";");
        }
    }
}
