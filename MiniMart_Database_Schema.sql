/* ============================================================================
   Mart Management System — Database Schema
   ============================================================================
   Target: Microsoft SQL Server
   Creates: Categories, Users, Products, Sales, SaleDetails
   ============================================================================ */

IF DB_ID(N'MiniMartDB') IS NULL
BEGIN
    CREATE DATABASE MiniMartDB;
END
GO

USE MiniMartDB;
GO

/* ============================================================================
   1. CATEGORIES
   ============================================================================ */

IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL
    DROP TABLE dbo.Categories;
GO

CREATE TABLE dbo.Categories
(
    CategoryId  INT IDENTITY(1,1) NOT NULL,
    Name        NVARCHAR(100)     NOT NULL,

    CONSTRAINT PK_Categories PRIMARY KEY CLUSTERED (CategoryId),
    CONSTRAINT UQ_Categories_Name UNIQUE (Name)
);
GO

/* ============================================================================
   2. USERS
   ============================================================================ */

IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
    DROP TABLE dbo.Users;
GO

CREATE TABLE dbo.Users
(
    UserId        INT IDENTITY(1,1)   NOT NULL,
    Username      NVARCHAR(50)        NOT NULL,
    PasswordHash  NVARCHAR(256)       NOT NULL,
    Role          NVARCHAR(20)        NOT NULL,
    IsActive      BIT                 NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    CreatedAt     DATETIME2(0)        NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSDATETIME()),

    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (UserId),
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT CK_Users_Role CHECK (Role IN (N'Admin', N'Cashier'))
);
GO

/* ============================================================================
   3. PRODUCTS
   ============================================================================ */

IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL
    DROP TABLE dbo.Products;
GO

CREATE TABLE dbo.Products
(
    ProductId      INT IDENTITY(1,1)  NOT NULL,
    Name           NVARCHAR(150)      NOT NULL,
    SKU            NVARCHAR(50)       NOT NULL,
    UnitPrice      DECIMAL(10,2)      NOT NULL,
    CostPrice      DECIMAL(10,2)      NOT NULL,
    StockQuantity  INT                NOT NULL CONSTRAINT DF_Products_StockQuantity DEFAULT (0),
    ReorderLevel   INT                NOT NULL CONSTRAINT DF_Products_ReorderLevel DEFAULT (5),
    CategoryId     INT                NOT NULL,
    IsActive       BIT                NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT (1),

    CONSTRAINT PK_Products PRIMARY KEY CLUSTERED (ProductId),
    CONSTRAINT UQ_Products_SKU UNIQUE (SKU),
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId)
        REFERENCES dbo.Categories (CategoryId),
    CONSTRAINT CK_Products_UnitPrice CHECK (UnitPrice >= 0),
    CONSTRAINT CK_Products_CostPrice CHECK (CostPrice >= 0),
    CONSTRAINT CK_Products_StockQuantity CHECK (StockQuantity >= 0),
    CONSTRAINT CK_Products_ReorderLevel CHECK (ReorderLevel >= 0)
);
GO

CREATE NONCLUSTERED INDEX IX_Products_CategoryId ON dbo.Products (CategoryId);
CREATE NONCLUSTERED INDEX IX_Products_Name ON dbo.Products (Name);
GO

/* ============================================================================
   4. SALES  (Transaction header)
   ============================================================================ */

IF OBJECT_ID(N'dbo.Sales', N'U') IS NOT NULL
    DROP TABLE dbo.Sales;
GO

CREATE TABLE dbo.Sales
(
    SaleId       INT IDENTITY(1,1)  NOT NULL,
    SaleDate     DATETIME2(0)       NOT NULL CONSTRAINT DF_Sales_SaleDate DEFAULT (SYSDATETIME()),
    TotalAmount  DECIMAL(10,2)      NOT NULL,
    AmountPaid   DECIMAL(10,2)      NOT NULL,
    ChangeDue    DECIMAL(10,2)      NOT NULL,
    CashierId    INT                NOT NULL,

    CONSTRAINT PK_Sales PRIMARY KEY CLUSTERED (SaleId),
    CONSTRAINT FK_Sales_Users FOREIGN KEY (CashierId)
        REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Sales_TotalAmount CHECK (TotalAmount >= 0),
    CONSTRAINT CK_Sales_AmountPaid CHECK (AmountPaid >= 0),
    CONSTRAINT CK_Sales_ChangeDue CHECK (ChangeDue >= 0)
);
GO

CREATE NONCLUSTERED INDEX IX_Sales_CashierId ON dbo.Sales (CashierId);
CREATE NONCLUSTERED INDEX IX_Sales_SaleDate ON dbo.Sales (SaleDate);
GO

/* ============================================================================
   5. SALEDETAILS  (Transaction line items)
   ============================================================================ */

IF OBJECT_ID(N'dbo.SaleDetails', N'U') IS NOT NULL
    DROP TABLE dbo.SaleDetails;
GO

CREATE TABLE dbo.SaleDetails
(
    SaleDetailId  INT IDENTITY(1,1)  NOT NULL,
    SaleId        INT                NOT NULL,
    ProductId     INT                NOT NULL,
    Quantity      INT                NOT NULL,
    UnitPrice     DECIMAL(10,2)      NOT NULL,

    CONSTRAINT PK_SaleDetails PRIMARY KEY CLUSTERED (SaleDetailId),
    CONSTRAINT FK_SaleDetails_Sales FOREIGN KEY (SaleId)
        REFERENCES dbo.Sales (SaleId)
        ON DELETE CASCADE,
    CONSTRAINT FK_SaleDetails_Products FOREIGN KEY (ProductId)
        REFERENCES dbo.Products (ProductId),
    CONSTRAINT CK_SaleDetails_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_SaleDetails_UnitPrice CHECK (UnitPrice >= 0)
);
GO

CREATE NONCLUSTERED INDEX IX_SaleDetails_SaleId ON dbo.SaleDetails (SaleId);
CREATE NONCLUSTERED INDEX IX_SaleDetails_ProductId ON dbo.SaleDetails (ProductId);
GO