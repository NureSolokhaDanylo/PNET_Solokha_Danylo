USE PharmacyDB;
GO

SET NOCOUNT ON;

INSERT INTO Categories (Name, Description) VALUES 
('Antibiotics', 'Drugs for bacterial infections'),
('Vitamins', 'Bio-active supplements'),
('Painkillers', 'Analgesics and pain relief'),
('Cardiovascular', 'Heart and blood pressure drugs'),
('Respiratory', 'Lungs and breathing support');

INSERT INTO Suppliers (Name, Country, Notes) VALUES 
('Global Pharma Corp', 'USA', 'Priority partner'),
('EuroMed Ltd', 'Germany', 'High quality standards'),
('AsiaDrugs Co', 'Japan', 'Innovation leader'),
('UK BioLabs', 'United Kingdom', 'Strict regulations'),
('Nordic Health', 'Sweden', 'Eco-friendly manufacturing');

DECLARE @i INT = 1;
DECLARE @catCount INT = (SELECT COUNT(*) FROM Categories);
DECLARE @supCount INT = (SELECT COUNT(*) FROM Suppliers);

WHILE @i <= 50
BEGIN
    INSERT INTO Medicines (Name, CategoryId, SupplierId, BasePrice, TotalStock)
    VALUES (
        'Medicine ' + CAST(@i AS VARCHAR),
        (ABS(CHECKSUM(NEWID())) % @catCount) + 1,
        (ABS(CHECKSUM(NEWID())) % @supCount) + 1,
        CAST((RAND() * 500) + 10 AS DECIMAL(18, 2)),
        0
    );
    SET @i = @i + 1;
END

SET @i = 1;
DECLARE @medCount INT = (SELECT COUNT(*) FROM Medicines);

WHILE @i <= 100
BEGIN
    DECLARE @qty INT = (ABS(CHECKSUM(NEWID())) % 100) + 10;
    DECLARE @mId INT = (ABS(CHECKSUM(NEWID())) % @medCount) + 1;

    INSERT INTO Inventory (MedicineId, BatchNumber, ExpiryDate, Quantity, Location)
    VALUES (
        @mId,
        'BATCH-' + CAST(ABS(CHECKSUM(NEWID())) % 10000 AS VARCHAR),
        DATEADD(DAY, (ABS(CHECKSUM(NEWID())) % 730), GETDATE()),
        @qty,
        'Sector-' + CHAR(65 + (ABS(CHECKSUM(NEWID())) % 6))
    );

    UPDATE Medicines SET TotalStock = TotalStock + @qty WHERE MedicineId = @mId;
    
    SET @i = @i + 1;
END

SET @i = 1;
WHILE @i <= 200
BEGIN
    DECLARE @mIdSales INT = (ABS(CHECKSUM(NEWID())) % @medCount) + 1;
    DECLARE @price DECIMAL(18,2) = (SELECT BasePrice FROM Medicines WHERE MedicineId = @mIdSales);
    
    INSERT INTO Sales (MedicineId, Quantity, SoldPrice, SaleDate, Discount)
    VALUES (
        @mIdSales,
        (ABS(CHECKSUM(NEWID())) % 5) + 1,
        @price * (1 - (CAST(ABS(CHECKSUM(NEWID())) % 15 AS DECIMAL(5,2)) / 100)),
        DATEADD(DAY, -(ABS(CHECKSUM(NEWID())) % 365), GETDATE()),
        ABS(CHECKSUM(NEWID())) % 15
    );
    SET @i = @i + 1;
END

PRINT 'Seeding completed successfully.';
GO
