IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'PharmacyDB')
BEGIN
    CREATE DATABASE PharmacyDB;
END
GO

USE PharmacyDB;
GO

IF OBJECT_ID('dbo.SystemAudit', 'U') IS NOT NULL DROP TABLE dbo.SystemAudit;
IF OBJECT_ID('dbo.SalesArchive', 'U') IS NOT NULL DROP TABLE dbo.SalesArchive;
IF OBJECT_ID('dbo.Sales', 'U') IS NOT NULL DROP TABLE dbo.Sales;
IF OBJECT_ID('dbo.Inventory', 'U') IS NOT NULL DROP TABLE dbo.Inventory;
IF OBJECT_ID('dbo.Medicines', 'U') IS NOT NULL DROP TABLE dbo.Medicines;
IF OBJECT_ID('dbo.Suppliers', 'U') IS NOT NULL DROP TABLE dbo.Suppliers;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
GO

CREATE TABLE Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NULL
);

CREATE TABLE Suppliers (
    SupplierId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Country NVARCHAR(50) NOT NULL,
    Notes NVARCHAR(MAX) NULL,
    LastAuditDate DATETIME DEFAULT GETDATE()
);

CREATE TABLE Medicines (
    MedicineId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    CategoryId INT NOT NULL,
    SupplierId INT NOT NULL,
    BasePrice DECIMAL(18, 2) NOT NULL,
    TotalStock INT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    CONSTRAINT FK_Medicines_Categories FOREIGN KEY (CategoryId) 
        REFERENCES Categories(CategoryId) ON DELETE NO ACTION,
    CONSTRAINT FK_Medicines_Suppliers FOREIGN KEY (SupplierId) 
        REFERENCES Suppliers(SupplierId) ON DELETE NO ACTION
);

CREATE TABLE Inventory (
    InventoryId INT IDENTITY(1,1) PRIMARY KEY,
    MedicineId INT NOT NULL,
    BatchNumber NVARCHAR(50) NOT NULL,
    ExpiryDate DATE NOT NULL,
    Quantity INT NOT NULL,
    Location NVARCHAR(50) DEFAULT 'Main Shelf',
    CONSTRAINT FK_Inventory_Medicines FOREIGN KEY (MedicineId) 
        REFERENCES Medicines(MedicineId) ON DELETE CASCADE
);

CREATE TABLE Sales (
    SaleId INT IDENTITY(1,1) PRIMARY KEY,
    MedicineId INT NOT NULL,
    Quantity INT NOT NULL,
    SoldPrice DECIMAL(18, 2) NOT NULL,
    SaleDate DATETIME DEFAULT GETDATE(),
    Discount DECIMAL(5, 2) DEFAULT 0,
    CONSTRAINT FK_Sales_Medicines FOREIGN KEY (MedicineId) 
        REFERENCES Medicines(MedicineId) ON DELETE NO ACTION
);

CREATE TABLE SalesArchive (
    ArchiveId INT IDENTITY(1,1) PRIMARY KEY,
    SaleId INT,
    MedicineId INT,
    Quantity INT,
    SaleDate DATETIME,
    Reason NVARCHAR(255),
    ArchivedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE SystemAudit (
    LogId INT IDENTITY(1,1) PRIMARY KEY,
    ActionType NVARCHAR(50),
    TableName NVARCHAR(50),
    RecordId INT,
    OldValue NVARCHAR(MAX),
    NewValue NVARCHAR(MAX),
    LogDate DATETIME DEFAULT GETDATE(),
    UserInfo NVARCHAR(100) DEFAULT CURRENT_USER
);
GO
