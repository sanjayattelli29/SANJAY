using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RestoreChatTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Chats]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [Chats] (
                        [Id] nvarchar(450) NOT NULL,
                        [PolicyId] nvarchar(450) NOT NULL,
                        [CustomerId] nvarchar(max) NOT NULL,
                        [AgentId] nvarchar(max) NOT NULL,
                        [CustomerEmail] nvarchar(max) NULL,
                        [AgentEmail] nvarchar(max) NULL,
                        [PolicyName] nvarchar(max) NULL,
                        [Category] nvarchar(max) NULL,
                        [CoverageAmount] decimal(18,2) NOT NULL,
                        [DateActivated] datetime2 NOT NULL,
                        [LastMessage] nvarchar(max) NULL,
                        [LastMessageTime] datetime2 NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_Chats] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_Chats_PolicyApplications_PolicyId] FOREIGN KEY ([PolicyId]) REFERENCES [PolicyApplications] ([Id]) ON DELETE CASCADE
                    );
                END

                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ChatMessages]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [ChatMessages] (
                        [Id] nvarchar(450) NOT NULL,
                        [ChatId] nvarchar(450) NOT NULL,
                        [SenderId] nvarchar(max) NOT NULL,
                        [SenderRole] nvarchar(max) NOT NULL,
                        [Message] nvarchar(max) NOT NULL,
                        [Timestamp] datetime2 NOT NULL,
                        [IsRead] bit NOT NULL,
                        CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_ChatMessages_Chats_ChatId] FOREIGN KEY ([ChatId]) REFERENCES [Chats] ([Id]) ON DELETE CASCADE
                    );
                END

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChatMessages_ChatId' AND object_id = OBJECT_ID('[dbo].[ChatMessages]'))
                BEGIN
                    CREATE INDEX [IX_ChatMessages_ChatId] ON [ChatMessages] ([ChatId]);
                END

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Chats_PolicyId' AND object_id = OBJECT_ID('[dbo].[Chats]'))
                BEGIN
                    CREATE INDEX [IX_Chats_PolicyId] ON [Chats] ([PolicyId]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "Chats");
        }
    }
}
