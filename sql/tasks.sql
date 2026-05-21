-- 1.3.1
SELECT CONVERT(CHAR, GETDATE(), 101)
GO

-- 1.3.2.1
CREATE OR ALTER PROCEDURE usp_InsertSupplier
    @Name NVARCHAR(100),
    @Country NVARCHAR(50),
    @Notes NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Suppliers (Name, Country, Notes, LastAuditDate)
    VALUES (@Name, @Country, @Notes, GETDATE());
END;
GO

-- 1.3.2.2
CREATE OR ALTER PROCEDURE usp_ProcessDelivery
    @MedicineId INT,
    @BatchNumber NVARCHAR(50),
    @ExpiryDate DATETIME,
    @Quantity INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Medicines WHERE MedicineId = @MedicineId)
    BEGIN
        RAISERROR('Error: Medicine with ID %d not found.', 16, 1, @MedicineId);
        RETURN;
    END

    IF (@ExpiryDate > GETDATE()) AND (@Quantity >= 1)
    BEGIN
        INSERT INTO Inventory (MedicineId, BatchNumber, ExpiryDate, Quantity)
        VALUES (@MedicineId, @BatchNumber, @ExpiryDate, @Quantity);

        UPDATE Medicines
        SET TotalStock = TotalStock + @Quantity
        WHERE MedicineId = @MedicineId;
    END
    ELSE
    BEGIN
        RAISERROR('Error: Invalid expiry date or quantity.', 16, 1);
    END
END;
GO

-- 1.3.3.1
CREATE OR ALTER FUNCTION fn_CountExpensiveMedicines
(
    @Country NVARCHAR(50)
)
RETURNS INT
AS
BEGIN
    DECLARE @Result INT;
    DECLARE @AvgPrice DECIMAL(18, 2);

    SELECT @AvgPrice = AVG(m.BasePrice)
    FROM Medicines m
    JOIN Suppliers s ON m.SupplierId = s.SupplierId
    WHERE s.Country = @Country;

    IF @AvgPrice IS NULL
    BEGIN
        RETURN 0;
    END

    SELECT @Result = COUNT(*)
    FROM Medicines
    WHERE BasePrice > @AvgPrice;

    RETURN @Result;
END;
GO

-- 1.3.3.2
CREATE OR ALTER FUNCTION fn_GetSupplierStockValue()
RETURNS TABLE
AS
RETURN
(
    SELECT
        s.SupplierId,
        s.Name AS SupplierName,
        SUM(m.BasePrice * i.Quantity) AS TotalValue
    FROM Inventory i
    JOIN Medicines m ON m.MedicineId = i.MedicineId
    JOIN Suppliers s ON s.SupplierId = m.SupplierId
    GROUP BY s.SupplierId, s.Name
);
GO

-- 1.3.4
CREATE OR ALTER TRIGGER tr_LogPriceChanges
ON Medicines
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE(BasePrice)
    BEGIN
        INSERT INTO SystemAudit (ActionType, TableName, RecordId, OldValue, NewValue)
        SELECT
            'PRICE_CHANGE',
            'Medicines',
            i.MedicineId,
            CAST(d.BasePrice AS NVARCHAR(MAX)),
            CAST(i.BasePrice AS NVARCHAR(MAX))
        FROM inserted i
        JOIN deleted d ON i.MedicineId = d.MedicineId
        WHERE i.BasePrice <> d.BasePrice;
    END
END;
GO

-- 1.3.5 Завдання 5
CREATE OR ALTER PROCEDURE usp_UpdateSupplierTopSeller
    @SupplierId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TopMedicineName NVARCHAR(100);

    SELECT TOP 1 @TopMedicineName = m.Name
    FROM Medicines m
    JOIN Sales s ON m.MedicineId = s.MedicineId
    WHERE m.SupplierId = @SupplierId
    GROUP BY m.MedicineId, m.Name
    ORDER BY SUM(s.SoldPrice * s.Quantity) DESC;

    IF @TopMedicineName IS NOT NULL
    BEGIN
        UPDATE Suppliers
        SET Notes = 'Top Selling Medicine: ' + @TopMedicineName
        WHERE SupplierId = @SupplierId;
    END
    ELSE
    BEGIN
        UPDATE Suppliers
        SET Notes = 'No sales data available yet'
        WHERE SupplierId = @SupplierId;
    END
END;
GO

-- 1.3.5 Завдання 10
CREATE OR ALTER PROCEDURE usp_ArchiveSmallSalesByCategory
    @CategoryId INT,
    @k INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SaleId INT, @MedicineId INT, @Quantity INT, @SaleDate DATETIME;
    
    DECLARE sale_cursor CURSOR SCROLL FOR
    SELECT s.SaleId, s.MedicineId, s.Quantity, s.SaleDate
    FROM Sales s
    JOIN Medicines m ON s.MedicineId = m.MedicineId
    WHERE m.CategoryId = @CategoryId AND s.Quantity < @k;

    OPEN sale_cursor;

    FETCH NEXT FROM sale_cursor INTO @SaleId, @MedicineId, @Quantity, @SaleDate;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        INSERT INTO SalesArchive (SaleId, MedicineId, Quantity, SaleDate, Reason)
        VALUES (@SaleId, @MedicineId, @Quantity, @SaleDate, 'Small sale archive');

        DELETE FROM Sales WHERE SaleId = @SaleId;

        FETCH NEXT FROM sale_cursor INTO @SaleId, @MedicineId, @Quantity, @SaleDate;
    END

    CLOSE sale_cursor;
    DEALLOCATE sale_cursor;
END;
GO

-- 1.3.5 Завдання 15
CREATE OR ALTER TRIGGER tr_ProtectStockDeduction
ON Inventory
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentTime TIME = CAST(GETDATE() AS TIME);

    IF @CurrentTime > '20:00:00' OR @CurrentTime < '08:00:00'
    BEGIN
        INSERT INTO SystemAudit (ActionType, TableName, RecordId, NewValue)
        VALUES ('SECURITY_ALERT', 'Inventory', NULL, 'Attempted stock modification in non-working hours');

        RAISERROR('Stock modification is prohibited between 20:00 and 08:00.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO

-- 1.3.5 Завдання 18
CREATE OR ALTER TRIGGER tr_UpdateAuditOnDelivery
ON Inventory
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO SystemAudit (ActionType, TableName, RecordId, NewValue)
    SELECT 
        'DELIVERY_LOG',
        'Inventory',
        i.InventoryId,
        'Last delivery: Batch ' + i.BatchNumber + ', Quantity: ' + CAST(i.Quantity AS NVARCHAR(10)) + ' pcs.'
    FROM inserted i;
END;
GO
