USE [Warranty]
GO
/****** Object:  Table [dbo].[__EFMigrationsHistory]    Script Date: 10/4/2026 4:06:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Products]    Script Date: 10/4/2026 4:06:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Products](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Brand] [nvarchar](100) NOT NULL,
	[Model] [nvarchar](100) NOT NULL,
	[SerialNumber] [nvarchar](100) NOT NULL,
	[WarrantyMonths] [int] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Products] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RefreshTokens]    Script Date: 10/4/2026 4:06:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RefreshTokens](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[Token] [nvarchar](64) NOT NULL,
	[ExpiresAt] [datetime2](7) NOT NULL,
	[IsRevoked] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_RefreshTokens] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RepairRequests]    Script Date: 10/4/2026 4:06:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RepairRequests](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[WarrantyCardId] [int] NOT NULL,
	[ReceptionistId] [int] NULL,
	[TechnicianId] [int] NULL,
	[Description] [nvarchar](4000) NOT NULL,
	[Status] [nvarchar](30) NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_RepairRequests] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RepairRequestStatusHistories]    Script Date: 10/4/2026 4:06:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RepairRequestStatusHistories](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RepairRequestId] [int] NOT NULL,
	[OldStatus] [nvarchar](30) NULL,
	[NewStatus] [nvarchar](30) NOT NULL,
	[ChangedBy] [int] NOT NULL,
	[ChangedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_RepairRequestStatusHistories] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Users]    Script Date: 10/4/2026 4:06:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FullName] [nvarchar](150) NOT NULL,
	[Email] [nvarchar](256) NOT NULL,
	[PasswordHash] [nvarchar](100) NOT NULL,
	[Phone] [nvarchar](30) NULL,
	[Role] [nvarchar](30) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[WarrantyCards]    Script Date: 10/4/2026 4:06:32 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[WarrantyCards](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ProductId] [int] NOT NULL,
	[CustomerId] [int] NOT NULL,
	[StartDate] [datetime2](7) NOT NULL,
	[EndDate] [datetime2](7) NOT NULL,
	[Status] [nvarchar](30) NOT NULL,
 CONSTRAINT [PK_WarrantyCards] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260929012628_InitialCreate', N'10.0.12')
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260929055711_InitialCreate2', N'10.0.12')
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260929060030_InitialCreate3', N'10.0.12')
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260929060738_InitialCreate4', N'10.0.12')
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260929064058_InitialCreate5', N'10.0.12')
GO
SET IDENTITY_INSERT [dbo].[Products] ON 

INSERT [dbo].[Products] ([Id], [Name], [Brand], [Model], [SerialNumber], [WarrantyMonths], [CreatedAt]) VALUES (1, N'Dell XPS 13', N'Dell', N'XPS 13 9310', N'DLSXPS13-001', 24, CAST(N'2024-02-05T10:00:00.0000000' AS DateTime2))
INSERT [dbo].[Products] ([Id], [Name], [Brand], [Model], [SerialNumber], [WarrantyMonths], [CreatedAt]) VALUES (2, N'iPhone 15 Pro', N'Apple', N'A3102', N'IP15P-2024-001', 12, CAST(N'2024-02-12T11:35:00.0000000' AS DateTime2))
INSERT [dbo].[Products] ([Id], [Name], [Brand], [Model], [SerialNumber], [WarrantyMonths], [CreatedAt]) VALUES (3, N'Samsung Galaxy Tab S9', N'Samsung', N'SM-X710', N'TABS9-2024-001', 18, CAST(N'2024-03-01T09:10:00.0000000' AS DateTime2))
INSERT [dbo].[Products] ([Id], [Name], [Brand], [Model], [SerialNumber], [WarrantyMonths], [CreatedAt]) VALUES (4, N'LG UltraWide 34" Monitor', N'LG', N'34WN80C-B', N'LG34-2024-001', 24, CAST(N'2024-03-15T15:25:00.0000000' AS DateTime2))
INSERT [dbo].[Products] ([Id], [Name], [Brand], [Model], [SerialNumber], [WarrantyMonths], [CreatedAt]) VALUES (5, N'Canon Pixma G2020', N'Canon', N'G2020', N'CANON-G2020-001', 12, CAST(N'2024-04-08T13:40:00.0000000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[Products] OFF
GO
SET IDENTITY_INSERT [dbo].[RefreshTokens] ON 

INSERT [dbo].[RefreshTokens] ([Id], [UserId], [Token], [ExpiresAt], [IsRevoked], [CreatedAt]) VALUES (1, 1, N'885EB59E1698D38837DCD001CAF09671F47A8F318A33AEC5363D498D7C4A1432', CAST(N'2026-10-11T09:05:02.7064570' AS DateTime2), 0, CAST(N'2026-10-02T09:05:02.7065351' AS DateTime2))
INSERT [dbo].[RefreshTokens] ([Id], [UserId], [Token], [ExpiresAt], [IsRevoked], [CreatedAt]) VALUES (2, 2, N'1B724237FA413E301E324668CBC9021D2F5B7034E2B43F3B501AAB61E84A617F', CAST(N'2026-10-11T09:05:02.7065814' AS DateTime2), 0, CAST(N'2026-10-02T09:05:02.7065815' AS DateTime2))
INSERT [dbo].[RefreshTokens] ([Id], [UserId], [Token], [ExpiresAt], [IsRevoked], [CreatedAt]) VALUES (3, 3, N'3C6A47638E9F859CED1B3B2BFA5237AACD39CCE4577B2F20E19D3B8C5190F256', CAST(N'2026-10-11T09:05:02.7065823' AS DateTime2), 0, CAST(N'2026-10-02T09:05:02.7065824' AS DateTime2))
INSERT [dbo].[RefreshTokens] ([Id], [UserId], [Token], [ExpiresAt], [IsRevoked], [CreatedAt]) VALUES (4, 4, N'1E551866806044FB95AD21316E65FD59CEA020D3BE57B5945D91EAB70C14BCFF', CAST(N'2026-10-11T09:05:02.7065858' AS DateTime2), 0, CAST(N'2026-10-02T09:05:02.7065859' AS DateTime2))
INSERT [dbo].[RefreshTokens] ([Id], [UserId], [Token], [ExpiresAt], [IsRevoked], [CreatedAt]) VALUES (5, 5, N'4769C3A589C549E6F53F35A31C7D2163990FDF5D5DB419F67BCBBF90AEE2CB71', CAST(N'2026-10-11T09:05:02.7065881' AS DateTime2), 0, CAST(N'2026-10-02T09:05:02.7065881' AS DateTime2))
SET IDENTITY_INSERT [dbo].[RefreshTokens] OFF
GO
SET IDENTITY_INSERT [dbo].[RepairRequests] ON 

INSERT [dbo].[RepairRequests] ([Id], [WarrantyCardId], [ReceptionistId], [TechnicianId], [Description], [Status], [CreatedAt], [UpdatedAt]) VALUES (1, 1, 3, 4, N'Laptop battery drains quickly and device turns off during meetings.', N'Received', CAST(N'2025-01-20T08:40:00.0000000' AS DateTime2), CAST(N'2025-01-20T08:40:00.0000000' AS DateTime2))
INSERT [dbo].[RepairRequests] ([Id], [WarrantyCardId], [ReceptionistId], [TechnicianId], [Description], [Status], [CreatedAt], [UpdatedAt]) VALUES (2, 2, 3, 4, N'Touchscreen is unresponsive after a minor drop.', N'InProgress', CAST(N'2025-02-02T10:00:00.0000000' AS DateTime2), CAST(N'2025-02-05T09:15:00.0000000' AS DateTime2))
INSERT [dbo].[RepairRequests] ([Id], [WarrantyCardId], [ReceptionistId], [TechnicianId], [Description], [Status], [CreatedAt], [UpdatedAt]) VALUES (3, 3, 3, 4, N'Tablet charging port is loose and power cable disconnects intermittently.', N'Completed', CAST(N'2024-07-05T14:20:00.0000000' AS DateTime2), CAST(N'2024-07-11T16:10:00.0000000' AS DateTime2))
INSERT [dbo].[RepairRequests] ([Id], [WarrantyCardId], [ReceptionistId], [TechnicianId], [Description], [Status], [CreatedAt], [UpdatedAt]) VALUES (4, 4, 3, 4, N'Monitor flickers black every few minutes while using design software.', N'Returned', CAST(N'2025-04-12T12:00:00.0000000' AS DateTime2), CAST(N'2025-04-18T17:30:00.0000000' AS DateTime2))
INSERT [dbo].[RepairRequests] ([Id], [WarrantyCardId], [ReceptionistId], [TechnicianId], [Description], [Status], [CreatedAt], [UpdatedAt]) VALUES (5, 5, 3, 4, N'Printer repeatedly jams paper after installing a new cartridge.', N'Cancelled', CAST(N'2023-04-25T09:35:00.0000000' AS DateTime2), CAST(N'2023-04-28T13:45:00.0000000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[RepairRequests] OFF
GO
SET IDENTITY_INSERT [dbo].[RepairRequestStatusHistories] ON 

INSERT [dbo].[RepairRequestStatusHistories] ([Id], [RepairRequestId], [OldStatus], [NewStatus], [ChangedBy], [ChangedAt]) VALUES (1, 1, NULL, N'Received', 3, CAST(N'2025-01-20T08:41:00.0000000' AS DateTime2))
INSERT [dbo].[RepairRequestStatusHistories] ([Id], [RepairRequestId], [OldStatus], [NewStatus], [ChangedBy], [ChangedAt]) VALUES (2, 2, N'Received', N'InProgress', 4, CAST(N'2025-02-05T09:15:00.0000000' AS DateTime2))
INSERT [dbo].[RepairRequestStatusHistories] ([Id], [RepairRequestId], [OldStatus], [NewStatus], [ChangedBy], [ChangedAt]) VALUES (3, 3, N'InProgress', N'Completed', 4, CAST(N'2024-07-11T16:10:00.0000000' AS DateTime2))
INSERT [dbo].[RepairRequestStatusHistories] ([Id], [RepairRequestId], [OldStatus], [NewStatus], [ChangedBy], [ChangedAt]) VALUES (4, 4, N'Completed', N'Returned', 4, CAST(N'2025-04-18T17:30:00.0000000' AS DateTime2))
INSERT [dbo].[RepairRequestStatusHistories] ([Id], [RepairRequestId], [OldStatus], [NewStatus], [ChangedBy], [ChangedAt]) VALUES (5, 5, N'Received', N'Cancelled', 3, CAST(N'2023-04-28T13:45:00.0000000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[RepairRequestStatusHistories] OFF
GO
SET IDENTITY_INSERT [dbo].[Users] ON 

INSERT [dbo].[Users] ([Id], [FullName], [Email], [PasswordHash], [Phone], [Role], [IsActive], [CreatedAt]) VALUES (1, N'Nguyen Van An', N'admin@warranty.local', N'$2a$11$C2CoFJFyg8ClJvqx9yN9oeexOXjSs22UF7M4YWqq61g5phuJkCB92', N'0901000001', N'Admin', 1, CAST(N'2024-01-15T08:30:00.0000000' AS DateTime2))
INSERT [dbo].[Users] ([Id], [FullName], [Email], [PasswordHash], [Phone], [Role], [IsActive], [CreatedAt]) VALUES (2, N'Tran Thi Binh', N'manager@warranty.local', N'$2a$11$FEUXDGBxt/F9kSuhdRlg4e9m28YRTA4i2g2kWiibRwsrV.KVPIL8u', N'0901000002', N'Manager', 1, CAST(N'2024-01-20T09:00:00.0000000' AS DateTime2))
INSERT [dbo].[Users] ([Id], [FullName], [Email], [PasswordHash], [Phone], [Role], [IsActive], [CreatedAt]) VALUES (3, N'Le Hoai Chi', N'reception@warranty.local', N'$2a$11$QMnotL1.3s/NdY3mnyzzLOeKKPbjfmxLBydqxNx/.REgj3hHykTGa', N'0901000003', N'Receptionist', 1, CAST(N'2024-02-03T08:15:00.0000000' AS DateTime2))
INSERT [dbo].[Users] ([Id], [FullName], [Email], [PasswordHash], [Phone], [Role], [IsActive], [CreatedAt]) VALUES (4, N'Pham Van Duy', N'tech01@warranty.local', N'$2a$11$WQCBhJVftJhMcRY2aIb4GOEtOuORFmxTRszSbwOmxnkXTv3ES.pkW', N'0901000004', N'Technician', 1, CAST(N'2024-02-10T07:45:00.0000000' AS DateTime2))
INSERT [dbo].[Users] ([Id], [FullName], [Email], [PasswordHash], [Phone], [Role], [IsActive], [CreatedAt]) VALUES (5, N'Nguyen Thi Emy', N'customer01@warranty.local', N'$2a$11$3f5x3f9Bchvill.OAQrGP.mTZnDAXUQk/bgnzRIXqh8g0xwv08Hja', N'0901000005', N'Customer', 1, CAST(N'2024-03-08T14:20:00.0000000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[Users] OFF
GO
SET IDENTITY_INSERT [dbo].[WarrantyCards] ON 

INSERT [dbo].[WarrantyCards] ([Id], [ProductId], [CustomerId], [StartDate], [EndDate], [Status]) VALUES (1, 1, 5, CAST(N'2024-01-15T00:00:00.0000000' AS DateTime2), CAST(N'2025-01-15T00:00:00.0000000' AS DateTime2), N'Active')
INSERT [dbo].[WarrantyCards] ([Id], [ProductId], [CustomerId], [StartDate], [EndDate], [Status]) VALUES (2, 2, 5, CAST(N'2024-03-01T00:00:00.0000000' AS DateTime2), CAST(N'2025-03-01T00:00:00.0000000' AS DateTime2), N'Active')
INSERT [dbo].[WarrantyCards] ([Id], [ProductId], [CustomerId], [StartDate], [EndDate], [Status]) VALUES (3, 3, 5, CAST(N'2023-06-10T00:00:00.0000000' AS DateTime2), CAST(N'2024-06-10T00:00:00.0000000' AS DateTime2), N'Expired')
INSERT [dbo].[WarrantyCards] ([Id], [ProductId], [CustomerId], [StartDate], [EndDate], [Status]) VALUES (4, 4, 5, CAST(N'2024-11-20T00:00:00.0000000' AS DateTime2), CAST(N'2025-11-20T00:00:00.0000000' AS DateTime2), N'Active')
INSERT [dbo].[WarrantyCards] ([Id], [ProductId], [CustomerId], [StartDate], [EndDate], [Status]) VALUES (5, 5, 5, CAST(N'2022-05-25T00:00:00.0000000' AS DateTime2), CAST(N'2023-05-25T00:00:00.0000000' AS DateTime2), N'Voided')
SET IDENTITY_INSERT [dbo].[WarrantyCards] OFF
GO
ALTER TABLE [dbo].[RefreshTokens]  WITH CHECK ADD  CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RefreshTokens] CHECK CONSTRAINT [FK_RefreshTokens_Users_UserId]
GO
ALTER TABLE [dbo].[RepairRequests]  WITH CHECK ADD  CONSTRAINT [FK_RepairRequests_Users_ReceptionistId] FOREIGN KEY([ReceptionistId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[RepairRequests] CHECK CONSTRAINT [FK_RepairRequests_Users_ReceptionistId]
GO
ALTER TABLE [dbo].[RepairRequests]  WITH CHECK ADD  CONSTRAINT [FK_RepairRequests_Users_TechnicianId] FOREIGN KEY([TechnicianId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[RepairRequests] CHECK CONSTRAINT [FK_RepairRequests_Users_TechnicianId]
GO
ALTER TABLE [dbo].[RepairRequests]  WITH CHECK ADD  CONSTRAINT [FK_RepairRequests_WarrantyCards_WarrantyCardId] FOREIGN KEY([WarrantyCardId])
REFERENCES [dbo].[WarrantyCards] ([Id])
GO
ALTER TABLE [dbo].[RepairRequests] CHECK CONSTRAINT [FK_RepairRequests_WarrantyCards_WarrantyCardId]
GO
ALTER TABLE [dbo].[RepairRequestStatusHistories]  WITH CHECK ADD  CONSTRAINT [FK_RepairRequestStatusHistories_RepairRequests_RepairRequestId] FOREIGN KEY([RepairRequestId])
REFERENCES [dbo].[RepairRequests] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RepairRequestStatusHistories] CHECK CONSTRAINT [FK_RepairRequestStatusHistories_RepairRequests_RepairRequestId]
GO
ALTER TABLE [dbo].[RepairRequestStatusHistories]  WITH CHECK ADD  CONSTRAINT [FK_RepairRequestStatusHistories_Users_ChangedBy] FOREIGN KEY([ChangedBy])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[RepairRequestStatusHistories] CHECK CONSTRAINT [FK_RepairRequestStatusHistories_Users_ChangedBy]
GO
ALTER TABLE [dbo].[WarrantyCards]  WITH CHECK ADD  CONSTRAINT [FK_WarrantyCards_Products_ProductId] FOREIGN KEY([ProductId])
REFERENCES [dbo].[Products] ([Id])
GO
ALTER TABLE [dbo].[WarrantyCards] CHECK CONSTRAINT [FK_WarrantyCards_Products_ProductId]
GO
ALTER TABLE [dbo].[WarrantyCards]  WITH CHECK ADD  CONSTRAINT [FK_WarrantyCards_Users_CustomerId] FOREIGN KEY([CustomerId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[WarrantyCards] CHECK CONSTRAINT [FK_WarrantyCards_Users_CustomerId]
GO
ALTER TABLE [dbo].[Products]  WITH CHECK ADD  CONSTRAINT [CK_Products_WarrantyMonths] CHECK  (([WarrantyMonths]>(0)))
GO
ALTER TABLE [dbo].[Products] CHECK CONSTRAINT [CK_Products_WarrantyMonths]
GO
ALTER TABLE [dbo].[WarrantyCards]  WITH CHECK ADD  CONSTRAINT [CK_WarrantyCards_DateRange] CHECK  (([EndDate]>=[StartDate]))
GO
ALTER TABLE [dbo].[WarrantyCards] CHECK CONSTRAINT [CK_WarrantyCards_DateRange]
GO
