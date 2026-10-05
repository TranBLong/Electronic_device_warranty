USE [Warranty]
GO
/****** Object:  Table [dbo].[__EFMigrationsHistory]    Script Date: 10/5/2026 12:55:56 AM ******/
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
/****** Object:  Table [dbo].[Categories]    Script Date: 10/5/2026 12:55:56 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Categories](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[Description] [nvarchar](500) NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Feedbacks]    Script Date: 10/5/2026 12:55:56 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Feedbacks](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RepairRequestId] [int] NOT NULL,
	[CustomerId] [int] NOT NULL,
	[Rating] [int] NOT NULL,
	[Comment] [nvarchar](1000) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Feedbacks] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Invoices]    Script Date: 10/5/2026 12:55:56 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Invoices](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RepairRequestId] [int] NOT NULL,
	[InvoiceNumber] [nvarchar](50) NOT NULL,
	[PartsTotal] [decimal](18, 2) NOT NULL,
	[LaborTotal] [decimal](18, 2) NOT NULL,
	[TotalAmount] [decimal](18, 2) NOT NULL,
	[Status] [nvarchar](30) NOT NULL,
	[IssuedAt] [datetime2](7) NOT NULL,
	[PaidAt] [datetime2](7) NULL,
 CONSTRAINT [PK_Invoices] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Notifications]    Script Date: 10/5/2026 12:55:56 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Notifications](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[RepairRequestId] [int] NULL,
	[Type] [nvarchar](50) NOT NULL,
	[Title] [nvarchar](200) NOT NULL,
	[Message] [nvarchar](1000) NOT NULL,
	[IsRead] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Parts]    Script Date: 10/5/2026 12:55:56 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Parts](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Code] [nvarchar](50) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Description] [nvarchar](1000) NULL,
	[UnitPrice] [decimal](18, 2) NOT NULL,
	[StockQuantity] [int] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Parts] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Payments]    Script Date: 10/5/2026 12:55:56 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Payments](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InvoiceId] [int] NOT NULL,
	[ReceivedById] [int] NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[Method] [nvarchar](30) NOT NULL,
	[Note] [nvarchar](500) NULL,
	[PaidAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Payments] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Products]    Script Date: 10/5/2026 12:55:56 AM ******/
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
	[CategoryId] [int] NULL,
 CONSTRAINT [PK_Products] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RefreshTokens]    Script Date: 10/5/2026 12:55:56 AM ******/
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
/****** Object:  Table [dbo].[RepairRequestAttachments]    Script Date: 10/5/2026 12:55:56 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RepairRequestAttachments](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RepairRequestId] [int] NOT NULL,
	[UploadedById] [int] NOT NULL,
	[FileName] [nvarchar](255) NOT NULL,
	[FilePath] [nvarchar](500) NOT NULL,
	[ContentType] [nvarchar](100) NOT NULL,
	[FileSize] [bigint] NOT NULL,
	[UploadedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_RepairRequestAttachments] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RepairRequestParts]    Script Date: 10/5/2026 12:55:56 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RepairRequestParts](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RepairRequestId] [int] NOT NULL,
	[PartId] [int] NOT NULL,
	[Quantity] [int] NOT NULL,
	[UnitPrice] [decimal](18, 2) NOT NULL,
	[AddedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_RepairRequestParts] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RepairRequests]    Script Date: 10/5/2026 12:55:56 AM ******/
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
	[IsChargeable] [bit] NOT NULL,
	[LaborCost] [decimal](18, 2) NOT NULL,
	[ServiceCenterId] [int] NULL,
 CONSTRAINT [PK_RepairRequests] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RepairRequestStatusHistories]    Script Date: 10/5/2026 12:55:56 AM ******/
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
/****** Object:  Table [dbo].[ServiceCenters]    Script Date: 10/5/2026 12:55:56 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ServiceCenters](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Address] [nvarchar](500) NOT NULL,
	[Phone] [nvarchar](30) NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_ServiceCenters] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Users]    Script Date: 10/5/2026 12:55:56 AM ******/
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
	[ServiceCenterId] [int] NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[WarrantyCards]    Script Date: 10/5/2026 12:55:56 AM ******/
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
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261004171912_AddCategoryAndParts', N'10.0.12')
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261004175209_ModelComplete', N'10.0.12')
GO
SET IDENTITY_INSERT [dbo].[Categories] ON 

INSERT [dbo].[Categories] ([Id], [Name], [Description], [IsActive], [CreatedAt]) VALUES (1, N'Laptop', N'Laptops and ultrabooks.', 1, CAST(N'2024-01-12T09:00:00.0000000' AS DateTime2))
INSERT [dbo].[Categories] ([Id], [Name], [Description], [IsActive], [CreatedAt]) VALUES (2, N'Smartphone', N'Mobile phones.', 1, CAST(N'2024-01-12T09:05:00.0000000' AS DateTime2))
INSERT [dbo].[Categories] ([Id], [Name], [Description], [IsActive], [CreatedAt]) VALUES (3, N'Tablet', N'Tablets and e-readers.', 1, CAST(N'2024-01-12T09:10:00.0000000' AS DateTime2))
INSERT [dbo].[Categories] ([Id], [Name], [Description], [IsActive], [CreatedAt]) VALUES (4, N'Monitor', N'Computer monitors and displays.', 1, CAST(N'2024-01-12T09:15:00.0000000' AS DateTime2))
INSERT [dbo].[Categories] ([Id], [Name], [Description], [IsActive], [CreatedAt]) VALUES (5, N'Printer', N'Inkjet and laser printers.', 1, CAST(N'2024-01-12T09:20:00.0000000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[Categories] OFF
GO
SET IDENTITY_INSERT [dbo].[Feedbacks] ON 

INSERT [dbo].[Feedbacks] ([Id], [RepairRequestId], [CustomerId], [Rating], [Comment], [CreatedAt]) VALUES (1, 3, 5, 4, N'Fixed quickly, but I wish the warranty had covered it.', CAST(N'2024-07-13T09:00:00.0000000' AS DateTime2))
INSERT [dbo].[Feedbacks] ([Id], [RepairRequestId], [CustomerId], [Rating], [Comment], [CreatedAt]) VALUES (2, 4, 5, 4, N'Technician explained the power surge cause clearly.', CAST(N'2025-04-20T10:00:00.0000000' AS DateTime2))
INSERT [dbo].[Feedbacks] ([Id], [RepairRequestId], [CustomerId], [Rating], [Comment], [CreatedAt]) VALUES (3, 6, 5, 5, N'Keyboard works perfectly again. Very professional staff.', CAST(N'2024-08-22T08:30:00.0000000' AS DateTime2))
INSERT [dbo].[Feedbacks] ([Id], [RepairRequestId], [CustomerId], [Rating], [Comment], [CreatedAt]) VALUES (4, 7, 5, 3, N'Camera is fixed, but the repair took longer than expected.', CAST(N'2024-10-16T09:15:00.0000000' AS DateTime2))
INSERT [dbo].[Feedbacks] ([Id], [RepairRequestId], [CustomerId], [Rating], [Comment], [CreatedAt]) VALUES (5, 8, 5, 5, N'Stuck pixels gone and the monitor was returned within a week.', CAST(N'2025-01-20T10:45:00.0000000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[Feedbacks] OFF
GO
SET IDENTITY_INSERT [dbo].[Invoices] ON 

INSERT [dbo].[Invoices] ([Id], [RepairRequestId], [InvoiceNumber], [PartsTotal], [LaborTotal], [TotalAmount], [Status], [IssuedAt], [PaidAt]) VALUES (1, 1, N'INV-2025-0001', CAST(1850000.00 AS Decimal(18, 2)), CAST(0.00 AS Decimal(18, 2)), CAST(0.00 AS Decimal(18, 2)), N'Paid', CAST(N'2025-01-20T08:50:00.0000000' AS DateTime2), CAST(N'2025-01-20T08:50:00.0000000' AS DateTime2))
INSERT [dbo].[Invoices] ([Id], [RepairRequestId], [InvoiceNumber], [PartsTotal], [LaborTotal], [TotalAmount], [Status], [IssuedAt], [PaidAt]) VALUES (2, 2, N'INV-2025-0002', CAST(6620000.00 AS Decimal(18, 2)), CAST(300000.00 AS Decimal(18, 2)), CAST(6920000.00 AS Decimal(18, 2)), N'Unpaid', CAST(N'2025-02-02T11:00:00.0000000' AS DateTime2), NULL)
INSERT [dbo].[Invoices] ([Id], [RepairRequestId], [InvoiceNumber], [PartsTotal], [LaborTotal], [TotalAmount], [Status], [IssuedAt], [PaidAt]) VALUES (3, 3, N'INV-2024-0001', CAST(450000.00 AS Decimal(18, 2)), CAST(150000.00 AS Decimal(18, 2)), CAST(600000.00 AS Decimal(18, 2)), N'Paid', CAST(N'2024-07-05T15:00:00.0000000' AS DateTime2), CAST(N'2024-07-11T16:30:00.0000000' AS DateTime2))
INSERT [dbo].[Invoices] ([Id], [RepairRequestId], [InvoiceNumber], [PartsTotal], [LaborTotal], [TotalAmount], [Status], [IssuedAt], [PaidAt]) VALUES (4, 4, N'INV-2025-0003', CAST(980000.00 AS Decimal(18, 2)), CAST(200000.00 AS Decimal(18, 2)), CAST(1180000.00 AS Decimal(18, 2)), N'Paid', CAST(N'2025-04-18T16:45:00.0000000' AS DateTime2), CAST(N'2025-04-18T17:00:00.0000000' AS DateTime2))
INSERT [dbo].[Invoices] ([Id], [RepairRequestId], [InvoiceNumber], [PartsTotal], [LaborTotal], [TotalAmount], [Status], [IssuedAt], [PaidAt]) VALUES (5, 5, N'INV-2023-0001', CAST(0.00 AS Decimal(18, 2)), CAST(0.00 AS Decimal(18, 2)), CAST(0.00 AS Decimal(18, 2)), N'Cancelled', CAST(N'2023-04-28T13:50:00.0000000' AS DateTime2), NULL)
SET IDENTITY_INSERT [dbo].[Invoices] OFF
GO
SET IDENTITY_INSERT [dbo].[Notifications] ON 

INSERT [dbo].[Notifications] ([Id], [UserId], [RepairRequestId], [Type], [Title], [Message], [IsRead], [CreatedAt]) VALUES (1, 5, 1, N'RepairStatusChanged', N'Repair request received', N'Your repair request #1 for Dell XPS 13 has been received at E-Warranty Service Center Hue.', 1, CAST(N'2025-01-20T08:45:00.0000000' AS DateTime2))
INSERT [dbo].[Notifications] ([Id], [UserId], [RepairRequestId], [Type], [Title], [Message], [IsRead], [CreatedAt]) VALUES (2, 4, 2, N'TechnicianAssigned', N'New repair assigned', N'You have been assigned repair request #2: iPhone 15 Pro touchscreen is unresponsive.', 1, CAST(N'2025-02-02T10:10:00.0000000' AS DateTime2))
INSERT [dbo].[Notifications] ([Id], [UserId], [RepairRequestId], [Type], [Title], [Message], [IsRead], [CreatedAt]) VALUES (3, 5, 2, N'RepairStatusChanged', N'Repair in progress', N'Repair request #2 for iPhone 15 Pro is now in progress.', 0, CAST(N'2025-02-05T09:20:00.0000000' AS DateTime2))
INSERT [dbo].[Notifications] ([Id], [UserId], [RepairRequestId], [Type], [Title], [Message], [IsRead], [CreatedAt]) VALUES (4, 5, 4, N'RepairStatusChanged', N'Device returned', N'Repair request #4 is complete and your LG UltraWide monitor has been returned.', 1, CAST(N'2025-04-18T17:35:00.0000000' AS DateTime2))
INSERT [dbo].[Notifications] ([Id], [UserId], [RepairRequestId], [Type], [Title], [Message], [IsRead], [CreatedAt]) VALUES (5, 5, NULL, N'WarrantyExpiring', N'Warranty expiring soon', N'The warranty for LG UltraWide Monitor (LG34-2024-001) expires on 20/11/2025.', 0, CAST(N'2025-10-21T08:00:00.0000000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[Notifications] OFF
GO
SET IDENTITY_INSERT [dbo].[Parts] ON 

INSERT [dbo].[Parts] ([Id], [Code], [Name], [Description], [UnitPrice], [StockQuantity], [IsActive], [CreatedAt]) VALUES (1, N'BAT-DELL-XPS13', N'Dell XPS 13 Replacement Battery', N'Original 52Wh lithium-polymer battery.', CAST(1850000.00 AS Decimal(18, 2)), 12, 1, CAST(N'2024-02-20T10:00:00.0000000' AS DateTime2))
INSERT [dbo].[Parts] ([Id], [Code], [Name], [Description], [UnitPrice], [StockQuantity], [IsActive], [CreatedAt]) VALUES (2, N'DIG-IP15P-TOUCH', N'iPhone 15 Pro Touch Screen Digitizer', N'OLED display assembly with digitizer.', CAST(6500000.00 AS Decimal(18, 2)), 5, 1, CAST(N'2024-02-20T10:10:00.0000000' AS DateTime2))
INSERT [dbo].[Parts] ([Id], [Code], [Name], [Description], [UnitPrice], [StockQuantity], [IsActive], [CreatedAt]) VALUES (3, N'USBC-TABS9-PORT', N'Galaxy Tab S9 USB-C Charging Port Board', N'Charging port sub-board with flex cable.', CAST(450000.00 AS Decimal(18, 2)), 25, 1, CAST(N'2024-02-20T10:20:00.0000000' AS DateTime2))
INSERT [dbo].[Parts] ([Id], [Code], [Name], [Description], [UnitPrice], [StockQuantity], [IsActive], [CreatedAt]) VALUES (4, N'LED-LG34-BL', N'LG 34-inch Monitor LED Backlight Strip', N'Replacement LED backlight strip set.', CAST(980000.00 AS Decimal(18, 2)), 8, 1, CAST(N'2024-02-20T10:30:00.0000000' AS DateTime2))
INSERT [dbo].[Parts] ([Id], [Code], [Name], [Description], [UnitPrice], [StockQuantity], [IsActive], [CreatedAt]) VALUES (5, N'KIT-SCREW-ADH', N'Precision Screw and Adhesive Kit', N'Universal screws and display adhesive seal.', CAST(120000.00 AS Decimal(18, 2)), 60, 1, CAST(N'2024-02-20T10:40:00.0000000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[Parts] OFF
GO
SET IDENTITY_INSERT [dbo].[Payments] ON 

INSERT [dbo].[Payments] ([Id], [InvoiceId], [ReceivedById], [Amount], [Method], [Note], [PaidAt]) VALUES (1, 2, 3, CAST(2000000.00 AS Decimal(18, 2)), N'BankTransfer', N'Deposit at drop-off.', CAST(N'2025-02-02T11:30:00.0000000' AS DateTime2))
INSERT [dbo].[Payments] ([Id], [InvoiceId], [ReceivedById], [Amount], [Method], [Note], [PaidAt]) VALUES (2, 2, 3, CAST(2000000.00 AS Decimal(18, 2)), N'Card', N'Second deposit after parts ordered.', CAST(N'2025-02-05T09:30:00.0000000' AS DateTime2))
INSERT [dbo].[Payments] ([Id], [InvoiceId], [ReceivedById], [Amount], [Method], [Note], [PaidAt]) VALUES (3, 3, 3, CAST(300000.00 AS Decimal(18, 2)), N'Cash', N'Deposit at drop-off.', CAST(N'2024-07-05T15:10:00.0000000' AS DateTime2))
INSERT [dbo].[Payments] ([Id], [InvoiceId], [ReceivedById], [Amount], [Method], [Note], [PaidAt]) VALUES (4, 3, 3, CAST(300000.00 AS Decimal(18, 2)), N'BankTransfer', N'Remaining balance on completion.', CAST(N'2024-07-11T16:30:00.0000000' AS DateTime2))
INSERT [dbo].[Payments] ([Id], [InvoiceId], [ReceivedById], [Amount], [Method], [Note], [PaidAt]) VALUES (5, 4, 3, CAST(1180000.00 AS Decimal(18, 2)), N'BankTransfer', N'Full payment on pickup.', CAST(N'2025-04-18T17:00:00.0000000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[Payments] OFF
GO
SET IDENTITY_INSERT [dbo].[Products] ON 

INSERT [dbo].[Products] ([Id], [Name], [Brand], [Model], [SerialNumber], [WarrantyMonths], [CreatedAt], [CategoryId]) VALUES (1, N'Dell XPS 13', N'Dell', N'XPS 13 9310', N'DLSXPS13-001', 24, CAST(N'2024-02-05T10:00:00.0000000' AS DateTime2), 1)
INSERT [dbo].[Products] ([Id], [Name], [Brand], [Model], [SerialNumber], [WarrantyMonths], [CreatedAt], [CategoryId]) VALUES (2, N'iPhone 15 Pro', N'Apple', N'A3102', N'IP15P-2024-001', 12, CAST(N'2024-02-12T11:35:00.0000000' AS DateTime2), 2)
INSERT [dbo].[Products] ([Id], [Name], [Brand], [Model], [SerialNumber], [WarrantyMonths], [CreatedAt], [CategoryId]) VALUES (3, N'Samsung Galaxy Tab S9', N'Samsung', N'SM-X710', N'TABS9-2024-001', 18, CAST(N'2024-03-01T09:10:00.0000000' AS DateTime2), 3)
INSERT [dbo].[Products] ([Id], [Name], [Brand], [Model], [SerialNumber], [WarrantyMonths], [CreatedAt], [CategoryId]) VALUES (4, N'LG UltraWide 34" Monitor', N'LG', N'34WN80C-B', N'LG34-2024-001', 24, CAST(N'2024-03-15T15:25:00.0000000' AS DateTime2), 4)
INSERT [dbo].[Products] ([Id], [Name], [Brand], [Model], [SerialNumber], [WarrantyMonths], [CreatedAt], [CategoryId]) VALUES (5, N'Canon Pixma G2020', N'Canon', N'G2020', N'CANON-G2020-001', 12, CAST(N'2024-04-08T13:40:00.0000000' AS DateTime2), 5)
SET IDENTITY_INSERT [dbo].[Products] OFF
GO
SET IDENTITY_INSERT [dbo].[RefreshTokens] ON 

INSERT [dbo].[RefreshTokens] ([Id], [UserId], [Token], [ExpiresAt], [IsRevoked], [CreatedAt]) VALUES (1, 1, N'66E50D75DEFD6CE341955567265CA0712BECC016B79C1E4B3C6F09814A8083CA', CAST(N'2026-10-11T13:09:44.1829969' AS DateTime2), 0, CAST(N'2026-10-02T13:09:44.1830776' AS DateTime2))
INSERT [dbo].[RefreshTokens] ([Id], [UserId], [Token], [ExpiresAt], [IsRevoked], [CreatedAt]) VALUES (2, 2, N'C196DBF040F0BBA35C3AD935C1CF9B77855005052E94D0214B06041FD61313D6', CAST(N'2026-10-11T13:09:44.1831278' AS DateTime2), 0, CAST(N'2026-10-02T13:09:44.1831280' AS DateTime2))
INSERT [dbo].[RefreshTokens] ([Id], [UserId], [Token], [ExpiresAt], [IsRevoked], [CreatedAt]) VALUES (3, 3, N'F975A6B5A06005CDBB4D335FD3C5CB043F09578A42367A24132A09A7EC326D0F', CAST(N'2026-10-11T13:09:44.1831292' AS DateTime2), 0, CAST(N'2026-10-02T13:09:44.1831293' AS DateTime2))
INSERT [dbo].[RefreshTokens] ([Id], [UserId], [Token], [ExpiresAt], [IsRevoked], [CreatedAt]) VALUES (4, 4, N'56767ADFC46DF00E02A4A261119AE81D1FC87DB968E0D8662AEDA91F71D31458', CAST(N'2026-10-11T13:09:44.1831318' AS DateTime2), 0, CAST(N'2026-10-02T13:09:44.1831319' AS DateTime2))
INSERT [dbo].[RefreshTokens] ([Id], [UserId], [Token], [ExpiresAt], [IsRevoked], [CreatedAt]) VALUES (5, 5, N'A3848E78F9FA0CB5BB99659B188957EE1C8060D37A99052DCDFBD56EF32A10E6', CAST(N'2026-10-11T13:09:44.1831327' AS DateTime2), 0, CAST(N'2026-10-02T13:09:44.1831328' AS DateTime2))
INSERT [dbo].[RefreshTokens] ([Id], [UserId], [Token], [ExpiresAt], [IsRevoked], [CreatedAt]) VALUES (6, 1, N'10DEAF4DB8A2AC65429A1129367BF84A8D574B835DDC997001F34AC18F20FAD8', CAST(N'2026-10-11T17:54:35.7297094' AS DateTime2), 0, CAST(N'2026-10-04T17:54:35.7301280' AS DateTime2))
SET IDENTITY_INSERT [dbo].[RefreshTokens] OFF
GO
SET IDENTITY_INSERT [dbo].[RepairRequestAttachments] ON 

INSERT [dbo].[RepairRequestAttachments] ([Id], [RepairRequestId], [UploadedById], [FileName], [FilePath], [ContentType], [FileSize], [UploadedAt]) VALUES (1, 1, 3, N'battery-health-report.png', N'uploads/repair-requests/1/battery-health-report.png', N'image/png', 245760, CAST(N'2025-01-20T08:42:00.0000000' AS DateTime2))
INSERT [dbo].[RepairRequestAttachments] ([Id], [RepairRequestId], [UploadedById], [FileName], [FilePath], [ContentType], [FileSize], [UploadedAt]) VALUES (2, 2, 3, N'cracked-screen-front.jpg', N'uploads/repair-requests/2/cracked-screen-front.jpg', N'image/jpeg', 1482310, CAST(N'2025-02-02T10:05:00.0000000' AS DateTime2))
INSERT [dbo].[RepairRequestAttachments] ([Id], [RepairRequestId], [UploadedById], [FileName], [FilePath], [ContentType], [FileSize], [UploadedAt]) VALUES (3, 2, 4, N'diagnostic-report.pdf', N'uploads/repair-requests/2/diagnostic-report.pdf', N'application/pdf', 312448, CAST(N'2025-02-05T09:10:00.0000000' AS DateTime2))
INSERT [dbo].[RepairRequestAttachments] ([Id], [RepairRequestId], [UploadedById], [FileName], [FilePath], [ContentType], [FileSize], [UploadedAt]) VALUES (4, 3, 5, N'charging-port-closeup.jpg', N'uploads/repair-requests/3/charging-port-closeup.jpg', N'image/jpeg', 986112, CAST(N'2024-07-05T14:15:00.0000000' AS DateTime2))
INSERT [dbo].[RepairRequestAttachments] ([Id], [RepairRequestId], [UploadedById], [FileName], [FilePath], [ContentType], [FileSize], [UploadedAt]) VALUES (5, 4, 5, N'monitor-flicker.mp4', N'uploads/repair-requests/4/monitor-flicker.mp4', N'video/mp4', 12582912, CAST(N'2025-04-12T11:50:00.0000000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[RepairRequestAttachments] OFF
GO
SET IDENTITY_INSERT [dbo].[RepairRequestParts] ON 

INSERT [dbo].[RepairRequestParts] ([Id], [RepairRequestId], [PartId], [Quantity], [UnitPrice], [AddedAt]) VALUES (1, 1, 1, 1, CAST(1850000.00 AS Decimal(18, 2)), CAST(N'2025-01-21T09:00:00.0000000' AS DateTime2))
INSERT [dbo].[RepairRequestParts] ([Id], [RepairRequestId], [PartId], [Quantity], [UnitPrice], [AddedAt]) VALUES (2, 2, 2, 1, CAST(6500000.00 AS Decimal(18, 2)), CAST(N'2025-02-05T09:20:00.0000000' AS DateTime2))
INSERT [dbo].[RepairRequestParts] ([Id], [RepairRequestId], [PartId], [Quantity], [UnitPrice], [AddedAt]) VALUES (3, 2, 5, 1, CAST(120000.00 AS Decimal(18, 2)), CAST(N'2025-02-05T09:25:00.0000000' AS DateTime2))
INSERT [dbo].[RepairRequestParts] ([Id], [RepairRequestId], [PartId], [Quantity], [UnitPrice], [AddedAt]) VALUES (4, 3, 3, 1, CAST(450000.00 AS Decimal(18, 2)), CAST(N'2024-07-08T10:00:00.0000000' AS DateTime2))
INSERT [dbo].[RepairRequestParts] ([Id], [RepairRequestId], [PartId], [Quantity], [UnitPrice], [AddedAt]) VALUES (5, 4, 4, 1, CAST(980000.00 AS Decimal(18, 2)), CAST(N'2025-04-15T13:00:00.0000000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[RepairRequestParts] OFF
GO
SET IDENTITY_INSERT [dbo].[RepairRequests] ON 

INSERT [dbo].[RepairRequests] ([Id], [WarrantyCardId], [ReceptionistId], [TechnicianId], [Description], [Status], [CreatedAt], [UpdatedAt], [IsChargeable], [LaborCost], [ServiceCenterId]) VALUES (1, 1, 3, 4, N'Laptop battery drains quickly and device turns off during meetings.', N'Received', CAST(N'2025-01-20T08:40:00.0000000' AS DateTime2), CAST(N'2025-01-20T08:40:00.0000000' AS DateTime2), 0, CAST(0.00 AS Decimal(18, 2)), 1)
INSERT [dbo].[RepairRequests] ([Id], [WarrantyCardId], [ReceptionistId], [TechnicianId], [Description], [Status], [CreatedAt], [UpdatedAt], [IsChargeable], [LaborCost], [ServiceCenterId]) VALUES (2, 2, 3, 4, N'Touchscreen is unresponsive after a minor drop.', N'InProgress', CAST(N'2025-02-02T10:00:00.0000000' AS DateTime2), CAST(N'2025-02-05T09:15:00.0000000' AS DateTime2), 1, CAST(300000.00 AS Decimal(18, 2)), 1)
INSERT [dbo].[RepairRequests] ([Id], [WarrantyCardId], [ReceptionistId], [TechnicianId], [Description], [Status], [CreatedAt], [UpdatedAt], [IsChargeable], [LaborCost], [ServiceCenterId]) VALUES (3, 3, 3, 4, N'Tablet charging port is loose and power cable disconnects intermittently.', N'Completed', CAST(N'2024-07-05T14:20:00.0000000' AS DateTime2), CAST(N'2024-07-11T16:10:00.0000000' AS DateTime2), 1, CAST(150000.00 AS Decimal(18, 2)), 1)
INSERT [dbo].[RepairRequests] ([Id], [WarrantyCardId], [ReceptionistId], [TechnicianId], [Description], [Status], [CreatedAt], [UpdatedAt], [IsChargeable], [LaborCost], [ServiceCenterId]) VALUES (4, 4, 3, 4, N'Monitor flickers black every few minutes while using design software.', N'Returned', CAST(N'2025-04-12T12:00:00.0000000' AS DateTime2), CAST(N'2025-04-18T17:30:00.0000000' AS DateTime2), 1, CAST(200000.00 AS Decimal(18, 2)), 1)
INSERT [dbo].[RepairRequests] ([Id], [WarrantyCardId], [ReceptionistId], [TechnicianId], [Description], [Status], [CreatedAt], [UpdatedAt], [IsChargeable], [LaborCost], [ServiceCenterId]) VALUES (5, 5, 3, 4, N'Printer repeatedly jams paper after installing a new cartridge.', N'Cancelled', CAST(N'2023-04-25T09:35:00.0000000' AS DateTime2), CAST(N'2023-04-28T13:45:00.0000000' AS DateTime2), 0, CAST(0.00 AS Decimal(18, 2)), 1)
INSERT [dbo].[RepairRequests] ([Id], [WarrantyCardId], [ReceptionistId], [TechnicianId], [Description], [Status], [CreatedAt], [UpdatedAt], [IsChargeable], [LaborCost], [ServiceCenterId]) VALUES (6, 1, 3, 4, N'Several keyboard keys stop responding after a firmware update.', N'Returned', CAST(N'2024-08-10T09:00:00.0000000' AS DateTime2), CAST(N'2024-08-20T16:00:00.0000000' AS DateTime2), 0, CAST(0.00 AS Decimal(18, 2)), 1)
INSERT [dbo].[RepairRequests] ([Id], [WarrantyCardId], [ReceptionistId], [TechnicianId], [Description], [Status], [CreatedAt], [UpdatedAt], [IsChargeable], [LaborCost], [ServiceCenterId]) VALUES (7, 2, 3, 4, N'Rear camera autofocus keeps hunting and produces blurry photos.', N'Returned', CAST(N'2024-10-05T10:30:00.0000000' AS DateTime2), CAST(N'2024-10-14T15:30:00.0000000' AS DateTime2), 0, CAST(0.00 AS Decimal(18, 2)), 1)
INSERT [dbo].[RepairRequests] ([Id], [WarrantyCardId], [ReceptionistId], [TechnicianId], [Description], [Status], [CreatedAt], [UpdatedAt], [IsChargeable], [LaborCost], [ServiceCenterId]) VALUES (8, 4, 3, 4, N'Monitor has a cluster of stuck pixels near the upper-left corner.', N'Returned', CAST(N'2025-01-10T11:00:00.0000000' AS DateTime2), CAST(N'2025-01-18T17:00:00.0000000' AS DateTime2), 0, CAST(0.00 AS Decimal(18, 2)), 1)
SET IDENTITY_INSERT [dbo].[RepairRequests] OFF
GO
SET IDENTITY_INSERT [dbo].[RepairRequestStatusHistories] ON 

INSERT [dbo].[RepairRequestStatusHistories] ([Id], [RepairRequestId], [OldStatus], [NewStatus], [ChangedBy], [ChangedAt]) VALUES (1, 1, NULL, N'Received', 3, CAST(N'2025-01-20T08:41:00.0000000' AS DateTime2))
INSERT [dbo].[RepairRequestStatusHistories] ([Id], [RepairRequestId], [OldStatus], [NewStatus], [ChangedBy], [ChangedAt]) VALUES (2, 2, N'Received', N'InProgress', 4, CAST(N'2025-02-05T09:15:00.0000000' AS DateTime2))
INSERT [dbo].[RepairRequestStatusHistories] ([Id], [RepairRequestId], [OldStatus], [NewStatus], [ChangedBy], [ChangedAt]) VALUES (3, 3, N'InProgress', N'Completed', 4, CAST(N'2024-07-11T16:10:00.0000000' AS DateTime2))
INSERT [dbo].[RepairRequestStatusHistories] ([Id], [RepairRequestId], [OldStatus], [NewStatus], [ChangedBy], [ChangedAt]) VALUES (4, 4, N'Completed', N'Returned', 4, CAST(N'2025-04-18T17:30:00.0000000' AS DateTime2))
INSERT [dbo].[RepairRequestStatusHistories] ([Id], [RepairRequestId], [OldStatus], [NewStatus], [ChangedBy], [ChangedAt]) VALUES (5, 5, N'Received', N'Cancelled', 3, CAST(N'2023-04-28T13:45:00.0000000' AS DateTime2))
INSERT [dbo].[RepairRequestStatusHistories] ([Id], [RepairRequestId], [OldStatus], [NewStatus], [ChangedBy], [ChangedAt]) VALUES (6, 6, N'Completed', N'Returned', 4, CAST(N'2024-08-20T16:00:00.0000000' AS DateTime2))
INSERT [dbo].[RepairRequestStatusHistories] ([Id], [RepairRequestId], [OldStatus], [NewStatus], [ChangedBy], [ChangedAt]) VALUES (7, 7, N'Completed', N'Returned', 4, CAST(N'2024-10-14T15:30:00.0000000' AS DateTime2))
INSERT [dbo].[RepairRequestStatusHistories] ([Id], [RepairRequestId], [OldStatus], [NewStatus], [ChangedBy], [ChangedAt]) VALUES (8, 8, N'Completed', N'Returned', 4, CAST(N'2025-01-18T17:00:00.0000000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[RepairRequestStatusHistories] OFF
GO
SET IDENTITY_INSERT [dbo].[ServiceCenters] ON 

INSERT [dbo].[ServiceCenters] ([Id], [Name], [Address], [Phone], [IsActive], [CreatedAt]) VALUES (1, N'E-Warranty Service Center Hue', N'12 Le Loi, Phu Hoi, Hue', N'02343822001', 1, CAST(N'2024-01-10T08:00:00.0000000' AS DateTime2))
INSERT [dbo].[ServiceCenters] ([Id], [Name], [Address], [Phone], [IsActive], [CreatedAt]) VALUES (2, N'E-Warranty Service Center Da Nang', N'254 Nguyen Van Linh, Thanh Khe, Da Nang', N'02363650111', 1, CAST(N'2024-01-10T08:30:00.0000000' AS DateTime2))
INSERT [dbo].[ServiceCenters] ([Id], [Name], [Address], [Phone], [IsActive], [CreatedAt]) VALUES (3, N'E-Warranty Service Center Ha Noi', N'18 Tran Duy Hung, Cau Giay, Ha Noi', N'02435550123', 1, CAST(N'2024-02-01T09:00:00.0000000' AS DateTime2))
INSERT [dbo].[ServiceCenters] ([Id], [Name], [Address], [Phone], [IsActive], [CreatedAt]) VALUES (4, N'E-Warranty Service Center Ho Chi Minh', N'72 Nguyen Hue, District 1, Ho Chi Minh City', N'02838220456', 1, CAST(N'2024-02-15T09:30:00.0000000' AS DateTime2))
INSERT [dbo].[ServiceCenters] ([Id], [Name], [Address], [Phone], [IsActive], [CreatedAt]) VALUES (5, N'E-Warranty Service Center Can Tho', N'35 Mau Than, Ninh Kieu, Can Tho', N'02923810789', 0, CAST(N'2024-03-05T10:00:00.0000000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[ServiceCenters] OFF
GO
SET IDENTITY_INSERT [dbo].[Users] ON 

INSERT [dbo].[Users] ([Id], [FullName], [Email], [PasswordHash], [Phone], [Role], [IsActive], [CreatedAt], [ServiceCenterId]) VALUES (1, N'Nguyen Van An', N'admin@warranty.local', N'$2a$11$Gu/uuKCyEEBVTE0HK9FlqeOPFZm1xLvYE6wNiEhyIkASq7uHpVw2u', N'0901000001', N'Admin', 1, CAST(N'2024-01-15T08:30:00.0000000' AS DateTime2), NULL)
INSERT [dbo].[Users] ([Id], [FullName], [Email], [PasswordHash], [Phone], [Role], [IsActive], [CreatedAt], [ServiceCenterId]) VALUES (2, N'Tran Thi Binh', N'manager@warranty.local', N'$2a$11$XWRalapeS3RU/Nbg2UZwcOhUNB0vtig4o1ZToWK5xgqkhSBKjknoK', N'0901000002', N'Manager', 1, CAST(N'2024-01-20T09:00:00.0000000' AS DateTime2), 1)
INSERT [dbo].[Users] ([Id], [FullName], [Email], [PasswordHash], [Phone], [Role], [IsActive], [CreatedAt], [ServiceCenterId]) VALUES (3, N'Le Hoai Chi', N'reception@warranty.local', N'$2a$11$ceg/vQEqOIpfJSD5ExeWNeS8KaNmousZTl3bj2b6gbu5eTU0YlLme', N'0901000003', N'Receptionist', 1, CAST(N'2024-02-03T08:15:00.0000000' AS DateTime2), 1)
INSERT [dbo].[Users] ([Id], [FullName], [Email], [PasswordHash], [Phone], [Role], [IsActive], [CreatedAt], [ServiceCenterId]) VALUES (4, N'Pham Van Duy', N'tech01@warranty.local', N'$2a$11$uHNW609afpTTEdOh1Ys.AuD6RCMN9EszrgXI/Mrq2SnezKNqp8sEK', N'0901000004', N'Technician', 1, CAST(N'2024-02-10T07:45:00.0000000' AS DateTime2), 1)
INSERT [dbo].[Users] ([Id], [FullName], [Email], [PasswordHash], [Phone], [Role], [IsActive], [CreatedAt], [ServiceCenterId]) VALUES (5, N'Nguyen Thi Emy', N'customer01@warranty.local', N'$2a$11$a.DAZwA2TUT6aAWr9K2DZOQ2b0EvgV2Dd/BS10i1H6eBhjaJ/S3G6', N'0901000005', N'Customer', 1, CAST(N'2024-03-08T14:20:00.0000000' AS DateTime2), NULL)
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
ALTER TABLE [dbo].[RepairRequests] ADD  DEFAULT (CONVERT([bit],(0))) FOR [IsChargeable]
GO
ALTER TABLE [dbo].[RepairRequests] ADD  DEFAULT ((0.0)) FOR [LaborCost]
GO
ALTER TABLE [dbo].[Feedbacks]  WITH CHECK ADD  CONSTRAINT [FK_Feedbacks_RepairRequests_RepairRequestId] FOREIGN KEY([RepairRequestId])
REFERENCES [dbo].[RepairRequests] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Feedbacks] CHECK CONSTRAINT [FK_Feedbacks_RepairRequests_RepairRequestId]
GO
ALTER TABLE [dbo].[Feedbacks]  WITH CHECK ADD  CONSTRAINT [FK_Feedbacks_Users_CustomerId] FOREIGN KEY([CustomerId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[Feedbacks] CHECK CONSTRAINT [FK_Feedbacks_Users_CustomerId]
GO
ALTER TABLE [dbo].[Invoices]  WITH CHECK ADD  CONSTRAINT [FK_Invoices_RepairRequests_RepairRequestId] FOREIGN KEY([RepairRequestId])
REFERENCES [dbo].[RepairRequests] ([Id])
GO
ALTER TABLE [dbo].[Invoices] CHECK CONSTRAINT [FK_Invoices_RepairRequests_RepairRequestId]
GO
ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [FK_Notifications_RepairRequests_RepairRequestId] FOREIGN KEY([RepairRequestId])
REFERENCES [dbo].[RepairRequests] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[Notifications] CHECK CONSTRAINT [FK_Notifications_RepairRequests_RepairRequestId]
GO
ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Notifications] CHECK CONSTRAINT [FK_Notifications_Users_UserId]
GO
ALTER TABLE [dbo].[Payments]  WITH CHECK ADD  CONSTRAINT [FK_Payments_Invoices_InvoiceId] FOREIGN KEY([InvoiceId])
REFERENCES [dbo].[Invoices] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Payments] CHECK CONSTRAINT [FK_Payments_Invoices_InvoiceId]
GO
ALTER TABLE [dbo].[Payments]  WITH CHECK ADD  CONSTRAINT [FK_Payments_Users_ReceivedById] FOREIGN KEY([ReceivedById])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[Payments] CHECK CONSTRAINT [FK_Payments_Users_ReceivedById]
GO
ALTER TABLE [dbo].[Products]  WITH CHECK ADD  CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY([CategoryId])
REFERENCES [dbo].[Categories] ([Id])
GO
ALTER TABLE [dbo].[Products] CHECK CONSTRAINT [FK_Products_Categories_CategoryId]
GO
ALTER TABLE [dbo].[RefreshTokens]  WITH CHECK ADD  CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RefreshTokens] CHECK CONSTRAINT [FK_RefreshTokens_Users_UserId]
GO
ALTER TABLE [dbo].[RepairRequestAttachments]  WITH CHECK ADD  CONSTRAINT [FK_RepairRequestAttachments_RepairRequests_RepairRequestId] FOREIGN KEY([RepairRequestId])
REFERENCES [dbo].[RepairRequests] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RepairRequestAttachments] CHECK CONSTRAINT [FK_RepairRequestAttachments_RepairRequests_RepairRequestId]
GO
ALTER TABLE [dbo].[RepairRequestAttachments]  WITH CHECK ADD  CONSTRAINT [FK_RepairRequestAttachments_Users_UploadedById] FOREIGN KEY([UploadedById])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[RepairRequestAttachments] CHECK CONSTRAINT [FK_RepairRequestAttachments_Users_UploadedById]
GO
ALTER TABLE [dbo].[RepairRequestParts]  WITH CHECK ADD  CONSTRAINT [FK_RepairRequestParts_Parts_PartId] FOREIGN KEY([PartId])
REFERENCES [dbo].[Parts] ([Id])
GO
ALTER TABLE [dbo].[RepairRequestParts] CHECK CONSTRAINT [FK_RepairRequestParts_Parts_PartId]
GO
ALTER TABLE [dbo].[RepairRequestParts]  WITH CHECK ADD  CONSTRAINT [FK_RepairRequestParts_RepairRequests_RepairRequestId] FOREIGN KEY([RepairRequestId])
REFERENCES [dbo].[RepairRequests] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RepairRequestParts] CHECK CONSTRAINT [FK_RepairRequestParts_RepairRequests_RepairRequestId]
GO
ALTER TABLE [dbo].[RepairRequests]  WITH CHECK ADD  CONSTRAINT [FK_RepairRequests_ServiceCenters_ServiceCenterId] FOREIGN KEY([ServiceCenterId])
REFERENCES [dbo].[ServiceCenters] ([Id])
GO
ALTER TABLE [dbo].[RepairRequests] CHECK CONSTRAINT [FK_RepairRequests_ServiceCenters_ServiceCenterId]
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
ALTER TABLE [dbo].[Users]  WITH CHECK ADD  CONSTRAINT [FK_Users_ServiceCenters_ServiceCenterId] FOREIGN KEY([ServiceCenterId])
REFERENCES [dbo].[ServiceCenters] ([Id])
GO
ALTER TABLE [dbo].[Users] CHECK CONSTRAINT [FK_Users_ServiceCenters_ServiceCenterId]
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
ALTER TABLE [dbo].[Feedbacks]  WITH CHECK ADD  CONSTRAINT [CK_Feedbacks_Rating] CHECK  (([Rating]>=(1) AND [Rating]<=(5)))
GO
ALTER TABLE [dbo].[Feedbacks] CHECK CONSTRAINT [CK_Feedbacks_Rating]
GO
ALTER TABLE [dbo].[Invoices]  WITH CHECK ADD  CONSTRAINT [CK_Invoices_TotalAmount] CHECK  (([TotalAmount]>=(0)))
GO
ALTER TABLE [dbo].[Invoices] CHECK CONSTRAINT [CK_Invoices_TotalAmount]
GO
ALTER TABLE [dbo].[Parts]  WITH CHECK ADD  CONSTRAINT [CK_Parts_StockQuantity] CHECK  (([StockQuantity]>=(0)))
GO
ALTER TABLE [dbo].[Parts] CHECK CONSTRAINT [CK_Parts_StockQuantity]
GO
ALTER TABLE [dbo].[Parts]  WITH CHECK ADD  CONSTRAINT [CK_Parts_UnitPrice] CHECK  (([UnitPrice]>=(0)))
GO
ALTER TABLE [dbo].[Parts] CHECK CONSTRAINT [CK_Parts_UnitPrice]
GO
ALTER TABLE [dbo].[Payments]  WITH CHECK ADD  CONSTRAINT [CK_Payments_Amount] CHECK  (([Amount]>(0)))
GO
ALTER TABLE [dbo].[Payments] CHECK CONSTRAINT [CK_Payments_Amount]
GO
ALTER TABLE [dbo].[Products]  WITH CHECK ADD  CONSTRAINT [CK_Products_WarrantyMonths] CHECK  (([WarrantyMonths]>(0)))
GO
ALTER TABLE [dbo].[Products] CHECK CONSTRAINT [CK_Products_WarrantyMonths]
GO
ALTER TABLE [dbo].[RepairRequestParts]  WITH CHECK ADD  CONSTRAINT [CK_RepairRequestParts_Quantity] CHECK  (([Quantity]>(0)))
GO
ALTER TABLE [dbo].[RepairRequestParts] CHECK CONSTRAINT [CK_RepairRequestParts_Quantity]
GO
ALTER TABLE [dbo].[WarrantyCards]  WITH CHECK ADD  CONSTRAINT [CK_WarrantyCards_DateRange] CHECK  (([EndDate]>=[StartDate]))
GO
ALTER TABLE [dbo].[WarrantyCards] CHECK CONSTRAINT [CK_WarrantyCards_DateRange]
GO
